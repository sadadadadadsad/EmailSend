using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using ReceiveTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReceiveTest.Services
{
    public class EmailServiceBase
    {
        private readonly SmtpClient client;

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="emailDto"></param> 邮件信息
        /// <param name="emailConfig"></param> 邮件配置
        /// <param name="message"></param> MimeMessage
        /// <param name="IsSingleSend"></param> 是否单一收件人
        protected void Send(EmailDto emailDto, MimeMessage message, bool IsSingleSend = false)
        {
            try
            {

                message = message ?? new MimeMessage();
                message.Subject = emailDto.Subject;
                if (!string.IsNullOrEmpty(emailDto.Reply))
                {
                    message.ReplyTo.Add(new MailboxAddress(emailDto.ToName, emailDto.To));
                }

                if (!string.IsNullOrEmpty(emailDto.Cc))
                {
                    message.Cc.Add(new MailboxAddress(emailDto.CcName, emailDto.Cc));
                }
                string validBody = !string.IsNullOrEmpty(emailDto.TextBody) ? emailDto.TextBody : emailDto.HtmlBody;
                if (validBody == emailDto.TextBody)

                    message.Body = new TextPart(TextFormat.Text) { Text = validBody }; //正文

                else if (validBody == emailDto.HtmlBody)
                    message.Body = new TextPart(TextFormat.Html) { Text = validBody };



                    if (IsSingleSend && emailDto.To.Contains(",")) //接受者为多人,通过轮询实现单发，其他接受者不知道有多个收件人
                    {
                        string[] ToArray = emailDto.To.Split(',');
                        string[] ToNameArray = emailDto.ToName.Split(",");
                        if (ToArray.Length != ToNameArray.Length)
                        {
                            Console.WriteLine("接受者地址和显示名称数量不匹配");
                            throw new Exception();
                        }
                        for (int i = 0; i < ToArray.Length; i++)
                        {
                            string To = ToArray[i].Trim();
                            string ToName = ToArray[i].Trim();
                            message.To.Add(new MailboxAddress(ToName, To));
                        }
                    }
                    else
                    {
                        if (!IsSingleSend && emailDto.To.Contains(",")) //接受者为单人
                        {
                            message.To.Add(new MailboxAddress(emailDto.ToName, emailDto.To));

                        }
                        else
                        {
                            message.To.Add(new MailboxAddress(string.IsNullOrEmpty(emailDto.ToName) ? emailDto.To : emailDto.ToName, emailDto.To)); //ToName为空则用To代替
                        }
                        StmpClientSend(message, emailDto);
                    }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        private async void StmpClientSend(MimeMessage message, EmailDto emailDto)
        {
            try
            {
                message.From.Add(new MailboxAddress(emailDto.DisplayName, emailDto.From));

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(emailDto.Host, emailDto.Port, SecureSocketOptions.SslOnConnect); //连接qq邮箱主机和端口，ssl安全协议 host？
                    await client.AuthenticateAsync(emailDto.From, emailDto.AuthorizationCode); //账户和授权码认证
                    await client.SendAsync(message); //发送
                    await client.DisconnectAsync(true);//断开连接
                }
            }
            catch (Exception ex)
            {
                await client.DisconnectAsync(true);
                Console.WriteLine($"邮件发送失败:{ex.Message}");
            }
        }
    }
}
