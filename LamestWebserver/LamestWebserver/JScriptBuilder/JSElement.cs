﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LamestWebserver.UI;

namespace LamestWebserver.JScriptBuilder
{
    /// <summary>
    /// A JavaScript powered HTML-Element
    /// </summary>
    public abstract class JSElement : HElement, IJSPiece
    {
        /// <summary>
        /// Additional Attributes added to the Element
        /// </summary>
        public string DescriptionTags = "";

        /// <summary>
        /// Constructs a new JSElement and sets it's ID to a HashValue
        /// </summary>
        protected JSElement()
        {
            ID = SessionContainer.GenerateHash();
        }

        /// <summary>
        /// Retrieves the Body of the currentDocument
        /// </summary>
        public static JSElementValue Body {
            get { return new JSElementValue("document.body"); }
        }

        /// <summary>
        /// Inserts this Element into the document body.
        /// </summary>
        /// <returns>A piece of JavaScript code</returns>
        public IJSValue CreateNew()
        {
            return new JSInstantFunction(new JSValue("document.body.insertAdjacentHTML(\"beforeend\", " + GetContent(SessionData.CurrentSession, CallingContext.Inner).Base64Encode() + ");")).DefineAndCall();
        }

        /// <summary>
        /// Inserts this Element into an Element with the specified ID.
        /// </summary>
        /// <param name="intoID">the ID of the Element</param>
        /// <returns>A piece of JavaScript code</returns>
        public IJSValue CreateNew(string intoID)
        {
            return new JSInstantFunction(new JSValue("document.getElementById(\"" + intoID + "\").insertAdjacentHTML(\"beforeend\", " + GetContent(SessionData.CurrentSession, CallingContext.Inner).Base64Encode() + ");")).DefineAndCall();
        }

        /// <inheritdoc />
        public string GetJsCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return "\"" + GetContent(sessionData, CallingContext.Inner).JSEncode() + "\"" + (context == CallingContext.Default ? ";" : " ");
        }

