﻿using MQMailSender.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using MailMQProducer.Services;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace MQMailSender.Services
{
    public class EmailService
    {
        private readonly RabbitMQConnectionFactory _connectionFactory;
        private const int MaxMessageSize = 1024 * 1024 * 17;
        public EmailService(RabbitMQConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string NormalExchangeName = "EmailNormalExchange";


        private const string NormalRoutingName = "EmailNormalRouting";

        public async void SendEmail(EmailDto emailDto)
        {
            try
            {
                using var channel = await _connectionFactory.CreateChannel();
                var jsonEmailDto = JsonConvert.SerializeObject(emailDto);
                var dToBody = Encoding.UTF8.GetBytes(jsonEmailDto);
                string filePath = emailDto.FilePath;
                var mailProps = new BasicProperties
                {
                    Headers = new Dictionary<string, object?> { { "MessageType", "MailEntity" } },
                    Persistent=true
                }; //标记邮件主体消息头
                var attachmentProps = new BasicProperties
                {
                    Headers = new Dictionary<string, object?> { { "MessageType", "Attachment" }  },
                    Persistent=true
                };//标记附件消息头
                var endAttachmentProps = new BasicProperties
                {
                    Headers = new Dictionary<string, object?> { { "MessageType", "AttachmentEndingMark" }  },
                    Persistent=true
                };//标记文件发送结束消息头
                var endEmailProps = new BasicProperties
                {
                    Headers = new Dictionary<string, object?> { { "MessageType", "EmailEndingMark" }  },
                    Persistent=true
                };//标记文件发送结束消息头

                //传输附件
                if (filePath != "string" && filePath != null)
                {
                        var bytes = File.ReadAllBytes(filePath);
                        var chunks = SplitFile(bytes);
                        foreach (var chunk in chunks)
                        {
                            byte[] msgWithChecksum = AddCheckMsgToChunk(chunk);
                            await channel.BasicPublishAsync(NormalExchangeName, NormalRoutingName, false, attachmentProps, msgWithChecksum);
                        await Task.Delay(1000);
                    }
                    await Task.Delay(1000);

                    await channel.BasicPublishAsync(NormalExchangeName, NormalRoutingName, false, endAttachmentProps, Encoding.UTF8.GetBytes("END_OF_FILE"));
                }
                //传输邮件主体
                await Task.Delay(1000);

                await channel.BasicPublishAsync(NormalExchangeName, NormalRoutingName, false, mailProps, dToBody);
                await Task.Delay(1000);

                await channel.BasicPublishAsync(NormalExchangeName, NormalRoutingName, false,endEmailProps, Encoding.UTF8.GetBytes("END_OF_EMAIL"));
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static byte[][] SplitFile(byte[] bytes)
        {
            int chunkCount = (int)Math.Ceiling((double)bytes.Length / MaxMessageSize);
            //计算分割后的块数，作为二维数组的其中一维
            byte[][] chunks = new byte[chunkCount][];

            int offset = 0;
            for (int i = 0; i < chunkCount; i++)
            {
                int chunkSize = Math.Min(MaxMessageSize, bytes.Length - offset); //数组大小
                byte[] chunk = new byte[chunkSize];
                Array.Copy(bytes, offset, chunk, 0, chunkSize); //复制进数组
                chunks[i] = chunk; //保存到二维数组
                offset += chunkSize; //索引增加
            }
            return chunks;
        }

        private static byte[] AddCheckMsgToChunk(byte[] chunk)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(chunk);
                var checksum = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                byte[] checksumBytes = Encoding.UTF8.GetBytes(checksum);
                byte[] message = new byte[chunk.Length + checksumBytes.Length];
                Array.Copy(chunk, message, chunk.Length);//将chunk复制到message
                Array.Copy(checksumBytes, 0, message, chunk.Length, checksumBytes.Length);
                //从message内消息块后一位开始，将校验码复制进message
                return message;
            }
        }

    }


}