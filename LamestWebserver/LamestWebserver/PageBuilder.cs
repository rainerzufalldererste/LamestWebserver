using System;
using System.Collections.Generic;
using System.Linq;

namespace LamestWebserver
{
    /// <summary>
    /// A Container for a complete WebPage with html, head and body tags.
    /// Can also be used as direct response if inherited well.
    /// </summary>
    public class PageBuilder : HContainer, IURLIdentifyable
    {
        private Func<SessionData, bool> conditionalCode;
        private bool condition = false;
        private string referealURL;

        /// <summary>
        /// a function pointer to the executed method on getContent(SessionData sessionData)
        /// </summary>
        public Func<SessionData, string> getContentMethod;

        /// <summary>
        /// the title of this page
        /// </summary>
        public string title;

        /// <summary>
        /// the URL at which this page is / will be available at
        /// </summary>
        public string URL { get; protected set; }

        /// <summary>
        /// Path to the stylesheets. Prefer strings. Else: toString() will be used
        /// </summary>
        public List<object> stylesheetLinks = new List<object>();

        /// <summary>
        /// javascript code directly bound into the page code
        /// </summary>
        public List<string> scripts = new List<string>();

        /// <summary>
        /// path to javascript code files
        /// </summary>
        public List<string> scriptLinks = new List<string>();

        /// <summary>
        /// additional lines added to the "head" segment of the page
        /// </summary>
        public string additionalHeadArguments;

        /// <summary>
        /// The icon to display
        /// </summary>
        public string favicon = null;

        /// <summary>
        /// CSS code directly bound into the page code
        /// </summary>
        public string stylesheetCode;

        /// <summary>
        /// Creates a new PageBuilder and registers it at the server for a specified url
        /// </summary>
        /// <param name="title">The window title</param>
        /// <param name="URL">the URL at which to register this page</param>
        public PageBuilder(string title, string URL)
        {
            this.title = title;
            this.URL = URL;
            getContentMethod = buildContent;

            register();
        }

        /// <summary>
        /// Creates a page builder and registers it as the server for a specified URL. If the conditionalCode returns false the page will not be parsed and the user will be refered to the referalURL
        /// </summary>
        /// <param name="title">The window title</param>
        /// <param name="URL">the URL at which to register this page</param>
        /// <param name="referalURL">the URL at which to refer if the conditionalCode returns false</param>
        /// <param name="conditionalCode">the conditionalCode</param>
        public PageBuilder(string title, string URL, string referalURL, Func<SessionData, bool> conditionalCode) : this(title, URL)
        {
            this.condition = true;
            this.conditionalCode = conditionalCode;
            this.referealURL = referalURL;
        }

        /// <summary>
        /// Creates a new PageBuilder, but does _NOT_ register it at the server for a specified url
        /// </summary>
        /// <param name="title"></param>
        public PageBuilder(string title)
        {
            this.title = title;
            getContentMethod = buildContent;
        }

        /// <summary>
        /// The method which is called to parse this element to string
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the contents as string</returns>
        protected string buildContent(SessionData sessionData)
        {
            if (condition && !conditionalCode(sessionData))
                return InstantPageResponse.generateRedirectCode(referealURL, sessionData);

            string ret = "<html> <head> <title>" + title + "</title>";

            if (!string.IsNullOrWhiteSpace(favicon))
                ret += "<link rel=\"shortcut icon\" href='" + favicon + "'> <link rel=\"icon\" sizes=\"any\" mask=\"\" href=" + favicon + ">";

            for (int i = 0; i < stylesheetLinks.Count; i++)
            {
                ret += "<link rel=\"stylesheet\" href='" + stylesheetLinks[i] + "'>";
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                ret += "<script type=\"text/javascript\">" + scripts[i] + "</script>";
            }

            for (int i = 0; i < scriptLinks.Count; i++)
            {
                ret += "<script type=\"text/javascript\" src=\"" + scriptLinks[i] + "\"></script>";
            }

            if (!string.IsNullOrWhiteSpace(stylesheetCode))
                ret += "<style type=\"text/css\">" + stylesheetCode + "</style>";

            if (!string.IsNullOrWhiteSpace(additionalHeadArguments))
                ret += additionalHeadArguments + " ";

            ret += "</head> <body ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style='" + Style + "' ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (!string.IsNullOrWhiteSpace(text))
                ret += System.Web.HttpUtility.HtmlEncode(text);

            for (int i = 0; i < base.elements.Count; i++)
            {
                ret += base.elements[i].getContent(sessionData);
            }

            ret += "</body> </html>";

            return ret;
        }

