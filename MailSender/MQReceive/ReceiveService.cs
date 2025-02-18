using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQReceive
{

    public class ReceiveService : IDisposable
    {
        private string username { get; set; }
        private string password { get; set; }
        private string virtualhost { get; set; }
        private string hostname { get; set; }
        private string queuekey { get; set; }
        private IConnection conn { get; set; }
        private IChannel channel { get; set; }
        private string msg { get; set; }

        public void GetAppsettings()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            username = configuration["username"].ToString();
            password = configuration["password"].ToString();
            virtualhost = configuration["virtualhost"].ToString();
            hostname = configuration["hostname"].ToString();
            queuekey = configuration["queuekey"].ToString();
        }

        private async Task<bool> RMQConnect()
        {
            ConnectionFactory connFact = new ConnectionFactory();
            connFact.UserName = username;
            connFact.Password = password;
            connFact.VirtualHost = virtualhost;
            connFact.HostName = hostname;
            connFact.RequestedHeartbeat = new TimeSpan(60);

            int attempts = 0;
            while (attempts < 5)
            {
                attempts++;

                try
                {
                    conn = await connFact.CreateConnectionAsync();
                    CreateChannel();
                    return true;
                }
                catch (System.IO.EndOfStreamException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    Console.WriteLine(e.Message);
                    Thread.Sleep(5000);
                    return false;
                }
            }
            if (conn != null) conn.Dispose();

            return false;
        }

        private async void CreateChannel()
        {
            channel = await conn.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: queuekey, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public async Task<string> ReceiveMessage()
        {
            if (conn is null)
            {
                await RMQConnect();
            }
            while (true)
            {
                ReceiveMsg();
                if (!string.IsNullOrEmpty(msg)) break;
            }

            string result = ValidateMessage(msg);
            return result;
        }

        private void ReceiveMsg()
        {
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var msgbytes = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(msgbytes);
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                msg = message;
            };

            consumer.ShutdownAsync += async (model, ea) =>
            {
                await RMQConnect();
                ReceiveMsg();
            };

            channel.BasicConsumeAsync(queue: queuekey, autoAck: false, consumer: consumer);
            Thread.Sleep(100);
        }

        public string ValidateMessage(string message)  //过滤信息
        {
            string ValidMessage = "";
            if (message.Length > 0) //是否为空 
            {
                string[] Names = message.Split(new char[] { ',' }); //按，分割信息
                if (Names.Length > 1)
                {
                    if (!string.IsNullOrWhiteSpace(Names[1])) //检查第二字符是否为空或全空白字符
                    {
                        ValidMessage = Names[1].Trim();  //去除空白字符
                    }
                    else ValidMessage = "";
                }
                else ValidMessage = "";
            }
            return ValidMessage; 
        }

        void IDisposable.Dispose()  //继承接口实现通道消除
        {
            if (channel != null) { channel.CloseAsync(); }
            if (conn != null) { conn.CloseAsync(); conn.Dispose(); }
            GC.SuppressFinalize(this);
        }
    }
}