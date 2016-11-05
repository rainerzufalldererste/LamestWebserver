using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LamestWebserver.JScriptBuilder
{
    public class JScript
    {
        public List<IJSPiece> pieces = new List<IJSPiece>();

        public JScript()
        {

        }

        public JScript(params IJSPiece[] pieces)
        {
            this.pieces = pieces.ToList();
        }

        public JScript(List<IJSPiece> pieces)
        {
            this.pieces = pieces;
        }

        public void appendCode(IJSPiece piece)
        {
            pieces.Add(piece);
        }
    }

    public static class JSMaster
    {
        public static string jsEncode(this string input)
        {
            return HttpUtility.HtmlEncode(input).Replace("&lt;", "<").Replace("&gt;", ">");
        }
    }

    public enum CallingContext
    {
        Default, Inner, NoSemicolon
    }

    public interface IJSPiece
    {
        string getCode(SessionData sessionData, CallingContext context = CallingContext.Default);
    }

    public class JSFunction : IJSValue
    {
        public List<IJSPiece> pieces = new List<IJSPiece>();
        public List<IJSValue> parameters = new List<IJSValue>();

        public string content { get; set; }
        public IJSValue FunctionPointer { get { return new JSValue(content); } }

        public JSFunction(string name, List<IJSValue> parameters)
        {
            if (String.IsNullOrWhiteSpace(name))
                this.content = "_func" + SessionContainer.generateHash();

            this.parameters = parameters;
        }

        public JSFunction(List<IJSValue> parameters)
        {
            this.content = "_func" + SessionContainer.generateHash();
            this.parameters = parameters;
        }

        public JSFunction(params IJSValue[] parameters)
        {
            this.content = "_func" + SessionContainer.generateHash();
            this.parameters = parameters.ToList();
        }

        public JSFunction(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                this.content = "_func" + SessionContainer.generateHash();
        }

        public JSFunction()
        {
            this.content = "_func" + SessionContainer.generateHash();
        }

        public void appendCode(IJSPiece piece)
        {
            pieces.Add(piece);
        }

        public string getCode(SessionData sessionData, CallingContext context)
        {
            string ret = "function " + content + " ( ";

            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0)
                    ret += ", ";

                ret += parameters[i].content;
            }

            ret += ") { ";

            for (int i = 0; i < pieces.Count; i++)
            {
                ret += pieces[i].getCode(sessionData, CallingContext.Default);
            }

            return ret + "}";
        }

        public JSPMethodCall callFunction(params IJSValue[] values)
        {
            return new JSPMethodCall(this.content, values);
        }

        public JSDirectFunctionCall DefineAndCall()
        {
            return new JSDirectFunctionCall(this);
        }
    }

    public class JSDirectFunctionCall : IJSValue
    {
        JSFunction function;

        public JSDirectFunctionCall(JSFunction function)
        {
            this.function = function;
        }

        public string content
        {
            get
            {
                return function.content;
            }
        }

        public string getCode(SessionData sessionData, CallingContext context)
        {
            return "(" + function.getCode(sessionData, CallingContext.Default) + ")()" + (context == CallingContext.Default ? ";" : " ");
        }
    }

    public class JSInstantFunction : JSFunction
    {
        public JSInstantFunction(params IJSPiece[] pieces)
        {
            base.content = "_ifunc" + SessionContainer.generateHash();
            base.pieces = pieces.ToList();
        }
    }

    public interface IJSValue : IJSPiece
    {
        string content { get; }
    }

    public class JSStringValue : JSValue
    {
        public JSStringValue(string value) : base(value)
        {
            this.content = value;
        }

        public string content { get; set; }

        public override string getCode(SessionData sessionData, CallingContext context)
        {
            return "\"" + content.jsEncode() + "\"" + (context == CallingContext.Default ? ";" : " ");
        }
    }

    public class JSValue : IJSValue
    {
        public string content { get; set; }

        public JSValue(object content)
        {
            this.content = content.ToString();
        }

        public JSValue(string content)
        {
            this.content = content;
        }

        public JSValue(int content)
        {
            this.content = content.ToString();
        }

        public JSValue(bool content)
        {
            this.content = content.ToString();
        }

        public JSValue(double content)
        {
            this.content = content.ToString();
        }

        public virtual string getCode(SessionData sessionData, CallingContext context)
        {
            return content + (context == CallingContext.Default ? ";" : " ");
        }
    }

    public class JSVariable : IJSValue
    {
        public string content { get; set; }

        public JSVariable(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                this.content = "_var" + SessionContainer.generateHash();
        }

        public string getCode(SessionData sessionData, CallingContext context)
        {
            return "var " + content + (context == CallingContext.Default ? ";" : " ");
        }
    }

    public class JSPMethodCall : IJSValue
    {
        private IJSValue[] parameters;
        private string methodName;

        public string content
        {
            get
            {
                return methodName;
            }
        }

        public JSPMethodCall(string methodName, params IJSValue[] parameters)
        {
            this.methodName = methodName;
            this.parameters = parameters;
        }

        public string getCode(SessionData sessionData, CallingContext context)
        {
            string ret = methodName + "(";

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    ret += ", ";

                ret += parameters[i].getCode(sessionData, CallingContext.Default);
            }

            return ret + ")" + (context == CallingContext.Default ? ";" : " ");
        }

        public static class window
        {
            public static JSPMethodCall requestAnimationFrame(JSFunction function)
            {
                return new JSPMethodCall("window.requestAnimationFrame", function.FunctionPointer);
            }
        }

        public static JSValue SetInnerHTMLAsync(IJSValue value, string href)
        {
            return new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + value.getCode(SessionData.currentSessionData, CallingContext.Inner) + ".innerHTML=this.responseText; } }; xmlhttp.open(\"GET\",\"" + href + "\",true);xmlhttp.send();");
        }

        public static IJSPiece HideElementByID(string id)
        {
            return new JSValue("document.getElementById(\"" + id + "\").style.display = \"none\";");
        }

        public static IJSPiece RemoveElementByID(string id)
        {
            return new JSValue("document.removeChild(document.getElementById(\"" + id + "\"));");
        }
    }

    public abstract class JSElement : HElement, IJSPiece
    {
        public string descriptionTags = "";

        public IJSValue CreateNew(CallingContext context = CallingContext.Default)
        {
            return new JSInstantFunction(new JSValue("document.body.insertAdjacentHTML(\"beforeend\", " + getCode(SessionData.currentSessionData, CallingContext.Inner) + ");")).DefineAndCall();
        }

        public string getCode(SessionData sessionData, CallingContext context)
        {
            return "\"" + getContent(sessionData, CallingContext.Inner).jsEncode() + "\"" + (context == CallingContext.Default ? ";" : " ");
        }

        protected string getDefaultAttributes()
        {
            string ret = " ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id=\"" + ID + "\" ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name=\"" + Name + "\" ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class=\"" + Class + "\" ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            return ret;
        }

        public abstract string getContent(SessionData sessionData, CallingContext context);

        public override string getContent(SessionData sessionData)
        {
            return getContent(sessionData, CallingContext.Default);
        }

        public static IJSValue getByID(string id)
        {
            return new JSPMethodCall("document.getElementById", new JSStringValue(id));
        }
    }

    public class JSMsgBox : JSElement
    {
        private List<JSElement> elements;
        private bool hasExitButton = true;

        public JSMsgBox(bool hasExitButton, params JSElement[] elements)
        {
            this.hasExitButton = hasExitButton;
            this.elements = elements.ToList();
        }

        public JSMsgBox(params JSElement[] elements)
        {
            this.elements = elements.ToList();
        }

        public override string getContent(SessionData sessionData, CallingContext context)
        {
            string ret = "<div id=\"_lws_jsbuilder_msgbox_outer\" class=\"_lws_jsbuilder_msgbox_outer\" style=\"display: block;position: fixed;top: 0;right: 0;left: 0;bottom: 0;width: 100%;height: 100%;z-index: 100;background-color: rgba(33, 33, 33, 0.75);\"><div class=\"_lws_jsbuilder_msgbox_inner\" style=\"display: block;position: relative;margin: auto;background-color: #fff;max-width: 600px;overflow: auto;margin-top: 8em;\">";

            if (hasExitButton)
                ret += "<button onclick=\"javascript: { document.getElementById('_lws_jsbuilder_msgbox_outer').style.display = 'none'; }\" class=\"_lws_jsbuilder_msgbox_exitbutton\">Exit</button>";

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData, CallingContext.Default).jsEncode();
            }

            return ret + "</div></div>";
        }

        public static string GlobalID = "_lws_jsbuilder_msgbox_outer";
    }

    public class JSButton : JSInteractableElement
    {
        public HButton.EButtonType buttonType = HButton.EButtonType.button;
        public string buttonText = "";
        
        public JSButton(string buttonText, HButton.EButtonType buttonType = HButton.EButtonType.button)
        {
            this.buttonText = buttonText;
            this.buttonType = buttonType;
        }

        public override string getContent(SessionData sessionData, CallingContext context)
        {
            return "<button type='" + buttonType + "' " + getDefaultAttributes() + getEventAttributes(sessionData, context) + ">" + buttonText.jsEncode() + "</button>";
        }
    }

    #region ARE YOU READY FOR THE WEB? THE TRUE WEB? FEEL FREE TO ENTER?

    /// <summary>
    /// See: http://www.w3schools.com/jsref/dom_obj_event.asp
    /// </summary>
    public abstract class JSInteractableElement : JSElement
    {
        public abstract override string getContent(SessionData sessionData, CallingContext context);

        // #AREYOUREADYFORTHEWEB?

        /// <summary>
        /// The event occurs when the user clicks on an element
        /// </summary>
        public IJSPiece onclick;

        /// <summary>
        /// The event occurs when the user right-clicks on an element to open a context menu
        /// </summary>
        public IJSPiece oncontextmenu;

        /// <summary>
        /// The event occurs when the user double-clicks on an element
        /// </summary>
        public IJSPiece ondblclick;

        /// <summary>
        /// The event occurs when the user presses a mouse button over an element
        /// </summary>
        public IJSPiece onmousedown;

        /// <summary>
        /// The event occurs when the pointer is moved onto an element
        /// </summary>
        public IJSPiece onmouseenter;

        /// <summary>
        /// The event occurs when the pointer is moved out of an element
        /// </summary>
        public IJSPiece onmouseleave;

        /// <summary>
        /// The event occurs when the pointer is moving while it is over an element
        /// </summary>
        public IJSPiece onmousemove;

        /// <summary>
        /// The event occurs when the pointer is moved onto an element, or onto one of its children
        /// </summary>
        public IJSPiece onmouseover;

        /// <summary>
        /// The event occurs when a user moves the mouse pointer out of an element, or out of one of its children
        /// </summary>
        public IJSPiece onmouseout;

        /// <summary>
        /// The event occurs when a user releases a mouse button over an element
        /// </summary>
        public IJSPiece onmouseup;

        /// <summary>
        /// The event occurs when the user is pressing a key
        /// </summary>
        public IJSPiece onkeydown;

        /// <summary>
        /// The event occurs when the user presses a key
        /// </summary>
        public IJSPiece onkeypress;

        /// <summary>
        /// The event occurs when the user releases a key
        /// </summary>
        public IJSPiece onkeyup;

        /// <summary>
        /// The event occurs when the loading of a resource has been aborted
        /// The event occurs when the loading of a media is aborted
        /// </summary>
        public IJSPiece onabort;

        /// <summary>
        /// The event occurs before the document is about to be unloaded
        /// </summary>
        public IJSPiece onbeforeunload;

        /// <summary>
        /// The event occurs when an error occurs while loading an external file
        /// The event occurs when an error occurred during the loading of a media file
        /// The event occurs when an error occurs with the event source
        /// </summary>
        public IJSPiece onerror;

        /// <summary>
        /// The event occurs when there has been changes to the anchor part of a URL
        /// </summary>
        public IJSPiece onhashchange;

        /// <summary>
        /// The event occurs when an object has loaded
        /// </summary>
        public IJSPiece onload;

        /// <summary>
        /// The event occurs when the user navigates to a webpage
        /// </summary>
        public IJSPiece onpageshow;

        /// <summary>
        /// The event occurs when the user navigates away from a webpage
        /// </summary>
        public IJSPiece onpagehide;

        /// <summary>
        /// The event occurs when the document view is resized
        /// </summary>
        public IJSPiece onresize;

        /// <summary>
        /// The event occurs when an element's scrollbar is being scrolled
        /// </summary>
        public IJSPiece onscroll;

        /// <summary>
        /// The event occurs once a page has unloaded (for body)
        /// </summary>
        public IJSPiece onunload;

        /// <summary>
        /// The event occurs when an element loses focus
        /// </summary>
        public IJSPiece onblur;

        /// <summary>
        /// The event occurs when the content of a form element, the selection, or the checked state have changed (for input, keygen, select, and textarea)
        /// </summary>
        public IJSPiece onchange;

        /// <summary>
        /// The event occurs when an element gets focus
        /// </summary>
        public IJSPiece onfocus;

        /// <summary>
        /// The event occurs when an element is about to get focus
        /// </summary>
        public IJSPiece onfocusin;

        /// <summary>
        /// The event occurs when an element is about to lose focus
        /// </summary>
        public IJSPiece onfocusout;

        /// <summary>
        /// The event occurs when an element gets user input
        /// </summary>
        public IJSPiece oninput;

        /// <summary>
        /// The event occurs when an element is invalid
        /// </summary>
        public IJSPiece oninvalid;

        /// <summary>
        /// The event occurs when a form is reset
        /// </summary>
        public IJSPiece onreset;

        /// <summary>
        /// The event occurs when the user writes something in a search field (for input="search")
        /// </summary>
        public IJSPiece onsearch;

        /// <summary>
        /// The event occurs after the user selects some text (for input and textarea)
        /// </summary>
        public IJSPiece onselect;

        /// <summary>
        /// The event occurs when a form is submitted
        /// </summary>
        public IJSPiece onsubmit;

        /// <summary>
        /// The event occurs when an element is being dragged
        /// </summary>
        public IJSPiece ondrag;

        /// <summary>
        /// The event occurs when the user has finished dragging an element
        /// </summary>
        public IJSPiece ondragend;

        /// <summary>
        /// The event occurs when the dragged element enters the drop target
        /// </summary>
        public IJSPiece ondragenter;

        /// <summary>
        /// The event occurs when the dragged element leaves the drop target
        /// </summary>
        public IJSPiece ondragleave;

        /// <summary>
        /// The event occurs when the dragged element is over the drop target
        /// </summary>
        public IJSPiece ondragover;

        /// <summary>
        /// The event occurs when the user starts to drag an element
        /// </summary>
        public IJSPiece ondragstart;

        /// <summary>
        /// The event occurs when the dragged element is dropped on the drop target
        /// </summary>
        public IJSPiece ondrop;

        /// <summary>
        /// The event occurs when the user copies the content of an element
        /// </summary>
        public IJSPiece oncopy;

        /// <summary>
        /// The event occurs when the user cuts the content of an element
        /// </summary>
        public IJSPiece oncut;

        /// <summary>
        /// The event occurs when the user pastes some content in an element
        /// </summary>
        public IJSPiece onpaste;

        /// <summary>
        /// The event occurs when a page has started printing, or if the print dialogue box has been closed
        /// </summary>
        public IJSPiece onafterprint;

        /// <summary>
        /// The event occurs when a page is about to be printed
        /// </summary>
        public IJSPiece onbeforeprint;

        /// <summary>
        /// The event occurs when the browser can start playing the media (when it has buffered enough to begin)
        /// </summary>
        public IJSPiece oncanplay;

        /// <summary>
        /// The event occurs when the browser can play through the media without stopping for buffering
        /// </summary>
        public IJSPiece oncanplaythrough;

        /// <summary>
        /// The event occurs when the duration of the media is changed
        /// </summary>
        public IJSPiece ondurationchange;

        /// <summary>
        /// The event occurs when something bad happens and the media file is suddenly unavailable (like unexpectedly disconnects)
        /// </summary>
        public IJSPiece onemptied;

        /// <summary>
        /// The event occurs when the media has reach the end (useful for messages like "thanks for listening")
        /// </summary>
        public IJSPiece onended;

        /// <summary>
        /// The event occurs when media data is loaded
        /// </summary>
        public IJSPiece onloadeddata;

        /// <summary>
        /// The event occurs when meta data (like dimensions and duration) are loaded
        /// </summary>
        public IJSPiece onloadedmetadata;

        /// <summary>
        /// The event occurs when the browser starts looking for the specified media
        /// </summary>
        public IJSPiece onloadstart;

        /// <summary>
        /// The event occurs when the media is paused either by the user or programmatically
        /// </summary>
        public IJSPiece onpause;

        /// <summary>
        /// The event occurs when the media has been started or is no longer paused
        /// </summary>
        public IJSPiece onplay;

        /// <summary>
        /// The event occurs when the media is playing after having been paused or stopped for buffering
        /// </summary>
        public IJSPiece onplaying;

        /// <summary>
        /// The event occurs when the browser is in the process of getting the media data (downloading the media)
        /// </summary>
        public IJSPiece onprogress;

        /// <summary>
        /// The event occurs when the playing speed of the media is changed
        /// </summary>
        public IJSPiece onratechange;

        /// <summary>
        /// The event occurs when the user is finished moving/skipping to a new position in the media
        /// </summary>
        public IJSPiece onseeked;

        /// <summary>
        /// The event occurs when the user starts moving/skipping to a new position in the media
        /// </summary>
        public IJSPiece onseeking;

        /// <summary>
        /// The event occurs when the browser is trying to get media data, but data is not available
        /// </summary>
        public IJSPiece onstalled;

        /// <summary>
        /// The event occurs when the browser is intentionally not getting media data
        /// </summary>
        public IJSPiece onsuspend;

        /// <summary>
        /// The event occurs when the playing position has changed (like when the user fast forwards to a different point in the media)
        /// </summary>
        public IJSPiece ontimeupdate;

        /// <summary>
        /// The event occurs when the volume of the media has changed (includes setting the volume to "mute")
        /// </summary>
        public IJSPiece onvolumechange;

        /// <summary>
        /// The event occurs when the media has paused but is expected to resume (like when the media pauses to buffer more data)
        /// </summary>
        public IJSPiece onwaiting;

        /// <summary>
        /// The event occurs when a CSS animation has completed
        /// </summary>
        public IJSPiece animationend;

        /// <summary>
        /// The event occurs when a CSS animation is repeated
        /// </summary>
        public IJSPiece animationiteration;

        /// <summary>
        /// The event occurs when a CSS animation has started
        /// </summary>
        public IJSPiece animationstart;

        /// <summary>
        /// The event occurs when a CSS transition has completed
        /// </summary>
        public IJSPiece transitionend;

        /// <summary>
        /// The event occurs when a message is received through the event source
        /// </summary>
        public IJSPiece onmessage;

        /// <summary>
        /// The event occurs when a connection with the event source is opened
        /// The event occurs when a message is received through or from an object (WebSocket, Web Worker, Event Source or a child frame or a parent window)
        /// </summary>
        public IJSPiece onopen;

        /// <summary>
        /// Deprecated. Use the onwheel event instead
        /// </summary>
        public IJSPiece onmousewheel;

        /// <summary>
        /// The event occurs when the browser starts to work online
        /// </summary>
        public IJSPiece ononline;

        /// <summary>
        /// The event occurs when the browser starts to work offline
        /// </summary>
        public IJSPiece onoffline;

        /// <summary>
        /// The event occurs when the window's history changes
        /// </summary>
        public IJSPiece onpopstate;

        /// <summary>
        /// The event occurs when a menu element is shown as a context menu
        /// </summary>
        public IJSPiece onshow;

        /// <summary>
        /// The event occurs when a Web Storage area is updated
        /// </summary>
        public IJSPiece onstorage;

        /// <summary>
        /// The event occurs when the user opens or closes the details element
        /// </summary>
        public IJSPiece ontoggle;

        /// <summary>
        /// The event occurs when the mouse wheel rolls up or down over an element
        /// </summary>
        public IJSPiece onwheel;

        /// <summary>
        /// The event occurs when the touch is interrupted
        /// </summary>
        public IJSPiece ontouchcancel;

        /// <summary>
        /// The event occurs when a finger is removed from a touch screen
        /// </summary>
        public IJSPiece ontouchend;

        /// <summary>
        /// The event occurs when a finger is dragged across the screen
        /// </summary>
        public IJSPiece ontouchmove;

        /// <summary>
        /// The event occurs when a finger is placed on a touch screen
        /// </summary>
        public IJSPiece ontouchstart;

        /// <summary>
        /// gets all event attributes for the given object
        /// </summary>
        /// <param name="sessionData">the sessionData</param>
        /// <returns>the event attributes as string</returns>
        public string getEventAttributes(SessionData sessionData, CallingContext context)
        {
            // #AREYOUREADYFORTHEWEB?

            string ret = " ";

            if (onabort != null)
                ret += "onabort=\"" + onabort.getCode(sessionData, context).jsEncode() + "\"";

            if (onafterprint != null)
                ret += "onafterprint=\"" + onafterprint.getCode(sessionData, context).jsEncode() + "\"";

            if (onbeforeprint != null)
                ret += "onbeforeprint=\"" + onbeforeprint.getCode(sessionData, context).jsEncode() + "\"";

            if (onbeforeunload != null)
                ret += "onbeforeunload=\"" + onbeforeunload.getCode(sessionData, context).jsEncode() + "\"";

            if (onblur != null)
                ret += "onblur=\"" + onblur.getCode(sessionData, context).jsEncode() + "\"";

            if (oncanplay != null)
                ret += "oncanplay=\"" + oncanplay.getCode(sessionData, context).jsEncode() + "\"";

            if (oncanplaythrough != null)
                ret += "oncanplaythrough=\"" + oncanplaythrough.getCode(sessionData, context).jsEncode() + "\"";

            if (onchange != null)
                ret += "onchange=\"" + onchange.getCode(sessionData, context).jsEncode() + "\"";

            if (onclick != null)
                ret += "onclick=\"" + onclick.getCode(sessionData, context).jsEncode() + "\"";

            if (oncontextmenu != null)
                ret += "oncontextmenu=\"" + oncontextmenu.getCode(sessionData, context).jsEncode() + "\"";

            if (oncopy != null)
                ret += "oncopy=\"" + oncopy.getCode(sessionData, context).jsEncode() + "\"";

            if (oncut != null)
                ret += "oncut=\"" + oncut.getCode(sessionData, context).jsEncode() + "\"";

            if (ondblclick != null)
                ret += "ondblclick=\"" + ondblclick.getCode(sessionData, context).jsEncode() + "\"";

            if (ondrag != null)
                ret += "ondrag=\"" + ondrag.getCode(sessionData, context).jsEncode() + "\"";

            if (ondragend != null)
                ret += "ondragend=\"" + ondragend.getCode(sessionData, context).jsEncode() + "\"";

            if (ondragenter != null)
                ret += "ondragenter=\"" + ondragenter.getCode(sessionData, context).jsEncode() + "\"";

            if (ondragleave != null)
                ret += "ondragleave=\"" + ondragleave.getCode(sessionData, context).jsEncode() + "\"";

            if (ondragover != null)
                ret += "ondragover=\"" + ondragover.getCode(sessionData, context).jsEncode() + "\"";

            if (ondragstart != null)
                ret += "ondragstart=\"" + ondragstart.getCode(sessionData, context).jsEncode() + "\"";

            if (ondrop != null)
                ret += "ondrop=\"" + ondrop.getCode(sessionData, context).jsEncode() + "\"";

            if (ondurationchange != null)
                ret += "ondurationchange=\"" + ondurationchange.getCode(sessionData, context).jsEncode() + "\"";

            if (onemptied != null)
                ret += "onemptied=\"" + onemptied.getCode(sessionData, context).jsEncode() + "\"";

            if (onended != null)
                ret += "onended=\"" + onended.getCode(sessionData, context).jsEncode() + "\"";

            if (onerror != null)
                ret += "onerror=\"" + onerror.getCode(sessionData, context).jsEncode() + "\"";

            if (onfocus != null)
                ret += "onfocus=\"" + onfocus.getCode(sessionData, context).jsEncode() + "\"";

            if (onfocusin != null)
                ret += "onfocusin=\"" + onfocusin.getCode(sessionData, context).jsEncode() + "\"";

            if (onfocusout != null)
                ret += "onfocusout=\"" + onfocusout.getCode(sessionData, context).jsEncode() + "\"";

            if (onhashchange != null)
                ret += "onhashchange=\"" + onhashchange.getCode(sessionData, context).jsEncode() + "\"";

            if (oninput != null)
                ret += "oninput=\"" + oninput.getCode(sessionData, context).jsEncode() + "\"";

            if (oninvalid != null)
                ret += "oninvalid=\"" + oninvalid.getCode(sessionData, context).jsEncode() + "\"";

            if (onkeydown != null)
                ret += "onkeydown=\"" + onkeydown.getCode(sessionData, context).jsEncode() + "\"";

            if (onkeypress != null)
                ret += "onkeypress=\"" + onkeypress.getCode(sessionData, context).jsEncode() + "\"";

            if (onkeyup != null)
                ret += "onkeyup=\"" + onkeyup.getCode(sessionData, context).jsEncode() + "\"";

            if (onload != null)
                ret += "onload=\"" + onload.getCode(sessionData, context).jsEncode() + "\"";

            if (onloadeddata != null)
                ret += "onloadeddata=\"" + onloadeddata.getCode(sessionData, context).jsEncode() + "\"";

            if (onloadedmetadata != null)
                ret += "onloadedmetadata=\"" + onloadedmetadata.getCode(sessionData, context).jsEncode() + "\"";

            if (onloadstart != null)
                ret += "onloadstart=\"" + onloadstart.getCode(sessionData, context).jsEncode() + "\"";

            if (onmessage != null)
                ret += "onmessage=\"" + onmessage.getCode(sessionData, context).jsEncode() + "\"";

            if (onmousedown != null)
                ret += "onmousedown=\"" + onmousedown.getCode(sessionData, context).jsEncode() + "\"";

            if (onmouseenter != null)
                ret += "onmouseenter=\"" + onmouseenter.getCode(sessionData, context).jsEncode() + "\"";

            if (onmouseleave != null)
                ret += "onmouseleave=\"" + onmouseleave.getCode(sessionData, context).jsEncode() + "\"";

            if (onmousemove != null)
                ret += "onmousemove=\"" + onmousemove.getCode(sessionData, context).jsEncode() + "\"";

            if (onmouseout != null)
                ret += "onmouseout=\"" + onmouseout.getCode(sessionData, context).jsEncode() + "\"";

            if (onmouseover != null)
                ret += "onmouseover=\"" + onmouseover.getCode(sessionData, context).jsEncode() + "\"";

            if (onmouseup != null)
                ret += "onmouseup=\"" + onmouseup.getCode(sessionData, context).jsEncode() + "\"";

            if (onmousewheel != null)
                ret += "onmousewheel=\"" + onmousewheel.getCode(sessionData, context).jsEncode() + "\"";

            if (onoffline != null)
                ret += "onoffline=\"" + onoffline.getCode(sessionData, context).jsEncode() + "\"";

            if (ononline != null)
                ret += "ononline=\"" + ononline.getCode(sessionData, context).jsEncode() + "\"";

            if (onopen != null)
                ret += "onopen=\"" + onopen.getCode(sessionData, context).jsEncode() + "\"";

            if (onpagehide != null)
                ret += "onpagehide=\"" + onpagehide.getCode(sessionData, context).jsEncode() + "\"";

            if (onpageshow != null)
                ret += "onpageshow=\"" + onpageshow.getCode(sessionData, context).jsEncode() + "\"";

            if (onpaste != null)
                ret += "onpaste=\"" + onpaste.getCode(sessionData, context).jsEncode() + "\"";

            if (onpause != null)
                ret += "onpause=\"" + onpause.getCode(sessionData, context).jsEncode() + "\"";

            if (onplay != null)
                ret += "onplay=\"" + onplay.getCode(sessionData, context).jsEncode() + "\"";

            if (onplaying != null)
                ret += "onplaying=\"" + onplaying.getCode(sessionData, context).jsEncode() + "\"";

            if (onpopstate != null)
                ret += "onpopstate=\"" + onpopstate.getCode(sessionData, context).jsEncode() + "\"";

            if (onprogress != null)
                ret += "onprogress=\"" + onprogress.getCode(sessionData, context).jsEncode() + "\"";

            if (onratechange != null)
                ret += "onratechange=\"" + onratechange.getCode(sessionData, context).jsEncode() + "\"";

            if (onreset != null)
                ret += "onreset=\"" + onreset.getCode(sessionData, context).jsEncode() + "\"";

            if (onresize != null)
                ret += "onresize=\"" + onresize.getCode(sessionData, context).jsEncode() + "\"";

            if (onscroll != null)
                ret += "onscroll=\"" + onscroll.getCode(sessionData, context).jsEncode() + "\"";

            if (onsearch != null)
                ret += "onsearch=\"" + onsearch.getCode(sessionData, context).jsEncode() + "\"";

            if (onseeked != null)
                ret += "onseeked=\"" + onseeked.getCode(sessionData, context).jsEncode() + "\"";

            if (onseeking != null)
                ret += "onseeking=\"" + onseeking.getCode(sessionData, context).jsEncode() + "\"";

            if (onselect != null)
                ret += "onselect=\"" + onselect.getCode(sessionData, context).jsEncode() + "\"";

            if (onshow != null)
                ret += "onshow=\"" + onshow.getCode(sessionData, context).jsEncode() + "\"";

            if (onstalled != null)
                ret += "onstalled=\"" + onstalled.getCode(sessionData, context).jsEncode() + "\"";

            if (onstorage != null)
                ret += "onstorage=\"" + onstorage.getCode(sessionData, context).jsEncode() + "\"";

            if (onsubmit != null)
                ret += "onsubmit=\"" + onsubmit.getCode(sessionData, context).jsEncode() + "\"";

            if (onsuspend != null)
                ret += "onsuspend=\"" + onsuspend.getCode(sessionData, context).jsEncode() + "\"";

            if (ontimeupdate != null)
                ret += "ontimeupdate=\"" + ontimeupdate.getCode(sessionData, context).jsEncode() + "\"";

            if (ontoggle != null)
                ret += "ontoggle=\"" + ontoggle.getCode(sessionData, context).jsEncode() + "\"";

            if (ontouchcancel != null)
                ret += "ontouchcancel=\"" + ontouchcancel.getCode(sessionData, context).jsEncode() + "\"";

            if (ontouchend != null)
                ret += "ontouchend=\"" + ontouchend.getCode(sessionData, context).jsEncode() + "\"";

            if (ontouchmove != null)
                ret += "ontouchmove=\"" + ontouchmove.getCode(sessionData, context).jsEncode() + "\"";

            if (ontouchstart != null)
                ret += "ontouchstart=\"" + ontouchstart.getCode(sessionData, context).jsEncode() + "\"";

            if (onunload != null)
                ret += "onunload=\"" + onunload.getCode(sessionData, context).jsEncode() + "\"";

            if (onvolumechange != null)
                ret += "onvolumechange=\"" + onvolumechange.getCode(sessionData, context).jsEncode() + "\"";

            if (onwaiting != null)
                ret += "onwaiting=\"" + onwaiting.getCode(sessionData, context).jsEncode() + "\"";

            if (onwheel != null)
                ret += "onwheel=\"" + onwheel.getCode(sessionData, context).jsEncode() + "\"";

            return ret;
        }
    }
    #endregion
}
