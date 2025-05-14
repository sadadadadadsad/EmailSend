using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Models
{
    public class RabbitMQSettings
    {
        /// <summary>
        /// 主机名
        /// </summary>

        public string? MQHostName { get; set; }
        /// <summary>
        /// 端口
        /// </summary>

        public int MQPort { get; set; }
        /// <summary>
        /// mq用户名
        /// </summary>

        public string? MQUserName { get; set; }
        /// <summary>
        /// mq用户密码
        /// </summary>

        public string? MQPassword { get; set; }



    }
}
