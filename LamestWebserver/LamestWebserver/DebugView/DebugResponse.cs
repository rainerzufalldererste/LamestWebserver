using LamestWebserver.Collections;
using LamestWebserver.RequestHandlers;
using LamestWebserver.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.DebugView
{
    public class DebugResponse : IRequestHandler
    {
        private static DebugContainerResponseNode _debugNode = new NamedDebugContainerResponseNode("LamestWebserver Debug View");

        public static string StyleSheet = "";

        public HttpResponse GetResponse(HttpRequest requestPacket)
        {
            // SessionData?
            SessionData sessionData = null;
            PositionQueue<Tuple<string, string>> unpackedUrls = UnpackUrlActions(requestPacket.RequestUrl);
            HElement e = _debugNode.GetContents(sessionData, unpackedUrls.Peek().Item2, unpackedUrls);

            // Add start & end & stylesheet & co.
            // Pack to HttpResponse.
            // Ship it.
            throw new NotImplementedException();
        }

        public PositionQueue<Tuple<string, string>> UnpackUrlActions(string url)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class DebugResponseNode
    {
        public abstract HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<string, string>> positionQueue);

        public static HLink GetLink(string subUrl, string requestedAction, PositionQueue<Tuple<string, string>> positionQueue)
        {
            List<Tuple<string, string>> already = positionQueue.GetPassed();

            throw new NotImplementedException();
        }
    }

    public class DebugContainerResponseNode : DebugResponseNode
    {
        internal List<DebugResponseNode> SubNodes;
        
        public DebugContainerResponseNode(params DebugResponseNode[] subNodes)
        {
            SubNodes = new List<DebugResponseNode>(subNodes);
        }

        public override HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<string, string>> positionQueue)
        {
            throw new NotImplementedException();
        }
    }

    public class NamedDebugContainerResponseNode : DebugContainerResponseNode
    {
        private string _name;
        private string _description;

        public List<HElement> elements = new List<HElement>();

        public NamedDebugContainerResponseNode(string name, string description = null)
        {
            _name = name;
            _description = description;
        }

        public override HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<string, string>> positionQueue)
        {
            HInlineContainer name = new HInlineContainer() { Text = _name };

            if(positionQueue.AtEnd())
            {
                return name + new HHeadline(_name) + (_description == null ? new HText(_description) : new HText()) + new HContainer(elements) + new HLine() + base.GetContents(sessionData, requestedAction, positionQueue);
            }
            else
            {
                return name + base.GetContents(sessionData, requestedAction, positionQueue);
            }
        }
    }
}
