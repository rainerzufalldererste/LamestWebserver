using LamestWebserver.JScriptBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
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
                ret += "\n\n" + Convert.ToBase64String(Encoding.ASCII.GetBytes(msg.JSEncode().Replace("&quot;", "\"")));

            return ret;
        }

        public abstract string GetNotification();
        
        public static Notification ExecuteScript(string script)
        {
            return new ExecuteScriptNotification(script);
        }

        public static Notification ReplaceDivWithContent(string divId, IJSValue content)
        {
            return new ExecuteScriptNotification(JSElement.getByID(divId).InnerHTML.Set(content).getCode(SessionData.currentSessionData, CallingContext.Inner));
        }

        public static Notification ReplaceDivWithContent(string divId, string content)
        {
            return ReplaceDivWithContent(divId, (JSStringValue) content);
        }

        public static Notification AddToDivContent(string divId, IJSValue content)
        {
            return new ExecuteScriptNotification(
                JSElement.getByID(divId).InnerHTML.Set(
                    JSElement.getByID(divId).InnerHTML + content
                    ).getCode(SessionData.currentSessionData, CallingContext.Inner));
        }

        public static Notification AddToDivContent(string divId, string content)
        {
            return AddToDivContent(divId, (JSStringValue) content);
        }

        public static Notification ReploadPage()
        {
            return new ExecuteScriptNotification(JSValue.CurrentBrowserURL.Set(JSValue.CurrentBrowserURL)
                .getCode(SessionData.currentSessionData, CallingContext.Inner));
        }

        public static Notification Redirect(IJSValue newPageUrl)
        {
            return new ExecuteScriptNotification(JSValue.CurrentBrowserURL.Set(newPageUrl)
                .getCode(SessionData.currentSessionData, CallingContext.Inner));
        }

        public static Notification Redirect(string newPageUrl)
        {
            return Redirect((JSStringValue)newPageUrl);
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

        public Thread handlerThread = null;

        private bool running = true;

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
                    _jselement = new JSPlainText("<script type='text/javascript'>" + NotificationHelper.NotificationCode(SessionData.currentSessionData, URL, out methodName, ID) + "</script>");

                    SendingFunction = new JSFunction(methodName);
                }

                return _jselement;
            }
        }

        private JSElement _jselement = null;

        public void serverClients()
        {
            while(running)
            {
                using (listMutex.Lock())
                {
                    for (int i = proxies.Count - 1; i >= 0; i--)
                    {
                        if (!proxies[i].isActive)
                        {
                            proxies.RemoveAt(i);
                            continue;
                        }

                        if ((DateTime.UtcNow - proxies[i].lastMessageSent) > WebSocketHandlerProxy.MaximumLastMessageTime)
                            proxies[i].Respond(new KeepAliveNotification().GetNotification());
                    }
                }

                Thread.Sleep(1);
            }
        }

        public void StopHandler()
        {
            running = false;
        }

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
            if(response.IsMessage)
                OnResponse(response);
        }

        private void connect(WebSocketHandlerProxy proxy)
        {
            using (listMutex.Lock())
            {
                ConnectedClients++;
                proxies.Add(proxy);
            }

            if(handlerThread == null)
            {
                handlerThread = new Thread(new ThreadStart(serverClients));
                handlerThread.Start();
            }

            // throw new WebSocketManagementOvertakeFlagException();
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
        public bool IsMessage = false;
        public string Message = null;
        public WebSocketHandlerProxy proxy;
        internal NotificationType notificationType;

        internal NotificationResponse(string input, WebSocketHandlerProxy proxy)
        {
            this.proxy = proxy;

            ParseNotificationResponse(input, this);
        }

        public void Reply(Notification notification)
        {
            proxy.Respond(notification.GetNotification());
        }

        public static void ParseNotificationResponse(string input, NotificationResponse response)
        {
            if (input.Length <= 0)
            {
                return;
            }

            string[] data = input.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if(data.Length <= 0)
            {
                return;
            }

            if (!System.Enum.TryParse<NotificationType>(data[0], out response.notificationType))
            {
                response.notificationType = NotificationType.Invalid;
                return;
            }

            switch(response.notificationType)
            {
                case NotificationType.Message:

                    response.IsMessage = true;
                    response.Message = "";

                    for (int i = 1; i < data.Length; i++)
                    {
                        response.Message += data[i];
                    }

                    break;

                default:
                    break;
            }
        }
    }
}
