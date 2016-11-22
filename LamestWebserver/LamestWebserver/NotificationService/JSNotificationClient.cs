using LamestWebserver.JScriptBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.NotificationService
{
    internal static class JSNotificationClient
    {
        internal static string NotificationCode(SessionData sessionData, string destinationURL, out string sendMsgMethodName, string NotificationHandlerID)
        {
            destinationURL = destinationURL.TrimStart('/', ' ');

            sendMsgMethodName = "func_send_" + NotificationHandlerID;

            return "var conn = new WebSocket('ws://" + sessionData._localEndPoint.ToString() + "/" + destinationURL + "');" +
                    "function " + sendMsgMethodName + " (type, msg){conn.send(type + \"\\n\\n\" + msg)};" +
                    "function " + sendMsgMethodName + " (msg){conn.send(\"" + NotificationType.Message + "\\\n\\n\" + msg)};" +
                    "conn.onmessage = function(event) { var answer = true; if(event.data.includes(\"\\n\\r\") && event.data.split(\"\\n\\r\", 2)[1] == \"" + NotificationOption.NoReply + "\") answer = false; " +
#if DEBUG
                    "console.log(event.data);" +
#endif

                    "var cmd = event.data.split(\"\\n\", 1)[0]; switch(cmd) { case \"" + NotificationType.KeepAlive + "\": if(answer) conn.send(\"" + NotificationType.KeepAlive + "\"); break;" +
                    "case \"" + NotificationType.ExecuteScript + "\": {var dat = event.data.split(\"\\n\\n\", 2)[1]; if(dat) eval(window.atob(dat));} if(answer) conn.send(\"" + NotificationType.Acknowledge + "\\r\\n\"); break;" +
                    "case \"" + NotificationType.ReplaceDivContent + "\": {var dat = event.data.split(\"\\n\\n\", 2)[1]; var dat0 = event.data.split(\"\\n\\n\", 2)[2]; if(dat && dat0) { document.getElementByID(dat).innerHTML = dat0; } else { conn.send(\"" + NotificationType.Invalid + "\") }} if(answer) conn.send(\"" + NotificationType.Acknowledge + "\\r\\n\"); break;" +
                    " } };" +
                    "conn.onopen = function (event) { conn.send(\"" + NotificationType.KeepAlive + "\") };";
        }
    }
}
