using ReceiveTest.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using ReceiveTest.Factory;
using ReceiveTest.Services;
using MailKit.Net.Smtp;


class Program
{
    static void Main(string[] args)
    {  
        var receiveMessage= new ReceiveMessage();
         receiveMessage.ReceiveMessageAsync();
        Console.WriteLine("1");
        Console.ReadLine();
       




    }
}
