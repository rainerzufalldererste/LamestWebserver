using LamestWebserver.JScriptBuilder;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using LamestWebserver.Synchronization;

namespace LamestWebserver.NotificationService
{
    /// <summary>
    /// An interface for notification based communication between client and server.
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>
        /// The URL of the ResponseService
        /// </summary>
        string URL { get; }

        /// <summary>
        /// Stops and Unregisters the Handler
        /// </summary>
        void StopHandler();

        /// <summary>
        /// Notifies all connected clients.
        /// </summary>
        /// <param name="notification">the notification to send</param>
        void Notify(Notification notification);
    }

    public interface INotificationResponse
    {
        string URL { get; }

        Notification GetResponse(SessionData sessionData, string Request);
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

        public static Notification ExecuteScript(IJSPiece piece)
        {
            return new ExecuteScriptNotification(piece.getCode(SessionData.CurrentSession));
        }

        public static Notification ReplaceDivWithContent(string divId, IJSValue content)
        {
            return
                new ExecuteScriptNotification(
                    JSElement.getByID(divId)
                        .InnerHTML.Set(content)
                        .getCode(SessionData.CurrentSession, CallingContext.Inner));
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
                        .getCode(SessionData.CurrentSession, CallingContext.Inner));
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
                ).getCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        public static Notification AddToDivContent(string divId, string content)
        {
            return AddToDivContent(divId, new JSValue(content.Base64Encode()));
        }

        public static Notification ReloadPage()
        {
            return new ExecuteScriptNotification(JSValue.CurrentBrowserURL.Set(JSValue.CurrentBrowserURL)
                .getCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        public static Notification Redirect(IJSValue newPageUrl)
        {
            return new ExecuteScriptNotification(JSValue.CurrentBrowserURL.Set(newPageUrl)
                .getCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        public static Notification Redirect(string newPageUrl)
        {
            return Redirect((JSStringValue) newPageUrl);
        }

        internal static Notification Invalid()
        {
            return new InvalidNotification();
        }

        internal static Notification Invalid(string text)
        {
            return new InvalidNotificationInfo(text);
        }

        public static string Log(Notification notification)
        {
            string ret = notification.NotificationType.ToString();

            if (notification is ExecuteScriptNotification)
                ret += "\n\t" + ((ExecuteScriptNotification) notification).script;

            return ret;
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

    public class InvalidNotificationInfo : Notification
    {
        private readonly string _text;
        public InvalidNotificationInfo(string text) : base(NotificationType.Invalid)
        {
            _text = text;
        }

        public override string GetNotification()
        {
            return base.ToString(new KeyValuePair<string, string>("info", _text));
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

    /// <summary>
    /// Provides a Notification Based System for Communicating via Websockets
    /// </summary>
    public class NotificationHandler : WebSocketCommunicationHandler, INotificationHandler
    {
        private static readonly List<NotificationHandler> AllNotificationHandlers = new List<NotificationHandler>();
        private static readonly Mutex AllNotificationHandlerMutex = new Mutex();

        /// <summary>
        /// The default time before a keepalive message is being sent.
        /// </summary>
        public static TimeSpan DefaultMaximumLastMessageTime = TimeSpan.FromSeconds(2d);

        /// <summary>
        /// The maximum time before a keepalive package is being sent.
        /// </summary>
        public readonly TimeSpan MaximumLastMessageTime = DefaultMaximumLastMessageTime;

        /// <summary>
        /// Stops all currently running NotificationHandlers
        /// </summary>
        public static void StopAllNotificationHandlers()
        {
            for (int i = AllNotificationHandlers.Count - 1; i >= 0; i--)
            {
                AllNotificationHandlers[i].StopHandler();
            }
        }

        private readonly UsableWriteLock _listWriteLock = new UsableWriteLock();

        private readonly List<WebSocketHandlerProxy> _proxies = new List<WebSocketHandlerProxy>();

        /// <summary>
        /// The function used for sending messages to the server from the client
        /// </summary>
        public JSFunction SendingFunction { get; private set; }

        /// <summary>
        /// The id of this NotificationHandler (for easier identification in the client)
        /// </summary>
        public string ID { get; private set; } = SessionContainer.generateHash();
        
        /// <summary>
        /// The amount of currently connected clients.
        /// </summary>
        public uint ConnectedClients { get; private set; } = 0;

        private readonly IPAddress _externalAddress = null;

        /// <summary>
        /// The thread that handles the keepalive sending
        /// </summary>
        protected Thread HandlerThread = null;

        private bool Running
        {
            get
            {
                using (_listWriteLock.LockRead())
                    return _running;
            }
            set
            {
                using (_listWriteLock.LockWrite())
                    _running = value;
            }
        }

        private bool _running = true;

        /// <summary>
        /// Specifies whether the OnNotification Event shall also be called for keepalive messages - or only on messages carrying information.
        /// </summary>
        public readonly bool NotifyForKeepalives;

        internal readonly bool TraceMessagesClient;

        /// <summary>
        /// This event is called whenever a client sends a notification
        /// </summary>
        public event Action<NotificationResponse> OnNotification;

        /// <summary>
        /// Constructs a new NotificationHandler listening for websocket requests at a specified URL
        /// </summary>
        /// <param name="URL">the URL at which the Websocket Response will be available at</param>
        /// <param name="notifyForKeepalives">shall the OnNotification event be fired if the Notification is just a KeepAliveMessage</param>
        /// <param name="externalAddress">at which IP-Address is the server registered at externally</param>
        /// <param name="traceMessagesClient">shall the communication be logged in the client browser console? (for debugging)</param>
        /// <param name="maximumLastMessageTime">the maximum time at which the server decides not to sent a keepalive package after not hearing from the client. (null means DefaultMaximumLastMessageTime)</param>
        public NotificationHandler(string URL, bool notifyForKeepalives = false, IPAddress externalAddress = null, bool traceMessagesClient = false, TimeSpan? maximumLastMessageTime = null) : base(URL)
        {
            if (maximumLastMessageTime.HasValue)
                MaximumLastMessageTime = maximumLastMessageTime.Value;

            NotifyForKeepalives = notifyForKeepalives;
            TraceMessagesClient = traceMessagesClient;

            this._externalAddress = externalAddress;

            OnMessage += (input, proxy) => HandleResponse(new NotificationResponse(input, proxy, URL, this));
            OnConnect += proxy => Connect(proxy);
            OnDisconnect += proxy => Disconnect(proxy);

            string methodName = NotificationHelper.GetFunctionName(ID);

            SendingFunction = new JSFunction(methodName);

            AllNotificationHandlerMutex.WaitOne();
            AllNotificationHandlers.Add(this);
            AllNotificationHandlerMutex.ReleaseMutex();
        }

        /// <summary>
        /// The javascript code that handles the Notification based Communication to the server
        /// </summary>
        public JSElement ConnectionElement => new JSPlainText("<script type='text/javascript'>" +
                                NotificationHelper.JsonNotificationCode(AbstractSessionIdentificator.CurrentSession, URL, ID, _externalAddress, TraceMessagesClient) + "</script>");

        private JSElement _jselement = null;

        /// <summary>
        /// The method which handles the sending of keepalive packages to the clients whenever the maximum time is reached.
        /// </summary>
        public void ServerClients()
        {
            while (Running)
            {
                using (_listWriteLock.LockWrite())
                {
                    for (int i = _proxies.Count - 1; i >= 0; i--)
                    {
                        if (!_proxies[i].IsActive)
                        {
                            _proxies.RemoveAt(i);
                            continue;
                        }

                        if ((DateTime.UtcNow - _proxies[i].LastMessageSent) >
                            this.MaximumLastMessageTime)
                            _proxies[i].Respond(new KeepAliveNotification().GetNotification());
                    }
                }

                Thread.Sleep(2);
            }
        }

        /// <summary>
        /// Stops the NotificationHandler &amp; the handler thread; unregisters the page.
        /// </summary>
        public void StopHandler()
        {
            Running = false;

            AllNotificationHandlerMutex.WaitOne();
            AllNotificationHandlers.Remove(this);
            AllNotificationHandlerMutex.ReleaseMutex();

            Unregister();
        }

        /// <summary>
        /// Notify all connected clients.
        /// </summary>
        /// <param name="notification">the notification to send</param>
        public void Notify(Notification notification)
        {
            Thread t = new Thread(() =>
            {
                string notificationText = notification.GetNotification();

                using (_listWriteLock.LockRead())
                    _proxies.ForEach(proxy => proxy.Respond(notificationText));
            });

            t.Start();
        }

        /// <summary>
        /// Handles Messages sent from the client
        /// </summary>
        /// <param name="response">the message from the client</param>
        public virtual void HandleResponse(NotificationResponse response)
        {
            try
            {
                if(TraceMessagesClient && response.NotificationType != NotificationType.KeepAlive && response.NotificationType != NotificationType.Acknowledge)
                    ServerHandler.LogMessage("WebSocket: Server << Client: " + NotificationResponse.Log(response));

                if (response.IsMessage || (response.NotificationType == NotificationType.KeepAlive && NotifyForKeepalives))
                    OnNotification(response);
            }
            catch (Exception e)
            {
                if (TraceMessagesClient)
                {
                    try
                    {
                        response.Reply(Notification.Invalid());
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }

                ServerHandler.LogMessage("Invalid WebSocket Response from Client\n" + e);
            }
        }

        private void Connect(WebSocketHandlerProxy proxy)
        {
            using (_listWriteLock.LockWrite())
            {
                ConnectedClients++;
                _proxies.Add(proxy);
            }

            if (HandlerThread == null)
            {
                HandlerThread = new Thread(new ThreadStart(ServerClients));
                HandlerThread.Start();
            }
        }

        private void Disconnect(WebSocketHandlerProxy proxy)
        {
            using (_listWriteLock.LockWrite())
            {
                ConnectedClients--;
                _proxies.Remove(proxy);
            }
        }

        /// <summary>
        /// Retrives JavaScript code to send a Message from the client to the server.
        /// </summary>
        /// <param name="messageGetter">The Method to get the Notification Contents from</param>
        /// <returns>A piece of JavaScript code</returns>
        public IJSPiece SendMessage(IJSPiece messageGetter)
        {
            return
                SendingFunction.callFunction(
                    new JSValue(messageGetter.getCode(AbstractSessionIdentificator.CurrentSession, CallingContext.Inner)));
        }

        /// <summary>
        /// Retrives JavaScript code to send a Message from the client to the server.
        /// </summary>
        /// <param name="message">the message to send as string</param>
        /// <returns>A piece of JavaScript code</returns>
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
        private NotificationHandler notificationHandler;
        private bool noreply = false;

        public AbstractSessionIdentificator SessionData { get; private set; }

        internal NotificationResponse(string input, WebSocketHandlerProxy proxy, string URL, NotificationHandler notificationHanlder)
        {
            this.proxy = proxy;
            this.notificationHandler = notificationHanlder;

            ParseNotificationResponse(input, this, URL);
        }

        public void Reply(Notification notification)
        {
            if(notificationHandler.TraceMessagesClient && notification.NotificationType != NotificationType.KeepAlive)
                ServerHandler.LogMessage("WebSocket: Server >> Client: " + Notification.Log(notification));

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

            response.SessionData = new SessionIdentificatorSlim(URL, response.proxy.Port, ssid);

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

        public static string Log(NotificationResponse response)
        {
            string ret = response.Message;

            if (response.Values.Count > 2)
            {
                foreach (var value in response.Values)
                {
                    if (value.Key != "ssid" && value.Key != "type")
                        ret += $"\n\t'{value.Key}' : '{value.Value}'";
                }
            }

            return ret;
        }
    }
}
