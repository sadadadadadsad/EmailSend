using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailPlugin.Contracts
{
    public interface IEmailPlugin
    {
        string Name { get; }
        Task StartAsync();
        Task StopAsync();
    }

}
