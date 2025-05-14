using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Models
{
    public class EmailDto
    {

        /// <summary>
        /// 发送者邮箱
        /// </summary>
        public string? From { get; set; }
        /// <summary>
        /// 发送者显示名
        /// </summary>
        public string? FromName { get; set; }
        /// <summary>
        /// 接收者邮箱（多个用英文“,”号分割）
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// 接收者名字
        /// </summary>
        public string? ToName { get; set; }
        /// <summary>
        /// 附件路径
        /// </summary>
        public string? FilePath { get; set; }
        /// <summary>
        /// 文件名
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// 文件源
        /// </summary>
        public byte[]? FileArray { get; set; }

        /// <summary>
        /// 抄送者地址
        /// </summary>
        public string? Cc { get; set; }
        /// <summary>
        /// 抄送者显示名
        /// </summary>
        public string? CcName { get; set; }



        /// <summary>
        /// 邮箱主题
        /// </summary>

        public string? Subject { get; set; }

        /// <summary>
        /// 邮箱纯文本内容
        /// </summary>
        public string? TextBody { get; set; }

        /// <summary>
        /// 邮箱html内容
        /// </summary>
        public string? HtmlBody { get; set; }


        /// <summary>
        /// 回复地址
        /// </summary>
        public string? Reply { get; set; }

        /// <summary>
        /// SMTP服务器主机名 如：smtp.163.com
        /// </summary>
        
        public string? Host { get; set; }

        /// <summary>
        /// SMTP服务器端口号 如：25
        /// </summary>
        
        public int Port { get; set; }
        /// <summary>
        /// 发送者邮箱授权码
        /// </summary>
        
        public string? AuthorizationCode { get; set; }


    }
}
