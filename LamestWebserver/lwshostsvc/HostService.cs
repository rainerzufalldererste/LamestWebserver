using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using lwshostcore;
using LamestWebserver;
using LamestWebserver.RequestHandlers;

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

            HostConfig.CurrentHostConfig.ApplyConfig();

            HostConfig.CurrentHostConfig.ApplyConfig();

            RequestHandler.CurrentResponseHandler.InsertSecondaryRequestHandler(new ErrorRequestHandler());
            RequestHandler.CurrentResponseHandler.AddRequestHandler(new WebSocketRequestHandler());
            RequestHandler.CurrentResponseHandler.AddRequestHandler(new PageResponseRequestHandler());
            RequestHandler.CurrentResponseHandler.AddRequestHandler(new OneTimePageResponseRequestHandler());

            foreach (var directory in HostConfig.CurrentHostConfig.WebserverFileDirectories)
            {
                RequestHandler.CurrentResponseHandler.AddRequestHandler(new CachedFileRequestHandler(directory));
                ServerHandler.LogMessage($"Added WebserverFileDirectory '{directory}'");
            }

            RequestHandler.CurrentResponseHandler.AddRequestHandler(new DirectoryResponseRequestHandler());

            foreach (var port in HostConfig.CurrentHostConfig.Ports)
            {
                try
                {
                    new WebServer(port);
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("Failed to bind port " + port + ":\n" + e);
                }
            }

            if (WebServer.ServerCount == 0)
            {
                ServerHandler.LogMessage("No server started. Aborting.");
                ServerHandler.StopHandler();
                return;
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
