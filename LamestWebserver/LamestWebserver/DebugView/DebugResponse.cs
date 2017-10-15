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
    public interface IDebugRespondable
    {
        DebugResponseNode GetDebugResponseNode();
    }

    public class DebugResponse : IRequestHandler
    {
        public static readonly Singleton<DebugResponse> DebugResponseInstance = new Singleton<DebugResponse>(() => new DebugResponse());

        public static readonly Singleton<ResponseHandler> DebugViewResponseHandler = new Singleton<ResponseHandler>(() => 
        {
            ResponseHandler ret = new ResponseHandler(nameof(DebugViewResponseHandler));

            ret.AddRequestHandler(DebugResponseInstance.Instance);

            return ret;
        });

        private static DebugContainerResponseNode _debugNode = DebugContainerResponseNode.ConstructRootNode("LamestWebserver Debug View", "All collected Debug Information can be retrieved using this page.");

#region StyleSheet
        public static string StyleSheet =
@"body {
    margin: 0;
    background: #111215;
}

p, a, h1, h2, h3, div.container, b, i {
    max-width: 1100px;
    margin: 1em auto 0.5em auto;
}

div.main {
    width: 75%;
    background: #151619;
    margin: 0em auto 0em auto;
    display: block;
    padding: 2em 5em 8em 5em;
    min-height: 60%;
}


div {
    font-size: 13pt;
    font-family: 'Segoe UI', 'Tahoma', 'Arial', sans-serif;
    color: #fff;
    line-height: 17pt;
}

span {
    background-color: #222327;
    padding: 0.1em 0.3em 0.25em 0.3em;
    margin: 0.3em;
    font-size: smaller;
    border-radius: 0.3em;
    color: #575d61;
}

hr {
    width: 80%;
    border-style: solid;
    border-width: 1px;
    margin: 1.5em auto;
    color: #575d61;
}

h1 {
    font-family: 'Segoe UI Light', Arial, sans-serif;
    font-weight: normal;
    font-size: 40pt;
    margin-bottom: 40pt;
    text-shadow: rgba(131, 223, 251, 0.43) 3px 3px 15px;
    line-height: 90%;
}

h2 {
    font-family: 'Segoe UI Semibold', Arial, sans-serif;
    color: #575d61;
    border-bottom: solid #575d61 1px;
    padding-bottom: 0.5em;
}

ul {
    font-size: 12pt;
    list-style-type: square;
}

::selection {
    background: #80cefb;
    color: #c3f3ff;
}

div.footer {
    margin-bottom: 10em;
    background-color: #222327;
    width: 75%;
    margin: 0em auto 10em auto;
    padding: 2.5em 5em 2.5em 5em;
}

.footer p {
    font-family: Consolas, 'Courier New', monospace;
    font-size: 11pt;
    padding: 0em;
    margin-left: 0;
    color: #6c6e75;
}

a {
    color: #aaa;
    text-decoration: none;
    display: block;
}

a:hover {
    color: #92b8ce;
    text-decoration: underline;
}

a.nav {
    color: inherit;
    margin: inherit;
    display: inherit;
}

table {
    width: 80%;
    margin: 1.5em auto;
    color: #aaa;
    font-family: Consolas, 'Courier New', monospace;
}

tr:nth-child(even) {
    background-color: rgba(0, 0, 0, 0.2);
}

table, th, td {
    border-left: 2px #575d61 solid;
    border-collapse: collapse;
}

td {
    padding: 0.1em 0 0.3em 0.5em;
}

th {
    border-left-color: #838996;
    background: #575d61;
    color: #fff;
    font-weight: normal;
    padding: 0.1em 0 0.2em 0.5em;
    text-align: left;
}

ul.subnodes {
    background-color: #111215;
    width: 75%;
    margin: 3em auto 1em auto;
    padding: 0.5em 3em 1em 3em;
    border: 0.1em solid #222327;
    font-family: Consolas, 'Courier New', monospace;
}

