using EmailServerPlugin.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailServerPlugin.Services
{
    public class DefaultSettingManager
    {
        private readonly DefaultEmailSettings _defaultSettings;

        public DefaultSettingManager(IConfiguration configuration)
        {
            _defaultSettings = configuration.GetSection("DefaultEmail").Get<DefaultEmailSettings>() ?? throw new ArgumentNullException("默认邮箱配置错误");
        }
        public DefaultEmailSettings GetDefaultSettings()
        {
            return _defaultSettings;
        }
    }
}
