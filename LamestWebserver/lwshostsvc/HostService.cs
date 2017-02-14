using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using lwshostcore;

namespace lwshostsvc
{
    public partial class HostService : ServiceBase
    {
        public HostService()
        {
            InitializeComponent();
        }

        private List<Host> hosts = new List<Host>();

        protected override void OnStart(string[] args)
        {
            foreach (var port in lwshostcore.HostConfig.CurrentHostConfig.Ports)
            {
                try
                {
                    LamestWebserver.Master.StartServer(port, lwshostcore.HostConfig.CurrentHostConfig.WebserverFileDirectory);
                }
                catch (Exception e)
                {
                    LamestWebserver.ServerHandler.LogMessage("Failed to bind port " + port + ":\n" + e);
                }
            }

            foreach (var hostDirectory in lwshostcore.HostConfig.CurrentHostConfig.BinaryDirectories)
            {
                hosts.Add(new Host(hostDirectory));
            }
        }

        protected override void OnStop()
        {
            Environment.Exit(0);
        }
    }
}
