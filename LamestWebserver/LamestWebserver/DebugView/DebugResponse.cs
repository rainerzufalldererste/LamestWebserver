using LamestWebserver.Collections;
using LamestWebserver.Core;
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
        private static DebugContainerResponseNode _debugNode = new DebugContainerResponseNode("LamestWebserver Debug View");

        public static string StyleSheet = "";

        public HttpResponse GetResponse(HttpRequest requestPacket)
        {
            SessionData sessionData = new HttpSessionData(requestPacket);
            PositionQueue<Tuple<ID, string>> unpackedUrls = UnpackUrlActions(requestPacket);
            HElement e = _debugNode.GetContents(sessionData, unpackedUrls.Peek().Item2, unpackedUrls);

            PageBuilder pb = new PageBuilder("LamestWebserver Debug View")
            {
                StylesheetCode = StyleSheet,
                Elements =
                {
                    new HContainer
                    {
                        Elements = { e },
                        Class = "main"
                    },
                    new HContainer
                    {
                        Elements =
                        {
                            new HText($"LamestWebserver DebugView v{typeof(DebugResponse).Assembly.GetName().Version}")
                            {
                                Class = "code"
                            }
                        },
                        Class = "footer"
                    }
                }
            };

            return new HttpResponse(requestPacket) { };
        }

        public PositionQueue<Tuple<ID, string>> UnpackUrlActions(HttpRequest request)
        {
            string[] links = request.RequestUrl.Split('/');

            PositionQueue<Tuple<ID, string>> ret = new PositionQueue<Tuple<ID, string>>();

            for (int i = 0; i < links.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(links[i]))
                    break;

                string action = request.VariablesHttpHead[$"action_{i}"];

                try
                {
                    ret.Push(new Tuple<ID, string>(new ID(links[i]), Master.DecodeUrl(action)));
                }
                catch
                {
                    break;
                }
            }

            return ret;
        }
    }

    public abstract class DebugResponseNode
    {
        public string Name;

        public abstract HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<ID, string>> positionQueue);

        public static HLink GetLink(string text, ID subUrl, PositionQueue<Tuple<ID, string>> positionQueue, string requestedAction = null)
        {
            List<Tuple<ID, string>> already = positionQueue.GetPassed();
            already.Add(positionQueue.Peek());

            string ret = "./";

            foreach (Tuple<ID, string> tuple in already)
                ret += tuple.Item1 + "/";

            ret += "?";

            for (int i = 0; i < already.Count - 1; i++)
                if (!string.IsNullOrEmpty(already[i].Item2))
                    ret += $"action_{i}={already[i].Item2}&";

            if(!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{already.Count - 1}={Master.FormatToHttpUrl(requestedAction)}";

            return new HLink(text, ret);
        }

        public static HLink GetLink(string text, ID subUrl, PositionQueue<Tuple<ID, string>> positionQueue, int position, string requestedAction)
        {
            List<Tuple<ID, string>> nodes = positionQueue.InternalList.GetRange(0, position);

            string ret = "./";

            foreach (Tuple<ID, string> tuple in nodes)
                ret += tuple.Item1 + "/";

            ret += "?";

            for (int i = 0; i < nodes.Count - 1; i++)
                if (!string.IsNullOrEmpty(nodes[i].Item2))
                    ret += $"action_{i}={nodes[i].Item2}&";

            if (!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{nodes.Count - 1}={Master.FormatToHttpUrl(requestedAction)}";

            return new HLink(text, ret);
        }
    }

    public class DebugContainerResponseNode : DebugResponseNode
    {
        private string _description;
        private AVLTree<ID, DebugResponseNode> SubNodes;

        public Func<SessionData, HElement> GetElements;

        public DebugContainerResponseNode(string name, string description = null, Func<SessionData, HElement> getElementFunc = null)
        {
            Name = name;
            _description = description;
            SubNodes = new AVLTree<ID, DebugResponseNode>();
            GetElements = getElementFunc ?? (s => new HText($"No {nameof(GetElements)} function was specified."));
        }

        public void AddNode(DebugResponseNode node)
        {
            SubNodes.Add(new ID(), node);
        }

        public override HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<ID, string>> positionQueue)
        {
            HInlineContainer name = new HInlineContainer() { Text = Name };

            if(positionQueue.AtEnd())
            {
                HList list = new HList(HList.EListType.UnorderedList, (from s in SubNodes select GetLink(s.Value.Name, s.Key, positionQueue)).ToArray());

                return name + new HHeadline(Name) + (_description == null ? new HText(_description) : new HText()) + new HContainer(GetElements(sessionData)) + new HLine() + list;
            }
            else
            {
                DebugResponseNode node = null;

                positionQueue.Pop();

                if(SubNodes)
                {
                    node = SubNodes[positionQueue.Peek().Item1];
                }

                if(ReferenceEquals(node, null))
                {
                    HList list = new HList(HList.EListType.UnorderedList, (from s in SubNodes select GetLink(s.Value.Name, s.Key, positionQueue, positionQueue.Position, null)).ToArray());

                    return name + new HNewLine() + new HText($"The ID '{positionQueue.Peek().Item1.Value} is not a child of this {nameof(DebugContainerResponseNode)}.'") { Class = "invalid" } + new HLine() + list;
                }
                else
                {
                    return name + node.GetContents(sessionData, positionQueue.Peek().Item2, positionQueue);
                }
            }
        }
    }

    public class StaticDebugContainerResponseNode : DebugContainerResponseNode
    {
        public List<HElement> Elements;

        public StaticDebugContainerResponseNode(string name, string description = null, params HElement[] elements) : base(name, description)
        {
            Elements = new List<HElement>(elements);
            GetElements = s => new HMultipleElements(Elements);
        }
    }
}
