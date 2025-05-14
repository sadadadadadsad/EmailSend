using EmailServerPlugin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Services
{
    public class RabbitMQConnectionFactory : IDisposable
    {
        private IConnection Connection { get; set; }
        private readonly RabbitMQSettings mQSettings;
        public RabbitMQConnectionFactory(IConfiguration configuration)
        {
            mQSettings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>() ?? throw new ArgumentNullException("RabbitMQ配置错误");
            //读取配置文件中为"RabbitMQ"的全部内容
        }
        public RabbitMQConnectionFactory()
        {

        }


        public async Task<IChannel> CreateChannelAsync()
        {
            if (Connection == null || Connection.IsOpen == false)
            {
                var factory = new ConnectionFactory()//给连接工厂属性填入配置文件的值
                {
                    HostName = mQSettings.MQHostName ,
                    UserName = mQSettings.MQUserName ,
                    Password = mQSettings.MQPassword ,
                    Port = mQSettings.MQPort ,

                };
                Connection = await factory.CreateConnectionAsync(); //创建连接
            }
            return await Connection.CreateChannelAsync(); //返回创建后的信道
        }




        public void Dispose()
        {
            if (Connection != null)
            {
                if (Connection.IsOpen)
                {
                    Connection.CloseAsync();
                }
                Connection.DisposeAsync();
            }
        }
    }
}
