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
    public interface IEmailService
    {
        public EmailServiceBase CreateEmailService();
        public MimeMessage Save(EmailDto emailDto, bool IsSingleSend, MimeMessage message = null);
        public void ReceiveMessageAsync();
    }
}
