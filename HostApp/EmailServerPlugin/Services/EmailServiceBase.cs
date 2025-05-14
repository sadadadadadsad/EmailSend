using EmailServerPlugin.Models;
using Ganss.Xss;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Services
{
    public class EmailServiceBase
    {
        private readonly SmtpClient client;
        private  DefaultEmailSettings _defaultSettings;
        private ReceiveSetting _receiveSetting;



        private const string NormalExchangeName = "EmailNormalExchange";
        private const string NormalQueueName = "NormalQueue";
        private const string NormalRoutingName = "EmailNormalRouting";

        private const string RetryExchangeName = "DeadLetterRetryExchange";
        private const string RetryRoutingName = "DeadLetterRetryRouting";
        private const string RetryQueueName = "DeadLetterRetryQueue";

        private const int MaxRetryCount = 5;
        private static readonly List<int> RetryErrorCodes = [421, 450, 451, 452];


        public EmailServiceBase(SmtpClient client)
        {
            this.client = client;
        }

        public EmailServiceBase()
        {
            client = new SmtpClient();
        }

        public EmailServiceBase CreateEmailService()
        {
            var client = new SmtpClient();
            return new EmailServiceBase(client);
        }
        public async Task Base(IChannel channel)
        {

            await channel.QueueDeclareAsync(queue: NormalQueueName,//定义正常消息队列
                durable: true,//消息持久化  
                exclusive: false,//是否排他  
                autoDelete: false,//是否自动删除  
                arguments: new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", RetryExchangeName }, // 失败后进入重试队列
                    { "x-dead-letter-routing-key", RetryRoutingName },
                    { "x-message-ttl", 5000 }
                });
            await channel.ExchangeDeclareAsync(NormalExchangeName, ExchangeType.Direct, true, false, null);//定义交换机及类型Direct
            await channel.QueueBindAsync(NormalQueueName, NormalExchangeName, NormalRoutingName, null);//将队列与交换机绑定
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);//平均调度    

            await channel.ExchangeDeclareAsync(RetryExchangeName, ExchangeType.Direct, true);
            await channel.QueueDeclareAsync(RetryQueueName, true, false, false, arguments: new Dictionary<string, object?>
            {
              { "x-dead-letter-exchange", NormalExchangeName },  
              { "x-dead-letter-routing-key", NormalRoutingName },
               { "x-message-ttl", 5000 }
            });
            await channel.QueueBindAsync(RetryQueueName, RetryExchangeName, RetryRoutingName);

            var mimeMessage = new MimeMessage();
            var receivedChunks = new List<byte[]>();
            EmailServiceBase emailServiceBase = new EmailServiceBase();
            EmailDto emailDto = new EmailDto();
            var emailService = emailServiceBase.CreateEmailService();

            //创建消费者  
            var consumer = new AsyncEventingBasicConsumer(channel);


            consumer.ReceivedAsync += async (model, ea) =>//注册事件处理方法  
            {
                int currentRetryCount = 0;
                try
                {
                    mimeMessage ??= new MimeMessage();
                    receivedChunks ??= new List<byte[]>();
                    emailServiceBase ??= new EmailServiceBase();
                    emailDto ??= new EmailDto();
                    byte[] temporaryFileArray = emailDto?.FileArray;
                    var headers = ea.BasicProperties.Headers;
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(message);
                    // 解析重试次数
                    if (headers?.TryGetValue("RetryCount", out var retryObj) == true)
                    {
                        currentRetryCount = (int)retryObj;
                    }

                    if (headers != null && headers.TryGetValue("MessageType", out var messageTypeObj))
                    {
                        string messageType = Encoding.UTF8.GetString(messageTypeObj as byte[]);
                        if (messageType == "AttachmentEndingMark")
                        {
                            byte[] combinedBytes = CombineChunks(receivedChunks);
                            emailDto.FileArray = combinedBytes;
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);//消息确认，防止在传输时消息丢失后mq自动删除队列导致消息直接消失

                            Console.WriteLine("文件接收并保存完成");
                        }
                        else if (messageType == "MailEntity")
                        {

                            emailDto = JsonConvert.DeserializeObject<EmailDto?>(message);//json->EmailDto
                            if (temporaryFileArray != null)
                            {
                                emailDto.FileArray = temporaryFileArray;
                            }
                            mimeMessage = emailService.Save(emailDto, mimeMessage);
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            //消息确认，防止在传输时消息丢失后mq自动删除队列导致消息直接消失

                        }
                        else if (messageType == "Attachment" && body.Length > 64)
                        {
                            int checksumLength = 64; // SHA256 校验码长度为 64 个字符
                            byte[] chunk = new byte[body.Length - checksumLength]; //原长度
                            byte[] receivedChecksumBytes = new byte[checksumLength];
                            Array.Copy(body, chunk, chunk.Length); //将body复制进chunk
                            Array.Copy(body, chunk.Length, receivedChecksumBytes, 0, checksumLength);//将body从消息体之后开始的部分复制到receivedChecksumBytes
                            string receivedChecksum = Encoding.UTF8.GetString(receivedChecksumBytes);//保存
                            string calculatedChecksum = CalculateChecksum(chunk);
                            if (receivedChecksum == calculatedChecksum)
                            {
                                receivedChunks.Add(chunk);
                                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            }
                            else
                            {
                                Console.WriteLine("消息块校验失败，可能存在数据损坏");
                            }
                        }
                        else if (messageType == "EmailEndingMark")
                        {
                            Console.WriteLine($"接收到来自{emailDto.From}的邮件消息，发送到{emailDto.To}");
                            await StmpClientSendAsync(mimeMessage, emailDto);
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);


                        }
                        else
                        {
                            Console.WriteLine("并未接收到邮件消息，请重试");
                            await channel.BasicNackAsync(ea.DeliveryTag, false, false); // 不再重新入队


                        }
                        //处理完消息后，确认收到  
                        //multiple是否批量确认  

                    }
                    else
                    {
                        Console.WriteLine("并未接收到头部信息，请MessageType填进Headers");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false); // 不再重新入队

                    }
                }
                catch (SmtpCommandException ex)
                {
                    int errorCode = (int)ex.StatusCode;
                    var properties = new BasicProperties
                    {
                        Persistent = true,
                        Headers = new Dictionary<string, object?>() //设置附加属性
                        {
                            ["LastError"] = ex.Message,
                            ["ErrorCode"] = errorCode
                        }
                    };
                    if (RetryErrorCodes.Contains(errorCode) && currentRetryCount < MaxRetryCount)
                    {
                        var delay = (int)Math.Pow(2, currentRetryCount) * 5000; // 重试时间指数递增5s, 10s, 20s...
                        properties.Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers)
                        {
                            //提取当前触发事件的附加属性的消息头
                            ["RetryCount"] = currentRetryCount + 1
                        };
                        //赋值RetryCount为当前已重试的次数
                        properties.Expiration = delay.ToString();//赋值当前重试时间
                        currentRetryCount++;
                        await PublishWithRetryAsync(channel, ea.Body, RetryExchangeName, RetryRoutingName, properties);
                        //调用重试发布方法
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false); //大于最大重试次数拒绝消息
                    }

                }

                catch (Exception ex)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false); // 不再重新入队
                    Console.WriteLine(ex.Message);
                }


            };
            //autoAck是否自动确认，false表示手动确认  
            await channel.BasicConsumeAsync(queue: NormalQueueName,
                 autoAck: false,
                 consumer: consumer); //为消息队列指定消费者对象
            Console.WriteLine("Press [enter] to exit");
            Console.ReadLine();
        }
        private async Task StmpClientSendAsync(MimeMessage message, EmailDto emailDto)
        {
            try
            {

                using (var client = new SmtpClient())
                {

                    await client.ConnectAsync(emailDto.Host, emailDto.Port, SecureSocketOptions.Auto); //连接邮箱主机和端口，ssl安全协议 host？
                    await client.AuthenticateAsync(emailDto.From, emailDto.AuthorizationCode); //账户和授权码认证
                    Console.WriteLine("SMTP连接已建立");
                    await client.SendAsync(message); //发送
                    await client.DisconnectAsync(true);//断开连接
                    Console.WriteLine("邮件发送成功");
                }
            }
            catch (AuthenticationException ex)//邮箱配置有误
            {
                using var client = new SmtpClient();
                try
                {
                    //提供默认邮箱发送邮件
                    var configuration = Startup.LoadConfiguration();
                    using var serviceProvider = Startup.ConfigureServices(configuration);
                    _defaultSettings = serviceProvider.GetRequiredService<DefaultSettingManager>().GetDefaultSettings();
                    await client.ConnectAsync(_defaultSettings.DEHost, _defaultSettings.DEPort, SecureSocketOptions.Auto);
                    await client.AuthenticateAsync(_defaultSettings.DEUser, _defaultSettings.DECode);
                    await client.SendAsync(message);
                    Console.WriteLine($"邮箱配置有误:{ex.Message}，已使用默认邮箱发送");
                }
                catch { Console.WriteLine("邮件发送失败"); }//默认邮箱发送失败
                finally
                {
                    await client.DisconnectAsync(true);

                }
            }
        }
        public MimeMessage Save(EmailDto emailDto, MimeMessage message)
        {

            try
            {
                var sanitizer = new HtmlSanitizer();
                message = message ?? new MimeMessage();
                var bodyBuilder = new BodyBuilder();
                message.Subject = emailDto.Subject;
                emailDto.FileName = Path.GetFileName(emailDto.FilePath);
                var configuration = Startup.LoadConfiguration();
                using var serviceProvider = Startup.ConfigureServices(configuration);
                _receiveSetting = serviceProvider.GetRequiredService<ReceiveSettingManager>().GetReceiveSettings();
                if (!string.IsNullOrEmpty(emailDto.From))//添加发送者
                {
                    message.From.Add(new MailboxAddress(emailDto.FromName, emailDto.From));

                }

                if (!string.IsNullOrEmpty(emailDto.Cc))
                {
                    message.Cc.Add(new MailboxAddress(emailDto.CcName, emailDto.Cc));//添加抄送者
                }
                if (!string.IsNullOrEmpty(emailDto.Reply))
                {
                    message.ReplyTo.Add(new MailboxAddress(_receiveSetting.UserName, _receiveSetting.User));//添加回复者
                }
                if (!string.IsNullOrEmpty(emailDto.FilePath) && emailDto.FileArray != null)//添加附件
                {
                    var attachment = new MimePart("application", "octet-stream")//设置二进制流类型
                    {
                        Content = new MimeContent(new MemoryStream(emailDto.FileArray)),//存入内存流
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64, //编码
                        FileName = emailDto.FileName
                    };
                    bodyBuilder.Attachments.Add(attachment);
                }

                if (!string.IsNullOrEmpty(emailDto.TextBody))
                {

                    bodyBuilder.TextBody = emailDto.TextBody;
                }
                if (!string.IsNullOrEmpty(emailDto.HtmlBody))
                {

                    bodyBuilder.HtmlBody = sanitizer.Sanitize(emailDto.HtmlBody);//清洗Html内容

                }
                message.Body = bodyBuilder.ToMessageBody();

                if (_receiveSetting.User.Contains(",")) //多个收件人
                {
                    string[] ToArray = _receiveSetting.User.Split(',');
                    string[] ToNameArray = _receiveSetting.User.Split(",");
                    if (ToArray.Length != ToNameArray.Length)
                    {
                        Console.WriteLine("接受者地址和显示名称数量不匹配");
                    }
                    for (int i = 0; i < ToArray.Length; i++)
                    {
                        string To = ToArray[i].Trim();
                        string ToName = ToNameArray[i].Trim();
                        message.To.Add(new MailboxAddress(ToName, To));
                    }
                }
                else if (!string.IsNullOrEmpty(_receiveSetting.User))//添加接收者
                {
                    message.To.Add(new MailboxAddress(_receiveSetting.UserName, _receiveSetting.User));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("已保存至实体类");
            return message;
        }
        private static string CalculateChecksum(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(data);//计算校验码
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); //转为字符串
            }
        }
        private static byte[] CombineChunks(List<byte[]> chunks)
        {
            int totalLength = 0;
            foreach (byte[] chunk in chunks)
            {
                totalLength += chunk.Length;//计算总长度
            }

            byte[] combinedBytes = new byte[totalLength];
            int offset = 0;
            foreach (byte[] chunk in chunks)
            {
                Array.Copy(chunk, 0, combinedBytes, offset, chunk.Length);//拼接二维数组中的每一项
                offset += chunk.Length;//增加索引
            }

            return combinedBytes;
        }
        private async Task PublishWithRetryAsync(IChannel channel, ReadOnlyMemory<byte> body,
            string exchange, string routingKey, BasicProperties properties)
        {
            const int maxPublishAttempts = 3;
            for (int i = 0; i < maxPublishAttempts; i++)
            {
                try
                {
                    Console.WriteLine($"邮件发送错误，正在尝试重试发布，第{i}次尝试");
                    await channel.BasicPublishAsync(exchange, routingKey, true, properties, body);
                    return;
                }
                catch (OperationInterruptedException) when (i < maxPublishAttempts - 1)
                {
                    await Task.Delay(1000 * (i + 1));
                }
            }
            throw new Exception("重试发布失败");

        }

        public  async Task CloseAsync(IChannel channel)
        {
            await channel.CloseAsync();
        }

    }
}