        /// <summary>
        /// The method used to grab contents as string to be registered as page for the server.
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the contents as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret;

            try
            {
                ret = getContentMethod.Invoke(sessionData);
            }
            catch (Exception e)
            {
                ret = Master.getErrorMsg("Exception in PageBuilder '" + URL + "'", "<b>An Error occured while processing the output</b><br>" + e.ToString());
            }

            return ret;
        }

        private void register()
        {
            Master.addFuntionToServer(URL, getContent);
        }

        /// <summary>
        /// via this method you can "unregister" this pages url (if this pageBuilder is registered) at the server.
        /// </summary>
        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }
    }

    /// <summary>
    /// A HTML Element
    /// </summary>
    public abstract class HElement
    {
        /// <summary>
        /// the ID of this element
        /// </summary>
        public string ID = "";

        /// <summary>
        /// the Name of this element
        /// </summary>
        public string Name = "";

        /// <summary>
        /// the class of this element
        /// </summary>
        public string Class = "";

        /// <summary>
        /// the style attribute of this element
        /// </summary>
        public string Style = "";

        /// <summary>
        /// the mouseover text and title attribute of this element
        /// </summary>
        public string Title = "";

        /// <summary>
        /// the method used to parse the element to string correctly
        /// </summary>
        /// <param name="sessionData">sessionData of the currentUser</param>
        /// <returns></returns>
        public abstract string getContent(SessionData sessionData);

        /// <summary>
        /// FISHY FISHY FISHY FISH, TASE A PIECE OF WISHY DISH
        /// </summary>
        /// <returns>element getContent(sessionData)</returns>
        public static string operator * (HElement element, SessionData sessionData)
        {
            return element.getContent(sessionData);
        }
        
        /// <summary>
        /// Parses this element to string
        /// </summary>
        /// <returns>this element as string</returns>
        public override string ToString()
        {
            return this.getContent(SessionData.currentSessionData);
        }

        /// <summary>
        /// casts a string to a HPlainText element
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator HElement(string s)
        {
            return new HPlainText(s);
        }

        /// <summary>
        /// Parses an element to string
        /// </summary>
        /// <param name="e">the element</param>
        public static explicit operator string(HElement e)
        {
            return e.ToString();
        }
    }

    /// <summary>
    /// A br element used for line breaks in HTML
    /// </summary>
    public class HNewLine : HElement
    {
        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            return "<br>";
        }
    }

    /// <summary>
    /// A hr element used to display a hoizontal line
    /// </summary>
    public class HLine : HElement
    {
        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<hr ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            return ret + ">";
        }
    }

    /// <summary>
    /// The contents of this element will directly be copied into the final html document.
    /// </summary>
    public class HPlainText : HElement
    {
        /// <summary>
        /// The text to copy to the HTML document
        /// </summary>
        public string text;

        /// <summary>
        /// Constructs a new By-Copy-Element. The contents will only be copied into the final HTML code.
        /// </summary>
        /// <param name="text">the text to copy into the final HTML code</param>
        public HPlainText(string text = "")
        {
            this.text = text;
        }

        /// <summary>
        /// returns the given text
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            return text;
        }
    }

    /// <summary>
    /// Represents a "a" element used for links
    /// </summary>
    public class HLink : HElement
    {
        string href, onclick, text;

        /// <summary>
        /// Additional attributes added to this tag
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Creates a new Link Element
        /// </summary>
        /// <param name="text">The Text of the Link</param>
        /// <param name="href">The URL this link points to</param>
        /// <param name="onclick">the Javasctipt action executed when clicking on this link</param>
        public HLink(string text = "", string href = "", string onclick = "")
        {
            this.text = text;
            this.href = href;
            this.onclick = onclick;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<a ";

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.POST)
            {
                if (href.Length > 0)
                {
                    if (href[0] == '#')
                        ret += "href='" + href + "' ";
                    else
                    {
                        ret += "href='#' ";

                        string hash = SessionContainer.generateUnusedHash();
                        string add = ";var f_"
                            + hash + "=document.createElement('form');f_"
                            + hash + ".setAttribute('method','POST');f_"
                            + hash + ".setAttribute('action','"
                                + href + "');f_"
                            + hash + ".setAttribute('enctype','application/x-www-form-urlencoded');var i_"
                            + hash + "=document.createElement('input');i_"
                            + hash + ".setAttribute('type','hidden');i_"
                            + hash + ".setAttribute('name','ssid');i_"
                            + hash + ".setAttribute('value','"
                                + sessionData.ssid + "');f_"
                            + hash + ".appendChild(i_"
                            + hash + ");document.body.appendChild(f_"
                            + hash + ");f_"
                            + hash + ".submit();document.body.remove(f_"
                            + hash + ");";

                        ret += " onclick=\"" + onclick + add + "\" ";
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(onclick))
                        ret += "onclick='" + onclick + "' ";
                }
            }
            else if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                if (!string.IsNullOrWhiteSpace(href))
                    ret += "href='" + href + "' ";

                if (!string.IsNullOrWhiteSpace(onclick))
                    ret += "onclick='" + onclick + "' ";
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "SessionIdTransmissionType is invalid or not supported in " + this.GetType().ToString() + ".");
            }

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (!string.IsNullOrWhiteSpace(text))
                ret += System.Web.HttpUtility.HtmlEncode(text);

            ret += "</a>";

            return ret;
        }
    }

    /// <summary>
    /// A img element representing an image in html
    /// </summary>
    public class HImage : HElement
    {
        string source;

        /// <summary>
        /// Additional attributes added to this tag
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Creates an Image
        /// </summary>
        /// <param name="source">the URL where the image is located at</param>
        public HImage(string source = "")
        {
            this.source = source;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<img ";

            if (!string.IsNullOrWhiteSpace(source))
                ret += "src='" + source + "' ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            return ret;
        }
    }

    /// <summary>
    /// A "p" tag, representing a textblock
    /// </summary>
    public class HText : HElement
    {
        /// <summary>
        /// The Text displayed in this textblock
        /// </summary>
        public string text;

        /// <summary>
        /// Additional attributes mentioned in the "p" tag
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Constructs a TextBlock
        /// </summary>
        /// <param name="text">the Text displayed</param>
        public HText(string text = "")
        {
            this.text = text;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<p ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">" + System.Web.HttpUtility.HtmlEncode(text) + "</p>";

            return ret;
        }
    }

    /// <summary>
    /// A h(1-6) tag in html (h1 by default) representing a Headline
    /// </summary>
    public class HHeadline : HElement
    {
        /// <summary>
        /// The Text displayed in this Headline
        /// </summary>
        public string text;

        /// <summary>
        /// Additional attributes added to this element
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// The level of this headline (1-6)
        /// </summary>
        private int level;

        /// <summary>
        /// Constructs a new Headline
        /// </summary>
        /// <param name="text">the text of this headline</param>
        /// <param name="level">the level of this headline</param>
        public HHeadline(string text = "", int level = 1)
        {
            this.text = text;
            this.level = level;

            if (level > 6 || level < 1)
                throw new Exception("the level has to be between 1 and 6!");
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<h" + level + " ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">" + System.Web.HttpUtility.HtmlEncode(text) + "</h" + level + ">";

            return ret;
        }
    }

    /// <summary>
    /// A input tag representing all kinds of Input Elements
    /// </summary>
    public class HInput : HElement
    {
        /// <summary>
        /// The Type of the input element
        /// </summary>
        public EInputType inputType;

        /// <summary>
        /// The Value of the input element
        /// </summary>
        public string value;

        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Constructs a new Input Element
        /// </summary>
        /// <param name="inputType">the type of the input element</param>
        /// <param name="name">the Name of the HTML element</param>
        /// <param name="value">the predefined value of this input element</param>
        public HInput(EInputType inputType, string name, string value = "")
        {
            this.inputType = inputType;
            this.Name = name;
            this.value = value;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<input ";

            ret += "type='" + (inputType != EInputType.datetime_local ? inputType.ToString() : "datetime-local") + "' ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(value))
                ret += "value='" + value + "' ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            return ret;
        }

        /// <summary>
        /// Contains all kinds of valid HTML Input Elements
        /// </summary>
        public enum EInputType : byte
        {
            /// <summary>
            /// A button
            /// </summary>
            button,
            
            /// <summary>
            /// A checkbox
            /// </summary>
            checkbox,
            
            /// <summary>
            /// A ColorPicker
            /// </summary>
            color,
            
            /// <summary>
            /// A Date Input
            /// </summary>
            date,
            
            /// <summary>
            /// A date and time input
            /// </summary>
            datetime,
            
            /// <summary>
            /// A date and time input for local time
            /// </summary>
            datetime_local,
            
            /// <summary>
            /// An Email Input
            /// </summary>
            email,
            
            /// <summary>
            /// A file selector
            /// </summary>
            file,
            
            /// <summary>
            /// A hidden name-value-pair
            /// </summary>
            hidden,
            
            /// <summary>
            /// An image
            /// </summary>
            image,
            
            /// <summary>
            /// A month selector
            /// </summary>
            month,
            
            /// <summary>
            /// A numeric input
            /// </summary>
            number,
            
            /// <summary>
            /// a password input (not displaying the contents entered as text)
            /// </summary>
            password,
            
            /// <summary>
            /// A radio button
            /// </summary>
            radio,
            
            /// <summary>
            /// An input for values within a given range
            /// </summary>
            range,
            
            /// <summary>
            /// A reset button
            /// </summary>
            reset,
            
            /// <summary>
            /// A search element
            /// </summary>
            search,
            
            /// <summary>
            /// A submit button
            /// </summary>
            submit,
            
            /// <summary>
            /// A tel input
            /// </summary>
            tel,
            
            /// <summary>
            /// A single line textfield (use HTextArea or JSTextArea for multiline Textfields)
            /// </summary>
            text,
            
            /// <summary>
            /// A Time input
            /// </summary>
            time,
            
            /// <summary>
            /// A url input
            /// </summary>
            url,

            /// <summary>
            /// A week input
            /// </summary>
            week,
        }
    }

    /// <summary>
    /// A div element representing a container
    /// </summary>
    public class HContainer : HElement
    {
        /// <summary>
        /// A list of all contained elements
        /// </summary>
        public List<HElement> elements = new List<HElement>();

        /// <summary>
        /// The text contained in this element
        /// </summary>
        public string text;

        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Adds an element to the element list
        /// </summary>
        /// <param name="element">the element</param>
        public void addElement(HElement element)
        {
            elements.Add(element);
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<div ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (!string.IsNullOrWhiteSpace(text))
                ret += System.Web.HttpUtility.HtmlEncode(text);

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData);
            }

            ret += "</div>";

            return ret;
        }

        /// <summary>
        /// Adds a bunch of elements to the element list
        /// </summary>
        /// <param name="list">a list of elements</param>
        public void addElements(List<HElement> list)
        {
            for(int i = 0; i < list.Count; i++)
            {
                addElement(list[i]);
            }
        }

        /// <summary>
        /// adds a bunch of elements to the elementlist
        /// </summary>
        /// <param name="list">a few elements</param>
        public void addElements(params HElement[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                addElement(list[i]);
            }
        }
    }

    /// <summary>
    /// A form element used for sending contents via POST to the server
    /// </summary>
    public class HForm : HContainer
    {
        /// <summary>
        /// The URL which will be called when submitting this form
        /// </summary>
        public string action;
        private bool fixedAction;
        private string redirectTRUE, redirectFALSE;
        Func<SessionData, bool> conditionalCode;

        /// <summary>
        /// Constructs a new Form pointing to the given action when submitted
        /// </summary>
        /// <param name="action">the URL to load when submitted</param>
        public HForm(string action)
        {
            this.action = action;
            fixedAction = true;
        }

        /// <summary>
        /// redirects if the conditional code returns true and executes other code if the conditional code returns false
        /// </summary>
        public HForm(string redirectURLifTRUE, string redirectURLifFALSE, Func<SessionData, bool> conditionalCode)
        {
            fixedAction = false;
            redirectTRUE = redirectURLifTRUE;
            redirectFALSE = redirectURLifFALSE;
            this.conditionalCode = conditionalCode;
        }

        /// <summary>
        /// creates a form containing a few values which are added to elements. It can also contain a submit button.
        /// </summary>
        public HForm(string action, bool addSubmitButton, string buttontext = "", params Tuple<string, string>[] values)
        {
            fixedAction = true;
            this.action = action;

            for (int i = 0; i < values.Length; i++)
            {
                elements.Add(new HInput(HInput.EInputType.hidden, values[i].Item1, values[i].Item2));
            }

            if (addSubmitButton)
                elements.Add(new HButton(buttontext, HButton.EButtonType.submit));
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<form ";

            if (fixedAction)
            {
                if (!string.IsNullOrWhiteSpace(action))
                    ret += "action='" + action + "' ";
            }
            else
            {
                ret += "action='" + InstantPageResponse.addOneTimeConditionalRedirect(redirectTRUE, redirectFALSE, true, conditionalCode) + "' ";
            }

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags + " ";

            ret += "method='POST' >";

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.POST)
            {
                ret += "<input type='hidden' name='ssid' value='" + sessionData.ssid + "'>";
            }

            if (!string.IsNullOrWhiteSpace(text))
                ret += System.Web.HttpUtility.HtmlEncode(text);

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData);
            }

            ret += "</form>";

            return ret;
        }
    }

    /// <summary>
    /// A button tag representing a button
    /// </summary>
    public class HButton : HContainer
    {
        string href, onclick;
        EButtonType type;

        /// <summary>
        /// Creates a button. SUBMIT BUTTONS SHOULDN'T HAVE A HREF!
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <param name="href">SUBMIT BUTTONS SHOULDN'T HAVE A HREF!</param>
        /// <param name="onclick"></param>
        public HButton(string text, EButtonType type = EButtonType.button, string href = "", string onclick = "")
        {
            this.text = text;
            this.href = href;
            this.onclick = onclick;
            this.type = type;
        }

        /// <summary>
        /// Creates a button.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="href">SUBMIT BUTTONS SHOULDN'T HAVE A HREF!</param>
        /// <param name="onclick"></param>
        public HButton(string text, string href = "", string onclick = "")
        {
            this.text = text;
            this.href = href;
            this.onclick = onclick;
            this.type = EButtonType.button;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<button type='" + type + "' ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.POST)
            {
                if (href.Length > 0 && type != EButtonType.submit)
                {
                    if (href[0] == '#')
                        ret += "href='" + href + "' ";
                    else
                        ret += "href='#' ";

                    string hash = SessionContainer.generateUnusedHash();
                    string add = ";var f_"
                        + hash + "=document.createElement('form');f_"
                        + hash + ".setAttribute('method','POST');f_"
                        + hash + ".setAttribute('action','"
                            + href + "');f_"
                        + hash + ".setAttribute('enctype','application/x-www-form-urlencoded');var i_"
                        + hash + "=document.createElement('input');i_"
                        + hash + ".setAttribute('type','hidden');i_"
                        + hash + ".setAttribute('name','ssid');i_"
                        + hash + ".setAttribute('value','"
                            + sessionData.ssid + "');f_"
                        + hash + ".appendChild(i_"
                        + hash + ");document.body.appendChild(f_"
                        + hash + ");f_"
                        + hash + ".submit();document.body.remove(f_"
                        + hash + ");";

                    ret += " onclick=\"" + onclick + add + "\"";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(onclick))
                        ret += "onclick='" + onclick + "' ";
                }
            }
            else if(SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                if (!string.IsNullOrWhiteSpace(onclick))
                {
                    ret += "onclick=\"" + onclick + ";";

                    if (!string.IsNullOrWhiteSpace(href) && type != EButtonType.submit)
                        ret += "location.href='" + href + "'\" ";
                    else
                        ret += "\" ";
                }
                else if (!string.IsNullOrWhiteSpace(href) && type != EButtonType.submit)
                {
                    ret += "onclick=\"location.href='" + href + "'\" ";
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "SessionIdTransmissionType is invalid or not supported in " + this.GetType().ToString() + ".");
            }

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags + " ";

            ret += ">";

            if (!string.IsNullOrWhiteSpace(text))
                ret += System.Web.HttpUtility.HtmlEncode(text);

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData);
            }

            ret += "</button>";

            return ret;
        }
        
        /// <summary>
        /// The type of a button
        /// </summary>
        public enum EButtonType : byte
        {
            /// <summary>
            /// A button which is only a button
            /// </summary>
            button,
            /// <summary>
            /// A button which resets the form it lives in
            /// </summary>
            reset,
            /// <summary>
            /// A button which submits the form it lives in
            /// </summary>
            submit
        }
    }

    /// <summary>
    /// a select element representing a DropDownMenu
    /// </summary>
    public class HDropDownMenu : HElement
    {
        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string descriptionTags;

        private Tuple<string, string>[] options;

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
        public HDropDownMenu(string name, int size, bool multipleSelectable, params Tuple<string, string>[] TextValuePairsToDisplay)
        {
            this.Name = name;
            this.size = size;
            this.multipleSelectable = multipleSelectable;
            this.options = TextValuePairsToDisplay;
        }

        /// <summary>
        /// Constructs a new DropDownMenu element
        /// </summary>
        /// <param name="name">the name of the element (for forms)</param>
        /// <param name="TextValuePairsToDisplay">All possibly selectable items as a tuple (Text displayed for the user, Value presented to form)</param>
        public HDropDownMenu(string name, params Tuple<string, string>[] TextValuePairsToDisplay)
        {
            this.Name = name;
            this.options = TextValuePairsToDisplay;
        }

        /// <summary>
        /// Selects an item based on the value given to it.
        /// Unselects everything else if !multipleSelectable.
        /// DOES NOT THROW AN EXCEPTION IF NO MATCHING INDEX HAS BEEN FOUND!
        /// </summary>
        /// <param name="value">the value to look for</param>
        /// <returns>this element for inline use.</returns>
        public HDropDownMenu SelectByValue(string value)
        {
            if (!multipleSelectable)
                selectedIndexes.Clear();

            for (int i = 0; i < options.Length; i++)
            {
                if(options[i].Item2 == value)
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
        public HDropDownMenu SelectByText(string text)
        {
            if (!multipleSelectable)
                selectedIndexes.Clear();

            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].Item1 == text)
                {
                    this.selectedIndexes.Add(i);
                }
            }

            return this;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<select ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            ret += "size=\"" + size + "\" ";

            if (multipleSelectable)
                ret += "multiple=\"multiple\" ";

            if (disabled)
                ret += "disabled=\"" + disabled + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if(options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    ret += "<option value=\"" + System.Web.HttpUtility.UrlEncode(options[i].Item2) + "\" ";

                    if (selectedIndexes.Contains(i))
                        ret += "selected=\"selected\" ";

                    ret += ">" + System.Web.HttpUtility.HtmlEncode(options[i].Item1) + "</option>";
                }
            }

            ret += "</select>";

            return ret;
        }
    }

    /// <summary>
    /// A ol or ul tag representing an ordered or unordered list
    /// </summary>
    public class HList : HContainer
    {
        private EListType listType;

        /// <summary>
        /// Constructs a new List Element
        /// </summary>
        /// <param name="listType">the type of the list</param>
        public HList(EListType listType)
        {
            this.listType = listType;
        }

        /// <summary>
        /// Constructs a new List Element
        /// </summary>
        /// <param name="listType">the type of the list</param>
        /// <param name="input">the contents of the list</param>
        public HList(EListType listType, IEnumerable<string> input) : this(listType)
        {
            List<HElement> data = new List<HElement>();

            foreach(string s in input)
            {
                data.Add(s.toHElement());
            }

            this.elements = data;
        }

        /// <summary>
        /// Constructs a new List Element
        /// </summary>
        /// <param name="listType">the type of the list</param>
        /// <param name="elements">the contents of the list</param>
        public HList(EListType listType, params HElement[] elements) : this(listType)
        {
            this.elements = elements.ToList();
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<" + (listType == EListType.OrderedList ? "ol" : "ul") + " ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (!string.IsNullOrWhiteSpace(text))
                ret += System.Web.HttpUtility.HtmlEncode(text);

            for (int i = 0; i < elements.Count; i++)
            {
                ret += "<li>" + elements[i].getContent(sessionData) + "</li>";
            }

            ret += "</" + (listType == EListType.OrderedList ? "ol" : "ul") + ">";

            return ret;
        }

        /// <summary>
        /// The type of the list
        /// </summary>
        public enum EListType : byte
        {
            /// <summary>
            /// A numerically ordered list
            /// </summary>
            OrderedList,
            /// <summary>
            /// A unordered list
            /// </summary>
            UnorderedList
        }
    }

    /// <summary>
    /// A table Element representing a table
    /// </summary>
    public class HTable : HElement
    {
        private List<List<HElement>> elements;
        private IEnumerable<object>[] data;

        /// <summary>
        /// Additional attributes to be added to this node
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Constructs a new Table containing the given elements
        /// </summary>
        /// <param name="elements">the contained elements</param>
        public HTable(List<List<HElement>> elements)
        {
            this.elements = elements;
        }

        /// <summary>
        /// Constructs a new Table containing the given data
        /// </summary>
        /// <param name="data">the contents of this table</param>
        public HTable(params IEnumerable<object>[] data)
        {
            this.data = data;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<table ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (elements != null)
            {
                foreach (ICollection<HElement> outer in elements)
                {
                    ret += "<tr>";

                    foreach (HElement element in outer)
                    {
                        ret += "<td>" + element.getContent(sessionData) + "</td>";
                    }

                    ret += "</tr>";
                }
            }
            else
            {
                foreach (IEnumerable<object> outer in data)
                {
                    ret += "<tr>";

                    foreach (object obj in outer)
                    {
                        ret += "<td>" + obj + "</td>";
                    }

                    ret += "</tr>";
                }
            }

            ret += "</table>";

            return ret;
        }
    }

    /// <summary>
    /// Represents a custom tag
    /// </summary>
    public class HTag : HContainer
    {
        /// <summary>
        /// if false, the element won't have a start and end tag but will only consist of a single tag (like img)
        /// </summary>
        public bool hasContent;

        /// <summary>
        /// the name of the tag
        /// </summary>
        public string tagName;

        /// <summary>
        /// Constructs a new custom tag
        /// </summary>
        /// <param name="tagName">the name of the custom tag</param>
        /// <param name="descriptionTags">Additional attributs</param>
        /// <param name="hasContent">if false, the element won't have a start and end tag but will only consist of a single tag (like img)</param>
        /// <param name="text">the text displayed in the content of this element</param>
        public HTag(string tagName, string descriptionTags, bool hasContent = false, string text = "")
        {
            this.tagName = tagName;
            this.descriptionTags = descriptionTags;
            this.hasContent = hasContent;
            this.text = text;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<" + tagName + " ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (hasContent)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    ret += System.Web.HttpUtility.HtmlEncode(text);

                for (int i = 0; i < elements.Count; i++)
                {
                    ret += elements[i].getContent(sessionData);
                }

                ret += "</" + tagName  + ">";
            }

            return ret;
        }
    }

    /// <summary>
    /// A script element representing embedded JavaScript-Code
    /// </summary>
    public class HScript : HElement
    {
        private object[] arguments;
        private bool dynamic;
        private string script;
        private ScriptCollection.scriptFuction scriptFunction;
        
        /// <summary>
        /// generates a static script (not the ones that need SessionData or the SSID)
        /// </summary>
        public HScript(string scriptText)
        {
            this.dynamic = false;
            this.script = scriptText;
        }

        /// <summary>
        /// generates a runtime defined script (like the ones, that need SessionData or the SSID)
        /// </summary>
        public HScript(ScriptCollection.scriptFuction scriptFunction, params object[] arguments)
        {
            this.dynamic = true;
            this.scriptFunction = scriptFunction;
            this.arguments = arguments;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            return "<script type\"text/javascript\">" + (dynamic ? scriptFunction(sessionData, arguments) : script) + "</script>";
        }
    }

    /// <summary>
    /// Represents a script element pointing to a script-file which has to be loaded as well
    /// </summary>
    public class HScriptLink : HElement
    {
        /// <summary>
        /// The URL of the script file
        /// </summary>
        public string URL;

        /// <summary>
        /// Constructs a new linking Script element
        /// </summary>
        /// <param name="URL">the url of the script to load</param>
        public HScriptLink(string URL)
        {
            this.URL = URL;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            return "<script type\"text/javascript\" src=\"" + URL + "\"></script>";
        }
    }

    /// <summary>
    /// A canvas element used for complex rendering
    /// </summary>
    public class HCanvas : HElement
    {
        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<canvas ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            return ret + "></canvas>";
        }
    }

    /// <summary>
    /// A textarea element - basically a multiline textbox
    /// </summary>
    public class HTextArea : HElement
    {
        /// <summary>
        /// The amount columns dispalyed
        /// </summary>
        public uint? cols;

        /// <summary>
        /// The amount rows dispalyed
        /// </summary>
        public uint? rows;

        /// <summary>
        /// The predefined value
        /// </summary>
        public string value;

        /// <summary>
        /// Additional attributes added to this tag
        /// </summary>
        public string descriptionTags;

        /// <summary>
        /// Constructs a new textarea element
        /// </summary>
        /// <param name="value">the default value of this textarea</param>
        /// <param name="cols">the amount of columns displayed</param>
        /// <param name="rows">the amount of rows displayed</param>
        public HTextArea(string value = "", uint? cols = null, uint? rows = null)
        {
            this.value = value;
            this.cols = cols;
            this.rows = rows;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string ret = "<textarea ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (cols.HasValue)
                ret += "cols=\"" + cols.Value + "\" ";
            
            if (rows.HasValue)
                ret += "rows=\"" + rows.Value + "\" ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            return ret + ">" + System.Web.HttpUtility.HtmlEncode(value) + "</textarea>";
        }
    }

    /// <summary>
    /// Non-static content, which is computed every request
    /// </summary>
    public class HRuntimeCode : HElement
    {
        /// <summary>
        /// the code to execute
        /// </summary>
        public Master.getContents runtimeCode;

        /// <summary>
        /// Creates non-static content, which is computed every request
        /// </summary>
        /// <param name="runtimeCode">The code to execute every request</param>
        public HRuntimeCode(Master.getContents runtimeCode)
        {
            this.runtimeCode = runtimeCode;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            return runtimeCode.Invoke(sessionData);
        }

        /// <summary>
        /// returns a conditional non-static piece of code, which is computed every request if conditionalCode returns true, codeIfTRUE is executed, if it returns false, codeIfFALSE is executed
        /// </summary>
        /// <param name="codeIfTRUE">The code to execute if conditionalCode returns TRUE</param>
        /// <param name="codeIfFALSE">The code to execute if conditionalCode returns FALSE</param>
        /// <param name="conditionalCode">The Conditional code</param>
        /// <returns>returns a HRuntimeCode : HElement</returns>
        public static HRuntimeCode getConditionalRuntimeCode(Master.getContents codeIfTRUE, Master.getContents codeIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            return new HRuntimeCode((SessionData sessionData) => 
                {
                    if (conditionalCode(sessionData))
                        return codeIfTRUE(sessionData);

                    return codeIfFALSE(sessionData);
                });
        }

        /// <summary>
        /// returns a conditional non-static HElement, which is computed every request if conditionalCode returns true, elementIfTRUE is returned, if it returns false, elementIfFALSE is returned
        /// </summary>
        /// <param name="elementIfTRUE"></param>
        /// <param name="elementIfFALSE"></param>
        /// <param name="conditionalCode">The Conditional code</param>
        /// <returns>returns a HRuntimeCode : HElement</returns>
        public static HRuntimeCode getConditionalRuntimeCode(HElement elementIfTRUE, HElement elementIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            return new HRuntimeCode((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return elementIfTRUE == null ? "" : elementIfTRUE.getContent(sessionData);

                return elementIfFALSE == null ? "" : elementIfFALSE.getContent(sessionData);
            });
        }
    }

    /// <summary>
    /// Non-static content, which is computed every request AND SYNCRONIZED
    /// </summary>
    public class HSyncronizedRuntimeCode : HElement
    {
        /// <summary>
        /// the code to execute
        /// </summary>
        public Master.getContents runtimeCode;

        private System.Threading.Mutex mutex = new System.Threading.Mutex();

        /// <summary>
        /// Creates non-static content, which is computed every request AND SYNCRONIZED
        /// </summary>
        /// <param name="runtimeCode">The code to execute every request</param>
        public HSyncronizedRuntimeCode(Master.getContents runtimeCode)
        {
            this.runtimeCode = runtimeCode;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <returns>the element as string</returns>
        public override string getContent(SessionData sessionData)
        {
            string s = "";

            try
            {
                mutex.WaitOne();
                s = runtimeCode.Invoke(sessionData);
                mutex.ReleaseMutex();
            }
            catch(Exception e)
            {
                mutex.ReleaseMutex();
                throw new Exception(e.Message, e);
            }

            return s;
        }

        /// <summary>
        /// returns a conditional non-static piece of code, which is computed every request if conditionalCode returns true, codeIfTRUE is executed, if it returns false, codeIfFALSE is executed AND SYNCRONIZED
        /// </summary>
        /// <param name="codeIfTRUE">The code to execute if conditionalCode returns TRUE</param>
        /// <param name="codeIfFALSE">The code to execute if conditionalCode returns FALSE</param>
        /// <param name="conditionalCode">The Conditional code</param>
        /// <returns>returns a HRuntimeCode : HElement</returns>
        public static HSyncronizedRuntimeCode getConditionalRuntimeCode(Master.getContents codeIfTRUE, Master.getContents codeIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            return new HSyncronizedRuntimeCode((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return codeIfTRUE(sessionData);

                return codeIfFALSE(sessionData);
            });
        }

        /// <summary>
        /// returns a conditional non-static HElement, which is computed every request if conditionalCode returns true, elementIfTRUE is returned, if it returns false, elementIfFALSE is returned AND SYNCRONIZED
        /// </summary>
        /// <param name="elementIfTRUE"></param>
        /// <param name="elementIfFALSE"></param>
        /// <param name="conditionalCode">The Conditional code</param>
        /// <returns>returns a HRuntimeCode : HElement</returns>
        public static HSyncronizedRuntimeCode getConditionalRuntimeCode(HElement elementIfTRUE, HElement elementIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            return new HSyncronizedRuntimeCode((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return elementIfTRUE == null ? "" : elementIfTRUE.getContent(sessionData);

                return elementIfFALSE == null ? "" : elementIfFALSE.getContent(sessionData);
            });
        }
    }
}
