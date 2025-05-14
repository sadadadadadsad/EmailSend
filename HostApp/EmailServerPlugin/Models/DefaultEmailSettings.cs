using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Models
{

    public class DefaultEmailSettings
    {
        /// <summary>
        /// 默认服务器
        /// </summary>
        public string? DEHost { get; set; }
        /// <summary>
        /// 默认端口
        /// </summary>
        public int DEPort { get; set; }
        /// <summary>
        /// 默认用户名
        /// </summary>
        public string? DEUser { get; set; } 
        /// <summary>
        /// 授权码
        /// </summary>
        public string? DECode { get; set; } 
    }
}
