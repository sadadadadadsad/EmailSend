using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReceiveTest.Models
{
    [JsonObject]
    public class RabbitMQConfig
    {
        /// <summary>
        /// 主机名
        /// </summary>

        public string HostName { get; set; }
        /// <summary>
        /// 端口
        /// </summary>

        public int Port { get; set; }
        /// <summary>
        /// mq用户名
        /// </summary>

        public string Username { get; set; }
        /// <summary>
        /// mq用户密码
        /// </summary>

        public string Password { get; set; }



    }
}
