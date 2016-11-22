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
        public jsconn() : base("jsconn")
        {
        }

        protected override HElement getElement(SessionData sessionData)
        {
            NotificationHandler handler = new NotificationHandler("jsconn");
            handler.onResponse += 
                (NotificationResponse response) => 
                {
                    response.Reply(Notification.ExecuteScript("alert(\"" + response.message + "\")"));
                };

            PageBuilder _pb = new PageBuilder("jsconn");

            _pb.addElement(handler.JSElement);

            JSInput input = new JSInput(HInput.EInputType.text, "text");
            input.onblur = handler.SendMessage(JSElement.getByID(input.ID).Value);

            _pb.addElement(input);

            return _pb;
        }
    }
}
