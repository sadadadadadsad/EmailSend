using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ReceiveTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReceiveTest.Services
{
    public class EmailService : EmailServiceBase
    {
        private static EmailDto emailDto;
        private readonly SmtpClient smtpClient;

        public EmailService(string path)
        {
            emailDto = new EmailDto().Create(path);
        }
        public EmailService(EmailDto dTo)
        {
            emailDto = dTo;
        }
        public EmailService(SmtpClient smtpClient)
        {
            this.smtpClient = smtpClient;
        }
        public async new void Send(EmailDto emailDto, bool IsSingleSend, MimeMessage message = null)
        {
            base.Send(emailDto,message, IsSingleSend);
        }
    }
}
