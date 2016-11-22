using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.NotificationService
{
    internal class NotificationService
    {
        public int keepaliveTime = 1000;

        List<TcpClient> clients = new List<TcpClient>();
        List<WebSocket> websockets = new List<WebSocket>();

        protected void handleClients()
        {

        }

        internal void HandleConnection(TcpClient client, WebSocket websocket)
        {
            clients.Add(client);
            websockets.Add(websocket);
        }
    }
}
