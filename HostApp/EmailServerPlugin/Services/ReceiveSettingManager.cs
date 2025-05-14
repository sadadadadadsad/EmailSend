using EmailServerPlugin.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Services
{
    public class ReceiveSettingManager
    {
        private readonly ReceiveSetting _receiveSetting;

        public ReceiveSettingManager(IConfiguration configuration)
        {
            _receiveSetting = configuration.GetSection("ReceiveEmail").Get<ReceiveSetting>() ?? throw new ArgumentNullException("订阅者邮箱配置错误");

        }
        public ReceiveSetting GetReceiveSettings()
        {
            return _receiveSetting;
        }
    }
}
