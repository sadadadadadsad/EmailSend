using MQMailSender.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using MailMQProducer.Services;

namespace MQMailSender.Services
{
    public class EmailService
    {
        private readonly RabbitMQConnectionFactory _connectionFactory;

        public EmailService(RabbitMQConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async void SendEmail(EmailDto emailDto)
        {
            using var channel = await _connectionFactory.CreateChannel();
            await channel.QueueDeclareAsync(queue: "", durable: true, exclusive: false, autoDelete: false, arguments: null);
            var jsonEmailDto = JsonConvert.SerializeObject(emailDto);
            var dToBody = Encoding.UTF8.GetBytes(jsonEmailDto);


                await channel.BasicPublishAsync(string.Empty, "email_queue", dToBody);


            var properties = new BasicProperties
            {
                Persistent = true
            };


            await channel.BasicPublishAsync(string.Empty, "email_queue", dToBody);
        }
        //public async void SendPath()
        //{
        //    using var channel = await _connectionFactory.CreateChannel();
        //    await channel.QueueDeclareAsync(queue: "1", durable: true, exclusive: false, autoDelete: false, arguments: null);
        //    var filePath=
        //    var message = JsonConvert.SerializeObject(filePath);
        //    var body = Encoding.UTF8.GetBytes(message);
        //    var properties = new BasicProperties
        //    {
        //        Persistent = true
        //    };
        //    await channel.BasicPublishAsync(string.Empty, "1", body);

    }

}