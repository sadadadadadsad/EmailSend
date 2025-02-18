using Newtonsoft.Json;

namespace MQMailSender.Models
{
    public class EmailDto
    {
        /// <summary>
        /// 主题行 
        /// </summary>
        private string? _subject;

        /// <summary>
        /// 发送者邮箱
        /// </summary>

        public string? From { get; set; }
        /// <summary>
        /// 接收者邮箱（多个用英文“,”号分割）
        /// </summary>
        public string? To { get; set; }

        /// <summary>
        /// 接收者名字
        /// </summary>
        public string ToName { get; set; }
        /// <summary>
        /// 附件地址
        /// </summary>
        public string? Path { get; set; }
        /// <summary>
        /// 抄送者地址
        /// </summary>
        public string? Cc { get; set; }

        public string? CcName { get; set; }



        /// <summary>
        /// 邮箱主题
        /// </summary>
        public string Subject
        {
            get
            {
                if (string.IsNullOrEmpty(_subject) && _subject.Length > 15)
                {
                    if (string.IsNullOrEmpty(Body = TextBody ?? HtmlBody))
                    {
                        return Body.Substring(0, 15);
                    }
                }
                return _subject;
            }
            set { _subject = value; }
        }

        /// <summary>
        /// 邮箱纯文本内容
        /// </summary>
        public string? TextBody { get; set; }

        /// <summary>
        /// 邮箱html内容
        /// </summary>
        public string? HtmlBody { get; set; }

        public string? Body { get; set; }

        /// <summary>
        /// 回复地址
        /// </summary>
        public string? Reply { get; set; }


        /// <summary>
        /// 读取该路径下的配置文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <returns></returns>
        public EmailDto Create(string configPath)
        {
            try
            {
                using var sr = new StreamReader(configPath);
                return JsonConvert.DeserializeObject<EmailDto>(sr.ReadToEnd());
            }
            catch
            {
                return new EmailDto();
            }
        }

        /// <summary>
        /// 服务器主机名 如：smtp.163.com
        /// </summary>
        [JsonProperty]
        public string Host { get; set; }

        /// <summary>
        /// 服务器端口号 如：25
        /// </summary>
        [JsonProperty]
        public int Port { get; set; }
        /// <summary>
        /// 发送者邮箱授权码
        /// </summary>
        [JsonProperty]
        public string AuthorizationCode { get; set; }

        /// <summary>
        /// 是否包含Html代码
        /// </summary>
        [JsonProperty]
        public bool IsHtml { get; set; }

        /// <summary>
        /// 发送者显示名
        /// </summary>
        [JsonProperty]
        public string DisplayName { get; set; }
        /// <summary>
        /// 是否启用SSL 默认：false 
        /// 如果启用 端口号要改为加密方式发送的
        /// </summary>
        [JsonProperty]
        public bool EnableSsl { get; set; }
    }
}
