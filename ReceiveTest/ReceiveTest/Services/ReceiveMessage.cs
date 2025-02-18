using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReceiveTest.Factory;
using ReceiveTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReceiveTest.Services
{
    
    public class ReceiveMessage
    {
        public async void ReceiveMessageAsync()
        {
            var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "",
                durable: true,//消息持久化  
                exclusive: false,//是否排他  
                autoDelete: false,//是否自动删除  
                arguments: null);//参数  

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);//平均调度    

            //创建消费者  
            var consumer = new AsyncEventingBasicConsumer(channel);
            //注册事件处理方法  
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var emailServiceFactory = new EmailServiceFactory();
                var mimeMessage = new MimeMessage();
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var emailDto = JsonConvert.DeserializeObject<EmailDto>(message); //转换为json
                var emailService = emailServiceFactory.CreateEmailService();
                Console.WriteLine(emailDto.From);
                //处理完消息后，确认收到  
                //multiple是否批量确认  
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);//消息确认，防止在传输时消息丢失后mq自动删除队列导致消息直接消失
                try
                {
                     emailService.Send(emailDto, emailDto.IsSingleSend, mimeMessage);
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"邮件发送失败{ex.Message}");
                }
            };

            //autoAck是否自动确认，false表示手动确认  
            await channel.BasicConsumeAsync(queue: "email_queue",
                 autoAck: true,
                 consumer: consumer); //为消息队列指定消费者对象
            Console.WriteLine("Press [enter] to exit");
            Console.ReadLine();
        }
    }
}
