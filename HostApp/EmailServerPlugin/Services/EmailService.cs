using EmailPlugin.Contracts;
using EmailServerPlugin.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EmailServerPlugin.Services
{
    public class EmailService : IEmailPlugin
    {
        private EmailServiceBase _serviceBase;
        private RabbitMQConnectionFactory _mqFactory;
        private IChannel _channel; 
        private readonly IServiceProvider _serviceProvider;


        string IEmailPlugin.Name => "Email Service Plugin";

        public async Task StartAsync()
        {

            try
            {
                EmailServiceBase service = new();
                _serviceBase = service;
                var configuration = Startup.LoadConfiguration();
                using var serviceProvider = Startup.ConfigureServices(configuration);
                _mqFactory = serviceProvider.GetRequiredService<RabbitMQConnectionFactory>();
                _channel = await _mqFactory.CreateChannelAsync();
                await _serviceBase.Base(_channel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动失败: {ex.Message}");
                throw;
            }

        }

        public async Task StopAsync()
        {
            if (_channel != null && _channel.IsOpen)
            {
                await _channel.CloseAsync();
                _channel.Dispose();


                // 清理工厂资源（如果需要）
                _mqFactory?.Dispose();

                // 关闭基础服务
                await _serviceBase.CloseAsync(_channel);
            }
        }
    }
}
