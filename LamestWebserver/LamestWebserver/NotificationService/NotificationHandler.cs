using LamestWebserver.JScriptBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.NotificationService
{
    public interface INotificationHandler
    {
        string URL { get; }

        void Unregister();

        void Notify(Notification notification);
    }

    public interface INotificationResponse
    {
        string URL { get; }

        Notification getResponse(SessionData sessionData, string Request);
    }

    public abstract class Notification
    {
        public readonly NotificationType NotificationType;
        protected bool NoReply = false;

        protected Notification(NotificationType type)
        {
            NotificationType = type;
        }

        public override string ToString()
        {
            string ret = NotificationType.ToString();

            if (NoReply)
                ret += "\r\n" + NotificationOption.NoReply;

            return ret;
        }

        public string ToString(string msg)
        {
            string ret = NotificationType.ToString();

            if (NoReply)
                ret += "\n\r" + NotificationOption.NoReply;

            if (!string.IsNullOrWhiteSpace(msg))
                ret += "\n\n" + Convert.ToBase64String(Encoding.ASCII.GetBytes(msg.jsEncode().Replace("&quot;", "\"")));

            return ret;
        }

        public abstract string GetNotification();
        
        public static Notification ExecuteScript(string script)
        {
            return new ExecuteScriptNotification(script);
        }
    }

    public class KeepAliveNotification : Notification
    {
        public KeepAliveNotification() : base(NotificationType.KeepAlive)
        {
        }

        public override string GetNotification()
        {
            return base.ToString();
        }
    }

    public class ExecuteScriptNotification : Notification
    {
        public string script { get; private set; }

        public ExecuteScriptNotification(string script) : base(NotificationType.ExecuteScript)
        {
            this.script = script;
        }

        public override string GetNotification()
        {
            return base.ToString(script);
        }
    }

    public enum NotificationType : byte
    {
        Acknowledge, KeepAlive, Message, Invalid, ExecuteScript, Redirect, ReloadPage, ReplacePageContent, ReplacePageBody, ReplaceDivContent, ExpandPageBodyWith, ExpandDivContentWith
    }

    public enum NotificationOption : byte
    {
        NoReply
    }

    public class NotificationHandler : WebSocketCommunicationHandler, INotificationHandler
    {
        private UsableMutex listMutex = new UsableMutex();

        private List<WebSocketHandlerProxy> proxies = new List<WebSocketHandlerProxy>();

        public JSFunction SendingFunction { get; private set; }
        public string ID { get; private set; } = SessionContainer.generateHash();
        public uint ConnectedClients { get; private set; } = 0;

        public NotificationHandler(string URL) : base(URL)
        {
            OnMessage += (input, proxy) => HandleResponse(new NotificationResponse(input, proxy));
            OnConnect += proxy => connect(proxy);
            OnDisconnect += proxy => disconnect(proxy);
        }

        public event Action<NotificationResponse> OnResponse;

        public JSElement JSElement
        {
            get
            {
                if (_jselement == null)
                {
                    string methodName;
                    _jselement = new JSPlainText("<script type='text/javascript'>" + JSNotificationClient.NotificationCode(SessionData.currentSessionData, URL, out methodName, ID) + "</script>");

                    SendingFunction = new JSFunction(methodName);
                }

                return _jselement;
            }
        }

        private JSElement _jselement = null;
        
        public void Notify(Notification notification)
        {
            string notificationText = notification.GetNotification();

            using (listMutex.Lock())
                proxies.ForEach(proxy => proxy.Respond(notificationText));
        }

        public void Unregister()
        {
            unregister();
        }

        public virtual void HandleResponse(NotificationResponse response)
        {
            if(response.isMessage)
                OnResponse(response);
        }

        private void connect(WebSocketHandlerProxy proxy)
        {
            using (listMutex.Lock())
            {
                ConnectedClients++;
                proxies.Add(proxy);
            }
        }

        private void disconnect(WebSocketHandlerProxy proxy)
        {
            using (listMutex.Lock())
            {
                ConnectedClients--;
                proxies.Remove(proxy);
            }
        }

        public IJSPiece SendMessage(IJSPiece messageGetter)
        {
            return SendingFunction.callFunction(new JSValue(messageGetter.getCode(SessionData.currentSessionData, CallingContext.Inner)));
        }

        public IJSPiece SendMessage(string message)
        {
            return SendingFunction.callFunction(new JSStringValue(message));
        }
    }

    public class NotificationResponse
    {
        public bool isMessage = true;
        public string message = "";
        public WebSocketHandlerProxy proxy;

        internal NotificationResponse(string input, WebSocketHandlerProxy proxy)
        {
            message = input;
            this.proxy = proxy;
        }

        public void Reply(Notification notification)
        {
            proxy.Respond(notification.GetNotification());
        }
    }
}
