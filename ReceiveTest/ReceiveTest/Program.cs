using ReceiveTest.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using ReceiveTest.Services;
using MailKit.Net.Smtp;


class Program
{
    static void Main(string[] args)
    {  
       EmailServiceBase service = new EmailServiceBase();
        service.ReceiveMessageAsync();
        Console.ReadLine();
    }
}
