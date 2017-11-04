using LamestWebserver.Collections;
using LamestWebserver.Core;
using LamestWebserver.RequestHandlers;
using LamestWebserver.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.RequestHandlers.DebugView
{
    /// <summary>
    /// An Object that a DebugResponseNode can be retrieved from.
    /// </summary>
    public interface IDebugRespondable
    {
        /// <summary>
        /// Retrieves the DebugResponseNode for this object.
        /// </summary>
        /// <returns>Retrieves the DebugResponseNode for this object.</returns>
        DebugResponseNode GetDebugResponseNode();
    }

    /// <summary>
    /// A request handler for the LamestWebserver DebugView.
    /// </summary>
    public class DebugResponse : IRequestHandler
    {
        /// <summary>
        /// The Singleton holding the main DebugResponse instance.
        /// </summary>
        public static readonly Singleton<DebugResponse> DebugResponseInstance = new Singleton<DebugResponse>(() => new DebugResponse());

        /// <summary>
        /// The Singleton holding the RequestHandler for the DebugResponse. This instance can simply be attatched to a Webserver in order to view the DebugView.
        /// </summary>
        public static readonly Singleton<RequestHandler> DebugViewRequestHandler = new Singleton<RequestHandler>(() => 
        {
            RequestHandler ret = new RequestHandler(nameof(DebugViewRequestHandler));

            ret.AddRequestHandler(DebugResponseInstance.Instance);

            return ret;
        });

        private static DebugContainerResponseNode _debugNode = DebugContainerResponseNode.ConstructRootNode("LamestWebserver Debug View", "All collected Debug Information can be retrieved using this page.");

#region StyleSheet
        /// <summary>
        /// The css stylesheet for the DebugView.
        /// </summary>
        public static string StyleSheet =
@"body {
    margin: 0;
    background: #111215;
}

p, a, h1, h2, h3, div.container, b, i, button, form, img {
    max-width: 1100px;
    margin: 1em auto 0.5em auto;
    display: block;
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
    margin: 1.5em auto;
    color: #575d61;
    border: 1.5px dashed #3c3c3c;
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
    color: #e2907e;
    text-decoration: none;
    display: block;
}

a:hover {
    color: #ffd45b;
    text-decoration: underline;
}

a:active {
    color: #ffe89e;
    text-decoration: underline;
}

.subnodes a {
    color: #8e8e8e;
}

.subnodes a:hover, .subnodes a:active {
    color: #ffffff;
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
    margin: 1em auto 1em auto;
    padding: 0.5em 3em 1em 3em;
    border: 0.1em solid #222327;
    font-family: Consolas, 'Courier New', monospace;
}

h3.subnodes {
    font-weight: lighter;
    color: #aaa;
    margin-top: 3em;
}

i.subnodes {
    display: block;
    margin-top: 3.5em;
    color: #575d61;
    font-size: 11pt;
}

hr.subnodes {
    width: 80%;
    margin: auto;
    margin-top: 5em;
    margin-bottom: -2em;
    border: 0;
    height: 1px;
    background: linear-gradient(to right, #ff6c8e, #ffb06c, #ffe66c);
}

hr.start {
    width: 80%;
    margin: auto;
    margin-top: -1.5em;
    border: 0;
    height: 1px;
    background: linear-gradient(to left, #ff6c8e, #ffb06c, #ffe66c);
}

p.code {
    background-color: #323438;
    color: #dedede;
    padding: 1.75em;
    word-wrap: break-word;
    font-family: Consolas, 'Courier New', monospace;
}

p.invalid, p.error {
    margin: 3em auto 3em auto;
    border: 0.2em solid #d02f55;
    padding: 1em 2em;
    background: repeating-linear-gradient( 45deg, #d02f55, #d02f55 0.5em, #f3416a 0.5em, #f3416a 1em );
    text-shadow: #000 1px 1px 2px;
}

p.warning {
    margin: 3em auto 3em auto;
    border: 0.2em solid #e49928;
    padding: 1em 2em;
    background: repeating-linear-gradient( 45deg, #f1bb42, #f1bb42 0.5em, #e49928 0.5em, #e49928 1em );
    text-shadow: #000 1px 1px 2px;
}

button {
    margin: 1.5em 10% 1.5em 10%;
    background: linear-gradient(20deg, #ff6e8b, #ffa770);
    border-radius: 5px;
    border: none;
    color: #ffffff;
    text-align: center;
    font-weight: bold;
    font-size: 11.5pt;
    padding: 0.5em 0.75em;
}

button:hover {
    background: linear-gradient(20deg, #ff8e7d, #ffcc6c);
}

button:active, button:focus {
    background: linear-gradient(20deg, #ffc16c, #ffe66b);
    outline: none;
}";
        #endregion

        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket, System.Diagnostics.Stopwatch currentStopwatch)
        {
            SessionData sessionData = new HttpSessionData(requestPacket);
            WalkableQueue<Tuple<ID, string>> unpackedUrls = UnpackUrlActions(requestPacket);
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
                        },
                        Class = "footer"
                    }
                }
            };

            return new HttpResponse(requestPacket, pb.GetContent(sessionData)) { Cookies = ((HttpSessionData)(sessionData)).Cookies.ToList() };
        }

        /// <summary>
        /// Unpacks the URL of the Request into a WalkableQueue.
        /// </summary>
        /// <param name="request">The HttpRequest to take the URL from.</param>
        /// <returns>A Walkable Queue containing the URL.</returns>
        public WalkableQueue<Tuple<ID, string>> UnpackUrlActions(HttpRequest request)
        {
            List<string> links = request.RequestUrl.Split('/').ToList();
            links.Remove("");

            WalkableQueue<Tuple<ID, string>> ret = new WalkableQueue<Tuple<ID, string>>();

            ret.Push(new Tuple<ID, string>(new ID(new ulong[] { 0xFFFFFFFFul }), null));

            for (int i = 0; i < links.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(links[i]))
                    break;

                string action = request.VariablesHttpHead[$"action_{i}"];

                try
                {
                    ret.Push(new Tuple<ID, string>(new ID(links[i]), action.DecodeUrl()));
                }
                catch
                {
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Adds a node to the static DebugNode.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public static void AddNode(DebugResponseNode node)
        {
            _debugNode.AddNode(node);
        }

        /// <summary>
        /// Removes a node from the static DebugNode.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        public static void RemoveNode(DebugResponseNode node)
        {
            _debugNode.RemoveNode(node);
        }

        /// <summary>
        /// Clears the static DebugNode.
        /// </summary>
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

    /// <summary>
    /// A Node of Contents for the DebugView.
    /// </summary>
    public abstract class DebugResponseNode
    {
        /// <summary>
        /// The Name of this DebugResponseNode.
        /// </summary>
        public string Name;

        private ID _id;
        private DebugContainerResponseNode _parentNode;

        /// <summary>
        /// The ID of this DebugResponseNode.
        /// </summary>
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

        /// <summary>
        /// The URL of this DebugResponseNode.
        /// </summary>
        public URL<ID> URL { get; private set; }

        /// <summary>
        /// Sets the the current node as Child of a given DebugContainerResponseNode.
        /// </summary>
        /// <param name="node">The parent node for this node.</param>
        internal void SetParentURL(DebugContainerResponseNode node)
        {
            // We don't want to expose this to someone who does not know how exactly this works. Please just use 'parentNode.AddNode(this);' to add this node to a parentNode.

            if (URL != null)
            {
                if (_parentNode != null)
                {
                    Logger.LogTrace($"SetParentURL: A DebugNodes parent is being modified and it will not be accessible anymore via the old URL. (The URL is changing from '{URL}' to '{node.URL}/{ID}'.)");
                    _parentNode.RemoveNode(this);
                }
            }

            if (node == null)
                URL = new URL<ID>(new ID[] { ID });
            else
                URL = node.URL.Append(ID);

            _parentNode = node;
        }

        /// <summary>
        /// Sets the current node as RootNode.
        /// </summary>
        protected void SetRootNode()
        {
            URL = new URL<ID>(new ID[0]);
        }

        /// <summary>
        /// Retrieves the contents of the DebugView node as HElement.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <param name="requestedAction">The requested Action for this particular node (if any).</param>
        /// <param name="walkableQueue">The current WalkableQueue containing all Subnodes of the requested URL.</param>
        /// <returns></returns>
        public abstract HElement GetContents(SessionData sessionData, string requestedAction, WalkableQueue<Tuple<ID, string>> walkableQueue);

        /// <summary>
        /// Creates a link to another DebugNode.
        /// </summary>
        /// <param name="text">The text of the link.</param>
        /// <param name="subUrl">The subUrl to link to.</param>
        /// <param name="walkableQueue">The current walkableQueue.</param>
        /// <param name="requestedAction">The requested Action for the linked DebugNode.</param>
        /// <returns>The link as HLink.</returns>
        public static HLink GetLink(string text, ID subUrl, WalkableQueue<Tuple<ID, string>> walkableQueue, string requestedAction = null)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (subUrl == null)
                throw new ArgumentNullException(nameof(subUrl));

            if (walkableQueue == null)
                throw new ArgumentNullException(nameof(walkableQueue));

            List<Tuple<ID, string>> already = walkableQueue.GetPassed();

            if (!walkableQueue.AtEnd())
                already.Add(walkableQueue.Peek());

            string ret = "/";

            foreach (Tuple<ID, string> tuple in already)
                ret += tuple.Item1 + "/";

            ret += "?";

            for (int i = 0; i < already.Count - 1; i++)
                if (!string.IsNullOrEmpty(already[i].Item2))
                    ret += $"action_{i}={already[i].Item2}&";

            if (!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{already.Count - 1}={requestedAction.EncodeUrl()}";

            return new HLink(text, ret);
        }

        /// <summary>
        /// Creates a link to another DebugNode.
        /// </summary>
        /// <param name="text">The text of the link.</param>
        /// <param name="url">The URL of the DebugNode to link to.</param>
        /// <param name="requestedAction">The requested Action for the linked DebugNode.</param>
        /// <returns>The link as HLink.</returns>
        public static HLink GetLink(string text, URL<ID> url, string requestedAction = null)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (url == null)
                throw new ArgumentNullException(nameof(url));

            string ret = "/" + url.ToString() + "?";

            if (!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{url.Count - 1}={requestedAction.EncodeUrl()}";

            return new HLink(text, ret);
        }

        /// <summary>
        /// Creates a link to another DebugNode.
        /// </summary>
        /// <param name="text">The text of the link.</param>
        /// <param name="subUrl">The subUrl to link to.</param>
        /// <param name="walkableQueue">The current walkableQueue.</param>
        /// <param name="position">The maximum Position to get from in the walkable queue.</param>
        /// <param name="requestedAction">The requested Action for the linked DebugNode.</param>
        /// <returns>The link as HLink.</returns>
        public static HLink GetLink(string text, ID subUrl, WalkableQueue<Tuple<ID, string>> walkableQueue, int position, string requestedAction = null)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (subUrl == null)
                throw new ArgumentNullException(nameof(subUrl));

            if (walkableQueue == null)
                throw new ArgumentNullException(nameof(walkableQueue));

            List<Tuple<ID, string>> nodes = walkableQueue.GetRange(1, position);

            string ret = "/";

            foreach (Tuple<ID, string> tuple in nodes)
                ret += tuple.Item1 + "/";

            ret += subUrl + "?";

            for (int i = 0; i < nodes.Count - 1; i++)
                if (!string.IsNullOrEmpty(nodes[i].Item2))
                    ret += $"action_{i}={nodes[i].Item2}&";

            if (!string.IsNullOrEmpty(requestedAction))
                ret += $"action_{nodes.Count - 1}={requestedAction.EncodeUrl()}";

            return new HLink(text, ret);
        }

        /// <summary>
        /// Creates a link to another DebugNode.
        /// </summary>
        /// <param name="node">The Node to link to.</param>
        /// <returns>The link as HLink.</returns>
        public static HLink GetLink(DebugResponseNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            return GetLink(node.Name, node.URL);
        }

        /// <summary>
        /// Creates a link to another DebugNode.
        /// </summary>
        /// <param name="respondable">The IDebugRespondable that contains the Node to link to.</param>
        /// <returns>The link as HLink.</returns>
        public static HLink GetLink(IDebugRespondable respondable)
        {
            if (respondable == null)
                throw new ArgumentNullException(nameof(respondable));

            return GetLink(respondable.GetDebugResponseNode());
        }
    }

    /// <summary>
    /// A kind of DebugResponseNode that can contain subnodes.
    /// </summary>
    public class DebugContainerResponseNode : DebugResponseNode
    {
        private string _description;
        private AVLTree<ID, DebugResponseNode> _subNodes;

        /// <summary>
        /// This Func is Called whenever the contents of this particular node are requested.
        /// </summary>
        public Func<SessionData, HElement> GetElements;

        private DebugContainerResponseNode() { }

        /// <summary>
        /// Creates a new DebugContainerResponseNode.
        /// </summary>
        /// <param name="name">The name of this DebugResponseNode.</param>
        /// <param name="description">The description for this DebugResponseNode.</param>
        /// <param name="getElementFunc">The function to execute whenever the contents of this DebugResponseNode are requested.</param>
        /// <param name="parentNode">The parent node of this node.</param>
        /// <param name="AddToParent">Shall this node be added to it's parent already?</param>
        public DebugContainerResponseNode(string name, string description = null, Func<SessionData, HElement> getElementFunc = null, DebugContainerResponseNode parentNode = null, bool AddToParent = true)
        {
            Name = name;
            _description = description;
            _subNodes = new AVLTree<ID, DebugResponseNode>();
            GetElements = getElementFunc ?? (s => new HText($"No {nameof(GetElements)} function was specified."));

            if (AddToParent)
            {
                if (parentNode == null)
                    DebugResponse.AddNode(this);
                else
                    parentNode.AddNode(this);
            }
        }

        /// <summary>
        /// Constructs a new root-DebugResponse-node.
        /// </summary>
        /// <param name="name">The name of the Node.</param>
        /// <param name="description">The description for the node.</param>
        /// <param name="getElementFunc">The function to call whenever the contents of the node will be requested.</param>
        /// <returns>A root-DebugResponse-node.</returns>
        public static DebugContainerResponseNode ConstructRootNode(string name, string description = null, Func<SessionData, HElement> getElementFunc = null)
        {
            DebugContainerResponseNode ret = new DebugContainerResponseNode();

            ret.Name = name;
            ret._description = description;
            ret._subNodes = new AVLTree<ID, DebugResponseNode>();
            ret.GetElements = getElementFunc ?? (s => new HPlainText());
            ret.SetRootNode();

            return ret;
        }

        /// <summary>
        /// Adds a specified node as sub-node of this node.
        /// </summary>
        /// <param name="node">The node to add as subnode.</param>
        public void AddNode(DebugResponseNode node)
        {
            node.SetParentURL(this);
            _subNodes.Add(node.ID, node);
        }

        /// <summary>
        /// Removes a specified node from the sub-nodes of this node.
        /// </summary>
        /// <param name="node">The node to remove from the subnodes.</param>
        public void RemoveNode(DebugResponseNode node)
        {
            _subNodes.Remove(new KeyValuePair<ID, DebugResponseNode>(node.ID, node));
        }

        /// <summary>
        /// Clears the Subnodes of this node.
        /// </summary>
        public void ClearNodes()
        {
            _subNodes.Clear();
        }

        /// <inheritdoc />
        public override HElement GetContents(SessionData sessionData, string requestedAction, WalkableQueue<Tuple<ID, string>> positionQueue)
        {
            if(positionQueue.AtEnd())
            {
                HElement list = new HList(HList.EListType.UnorderedList, (from s in _subNodes select GetLink(s.Value.Name, s.Key, positionQueue, positionQueue.Position, null)).ToArray())
                {
                    Class = "subnodes"
                };

                if (((HList)list).IsEmpty())
                    list = new HItalic("This DebugNode includes no Subnodes.") { Class = "subnodes" };
                else
                    list = new HHeadline("Subnodes of this DebugNode", 3) { Class = "subnodes" } + list;

                return new HHeadline(Name) + new HLine() { Class = "start" } + (_description == null ? new HText(_description) : (HElement)new HString()) + new HContainer(GetElements(sessionData)) + new HLine() { Class = "subnodes" } + list;
            }
            else
            {
                HLink navlink = GetLink(this);
                navlink.Class = "nav";
                HInlineContainer name = new HInlineContainer() { Elements = { navlink } };

                DebugResponseNode node = null;

                positionQueue.Pop();

                if(_subNodes)
                {
                    node = _subNodes[positionQueue.Peek().Item1];
                }

                if(ReferenceEquals(node, null))
                {
                    HElement list = new HList(HList.EListType.UnorderedList, (from s in _subNodes select GetLink(s.Value.Name, s.Value.URL)).ToArray())
                    {
                        Class = "subnodes"
                    };

                    if (((HList)list).IsEmpty())
                        list = new HItalic("This DebugNode includes no Subnodes.") { Class = "subnodes" };
                    else
                        list = new HHeadline("Subnodes of this DebugNode", 3) { Class = "subnodes" } + list;

                    return name + new HHeadline(Name) + new HLine() { Class = "start" } + new HText($"The ID '{positionQueue.Peek().Item1.Value}' is not a child of this {nameof(DebugContainerResponseNode)}.") { Class = "invalid" } + new HLine() { Class = "subnodes" } + list;
                }
                else
                {
                    return name + node.GetContents(sessionData, positionQueue.Peek().Item2, positionQueue);
                }
            }
        }
    }

    /// <summary>
    /// A DebugResponseNode that retrieves a static response.
    /// </summary>
    public class StaticDebugContainerResponseNode : DebugContainerResponseNode
    {
        /// <summary>
        /// The elements to return on request of the contents of this node.
        /// </summary>
        public List<HElement> Elements;

        /// <summary>
        /// Constructs a new StaticDebugContainerResponseNode.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        /// <param name="description">The description of this node.</param>
        /// <param name="elements">The elements contained in this node.</param>
        public StaticDebugContainerResponseNode(string name, string description = null, params HElement[] elements) : base(name, description)
        {
            Elements = new List<HElement>(elements);
            GetElements = s => new HMultipleElements(Elements);
        }
    }
}
