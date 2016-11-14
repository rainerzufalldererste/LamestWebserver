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

        ICollection<WebSocket> Sockets { get; }

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
        public readonly NotificationType NotificationType;
        protected Notification(NotificationType type)
        {
            NotificationType = type;
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
        KeepAlive, ExecuteScript, Redirect, ReloadPage, ReplacePageContent, ReplacePageBody, ReplaceDivContent, ExpandPageBodyWith, ExpandDivContentWith
    }
}
