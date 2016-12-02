using LamestWebserver;
using LamestWebserver.JScriptBuilder;
using LamestWebserver.NotificationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demos
{
    public sealed class jsconn : ElementResponse
    {
        NotificationHandler handler;

        public jsconn() : base("jsconn")
        {
            handler = new NotificationHandler("jsconn");
            handler.OnNotification +=
                    (NotificationResponse response) =>
                    {
                        Console.WriteLine("({0}) " + response.Message, handler.ConnectedClients);
                        response.Reply(Notification.ExecuteScript("alert(\"" + response.Message.Replace("\n", "\\n") + "\")"));
                    };
        }

        protected override HElement getElement(SessionData sessionData)
        {
            PageBuilder _pb = new PageBuilder("jsconn");

            _pb.addElement(handler.JSElement);

            JSInput input = new JSInput(HInput.EInputType.text, "text");
            input.onblur = handler.SendMessage(JSElement.getByID(input.ID).Value);

            _pb.addElement(input);

            return _pb;
        }
    }
}
