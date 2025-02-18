using MailKit.Net.Smtp;
using ReceiveTest.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReceiveTest.Factory
{
    public class EmailServiceFactory
    {
        public EmailService CreateEmailService()
        {
            var client = new SmtpClient();
            return new EmailService(client);
        }
    }
}
