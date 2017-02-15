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

            LamestWebserver.Master.addDirectoryPageToServer("/",
                (data, url) => new PageBuilder("LamestWebserver Host Service")
                               {
                                   elements = {new HContainer() {elements = {new HHeadline("No pages have been added to the Host Service yet.")}}}
                               }*data);

            foreach (var hostDirectory in lwshostcore.HostConfig.CurrentHostConfig.BinaryDirectories)
            {
                hosts.Add(new Host(hostDirectory, true));
            }
        }

        protected override void OnStop()
        {
            Environment.Exit(0);
        }
    }
}
