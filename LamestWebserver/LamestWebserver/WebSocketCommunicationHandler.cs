using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public class WebSocketCommunicationHandler : IURLIdentifyable
    {
        public string URL { get; private set; }

        public WebSocketCommunicationHandler(string URL)
        {
            this.URL = URL;

            register();
        }

        public WebSocketCommunicationHandler(string URL, Action<string> onMessage, Action onClose, Action<byte[]> onBinary, Action<byte[]> onPing, Action<byte[]> onPong) : this(URL)
        {
            this.OnMessage = onMessage;
            this.OnClose = onClose;
            this.OnBinary = onBinary;
            this.OnPing = onPing;
            this.OnPong = onPong;
        }

        public Action<string> OnMessage;
        public Action OnClose;
        public Action<byte[]> OnBinary;
        public Action<byte[]> OnPing = (byte[] bytes) => { currentHandler.FramePong(bytes); };
        public Action<byte[]> OnPong;

        [ThreadStatic]
        public static Fleck.IHandler currentHandler = null;

        protected void register()
        {
            Master.AddWebsocketHandler(this);
        }

        protected void unregister()
        {
            Master.RemoveWebsocketHandler(this.URL);
        }

        internal void WriteAsync(string message)
        {
            if (currentHandler == null)
                throw new InvalidOperationException("The current handler can not be null.");

            currentHandler.FrameText(message);
        }
    }
}
