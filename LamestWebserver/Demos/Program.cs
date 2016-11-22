using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using Fleck;

namespace Demos
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Demos for LamestWebServer\n");
            try
            {
                Master.StartServer(8080, "./web");
            }
            catch(InvalidOperationException)
            {
                Console.ReadLine();
                return;
            }

            pageBuilderTest.addLamePageBuilderTest();
            new pageFillerTest();
            CardGame.register();
            XmlTest.register();
            new jsconn();
            Console.WriteLine("Demos added.\nEnter 'exit' to quit.\n");

            ////////////////

            Master.addFuntionToServer("fleckdemo", (SessionData data) => { return @"<!DOCTYPE HTML PUBLIC '-//W3C//DTD HTML 4.0 Transitional//EN'>
<html>
<head>
    <title>websocket client</title>
    <script type='text/javascript'>
        var start = function () {
            var inc = document.getElementById('incomming');
            var wsImpl = window.WebSocket || window.MozWebSocket;
            var form = document.getElementById('sendForm');
            var input = document.getElementById('sendText');
            
            inc.innerHTML += 'connecting to server ..<br/>';
            // create a new websocket and connect
            window.ws = new wsImpl('ws://localhost:8181/');
            // when data is comming from the server, this metod is called
            ws.onmessage = function (evt) {
                inc.innerHTML += evt.data + '<br/>';
            };
            // when the connection is established, this method is called
            ws.onopen = function () {
                inc.innerHTML += '.. connection open<br/>';
            };
            // when the connection is closed, this method is called
            ws.onclose = function () {
                inc.innerHTML += '.. connection closed<br/>';
            }
            
			form.addEventListener('submit', function(e){
				e.preventDefault();
				var val = input.value;
				ws.send(val);
				input.value = '';
			});
            
        }
        window.onload = start;
    </script>
</head>
<body>
	<form id='sendForm'>
		<input id='sendText' placeholder='Text to send' />
	</form>
    <pre id='incomming'></pre>
</body>
</html>"; });

            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    allSockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    allSockets.Remove(socket);
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine(message);
                    allSockets.ToList().ForEach(s => s.Send("Echo: " + message));
                };
            });


            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

            //////////////////////////////////////

            /*string input = "";

            while (input != "exit")
            {
                input = Console.ReadLine();

                if (input == "lws")
                {
                    ServerHandler.Main(args);
                    break;
                }
            }*/

            Master.StopServers();
        }
    }
}
