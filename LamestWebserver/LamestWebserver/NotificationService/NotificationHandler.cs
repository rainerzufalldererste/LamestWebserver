using LamestWebserver.JScriptBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
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
            return ToString(new KeyValuePair<string, string>[0]);
        }

        public string ToString(params KeyValuePair<string, string>[] args)
        {
            JsonNotificationPacket packet = new JsonNotificationPacket(NotificationType, args);

            if (NoReply)
                packet.Values.Add(JsonNotificationPacket.NoReply_string, JsonNotificationPacket.NoReply_string);

            return packet.Serialize();
        }

        public abstract string GetNotification();

        public static Notification ExecuteScript(string script)
        {
            return new ExecuteScriptNotification(script);
        }

        public static Notification ReplaceDivWithContent(string divId, IJSValue content)
        {
            return
                new ExecuteScriptNotification(
                    JSElement.getByID(divId)
                        .InnerHTML.Set(content)
                        .getCode(SessionData.currentSessionData, CallingContext.Inner));
        }

        public static Notification ReplaceDivWithContent(string divId, string content)
        {
            return ReplaceDivWithContent(divId, new JSValue(content.Base64Encode()));
        }

        public static Notification ReplaceBodyWithContent(IJSValue content)
        {
            return
                new ExecuteScriptNotification(
                    JSElement.Body
                        .InnerHTML.Set(content)
                        .getCode(SessionData.currentSessionData, CallingContext.Inner));
        }

        public static Notification ReplaceBodyWithContent(string content)
        {
            return ReplaceBodyWithContent(new JSValue(content.Base64Encode()));
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
            return AddToDivContent(divId, new JSValue(content.Base64Encode()));
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
            return Redirect((JSStringValue) newPageUrl);
        }

        internal static Notification Invalid()
        {
            return new InvalidNotification();
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

    public class InvalidNotification : Notification
    {
        public InvalidNotification() : base(NotificationType.Invalid)
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
            return base.ToString(new KeyValuePair<string, string>(JsonNotificationPacket.Script_string, script));
        }
    }

    public enum NotificationType : byte
    {
        Acknowledge,
        KeepAlive,
        Message,
        Invalid,
        ExecuteScript,
        Redirect,
        ReloadPage,
        ReplacePageContent,
        ReplacePageBody,
        ReplaceDivContent,
        ExpandPageBodyWith,
        ExpandDivContentWith
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
        public readonly bool NotifyForKeepalives;

        public NotificationHandler(string URL, bool notifyForKeepalives = false) : base(URL)
        {
            NotifyForKeepalives = notifyForKeepalives;
            OnMessage += (input, proxy) => HandleResponse(new NotificationResponse(input, proxy, URL));
            OnConnect += proxy => connect(proxy);
            OnDisconnect += proxy => disconnect(proxy);

            string methodName;
            _jselement =
                new JSPlainText("<script type='text/javascript'>" +
                                NotificationHelper.JsonNotificationCode(SessionData.currentSessionData, URL,
                                    out methodName, ID) + "</script>");

            SendingFunction = new JSFunction(methodName);
        }

        public event Action<NotificationResponse> OnNotification;

        public JSElement JSElement => new JSPlainText("<script type='text/javascript'>" +
                                NotificationHelper.JsonNotificationCode(SessionData.currentSessionData, URL, ID) + "</script>");

        private JSElement _jselement = null;

        public void serverClients()
        {
            while (running)
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

                        if ((DateTime.UtcNow - proxies[i].lastMessageSent) >
                            WebSocketHandlerProxy.MaximumLastMessageTime)
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
            if (response.IsMessage || (response.NotificationType == NotificationType.KeepAlive && NotifyForKeepalives))
                OnNotification(response);
        }

        private void connect(WebSocketHandlerProxy proxy)
        {
            using (listMutex.Lock())
            {
                ConnectedClients++;
                proxies.Add(proxy);
            }

            if (handlerThread == null)
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
            return
                SendingFunction.callFunction(
                    new JSValue(messageGetter.getCode(SessionData.currentSessionData, CallingContext.Inner)));
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
        internal NotificationType NotificationType;
        private Dictionary<string, string> Values = new Dictionary<string, string>();
        private bool noreply = false;

        public SessionData SessionData { get; private set; }

        internal NotificationResponse(string input, WebSocketHandlerProxy proxy, string URL)
        {
            this.proxy = proxy;

            ParseNotificationResponse(input, this, URL);
        }

        public void Reply(Notification notification)
        {
            proxy.Respond(notification.GetNotification());
        }

        public static void ParseNotificationResponse(string input, NotificationResponse response, string URL)
        {
            if (input.Length <= 0)
            {
                return;
            }

            JsonNotificationPacket packet;

            try
            {
                packet = new JsonNotificationPacket(input);
            }
            catch (Exception)
            {
                if (input.Contains(NotificationType.Invalid.ToString()))
                    return;

                response.Reply(Notification.Invalid());
                return;
            }

            response.Values = packet.Values;
            response.NotificationType = packet.NotificationType;
            response.noreply = packet.noreply;

            string ssid = packet.SSID;

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                response.SessionData = new SessionData(
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    new List<string>(),
                    new List<KeyValuePair<string, string>> {new KeyValuePair<string, string>("ssid", ssid)},
                    URL, URL, input, null, response.proxy.GetNetworkStream());
            }
            else if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.POST)
            {
                response.SessionData = new SessionData(
                    new List<string>(),
                    new List<string>(),
                    new List<string> { "ssid" },
                    new List<string> { ssid },
                    new List<KeyValuePair<string, string>>(),
                    URL, URL, input, null, response.proxy.GetNetworkStream());
            }
            else
            {
                throw new InvalidOperationException("The SessionID transmission type '" + SessionContainer.SessionIdTransmissionType + "' is not supported in NoificationResponse.ParseNotificationResponse()");
            }

            switch(response.NotificationType)
            {
                case NotificationType.Message:
                    response.IsMessage = true;
                    packet.Values.TryGetValue("msg", out response.Message);
                    break;

                default:
                    response.IsMessage = false;
                    break;
            }
        }

        public string GetValue(string key)
        {
            return Values.ContainsKey(key) ? Values[key] : null;
        }
    }
}
