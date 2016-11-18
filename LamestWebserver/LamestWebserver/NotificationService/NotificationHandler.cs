using LamestWebserver.JScriptBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.NotificationService
{
    public interface INotificationHandler
    {
        string URL { get; }

        void Unsubscribe();

        void Notify(Notification notification);
    }

    public interface INotificationResponse
    {
        string URL { get; }

        Notification getResponse(SessionData sessionData, string Request);
    }

    public abstract class Notification
    {
        protected static ASCIIEncoding encoding = new ASCIIEncoding();

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
                ret += "\n\n" + Convert.ToBase64String(encoding.GetBytes(System.Web.HttpUtility.HtmlEncode(msg)));

            return ret;
        }
    }

    public class KeepAliveNotification : Notification
    {
        public KeepAliveNotification() : base(NotificationType.KeepAlive)
        {
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

    public class NotificationHandler : INotificationHandler
    {
        public JSMethodCall SendingFunction;
        public string ID = SessionContainer.generateHash();

        public JSElement getJSElement()
        {
            string methodName;
            var elemeent = new JSPlainText("<script type='text/javascript'>" + JSNotificationClient.NotificationCode(SessionData.currentSessionData, URL, out methodName, ID)  + "</script>");

            SendingFunction = new JSMethodCall(methodName);

            return elemeent;
        }

        public string URL { get; set; }

        public void Notify(Notification notification)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe()
        {
            throw new NotImplementedException();
        }
    }
}
