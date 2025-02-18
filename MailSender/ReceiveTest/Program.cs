using MQMailSender.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ReceiveTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            static async void Main(string[] args)
            {
                var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: "email_queue",
                    durable: true,//是否持久化  
                    exclusive: false,//是否排他  
                    autoDelete: false,//是否自动删除  
                    arguments: null);//参数  

                //这里可以设置prefetchCount的值，表示一次从队列中取多少条消息，默认是1，可以根据需要设置  
                //这里设置了prefetchCount为1，表示每次只取一条消息，然后处理完后再确认收到，这样可以保证消息的顺序性  
                //global是否全局  
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

                //创建消费者  
                var consumer = new AsyncEventingBasicConsumer(channel);
                //注册事件处理方法  
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var email = JsonConvert.DeserializeObject<EmailDto>(message);
                    Console.WriteLine(" [x] 发送邮件 {0}", email.To);
                    Console.WriteLine(" [x] 发送邮件 {0}", email.From);
                    //处理完消息后，确认收到  
                    //multiple是否批量确认  
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                };    //开始消费  
                      //queue队列名  
                      //autoAck是否自动确认，false表示手动确认  
                      //consumer消费者  
                await channel.BasicConsumeAsync(queue: "email_queue",
                     autoAck: false,
                     consumer: consumer);
            }

        }
    }
}
