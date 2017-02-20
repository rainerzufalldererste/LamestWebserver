using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using lwshostcore;
using LamestWebserver;

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
            try
            {
                Directory.Delete(Directory.GetCurrentDirectory() + "\\lwshost\\CurrentRun", true);
            }
            catch (Exception)
            {

            }

            lwshostcore.HostConfig.CurrentHostConfig.ApplyConfig();

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

            // Discover the HostServiceDefaultResponse
            Master.DiscoverPages();

            bool removed = false;

            Action<string> onRegister = s =>
            {
                if (!removed)
                    LamestWebserver.Master.RemoveDirectoryPageFromServer("/");

                removed = true;
            };

            foreach (var hostDirectory in lwshostcore.HostConfig.CurrentHostConfig.BinaryDirectories)
            {
                var host = new Host(hostDirectory, onRegister);
                hosts.Add(host);
            }
        }

        protected override void OnStop()
        {
            Master.StopServers();
            hosts.ForEach(h => h.Stop());
        }
    }
}
