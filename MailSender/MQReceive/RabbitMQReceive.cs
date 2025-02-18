using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQReceive
{
    public class RabbitMQReceive
    {
        static void Main(string[] args)
        {
            ReceiveService rmqService = new ReceiveService();
            rmqService.GetAppsettings();
            var name = rmqService.ReceiveMessage();

            Console.WriteLine("Received: Hello {0}, I am your father!", name);
        }
    }
}
