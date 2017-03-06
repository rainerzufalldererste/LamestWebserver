using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace LamestWebserver.NotificationService
{
    internal static class NotificationHelper
    {
        internal static string JsonNotificationCode(AbstractSessionIdentificator sessionData, string destinationURL, string NotificationHandlerID, IPEndPoint endpoint,
            bool trace = false, int timeKeepaliveClientside = 8000)
        {
            destinationURL = destinationURL.TrimStart('/', ' ');

            string sendMsgMethodName = GetFunctionName(NotificationHandlerID);

            string addr = endpoint.Address + ":" + endpoint.Port;

            StringBuilder builder = new StringBuilder();

            builder.Append("var conn; var excepted = 0; var interval_");
            builder.Append(sendMsgMethodName);
            builder.Append(";  function initconn_");
            builder.Append(sendMsgMethodName);
            builder.Append("(){ ");
#if DEBUG
            builder.Append("if(excepted > 50 || conn && conn.readyState <= 1) return; console.log(\"+> Trying to open new Connection... (from Client)\");");
#else
            if (trace)
                builder.Append("console.log(\"+> Trying to open new Connection... (from Client)\");");
#endif

            builder.Append(" try{ conn = new WebSocket('ws://");
            builder.Append(addr);
            builder.Append("/");
            builder.Append(destinationURL);
            builder.Append("'); conn.onmessage = function(event) { var rcv = window.JSON.parse(event.data); var answer = true; if(rcv.");
            builder.Append(JsonNotificationPacket.NoReply_string);
            builder.Append(") answer = false; ");
#if DEBUG
            builder.Append("console.log({mode: \"<< (from Server)\", type: rcv.");
            builder.Append(JsonNotificationPacket.NotificationType_string);
            builder.Append(", rcv});");
#else
            if (trace)
            {
                builder.Append("if(rcv.");
                builder.Append(JsonNotificationPacket.NotificationType_string);
                builder.Append(" != \"");
                builder.Append(NotificationType.KeepAlive);
                builder.Append("\") {console.log({mode: \"<< (from Server)\", message: rcv});}");
            }

#endif
            builder.Append("var cmd = rcv.");
            builder.Append(JsonNotificationPacket.NotificationType_string);
            builder.Append("; switch(cmd) { case \"");
            builder.Append(NotificationType.KeepAlive);
            builder.Append("\": if(answer) ");
            builder.Append(sendMsgMethodName);
            builder.Append("_(\"");
            builder.Append(NotificationType.KeepAlive);
            builder.Append("\", \"\"); break;");
            builder.Append("case \"");
            builder.Append(NotificationType.ExecuteScript);
            builder.Append("\": if(rcv.");
            builder.Append(JsonNotificationPacket.Script_string);
            builder.Append(") eval(rcv.");
            builder.Append(JsonNotificationPacket.Script_string);
            builder.Append("); if(answer) ");
            builder.Append(sendMsgMethodName);
            builder.Append("_(\"");
            builder.Append(NotificationType.Acknowledge);
            builder.Append("\", \"\"); break;");
            builder.Append(" } clearTimeout(interval_");
            builder.Append(sendMsgMethodName);
            builder.Append("); interval_");
            builder.Append(sendMsgMethodName);
            builder.Append(" = setInterval(try_keepalive");
            builder.Append(sendMsgMethodName);
            builder.Append(", ");
            builder.Append(timeKeepaliveClientside);
            builder.Append(") };");
            builder.Append("conn.onopen = function (event) { excepted = 0; ");
            builder.Append(sendMsgMethodName);
            builder.Append("_(\"");
            builder.Append(NotificationType.KeepAlive);
            builder.Append("\"); ");
#if DEBUG
            builder.Append("console.log(\"<+ Connection established (from Server)\");");
#else
            if (trace)
                builder.Append("console.log(\"<+ Connection established (from Server)\");");
#endif
            builder.Append("};}catch(e){excepted++; return;} ");
            builder.Append("conn.onclose = initconn_");
            builder.Append(sendMsgMethodName);
            builder.Append(";conn.onerror = function() { excepted++; initconn_");
            builder.Append(sendMsgMethodName);
            builder.Append("; }} function send_");
            builder.Append(sendMsgMethodName);
            builder.Append("(msg){var i = 0; var interv; function innerf() { if(i > 3) clearInterval(interv); try{conn.send(msg); clearInterval(interv);}catch(e){");
#if DEBUG
            builder.Append("console.log(\"~~ Waiting for Server...\");");
#endif
            builder.Append("initconn_");
            builder.Append(sendMsgMethodName);
            builder.Append("();} i++;}; interv = setInterval(innerf, 500);} function try_keepalive");
            builder.Append(sendMsgMethodName);
            builder.Append(" () {if(excepted > 50 || !conn || conn.readyState > 1){clearInterval(interval_");
            builder.Append(sendMsgMethodName);
            builder.Append(")} send_");
            builder.Append(sendMsgMethodName);
            builder.Append("('{type: \"");
            builder.Append(NotificationType.KeepAlive);
            builder.Append("\"}');");
#if DEBUG
            builder.Append("console.log(\"+> Sending KeepAlive due to quiet Server... (from Client)\");");
#else
            if (trace)
                builder.Append("console.log(\"+> Sending KeepAlive due to quiet Server... (from Client)\");");
#endif
            builder.Append("};");
            builder.Append("function ");
            builder.Append(sendMsgMethodName);
            builder.Append("_ (type, msg){");
#if DEBUG
            builder.Append("console.log({mode: \">> (from Client)\", type: type, message: msg});");
#else
            if (trace)
            {
                builder.Append("if(msg && msg != \"\" && msg != \"");
                builder.Append(NotificationType.KeepAlive);
                builder.Append("\" && msg != \"");
                builder.Append(NotificationType.Acknowledge);
                builder.Append("\") {console.log({mode: \">> (from Client)\", message: msg});}");
            }

#endif
            builder.Append("send_");
            builder.Append(sendMsgMethodName);
            builder.Append("(window.JSON.stringify({");
            builder.Append(JsonNotificationPacket.NotificationType_string);
            builder.Append(": type,");
            builder.Append(JsonNotificationPacket.SSID_string);
            builder.Append(": \"");
            builder.Append(sessionData.Ssid);
            builder.Append("\", msg: msg}));};");
            builder.Append("function ");
            builder.Append(sendMsgMethodName);
            builder.Append(" (msg){");
            builder.Append(sendMsgMethodName);
            builder.Append("_(\"");
            builder.Append(NotificationType.Message);
            builder.Append("\", msg);};");
            builder.Append("function ");
            builder.Append(sendMsgMethodName);
            builder.Append(" (msg, key, value) { var x = {");
            builder.Append(JsonNotificationPacket.NotificationType_string);
            builder.Append(": \"");
            builder.Append(NotificationType.Message);
            builder.Append("\",");
            builder.Append(JsonNotificationPacket.SSID_string);
            builder.Append(": \"");
            builder.Append(sessionData.Ssid);
            builder.Append("\", msg: msg}; ");
            builder.Append("x[key] = value;");
#if DEBUG
            builder.Append("console.log({mode: \">> (from Client)\", type: \"");
            builder.Append(NotificationType.Message);
            builder.Append("\", message: x});");
#else
            if (trace)
            {
                builder.Append("if(msg && msg != \"");
                builder.Append(NotificationType.KeepAlive);
                builder.Append("\" || msg != \"");
                builder.Append(NotificationType.Acknowledge);
                builder.Append("\") {console.log({mode: \">> (from Client)\", message: x});}");
            }

#endif
            builder.Append("send_");
            builder.Append(sendMsgMethodName);
            builder.Append("(window.JSON.stringify(x));};");
            builder.Append("initconn_");
            builder.Append(sendMsgMethodName);
            builder.Append("();");

            return builder.ToString();
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