        /// <summary>
        /// Retrieves the default attributes for a HTML element
        /// </summary>
        /// <returns>the attributes as string</returns>
        protected string GetDefaultAttributes()
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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            return ret;
        }

        /// <summary>
        /// Retrieves the HTML-Text of this Element
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <param name="context">the current CallingContext</param>
        /// <returns></returns>
        public abstract string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default);

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            return GetContent(sessionData);
        }

        /// <summary>
        /// Retrieves
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static JSElementValue GetByID(string id)
        {
            return new JSElementValue(new JSFunctionCall("document.getElementById", new JSStringValue(id)));
        }
    }

    /// <summary>
    /// Just a wrapper to put the text given in the constructor into the final document
    /// </summary>
    public class JSPlainText : JSElement
    {
        /// <summary>
        /// The text add to the final output
        /// </summary>
        public string Contents;

        /// <summary>
        /// Creates a pseudo element containing the given contents
        /// </summary>
        /// <param name="Contents">the contents to add to the final output</param>
        public JSPlainText(string Contents)
        {
            this.Contents = Contents;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return Contents;
        }
    }

    /// <summary>
    /// A simple MessageBox
    /// </summary>
    public class JSMsgBox : JSElement
    {
        private readonly List<JSElement> _elements;
        private readonly bool _hasExitButton = true;

        /// <summary>
        /// Constructs a new MessageBox
        /// </summary>
        /// <param name="hasExitButton">Should there be an Exit-Button?</param>
        /// <param name="elements">The contained elements</param>
        public JSMsgBox(bool hasExitButton, params JSElement[] elements)
        {
            _hasExitButton = hasExitButton;
            _elements = elements.ToList();
        }

        /// <summary>
        /// Constructs a new MessageBox
        /// </summary>
        /// <param name="elements">The contained elements</param>
        public JSMsgBox(params JSElement[] elements)
        {
            _elements = elements.ToList();
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string _content = "";

            for (int i = 0; i < _elements.Count; i++)
            {
                _content += _elements[i].GetContent(sessionData, CallingContext.Default);
            }

            string ret = "<div id=\"" + GlobalID + "\" class=\"" + GlobalID + "\" style=\"display: block;position: fixed;top: 0;right: 0;left: 0;bottom: 0;width: 100%;height: 100%;z-index: 100;background-color: rgba(33, 33, 33, 0.75);margin: 0px\"><div class=\"" + GlobalInnerID + "\" id=\"" + GlobalInnerID + "\" style=\"display: block;position: relative;margin: auto;background-color: #fff;max-width: 600px;overflow: auto;margin-top: 8em;\">";

            if (_hasExitButton)
                ret += "<button onclick=\"javascript: { document.body.removeChild(document.getElementById('_lws_jsbuilder_msgbox_outer')); }\" class=\"_lws_jsbuilder_msgbox_exitbutton\"></button>";

            ret += _content;

            return ret + "</div></div>";
        }

        /// <summary>
        /// The Id of the MessageBox Background
        /// </summary>
        public const string GlobalID = "_lws_jsbuilder_msgbox_outer";

        /// <summary>
        /// The Id of the MessageBox Foreground
        /// </summary>
        public const string GlobalInnerID = "_lws_jsbuilder_msgbox_inner";
    }

    /// <summary>
    /// A HTML Button Element
    /// </summary>
    public class JSButton : JSInteractableElement
    {
        /// <summary>
        /// The type of the button
        /// </summary>
        public HButton.EButtonType buttonType = HButton.EButtonType.button;

        /// <summary>
        /// The text displayed on the button
        /// </summary>
        public string buttonText = "";

        /// <summary>
        /// Constructs a new JSButton element
        /// </summary>
        /// <param name="buttonText">the text displayed on the button</param>
        /// <param name="buttonType">the type of the button</param>
        public JSButton(string buttonText, HButton.EButtonType buttonType = HButton.EButtonType.button) : base()
        {
            this.buttonText = buttonText;
            this.buttonType = buttonType;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return "<button type='" + buttonType + "' " + GetDefaultAttributes() + getEventAttributes(sessionData, context) + ">" + HttpUtility.HtmlEncode(buttonText).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</button>";
        }
    }

    /// <summary>
    /// A HTML Text Element
    /// </summary>
    public class JSText : JSInteractableElement
    {
        private readonly string _content;

        /// <summary>
        /// Constructs a new JSText element with the given content
        /// </summary>
        /// <param name="content">the content of the Text-Element</param>
        public JSText(string content) { _content = content; }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return "<p " + GetDefaultAttributes() + getEventAttributes(sessionData, CallingContext.Default) + ">" + HttpUtility.HtmlEncode(_content).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</p>";
        }
    }

    /// <summary>
    /// A HTML Input Element
    /// </summary>
    public class JSInput : JSInteractableElement
    {
        /// <summary>
        /// The inputType of this Element
        /// </summary>
        protected HInput.EInputType inputType;
        
        /// <summary>
        /// The Value of this Element
        /// </summary>
        public string Value;

        /// <summary>
        /// Constructs a new JSInput Element
        /// </summary>
        /// <param name="type">the type of the element</param>
        /// <param name="name">the name of the element</param>
        /// <param name="value">the value of the element</param>
        public JSInput(HInput.EInputType type, string name, string value = "") : base()
        {
            inputType = type;
            Name = name;
            this.Value = value;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return "<input type='" + inputType + "' " + GetDefaultAttributes() + " value='" + HttpUtility.HtmlEncode(Value).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "' " + getEventAttributes(sessionData, CallingContext.Default) + "></input>";
        }

        /// <summary>
        /// Sends this elements name and value to a remote server.
        /// </summary>
        /// <param name="URL">the event to reach</param>
        public virtual JSDirectFunctionCall SendNameValueAsync(string URL)
        {
            if (URL.Contains('?') && URL[URL.Length - 1] != '?')
            {
                URL += "&name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else if (URL[URL.Length - 1] != '?')
            {
                URL += "?name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else
            {
                URL += "name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }

            switch (inputType)
            {
                case HInput.EInputType.checkbox:
                case HInput.EInputType.radio:
                    URL += HttpUtility.UrlEncode(Value) + "&checked=";
                    return new JSInstantFunction(new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); } xmlhttp.open(\"GET\",\"" + URL + "\" + " + GetByID(ID).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ".checked, true);xmlhttp.send();")).DefineAndCall();

                default:
                    return new JSInstantFunction(new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); } xmlhttp.open(\"GET\",\"" + URL + "\" + " + GetByID(ID).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ".value, true);xmlhttp.send();")).DefineAndCall();
            }
        }

        /// <summary>
        /// Sends this elements name and value to a remote server and sets the response as InnerHtml of a HTML element.
        /// </summary>
        /// <param name="element">the element which innerHTML you want to override</param>
        /// <param name="URL">the event to reach</param>
        /// <param name="executeOnComplete">the code to execute when the task has been completed</param>
        public virtual JSDirectFunctionCall SetInnerHTMLWithNameValueAsync(IJSValue element, string URL, params IJSPiece[] executeOnComplete)
        {
            if (URL.Contains('?') && URL[URL.Length - 1] != '?')
            {
                URL += "&name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else if (URL[URL.Length - 1] != '?')
            {
                URL += "?name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else
            {
                URL += "name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }

            switch (inputType)
            {
                case HInput.EInputType.checkbox:
                case HInput.EInputType.radio:
                    URL += HttpUtility.UrlEncode(Value) + "&checked=";
                    return new JSInstantFunction(new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + element.GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ".innerHTML=this.responseText;"
                        + ((Func<string>)(() => { string ret = ""; executeOnComplete.ToList().ForEach(piece => ret += piece.GetJsCode(SessionData.CurrentSession)); return ret; })).Invoke()
                        + " } }; xmlhttp.open(\"GET\",\"" + URL + "\" + " + GetByID(ID).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ".checked,true);xmlhttp.send();")).DefineAndCall();

                default:
                    return new JSInstantFunction(new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + element.GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ".innerHTML=this.responseText;"
                        + ((Func<string>)(() => { string ret = ""; executeOnComplete.ToList().ForEach(piece => ret += piece.GetJsCode(SessionData.CurrentSession)); return ret; })).Invoke()
                        + " } }; xmlhttp.open(\"GET\",\"" + URL + "\" + " + JSFunctionCall.EncodeURIComponent(GetByID(ID).Value).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ",true);xmlhttp.send();")).DefineAndCall();
            }
        }
    }

    /// <summary>
    /// A HTML Text-Area Element
    /// </summary>
    public class JSTextArea : JSInput
    {
        /// <summary>
        /// The Columns displayed
        /// </summary>
        public uint? cols;

        /// <summary>
        /// The Rows displayed
        /// </summary>
        public uint? rows;

        /// <summary>
        /// Constructs a new JSTextArea
        /// </summary>
        /// <param name="name">the name</param>
        /// <param name="value">the default value</param>
        /// <param name="cols">the columns displayed</param>
        /// <param name="rows">the rows displayed</param>
        public JSTextArea(string name, string value = "", uint? cols = null, uint? rows = null) : base(HInput.EInputType.text, name, value)
        {
            Value = value;
            Name = name;
            this.cols = cols;
            this.rows = rows;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "<textarea " + GetDefaultAttributes() + getEventAttributes(sessionData, CallingContext.Default);

            if (cols.HasValue)
                ret += "cols='" + cols.Value + "' ";

            if (rows.HasValue)
                ret += "rows='" + rows.Value + "' ";

            ret += ">" + HttpUtility.HtmlEncode(Value).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</textarea>";

            return ret;
        }
    }

    /// <summary>
    /// A HTML Drop-Down-Menu Element
    /// </summary>
    public class JSDropDownMenu : JSInput
    {
        private readonly Tuple<string, string>[] _options;

        /// <summary>
        /// The amount of entries displayed if not expanded
        /// </summary>
        public int size = 1;

        /// <summary>
        /// does the dropdownmenu allow multiple selections?
        /// </summary>
        public bool multipleSelectable = false;

        /// <summary>
        /// is the dropdownmenu disabled for the user?
        /// </summary>
        public bool disabled = false;

        /// <summary>
        /// the selectedIndexes
        /// </summary>
        public List<int> selectedIndexes = new List<int>() { 0 };

        /// <summary>
        /// Constructs a new DropDownMenu element
        /// </summary>
        /// <param name="name">the name of the element (for forms)</param>
        /// <param name="size">The amount of entries displayed if not expanded</param>
        /// <param name="multipleSelectable">does the dropdownmenu allow multiple selections?</param>
        /// <param name="TextValuePairsToDisplay">All possibly selectable items as a tuple (Text displayed for the user, Value presented to form)</param>
        public JSDropDownMenu(string name, int size, bool multipleSelectable, params Tuple<string, string>[] TextValuePairsToDisplay) : base(HInput.EInputType.text, name, "")
        {
            this.Name = name;
            this.size = size;
            this.multipleSelectable = multipleSelectable;
            this._options = TextValuePairsToDisplay;
        }

        /// <summary>
        /// Constructs a new DropDownMenu element
        /// </summary>
        /// <param name="name">the name of the element (for forms)</param>
        /// <param name="TextValuePairsToDisplay">All possibly selectable items as a tuple (Text displayed for the user, Value presented to form)</param>
        public JSDropDownMenu(string name, params Tuple<string, string>[] TextValuePairsToDisplay) : base(HInput.EInputType.text, name, "")
        {
            this.Name = name;
            this._options = TextValuePairsToDisplay;
        }

        /// <summary>
        /// Selects an item based on the value given to it.
        /// Unselects everything else if !multipleSelectable.
        /// DOES NOT THROW AN EXCEPTION IF NO MATCHING INDEX HAS BEEN FOUND!
        /// </summary>
        /// <param name="value">the value to look for</param>
        /// <returns>this element for inline use.</returns>
        public JSDropDownMenu SelectByValue(string value)
        {
            if (!multipleSelectable)
                selectedIndexes.Clear();

            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i].Item2 == value)
                {
                    this.selectedIndexes.Add(i);
                }
            }

            return this;
        }

        /// <summary>
        /// Selects an item based on the text given to it.
        /// Unselects everything else if !multipleSelectable.
        /// DOES NOT THROW AN EXCEPTION IF NO MATCHING INDEX HAS BEEN FOUND!
        /// </summary>
        /// <param name="text">the text to look for</param>
        /// <returns>this element for inline use.</returns>
        public JSDropDownMenu SelectByText(string text)
        {
            if (!multipleSelectable)
                selectedIndexes.Clear();

            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i].Item1 == text)
                {
                    this.selectedIndexes.Add(i);
                }
            }

            return this;
        }
        
        /// <inheritdoc />
        public override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "<select " + GetDefaultAttributes() + getEventAttributes(sessionData, CallingContext.Default);

            ret += "size=\"" + size + "\" ";

            if (multipleSelectable)
                ret += "multiple=\"multiple\" ";

            if (disabled)
                ret += "disabled=\"" + disabled + "\" ";

            ret += ">\n";

            if (_options != null)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    ret += "<option value=\"" + HttpUtility.UrlEncode(_options[i].Item2) + "\" ";

                    if (selectedIndexes.Contains(i))
                        ret += "selected=\"selected\" ";

                    ret += ">" + HttpUtility.HtmlEncode(_options[i].Item1).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</option>";
                }
            }

            ret += "\n</select>\n";

            return ret;
        }
        
        /// <summary>
        /// Sends this elements name and value to a remote server.
        /// </summary>
        /// <param name="URL">the event to reach</param>
        public override JSDirectFunctionCall SendNameValueAsync(string URL)
        {
            if (URL.Contains('?') && URL[URL.Length - 1] != '?')
            {
                URL += "&name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else if (URL[URL.Length - 1] != '?')
            {
                URL += "?name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else
            {
                URL += "name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }

            return new JSInstantFunction(new JSValue("var xmlhttp; var elem = " + GetByID(ID).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ";if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); } xmlhttp.open(\"GET\",\"" + URL + "\" + elem.selectedOptions[0].value + \"&all=\" + (() => {var c = \"\"; for(var i = 0; i < elem.selectedOptions.length; i++){c += elem.selectedOptions[i].value;if(i+1<elem.selectedOptions.length) c+= \";\"} return c;})(),true);xmlhttp.send();")).DefineAndCall();
        }

        /// <summary>
        /// Sends this elements name and value to a remote server and sets the response as InnerHtml of a HTML element.
        /// </summary>
        /// <param name="element">the element which innerHTML you want to override</param>
        /// <param name="URL">the event to reach</param>
        /// <param name="executeOnComplete">code to execute when the action is done</param>
        public override JSDirectFunctionCall SetInnerHTMLWithNameValueAsync(IJSValue element, string URL, params IJSPiece[] executeOnComplete)
        {
            if (URL.Contains('?') && URL[URL.Length - 1] != '?')
            {
                URL += "&name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else if (URL[URL.Length - 1] != '?')
            {
                URL += "?name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }
            else
            {
                URL += "name=" + HttpUtility.UrlEncode(Name) + "&value=";
            }

            return new JSInstantFunction(new JSValue("var xmlhttp; var elem = " + GetByID(ID).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ";if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + element.GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ".innerHTML=this.responseText;"
                + ((Func<string>)(() => { string ret = ""; executeOnComplete.ToList().ForEach(piece => ret += piece.GetJsCode(SessionData.CurrentSession)); return ret; })).Invoke()
                + " } }; xmlhttp.open(\"GET\",\"" + URL + "\" + " + JSFunctionCall.EncodeURIComponent(new JSValue("elem.selectedOptions[0].value")).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + "+ \"&all=\" + (() => {var c = \"\"; for(var i = 0; i < elem.selectedOptions.length; i++){c += " + JSFunctionCall.EncodeURIComponent(new JSValue("elem.selectedOptions[i].value")).GetJsCode(SessionData.CurrentSession, CallingContext.Inner) + ";if(i+1<elem.selectedOptions.length) c+= \";\"} return c;})(),true);xmlhttp.send();")).DefineAndCall();
        }
    }

    #region ARE YOU READY FOR THE WEB? THE TRUE WEB? FEEL FREE TO ENTER?

    /// <summary>
    /// See: http://www.w3schools.com/jsref/dom_obj_event.asp
    /// </summary>
    public abstract class JSInteractableElement : JSElement
    {
        /// <inheritdoc />
        public abstract override string GetContent(SessionData sessionData, CallingContext context = CallingContext.Default);

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
        /// <param name="context">the current Calling Context</param>
        /// <returns>the event attributes as string</returns>
        public string getEventAttributes(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            // #AREYOUREADYFORTHEWEB?

            string ret = " ";

            if (onabort != null)
                ret += "onabort=" + onabort.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onafterprint != null)
                ret += "onafterprint=" + onafterprint.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onbeforeprint != null)
                ret += "onbeforeprint=" + onbeforeprint.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onbeforeunload != null)
                ret += "onbeforeunload=" + onbeforeunload.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onblur != null)
                ret += "onblur=" + onblur.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oncanplay != null)
                ret += "oncanplay=" + oncanplay.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oncanplaythrough != null)
                ret += "oncanplaythrough=" + oncanplaythrough.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onchange != null)
                ret += "onchange=" + onchange.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onclick != null)
                ret += "onclick=" + onclick.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oncontextmenu != null)
                ret += "oncontextmenu=" + oncontextmenu.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oncopy != null)
                ret += "oncopy=" + oncopy.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oncut != null)
                ret += "oncut=" + oncut.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondblclick != null)
                ret += "ondblclick=" + ondblclick.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondrag != null)
                ret += "ondrag=" + ondrag.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondragend != null)
                ret += "ondragend=" + ondragend.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondragenter != null)
                ret += "ondragenter=" + ondragenter.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondragleave != null)
                ret += "ondragleave=" + ondragleave.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondragover != null)
                ret += "ondragover=" + ondragover.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondragstart != null)
                ret += "ondragstart=" + ondragstart.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondrop != null)
                ret += "ondrop=" + ondrop.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ondurationchange != null)
                ret += "ondurationchange=" + ondurationchange.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onemptied != null)
                ret += "onemptied=" + onemptied.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onended != null)
                ret += "onended=" + onended.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onerror != null)
                ret += "onerror=" + onerror.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onfocus != null)
                ret += "onfocus=" + onfocus.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onfocusin != null)
                ret += "onfocusin=" + onfocusin.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onfocusout != null)
                ret += "onfocusout=" + onfocusout.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onhashchange != null)
                ret += "onhashchange=" + onhashchange.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oninput != null)
                ret += "oninput=" + oninput.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (oninvalid != null)
                ret += "oninvalid=" + oninvalid.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onkeydown != null)
                ret += "onkeydown=" + onkeydown.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onkeypress != null)
                ret += "onkeypress=" + onkeypress.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onkeyup != null)
                ret += "onkeyup=" + onkeyup.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onload != null)
                ret += "onload=" + onload.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onloadeddata != null)
                ret += "onloadeddata=" + onloadeddata.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onloadedmetadata != null)
                ret += "onloadedmetadata=" + onloadedmetadata.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onloadstart != null)
                ret += "onloadstart=" + onloadstart.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmessage != null)
                ret += "onmessage=" + onmessage.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmousedown != null)
                ret += "onmousedown=" + onmousedown.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmouseenter != null)
                ret += "onmouseenter=" + onmouseenter.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmouseleave != null)
                ret += "onmouseleave=" + onmouseleave.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmousemove != null)
                ret += "onmousemove=" + onmousemove.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmouseout != null)
                ret += "onmouseout=" + onmouseout.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmouseover != null)
                ret += "onmouseover=" + onmouseover.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmouseup != null)
                ret += "onmouseup=" + onmouseup.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onmousewheel != null)
                ret += "onmousewheel=" + onmousewheel.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onoffline != null)
                ret += "onoffline=" + onoffline.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ononline != null)
                ret += "ononline=" + ononline.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onopen != null)
                ret += "onopen=" + onopen.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onpagehide != null)
                ret += "onpagehide=" + onpagehide.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onpageshow != null)
                ret += "onpageshow=" + onpageshow.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onpaste != null)
                ret += "onpaste=" + onpaste.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onpause != null)
                ret += "onpause=" + onpause.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onplay != null)
                ret += "onplay=" + onplay.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onplaying != null)
                ret += "onplaying=" + onplaying.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onpopstate != null)
                ret += "onpopstate=" + onpopstate.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onprogress != null)
                ret += "onprogress=" + onprogress.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onratechange != null)
                ret += "onratechange=" + onratechange.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onreset != null)
                ret += "onreset=" + onreset.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onresize != null)
                ret += "onresize=" + onresize.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onscroll != null)
                ret += "onscroll=" + onscroll.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onsearch != null)
                ret += "onsearch=" + onsearch.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onseeked != null)
                ret += "onseeked=" + onseeked.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onseeking != null)
                ret += "onseeking=" + onseeking.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onselect != null)
                ret += "onselect=" + onselect.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onshow != null)
                ret += "onshow=" + onshow.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onstalled != null)
                ret += "onstalled=" + onstalled.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onstorage != null)
                ret += "onstorage=" + onstorage.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onsubmit != null)
                ret += "onsubmit=" + onsubmit.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onsuspend != null)
                ret += "onsuspend=" + onsuspend.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ontimeupdate != null)
                ret += "ontimeupdate=" + ontimeupdate.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ontoggle != null)
                ret += "ontoggle=" + ontoggle.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ontouchcancel != null)
                ret += "ontouchcancel=" + ontouchcancel.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ontouchend != null)
                ret += "ontouchend=" + ontouchend.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ontouchmove != null)
                ret += "ontouchmove=" + ontouchmove.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (ontouchstart != null)
                ret += "ontouchstart=" + ontouchstart.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onunload != null)
                ret += "onunload=" + onunload.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onvolumechange != null)
                ret += "onvolumechange=" + onvolumechange.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onwaiting != null)
                ret += "onwaiting=" + onwaiting.GetJsCode(sessionData, context).EvalBase64() + " ";

            if (onwheel != null)
                ret += "onwheel=" + onwheel.GetJsCode(sessionData, context).EvalBase64() + " ";

            return ret;
        }
    }
    #endregion
}
