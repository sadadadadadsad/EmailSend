using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using ReceiveTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace ReceiveTest.Services
{
    public class EmailServiceBase : IEmailService
    {
        private readonly SmtpClient client;
        public EmailServiceBase(SmtpClient client)
        {

        }
        public EmailServiceBase()
        {

        }
        public EmailServiceBase CreateEmailService()
        {
            var client = new SmtpClient();
            return new EmailServiceBase(client);
        }

        /// <summary>
        /// 配置邮件信息主体
        /// </summary>
        /// <param name="emailDto"></param> 邮件信息
        /// <param name="emailConfig"></param> 邮件配置
        /// <param name="message"></param> MimeMessage
        /// <param name="IsSingleSend"></param> 是否单一收件人
        public MimeMessage Save(EmailDto emailDto, bool IsSingleSend, MimeMessage message)
        {

            try
            {

                message = message ?? new MimeMessage();
                var bodyBuilder = new BodyBuilder();
                message.Subject = emailDto.Subject;
                emailDto.FileName = Path.GetFileName(emailDto.FilePath);
                if (!string.IsNullOrEmpty(emailDto.Reply))
                {
                    message.ReplyTo.Add(new MailboxAddress(emailDto.ToName, emailDto.To));
                }
                if (!string.IsNullOrEmpty(emailDto.To))
                {
                    message.To.Add(new MailboxAddress(emailDto.ToName, emailDto.To));
                }
                if (!string.IsNullOrEmpty(emailDto.Cc))
                {
                    message.Cc.Add(new MailboxAddress(emailDto.CcName, emailDto.Cc));
                }
                if (!string.IsNullOrEmpty(emailDto.From))
                {
                    message.From.Add(new MailboxAddress(emailDto.DisplayName, emailDto.From));

                }
                if (!string.IsNullOrEmpty(emailDto.FilePath) && emailDto.FileArray != null)
                {
                    var attachment = new MimePart("application", "octet-stream")
                    {   
                        Content = new MimeContent(new MemoryStream(emailDto.FileArray)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = emailDto.FileName
                    };
                    bodyBuilder.Attachments.Add(attachment);
                }


                string validBody = !string.IsNullOrEmpty(emailDto.TextBody) ? emailDto.TextBody : emailDto.HtmlBody;
                if (validBody == emailDto.TextBody)

                    bodyBuilder.TextBody = validBody;//正文

                else if (validBody == emailDto.HtmlBody)
                    bodyBuilder.HtmlBody = validBody;

                message.Body = bodyBuilder.ToMessageBody();


                if (IsSingleSend && emailDto.To.Contains(",")) //接受者为多人,通过轮询实现单发，其他接受者不知道有多个收件人
                {
                    string[] ToArray = emailDto.To.Split(',');
                    string[] ToNameArray = emailDto.ToName.Split(",");
                    if (ToArray.Length != ToNameArray.Length)
                    {
                        Console.WriteLine("接受者地址和显示名称数量不匹配");
                    }
                    for (int i = 0; i < ToArray.Length; i++)
                    {
                        string To = ToArray[i].Trim();
                        string ToName = ToArray[i].Trim();
                        message.To.Add(new MailboxAddress(ToName, To));
                    }
                }
                else
                {
                    if (!IsSingleSend && emailDto.To.Contains(",")) //接受者为单人
                    {
                        message.To.Add(new MailboxAddress(emailDto.ToName, emailDto.To));

                    }
                    else
                    {
                        message.To.Add(new MailboxAddress(string.IsNullOrEmpty(emailDto.ToName) ? emailDto.To : emailDto.ToName, emailDto.To)); //ToName为空则用To代替
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return message;
        }

        /// <summary>
        /// stmp协议发送邮件
        /// </summary>
        /// <param name="message"></param>
        /// <param name="emailDto"></param>
        private async void StmpClientSend(MimeMessage message, EmailDto emailDto)
        {
            try
            {

                using (var client = new SmtpClient())
                {

                    await client.ConnectAsync(emailDto.Host, emailDto.Port, SecureSocketOptions.SslOnConnect); //连接qq邮箱主机和端口，ssl安全协议 host？
                    await client.AuthenticateAsync(emailDto.From, emailDto.AuthorizationCode); //账户和授权码认证
                    await client.SendAsync(message); //发送
                    await client.DisconnectAsync(true);//断开连接
                    Console.WriteLine("邮件发送成功");
                }
            }
            catch (Exception ex)
            {
                if (client != null)
                {
                    await client.DisconnectAsync(true);
                }
                Console.WriteLine($"邮件发送失败:{ex.Message}");
            }
        }


        /// <summary>
        /// MQ连接
        /// </summary>
        public async void ReceiveMessageAsync()
        {

            var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "emailQueue",
                durable: true,//消息持久化  
                exclusive: false,//是否排他  
                autoDelete: false,//是否自动删除  
                arguments: null);//参数  

            await channel.ExchangeDeclareAsync("emailExchange", ExchangeType.Fanout, true, false, null);
            await channel.QueueBindAsync("emailQueue", "emailExchange", "emailRoute", null);
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);//平均调度    
            var mimeMessage= new MimeMessage();
            var receivedChunks = new List<byte[]>();
            EmailServiceBase emailServiceBase = new EmailServiceBase();
            EmailDto emailDto = new EmailDto();
            var emailService = emailServiceBase.CreateEmailService();

            //创建消费者  
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                     mimeMessage =mimeMessage?? new MimeMessage();
                    receivedChunks = receivedChunks ?? new List<byte[]>();
                    emailServiceBase = emailServiceBase?? new EmailServiceBase();
                    emailDto = emailDto ?? new EmailDto();
                    byte[] temporaryFileArray = emailDto?.FileArray;
                    var headers = ea.BasicProperties.Headers;
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    if (headers != null && headers.TryGetValue("MessageType", out var messageTypeObj))
                    {
                        string messageType = Encoding.UTF8.GetString(messageTypeObj as byte[]);
                        if (messageType == "AttachmentEndingMark")
                        {
                            byte[] combinedBytes = CombineChunks(receivedChunks);
                            emailDto.FileArray = combinedBytes;
                            var msg = emailDto.FileArray.ToString();
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);//消息确认，防止在传输时消息丢失后mq自动删除队列导致消息直接消失

                            Console.WriteLine("文件接收并保存完成");
                        }
                        else if (messageType == "MailEntity")
                        {

                            emailDto = JsonConvert.DeserializeObject<EmailDto>(message); //转换为json
                            if(temporaryFileArray!=null)
                            {
                                emailDto.FileArray = temporaryFileArray;
                            }
                            mimeMessage = emailService.Save(emailDto, emailDto.IsSingleSend, mimeMessage);
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);//消息确认，防止在传输时消息丢失后mq自动删除队列导致消息直接消失

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
                                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);//消息确认，防止在传输时消息丢失后mq自动删除队列导致消息直接消失

                            }
                            else
                            {
                                Console.WriteLine("消息块校验失败，可能存在数据损坏");
                            }
                        }
                        else if (messageType == "EmailEndingMark")
                        {
                            StmpClientSend(mimeMessage, emailDto);
                            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);


                        }
                        //处理完消息后，确认收到  
                        //multiple是否批量确认  

                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                }


            };
            //autoAck是否自动确认，false表示手动确认  
            await channel.BasicConsumeAsync(queue: "emailQueue",
                 autoAck: false,
                 consumer: consumer); //为消息队列指定消费者对象
            //注册事件处理方法  
            Console.WriteLine("Press [enter] to exit");
            Console.ReadLine();
        }

        private static string CalculateChecksum(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(data);
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
        private static byte[] CombineChunks(List<byte[]> chunks)
        {
            int totalLength = 0;
            foreach (byte[] chunk in chunks)
            {
                totalLength += chunk.Length;
            }

            byte[] combinedBytes = new byte[totalLength];
            int offset = 0;
            foreach (byte[] chunk in chunks)
            {
                Array.Copy(chunk, 0, combinedBytes, offset, chunk.Length);
                offset += chunk.Length;
            }

            return combinedBytes;
        }

    }
}

