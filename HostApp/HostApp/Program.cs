using EmailPlugin.Contracts;
using System.Reflection;

namespace HostApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var pluginManager = new PluginManager();
            pluginManager.LoadPlugins("Plugins");

            await pluginManager.StartAllAsync();
            Console.WriteLine("邮件服务已启动，按 Enter 退出...");
            Console.ReadLine();

            await pluginManager.StopAllAsync();
        }
        public class PluginManager
        {
            private readonly List<IEmailPlugin> _plugins = new();

            public void LoadPlugins(string pluginDir)
            {
                foreach (var dll in Directory.GetFiles(pluginDir, "*.dll"))
                {
                    var assembly = Assembly.LoadFrom(dll);
                    foreach (var type in assembly.GetExportedTypes())
                    {
                        if (typeof(IEmailPlugin).IsAssignableFrom(type)&&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            var plugin = (IEmailPlugin)Activator.CreateInstance(type)!;
                            _plugins.Add(plugin);
                            Console.WriteLine($"插件 {plugin.Name} 已加载");
                        }
                    }
                }
            }

            public async Task StartAllAsync()
            {
                foreach (var plugin in _plugins)
                {
                    await plugin.StartAsync();
                }
            }

            public async Task StopAllAsync()
            {
                foreach (var plugin in _plugins)
                {
                    await plugin.StopAsync();
                }
            }
        }
    }
}
