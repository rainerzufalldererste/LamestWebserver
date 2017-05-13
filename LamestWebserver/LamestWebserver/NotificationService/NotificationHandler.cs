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

    /// <summary>
    /// A Notification for Communicating message based via WebSockets
    /// </summary>
    public abstract class Notification
    {
        /// <summary>
        /// The type of the current notification
        /// </summary>
        public readonly NotificationType NotificationType;

        /// <summary>
        /// shall the client / server not reply to this notification?
        /// </summary>
        protected bool NoReply = false;

        /// <summary>
        /// Constructs a new Notification of the given type
        /// </summary>
        /// <param name="type"></param>
        protected Notification(NotificationType type)
        {
            NotificationType = type;
        }

        /// <summary>
        /// Returns the current notification as string (json)
        /// </summary>
        /// <returns>the current notification as string (json)</returns>
        public override string ToString()
        {
            return ToString(new KeyValuePair<string, string>[0]);
        }

        /// <summary>
        /// Returns the current notification as string (json)
        /// </summary>
        /// <param name="args">the arguments listed in the message</param>
        /// <returns>the current notification as string (json)</returns>
        public string ToString(params KeyValuePair<string, string>[] args)
        {
            JsonNotificationPacket packet = new JsonNotificationPacket(NotificationType, args);

            if (NoReply)
                packet.Values.Add(JsonNotificationPacket.NoReply_string, JsonNotificationPacket.NoReply_string);

            return packet.Serialize();
        }

        /// <summary>
        /// Retrieves the current notification
        /// </summary>
        /// <returns>Returns the notification as string</returns>
        public abstract string GetNotification();

        /// <summary>
        /// Creates a notification to execute a javascript piece of code in the client
        /// </summary>
        /// <param name="script">the script to execute</param>
        /// <returns>the specified notification</returns>
        public static Notification ExecuteScript(string script)
        {
            return new ExecuteScriptNotification(script);
        }

        /// <summary>
        /// Creates a notification to execute a javascript piece of code in the client
        /// </summary>
        /// <param name="piece">the script to execute</param>
        /// <returns>the specified notification</returns>
        public static Notification ExecuteScript(IJSPiece piece)
        {
            return new ExecuteScriptNotification(piece.GetJsCode(SessionData.CurrentSession));
        }

        /// <summary>
        /// Creates a notification to replace a given div element identified by an id with the specific new content
        /// </summary>
        /// <param name="divId">the id of the div element</param>
        /// <param name="content">the content to replace it's contents with</param>
        /// <returns>the specified notification</returns>
        public static Notification ReplaceDivWithContent(string divId, IJSValue content)
        {
            return
                new ExecuteScriptNotification(
                    JSElement.GetByID(divId)
                        .InnerHTML.Set(content)
                        .GetJsCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        /// <summary>
        /// Creates a notification to replace a given div element identified by an id with the specific new content
        /// </summary>
        /// <param name="divId">the id of the div element</param>
        /// <param name="content">the content to replace it's contents with</param>
        /// <returns>the specified notification</returns>
        public static Notification ReplaceDivWithContent(string divId, string content)
        {
            return ReplaceDivWithContent(divId, new JSValue(content.Base64Encode()));
        }

        /// <summary>
        /// Creates a notification to replace the documents body with specific new content
        /// </summary>
        /// <param name="content">the content to replace the body with</param>
        /// <returns>the specified notification</returns>
        public static Notification ReplaceBodyWithContent(IJSValue content)
        {
            return
                new ExecuteScriptNotification(
                    JSElement.Body
                        .InnerHTML.Set(content)
                        .GetJsCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        /// <summary>
        /// Creates a notification to replace the documents body with specific new content
        /// </summary>
        /// <param name="content">the content to replace the body with</param>
        /// <returns>the specified notification</returns>
        public static Notification ReplaceBodyWithContent(string content)
        {
            return ReplaceBodyWithContent(new JSValue(content.Base64Encode()));
        }

        /// <summary>
        /// Creates a notification to add the given content to a specified div
        /// </summary>
        /// <param name="divId">the div to add content to</param>
        /// <param name="content">the content to add</param>
        /// <returns>the specified notification</returns>
        public static Notification AddContentToDiv(string divId, IJSValue content)
        {
            return new ExecuteScriptNotification(
                JSElement.GetByID(divId).InnerHTML.Set(
                    JSElement.GetByID(divId).InnerHTML + content
                ).GetJsCode(SessionData.CurrentSession, CallingContext.Inner));
        }
        
        /// <summary>
        /// Creates a notification to add the given content to a specified div
        /// </summary>
        /// <param name="divId">the div to add content to</param>
        /// <param name="content">the content to add</param>
        /// <returns>the specified notification</returns>
        public static Notification AddContentToDiv(string divId, string content)
        {
            return AddContentToDiv(divId, new JSValue(content.Base64Encode()));
        }
        
        /// <summary>
        /// Creates a notification to reload the current page
        /// </summary>
        /// <returns>the specified notification</returns>
        public static Notification ReloadPage()
        {
            return new ExecuteScriptNotification(JSValue.CurrentBrowserURL.Set(JSValue.CurrentBrowserURL)
                .GetJsCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        /// <summary>
        /// Creates a notification to redirect the client to a new page
        /// </summary>
        /// <param name="newPageUrl">the url of the new page</param>
        /// <returns>the specified notification</returns>
        public static Notification Redirect(IJSValue newPageUrl)
        {
            return new ExecuteScriptNotification(JSValue.CurrentBrowserURL.Set(newPageUrl)
                .GetJsCode(SessionData.CurrentSession, CallingContext.Inner));
        }

        /// <summary>
        /// Creates a notification to redirect the client to a new page
        /// </summary>
        /// <param name="newPageUrl">the url of the new page</param>
        /// <returns>the specified notification</returns>
        public static Notification Redirect(string newPageUrl)
        {
            return Redirect((JSStringValue) newPageUrl);
        }

        /// <summary>
        /// Creates a notification to tell that something went wrong
        /// </summary>
        /// <returns>the specified notification</returns>
        internal static Notification Invalid()
        {
            return new InvalidNotification();
        }

        /// <summary>
        /// Creates a notification to tell that something went wrong
        /// </summary>
        /// <param name="text">the description of what went wrong</param>
        /// <returns>the specified notification</returns>
        internal static Notification Invalid(string text)
        {
            return new InvalidNotificationInfo(text);
        }

        /// <summary>
        /// Parses a Notification to string for logging purposes
        /// </summary>
        /// <param name="notification">the notification to parse</param>
        /// <returns>the notification as string</returns>
        public static string LogNotification(Notification notification)
        {
            string ret = notification.NotificationType.ToString();

            if (notification is ExecuteScriptNotification)
                ret += "\n\t" + ((ExecuteScriptNotification) notification).Script;

            return ret;
        }
    }

    /// <summary>
    /// A Notication to Keep the Connection alive
    /// </summary>
    public class KeepAliveNotification : Notification
    {
        internal KeepAliveNotification() : base(NotificationType.KeepAlive)
        {
        }

        /// <inheritdoc />
        public override string GetNotification()
        {
            return base.ToString();
        }
    }

    /// <summary>
    /// A Notication to signalize invalid behaviour (please resend last msg)
    /// </summary>
    public class InvalidNotification : Notification
    {
        internal InvalidNotification() : base(NotificationType.Invalid)
        {
        }

        /// <inheritdoc />
        public override string GetNotification()
        {
            return base.ToString();
        }
    }

    /// <summary>
    /// A Notication to signalize invalid behaviour with a description text (please resend last msg)
    /// </summary>
    public class InvalidNotificationInfo : Notification
    {
        private readonly string _text;

        internal InvalidNotificationInfo(string text) : base(NotificationType.Invalid)
        {
            _text = text;
        }

        /// <inheritdoc />
        public override string GetNotification()
        {
            return base.ToString(new KeyValuePair<string, string>("info", _text));
        }
    }

    /// <summary>
    /// A Notication to execute a given javascript on the client
    /// </summary>
    public class ExecuteScriptNotification : Notification
    {
        internal string Script { get; private set; }

        internal ExecuteScriptNotification(string script) : base(NotificationType.ExecuteScript)
        {
            this.Script = script;
        }

        /// <inheritdoc />
        public override string GetNotification()
        {
            return base.ToString(new KeyValuePair<string, string>(JsonNotificationPacket.Script_string, Script));
        }
    }

    /// <summary>
    /// The type of the Notification
    /// </summary>
    public enum NotificationType : byte
    {
        /// <summary>
        /// Signalize that the last transfer was successful
        /// </summary>
        Acknowledge,

        /// <summary>
        /// Kepps the connection open
        /// </summary>
        KeepAlive,

        /// <summary>
        /// Transfers a message from the client to the server
        /// </summary>
        Message,

        /// <summary>
        /// Transfers the information that something went wrong in the last message
        /// </summary>
        Invalid,

        /// <summary>
        /// Executes a javascript in the client
        /// </summary>
        ExecuteScript
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
        public string ID { get; private set; } = SessionContainer.GenerateHash();
        
        /// <summary>
        /// The amount of currently connected clients.
        /// </summary>
        public uint ConnectedClients { get; private set; } = 0;

        private readonly IPEndPoint _externalEndpoint = null;

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
        /// <param name="externalEndpoint">at which IP-Address and port is the server at for the client?</param>
        /// <param name="traceMessagesClient">shall the communication be logged in the client browser console? (for debugging)</param>
        /// <param name="maximumLastMessageTime">the maximum time at which the server decides not to sent a keepalive package after not hearing from the client. (null means DefaultMaximumLastMessageTime)</param>
        public NotificationHandler(string URL, bool notifyForKeepalives = false, IPEndPoint externalEndpoint = null, bool traceMessagesClient = false, TimeSpan? maximumLastMessageTime = null) : base(URL)
        {
            if (maximumLastMessageTime.HasValue)
                MaximumLastMessageTime = maximumLastMessageTime.Value;

            NotifyForKeepalives = notifyForKeepalives;
            TraceMessagesClient = traceMessagesClient;

            this._externalEndpoint = externalEndpoint;

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
                                NotificationHelper.JsonNotificationCode(SessionData.CurrentSession, URL, ID, _externalEndpoint, TraceMessagesClient) + "</script>");

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
                    ServerHandler.LogMessage("WebSocket: Server << Client: " + NotificationResponse.LogNotificatoin(response));

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
                    new JSValue(messageGetter.GetJsCode(SessionData.CurrentSession, CallingContext.Inner)));
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

    /// <summary>
    /// The Response to a Notification from a client
    /// </summary>
    public class NotificationResponse
    {
        /// <summary>
        /// Does the Reponse from the client contain a message?
        /// </summary>
        public bool IsMessage = false;

        /// <summary>
        /// The message sent by the client (if any)
        /// </summary>
        public string Message = null;

        /// <summary>
        /// The Hanlder for the current connection
        /// </summary>
        public WebSocketHandlerProxy HandlerProxy;

        internal NotificationType NotificationType;
        private Dictionary<string, string> _values = new Dictionary<string, string>();
        private readonly NotificationHandler _notificationHandler;
        private bool _noreply = false;

        /// <summary>
        /// The current SessionData
        /// </summary>
        public SessionData SessionData { get; private set; }

        internal NotificationResponse(string input, WebSocketHandlerProxy proxy, string URL, NotificationHandler notificationHanlder)
        {
            this.HandlerProxy = proxy;
            this._notificationHandler = notificationHanlder;

            ParseNotificationResponse(input, this, URL);
        }

        /// <summary>
        /// Reply directly to the client who sent this message.
        /// </summary>
        /// <param name="notification">the Notification to send to the client</param>
        public void Reply(Notification notification)
        {
            if(_notificationHandler.TraceMessagesClient && notification.NotificationType != NotificationType.KeepAlive)
                ServerHandler.LogMessage("WebSocket: Server >> Client: " + Notification.LogNotification(notification));

            HandlerProxy.Respond(notification.GetNotification());
        }

        /// <summary>
        /// Parses a Notification response from string
        /// </summary>
        /// <param name="input">the response string</param>
        /// <param name="response">the current response</param>
        /// <param name="URL">the url of the request</param>
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

            response._values = packet.Values;
            response.NotificationType = packet.NotificationType;
            response._noreply = packet.noreply;

            string ssid = packet.SSID;

            response.SessionData = new SessionIdentificatorSlim(URL, ssid);

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

        /// <summary>
        /// Retrieves a value from the values the client sent.
        /// </summary>
        /// <param name="key">the key of the value</param>
        /// <returns>the value</returns>
        public string GetValue(string key)
        {
            return _values.ContainsKey(key) ? _values[key] : null;
        }

        /// <summary>
        /// Parses the given notificationResponse to string to be used for logging purposes
        /// </summary>
        /// <param name="response">the notificationResponse</param>
        /// <returns>the notificationResponse as string</returns>
        public static string LogNotificatoin(NotificationResponse response)
        {
            string ret = response.Message;

            if (response._values.Count > 2)
            {
                foreach (var value in response._values)
                {
                    if (value.Key != "ssid" && value.Key != "type")
                        ret += $"\n\t'{value.Key}' : '{value.Value}'";
                }
            }

            return ret;
        }
    }
}