p.invalid, p.error {
    margin: 3em auto 3em auto;
    border-radius: 0.5em;
    padding: 1em 2em;
    background: repeating-linear-gradient( 45deg, #ff5d4d, #ff5d4d 0.5em, #e03d2c 0.5em, #e03d2c 1em );
    text-shadow: #000 1px 1px 2px;
}

p.warning {
    margin: 3em auto 3em auto;
    border-radius: 0.5em;
    padding: 1em 2em;
    background: repeating-linear-gradient( 45deg, #e2c42f, #e2c42f 0.5em, #d8b91e 0.5em, #d8b91e 1em );
    text-shadow: #000 1px 1px 2px;
}";
#endregion

        public HttpResponse GetResponse(HttpRequest requestPacket)
        {
            SessionData sessionData = new HttpSessionData(requestPacket);
            PositionQueue<Tuple<ID, string>> unpackedUrls = UnpackUrlActions(requestPacket);
            HElement e = _debugNode.GetContents(sessionData, unpackedUrls.Peek()?.Item2, unpackedUrls);

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

            return new HttpResponse(requestPacket, pb.GetContent(sessionData)) { Cookies = ((HttpSessionData)(sessionData)).Cookies.ToList() };
        }

        public PositionQueue<Tuple<ID, string>> UnpackUrlActions(HttpRequest request)
        {
            List<string> links = request.RequestUrl.Split('/').ToList();
            links.Remove("");

            PositionQueue<Tuple<ID, string>> ret = new PositionQueue<Tuple<ID, string>>();

            ret.Push(new Tuple<ID, string>(new ID(new ulong[] { 0xFFFFFFFFul }), null));

            for (int i = 0; i < links.Count; i++)
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

        public static void AddNode(DebugResponseNode node)
        {
            _debugNode.AddNode(node);
        }

        public static void RemoveNode(DebugResponseNode node)
        {
            _debugNode.RemoveNode(node);
        }

        public static void ClearNodes()
        {
            _debugNode.ClearNodes();
        }

        /// <inheritdoc />
        public bool Equals(IRequestHandler other)
        {
            if (other == null)
                return false;

            if (!other.GetType().Equals(GetType()))
                return false;

            return true;
        }
    }

    public abstract class DebugResponseNode
    {
        public string Name;

        private ID _id;

        public ID ID
        {
            get
            {
                if (_id == null)
                    _id = new ID();

                return _id;
            }

            private set
            {
                _id = value;
            }
        }

        public URL<ID> URL { get; private set; }

        public void SetParent(DebugContainerResponseNode node)
        {
            if (URL != null)
                throw new InvalidOperationException($"The field {nameof(URL)} can only be set once.");

            if (node == null)
                URL = new URL<ID>(new ID[] { ID });
            else
                URL = node.URL.Append(ID);
        }

        protected void SetRootNode()
        {
            URL = new URL<ID>(new ID[0]);
        }

        public abstract HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<ID, string>> positionQueue);

        public static HLink GetLink(string text, ID subUrl, PositionQueue<Tuple<ID, string>> positionQueue, string requestedAction = null)
        {
            List<Tuple<ID, string>> already = positionQueue.GetPassed();

            if (!positionQueue.AtEnd())
                already.Add(positionQueue.Peek());

            string ret = "/";

            foreach (Tuple<ID, string> tuple in already)
                ret += tuple.Item1 + "/";

            ret += "?";

            for (int i = 0; i < already.Count - 1; i++)
                if (!string.IsNullOrEmpty(already[i].Item2))
                    ret += $"action_{i}={already[i].Item2}&";

            if (!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{already.Count - 1}={Master.FormatToHttpUrl(requestedAction)}";

            return new HLink(text, ret);
        }

        public static HLink GetLink(string text, URL<ID> url, string requestedAction = null)
        {
            string ret = "/" + url.ToString() + "?";

            if (!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{url.Count - 1}={Master.FormatToHttpUrl(requestedAction)}";

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

        public static HLink GetLink(DebugResponseNode node)
        {
            return GetLink(node.Name, node.URL);
        }

        public static HLink GetLink(IDebugRespondable respondable)
        {
            return GetLink(respondable.GetDebugResponseNode());
        }
    }

    public class DebugContainerResponseNode : DebugResponseNode
    {
        private string _description;
        private AVLTree<ID, DebugResponseNode> SubNodes;

        public Func<SessionData, HElement> GetElements;

        private DebugContainerResponseNode() { }

        public DebugContainerResponseNode(string name, string description = null, Func<SessionData, HElement> getElementFunc = null, DebugContainerResponseNode parentNode = null)
        {
            Name = name;
            _description = description;
            SubNodes = new AVLTree<ID, DebugResponseNode>();
            GetElements = getElementFunc ?? (s => new HText($"No {nameof(GetElements)} function was specified."));

            if (parentNode == null)
                DebugResponse.AddNode(this);
            else
                parentNode.AddNode(this);
        }

        internal static DebugContainerResponseNode ConstructRootNode(string name, string description = null, Func<SessionData, HElement> getElementFunc = null)
        {
            DebugContainerResponseNode ret = new DebugContainerResponseNode();

            ret.Name = name;
            ret._description = description;
            ret.SubNodes = new AVLTree<ID, DebugResponseNode>();
            ret.GetElements = getElementFunc ?? (s => new HPlainText());
            ret.SetRootNode();

            return ret;
        }

        public void AddNode(DebugResponseNode node)
        {
            node.SetParent(this);
            SubNodes.Add(node.ID, node);
        }

        public void RemoveNode(DebugResponseNode node)
        {
            SubNodes.Remove(new KeyValuePair<ID, DebugResponseNode>(node.ID, node));
        }

        public void ClearNodes()
        {
            SubNodes.Clear();
        }

        public override HElement GetContents(SessionData sessionData, string requestedAction, PositionQueue<Tuple<ID, string>> positionQueue)
        {
            if(positionQueue.AtEnd())
            {
                HList list = new HList(HList.EListType.UnorderedList, (from s in SubNodes select GetLink(s.Value)).ToArray())
                {
                    Class = "subnodes"
                };

                return new HHeadline(Name) + (_description == null ? new HText(_description) : new HText()) + new HContainer(GetElements(sessionData)) + list;
            }
            else
            {
                HLink navlink = GetLink(this);
                navlink.Class = "nav";
                HInlineContainer name = new HInlineContainer() { Elements = { navlink } };

                DebugResponseNode node = null;

                positionQueue.Pop();

                if(SubNodes)
                {
                    node = SubNodes[positionQueue.Peek().Item1];
                }

                if(ReferenceEquals(node, null))
                {
                    HList list = new HList(HList.EListType.UnorderedList, (from s in SubNodes select GetLink(s.Value.Name, s.Key, positionQueue, positionQueue.Position, null)).ToArray())
                    {
                        Class = "subnodes"
                    };

                    return name + new HNewLine() + new HText($"The ID '{positionQueue.Peek().Item1.Value}' is not a child of this {nameof(DebugContainerResponseNode)}.") { Class = "invalid" } + list;
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
