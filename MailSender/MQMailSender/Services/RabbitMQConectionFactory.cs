using MQMailSender.Models;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MailMQProducer.Services
{
    public class RabbitMQConnectionFactory : IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private IConnection _connection { get; set; }

        public RabbitMQConnectionFactory(IOptions<RabbitMQSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<IChannel> CreateChannel()
        {
            if (_connection == null || _connection.IsOpen == false)
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _settings.Hostname,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    Port=_settings.Port,
                    
                };
                _connection = await factory.CreateConnectionAsync();
            }
            return await _connection.CreateChannelAsync();
        }




        public void Dispose() 
        {
            if (_connection != null)
            {
                if (_connection.IsOpen)
                {
                    _connection.CloseAsync();
                }
                _connection.DisposeAsync();
            }
        }
    }
}