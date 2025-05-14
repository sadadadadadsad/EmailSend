using EmailServerPlugin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace EmailServerPlugin.Services
{
    public static class Startup
    {
        private const string UserConfigFileName = "appsettings.json";
        public static  IConfiguration LoadConfiguration()
        {

            try
            {
                var builder = new ConfigurationBuilder();
                builder.AddJsonStream(GetConfigStream());
                var userConfigPath = GetUserConfigPath();
                if (File.Exists(userConfigPath))
                {
                    builder.AddJsonFile(userConfigPath, optional: true, reloadOnChange: false);
                }
                return builder.Build();
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"{ex.GetType().Name}");
                Console.WriteLine($"{ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
                throw new InvalidOperationException("加载配置文件失败", ex);
            }
        }
        private static string GetPluginDirectory()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation)!;
        }

        // 获取用户自定义配置文件完整路径
        private static string GetUserConfigPath()
        {
            return Path.Combine(GetPluginDirectory(), UserConfigFileName);
        }
        private static Stream GetConfigStream()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "EmailServerPlugin.appsettings.json";
            var stream = assembly.GetManifestResourceStream(resourceName);
            return stream ?? throw new FileNotFoundException("配置文件未找到");
        }
        public static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddSingleton(configuration);

            services.AddSingleton<RabbitMQConnectionFactory>();
            services.AddSingleton<EmailServiceBase>();
            services.AddSingleton<DefaultSettingManager>();
            services.AddSingleton<ReceiveSettingManager>();
            return services.BuildServiceProvider();
        }
    }


}
