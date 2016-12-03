using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LamestWebserver.NotificationService
{
    internal static class NotificationHelper
    {
        [Obsolete]
        internal static string NotificationCode(SessionData sessionData, string destinationURL, string NotificationHandlerID)
        {
            destinationURL = destinationURL.TrimStart('/', ' ');

            string sendMsgMethodName = GetFunctionName(NotificationHandlerID);

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

        internal static string JsonNotificationCode(SessionData sessionData, string destinationURL, string NotificationHandlerID, bool trace = false)
        {
            destinationURL = destinationURL.TrimStart('/', ' ');

            string sendMsgMethodName = GetFunctionName(NotificationHandlerID);

            return "var conn = new WebSocket('ws://" + sessionData._localEndPoint.ToString() + "/" + destinationURL + "');" +
                   "function " + sendMsgMethodName + "_ (type, msg){" +
#if DEBUG
                   "console.log({mode: \">> (from Client)\", type: type, message: msg});" +
#else
                   (trace ? "if(msg != \"" + NotificationType.KeepAlive + "\" || msg != \"" + NotificationType.Acknowledge + "\") {console.log({mode: \">> (from Client)\", message: msg});}" : "") + 
#endif
                   "conn.send(window.JSON.stringify({" + JsonNotificationPacket.NotificationType_string + ": type," + JsonNotificationPacket.SSID_string + ": \"" + sessionData.ssid +
                   "\", msg: msg}));};" +
                   "function " + sendMsgMethodName + " (msg){" + sendMsgMethodName + "_(\"" + NotificationType.Message + "\", msg);};" +
                   "function " + sendMsgMethodName + " (msg, key, value) { var x = {" + JsonNotificationPacket.NotificationType_string + ": \"" + NotificationType.Message + "\"," +
                   JsonNotificationPacket.SSID_string + ": \"" + sessionData.ssid + "\", msg: msg}; " +
                   "x[key] = value;" +
#if DEBUG
                   "console.log({mode: \">> (from Client)\", type: \"" + NotificationType.Message + "\", message: x});" +
#else
                   (trace ? "if(msg != \"" + NotificationType.KeepAlive + "\" || msg != \"" + NotificationType.Acknowledge + "\") {console.log({mode: \">> (from Client)\", message: x});}" : "") + 
#endif
                   "conn.send(window.JSON.stringify(x));};" +
                   "conn.onmessage = function(event) { var rcv = window.JSON.parse(event.data); var answer = true; if(rcv." + JsonNotificationPacket.NoReply_string +
                   ") answer = false; " +
#if DEBUG
                   "console.log({mode: \"<< (from Server)\", type: rcv." + JsonNotificationPacket.NotificationType_string + ", rcv});" +
#else
                   (trace ? "if(rcv." + JsonNotificationPacket.NotificationType_string + " != \"" + NotificationType.KeepAlive + "\") {console.log({mode: \"<< (from Server)\", message: rcv});}" : "") + 
#endif
                    "var cmd = rcv." + JsonNotificationPacket.NotificationType_string + "; switch(cmd) { case \"" + NotificationType.KeepAlive + "\": if(answer) " + sendMsgMethodName + "_(\"" + NotificationType.KeepAlive + "\", \"\"); break;" +
                    "case \"" + NotificationType.ExecuteScript + "\": {if(rcv." + JsonNotificationPacket.Script_string + ") eval(rcv." + JsonNotificationPacket.Script_string + ");} if(answer) " + sendMsgMethodName + "_(\"" + NotificationType.Acknowledge + "\", \"\"); break;" +
                    " } };" +
                    "conn.onopen = function (event) { " + sendMsgMethodName + "_(\"" + NotificationType.KeepAlive + "\") };";
        }

        internal static string GetFunctionName(string NotificationHandlerID) => "func_send_" + NotificationHandlerID;
    }

    internal class JsonNotificationPacket
    {
        internal Dictionary<string, string> Values = new Dictionary<string, string>();
        internal string SSID { get; private set; }
        internal NotificationType NotificationType = NotificationType.Invalid;
        internal bool noreply = false;

        internal const string SSID_string = "SSID";
        internal const string NotificationType_string = "NotificationType";
        internal const string NoReply_string = "noreply";
        internal const string Script_string = "script";

        internal JsonNotificationPacket(NotificationType notificationType, params KeyValuePair<string, string>[] values)
        {
            NotificationType = notificationType;

            foreach (var value in values)
            {
                Values.Add(value.Key, value.Value);
            }
        }

        internal JsonNotificationPacket(string input)
        {
            Values = (Dictionary<string, string>)JsonConvert.DeserializeObject(input, typeof(Dictionary<string, string>));

            if (Values.ContainsKey(SSID_string))
                SSID = Values[SSID_string];

            if (Values.ContainsKey(NoReply_string))
                noreply = true;

            if (Values.ContainsKey(NotificationType_string))
                Enum.TryParse(Values[NotificationType_string], out NotificationType);
        }

        internal string Serialize()
        {
            if (noreply)
                Values[NoReply_string] = NoReply_string;

            Values[NotificationType_string] = NotificationType.ToString();

            return JsonConvert.SerializeObject(Values);
        }
    }
}
