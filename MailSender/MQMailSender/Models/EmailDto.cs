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
        /// 附件地址 注意json格式需要两个\\进行转义
        /// </summary>
        public string? FilePath { get; set; }
        public string? FileArray { get; set; }
        /// <summary>
        /// 抄送者地址
        /// </summary>
        public string? Cc { get; set; }

        public string? CcName { get; set; }



        /// <summary>
        /// 邮箱主题
        /// </summary>
        public string Subject { get; set; }

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
        /// 读取该路径下的配置文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
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
        /// 发送者显示名
        /// </summary>
        [JsonProperty]
        public string DisplayName { get; set; }
    }
}
