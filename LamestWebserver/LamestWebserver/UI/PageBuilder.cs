using System;
using System.Collections.Generic;
using System.Linq;
using LamestWebserver.JScriptBuilder;
using System.Text;
using LamestWebserver.Caching;
using LamestWebserver.Core;

namespace LamestWebserver.UI
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
        /// a function pointer to the executed method on GetContent(ISessionIdentificator sessionData)
        /// </summary>
        public Func<SessionData, string> getContentMethod;

        /// <summary>
        /// the title of this page
        /// </summary>
        public string PageTitle;

        /// <summary>
        /// the URL at which this page is / will be available at
        /// </summary>
        public string URL { get; protected set; }

        /// <summary>
        /// Path to the stylesheets.
        /// </summary>
        public List<string> StylesheetLinks = new List<string>();

        /// <summary>
        /// javascript code directly bound into the page code
        /// </summary>
        public List<string> Scripts = new List<string>();

        /// <summary>
        /// path to javascript code files
        /// </summary>
        public List<string> ScriptLinks = new List<string>();

        /// <summary>
        /// additional lines added to the "head" segment of the page
        /// </summary>
        public string AdditionalHeadArguments;

        /// <summary>
        /// The icon to display
        /// </summary>
        public string Favicon = null;

        /// <summary>
        /// CSS code directly bound into the page code
        /// </summary>
        public string StylesheetCode;

        /// <summary>
        /// Creates a new PageBuilder and registers it at the server for a specified url
        /// </summary>
        /// <param name="pagetitle">The window title.</param>
        /// <param name="URL">the URL at which to register this page</param>
        public PageBuilder(string pagetitle, string URL) : base("body></html")
        {
            this.PageTitle = pagetitle;
            this.URL = URL;
            getContentMethod = BuildContent;

            Register();
        }

        /// <summary>
        /// Creates a page builder and registers it as the server for a specified URL. If the conditionalCode returns false the page will not be parsed and the user will be refered to the referalURL
        /// </summary>
        /// <param name="title">The window title.</param>
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
        /// <param name="title">The title of this window.</param>
        public PageBuilder(string title) : base("body></html")
        {
            this.PageTitle = title;
            getContentMethod = BuildContent;
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            string ret = "<html> <head> <title>" + PageTitle + "</title>";

            if (!string.IsNullOrWhiteSpace(Favicon))
                ret += "<link rel=\"shortcut icon\" href='" + Favicon + "'> <link rel=\"icon\" sizes=\"any\" mask=\"\" href=" + Favicon + ">";

            for (int i = 0; i < StylesheetLinks.Count; i++)
            {
                ret += "<link rel=\"stylesheet\" href='" + StylesheetLinks[i] + "'>";
            }

            for (int i = 0; i < Scripts.Count; i++)
            {
                ret += "<script type=\"text/javascript\">" + Scripts[i] + "</script>";
            }

            for (int i = 0; i < ScriptLinks.Count; i++)
            {
                ret += "<script type=\"text/javascript\" src=\"" + ScriptLinks[i] + "\"></script>";
            }

            if (!string.IsNullOrWhiteSpace(StylesheetCode))
                ret += "<style type=\"text/css\">" + StylesheetCode + "</style>";

            if (!string.IsNullOrWhiteSpace(AdditionalHeadArguments))
                ret += AdditionalHeadArguments + " ";

            ret += "</head> <body ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style='" + Style + "' ";

            if (!string.IsNullOrWhiteSpace(base.Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            return ret;
        }

        /// <summary>
        /// The method which is called to parse this element to string
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the contents as string</returns>
        protected string BuildContent(SessionData sessionData)
        {
            if (condition && !conditionalCode(sessionData))
                return InstantPageResponse.GenerateRedirectCode(referealURL, sessionData);

            string ret = GetTagHead();

            if (!string.IsNullOrWhiteSpace(Text))
                ret += System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;");

            for (int i = 0; i < base.Elements.Count; i++)
            {
                ret += base.Elements[i].GetContent(sessionData);
            }
            
            ret += $"</{Tag}>";

            return ret;
        }

        /// <summary>
        /// The method used to grab contents as string to be registered as page for the server.
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the contents as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret;

            try
            {
                ret = getContentMethod.Invoke(sessionData);
            }
            catch (Exception e)
            {
                ret = Master.GetErrorMsg("Exception in PageBuilder '" + URL + "'", "<b>An Error occured while processing the output</b><br>" + e.SafeToString());
            }

            return ret;
        }

        private void Register()
        {
            Master.AddPageResponseToServer(URL, GetContent);
        }

        /// <summary>
        /// via this method you can "unregister" this pages url (if this pageBuilder is registered) at the server.
        /// </summary>
        protected void RemoveFromServer()
        {
            Master.RemovePageResponseFromServer(URL);
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
        public abstract string GetContent(SessionData sessionData);

        /// <summary>
        /// element.GetContent(sessionData)
        /// </summary>
        /// <returns>element.GetContent(sessionData)</returns>
        public static string operator * (HElement element, SessionData sessionData)
        {
            return element.GetContent(sessionData);
        }
        
        /// <summary>
        /// Parses this element to string
        /// </summary>
        /// <returns>this element as string</returns>
        public override string ToString()
        {
            return this.GetContent(SessionData.CurrentSession);
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
        /// <param name="element">the element</param>
        public static explicit operator string(HElement element)
        {
            return element.ToString();
        }

        /// <summary>
        /// Adds two elements to be one HMultipleElements object
        /// </summary>
        /// <param name="a">the first HElement</param>
        /// <param name="b">the second HElement</param>
        /// <returns>a HMultipleElements object</returns>
        public static HMultipleElements operator +(HElement a, HElement b)
        {
            return new HMultipleElements(a,b);
        }

        /// <summary>
        /// Returns true if the HElement returns a static response.
        /// <paramref name="key">The key of the cache entry if cacheable.</paramref>
        /// <paramref name="defaultCachingType">The default CachingType to refer to.</paramref>
        /// <paramref name="response">The StringBuilder to attatch the response to.</paramref>
        /// </summary>
        public virtual bool IsStaticResponse(string key, ECachingType defaultCachingType, StringBuilder response = null)
        {
            if(response != null)
                response.Append(ToString());

            return false;
        }
    }

    /// <summary>
    /// A HElement inheriting the default IsCacheable response for a static response.
    /// </summary>
    public abstract class HCacheableElement : HElement
    {
        /// <inheritdoc />
        public override bool IsStaticResponse(string key, ECachingType defaultCachingType, StringBuilder response = null)
        {
            if (response != null)
                response.Append(ResponseCache.CurrentCacheInstance.Instance.GetCachedString(key, ToString));

            return true;
        }
    }

    /// <summary>
    /// A HElement inheriting a IsCacheable response for a response that could be cachable or not cacheable.
    /// </summary>
    public abstract class HSelectivelyCacheableElement : HElement
    {
        /// <summary>
        /// Is thie response cacheable?
        /// </summary>
        public ECachingType CachingType = ECachingType.Default;

        /// <inheritdoc />
        public override bool IsStaticResponse(string key, ECachingType defaultCachingType, StringBuilder response = null)
        {
            ECachingType ret = CachingType;

            if (ret == ECachingType.Default)
                ret = defaultCachingType;

            if (response != null)
            {
                if (ret == ECachingType.Cacheable)
                    response.Append(ResponseCache.CurrentCacheInstance.Instance.GetCachedString(key, ToString));
                else
                    response.Append(ToString());
            }

            return ret == ECachingType.Cacheable;
        }

        /// <summary>
        /// Sets the current HSelectivelyCacheableElement cacheable.
        /// </summary>
        /// <param name="cachingType">The CachingType to set. (ECachingType.Cacheable by default)</param>
        /// <returns>Returns the current HSelectivelyCacheableElement.</returns>
        public virtual HSelectivelyCacheableElement SetCacheable(ECachingType cachingType = ECachingType.Cacheable)
        {
            CachingType = cachingType;
            return this;
        }
    }

    /// <summary>
    /// A br element used for line breaks in HTML
    /// </summary>
    public class HNewLine : HCacheableElement
    {
        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            return "<br>";
        }
    }

    /// <summary>
    /// A hr element used to display a hoizontal line
    /// </summary>
    public class HLine : HSelectivelyCacheableElement
    {
        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<hr ";

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

            return ret + ">";
        }
    }

    /// <summary>
    /// The contents of this element will directly be copied into the final html document.
    /// </summary>
    public class HPlainText : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The text to copy to the HTML document
        /// </summary>
        public string Text;

        /// <summary>
        /// Constructs a new By-Copy-Element. The contents will only be copied into the final HTML code.
        /// </summary>
        /// <param name="text">the text to copy into the final HTML code</param>
        public HPlainText(string text = "")
        {
            this.Text = text;
        }

        /// <summary>
        /// returns the given text
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            return Text;
        }
    }

    /// <summary>
    /// Copies the given text to the final HTML-Response - html encoded.
    /// </summary>
    public class HString : HPlainText
    {
        /// <summary>
        /// Creates a new HEncodedString containing the given text.
        /// </summary>
        /// <param name="text"></param>
        public HString(string text = "") : base(text) { }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            return System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;");
        }
    }

    /// <summary>
    /// Combines multiple HElements without using a div to a single HElement object
    /// </summary>
    public class HMultipleElements : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The elements to display
        /// </summary>
        public List<HElement> Elements = new List<HElement>();

        /// <summary>
        /// Constructs a new HMultipleElements containing the given elements
        /// </summary>
        /// <param name="elements">the elements to add</param>
        public HMultipleElements(params HElement[] elements)
        {
            Elements = elements.ToList();
        }

        /// <summary>
        /// Constructs a new HMultipleElements containing the given elements
        /// </summary>
        /// <param name="elements">the elements to add</param>
        public HMultipleElements(IEnumerable<HElement> elements)
        {
            if (elements is List<HElement>)
                Elements = (List<HElement>)elements;
            else
                Elements = elements.ToList();
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string ret = "";

            Elements.ForEach(e => ret += e.GetContent(sessionData));

            return ret;
        }

        /// <summary>
        /// Adds elements to the current multiple element
        /// </summary>
        /// <param name="thisElement">one multipleElements object</param>
        /// <param name="otherElement">some HElement</param>
        /// <returns>the MultipleElements object containing the other multipleElements object</returns>
        public static HMultipleElements operator +(HMultipleElements thisElement, HElement otherElement)
        {
            if(otherElement is HMultipleElements)
                thisElement.Elements.AddRange(((HMultipleElements)otherElement).Elements);
            else
                thisElement.Elements.Add(otherElement);

            return thisElement;
        }

        /// <summary>
        /// Casts a HMultipleElements object to string
        /// </summary>
        /// <param name="multipleElements">the elements to cast</param>
        /// <returns>the elements as string</returns>
        public static implicit operator string(HMultipleElements multipleElements) => multipleElements.ToString();
        
        /// <summary>
        /// Retrieves cached contents for nested elements.
        /// </summary>
        /// <paramref name="key">The cache key of the container.</paramref>
        /// <paramref name="defaultCachingType">The default CachingType to refer to.</paramref>
        /// <paramref name="response">The StringBuilder to attatch the response to.</paramref>
        protected void GetCachedContents(string key, ECachingType defaultCachingType, StringBuilder response)
        {
            int firstSuccessfull = 0;

            for (int i = 0; i < Elements.Count; i++)
            {
                if (!Elements[i].IsStaticResponse(key + "/" + i, defaultCachingType))
                {
                    if (firstSuccessfull < i)
                    {
                        string responseString;

                        if (firstSuccessfull == i - 1)
                        {
                            if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/" + (i - 1), out responseString))
                                response.Append(responseString);
                            else
                                Elements[i - 1].IsStaticResponse(key + "/" + (i - 1), defaultCachingType, response);
                        }
                        else
                        {
                            if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/" + firstSuccessfull + "-" + (i - 1), out responseString))
                            {
                                response.Append(responseString);
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();

                                for (int j = firstSuccessfull; j < i; j++)
                                {
                                    sb.Append(Elements[j].ToString());
                                }

                                string s = sb.ToString();

                                ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(key + "/" + firstSuccessfull + "-" + (i - 1), s);
                                response.Append(s);
                            }
                        }
                    }

                    Elements[i].IsStaticResponse(key + "/" + i, ECachingType.Default, response); // <- Append the actual non-cacheable response.

                    firstSuccessfull = i + 1;
                }
                else if (i + 1 == Elements.Count)
                {
                    if (firstSuccessfull == i)
                    {
                        string responseString;

                        if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/" + i, out responseString))
                            response.Append(responseString);
                        else
                            Elements[i].IsStaticResponse(key + "/" + i, defaultCachingType, response);
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();

                        for (int j = firstSuccessfull; j < Elements.Count; j++)
                        {
                            sb.Append(Elements[j].ToString());
                        }

                        string s = sb.ToString();

                        ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(key + "/" + firstSuccessfull + "-" + i, s);
                        response.Append(s);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override bool IsStaticResponse(string key, ECachingType defaultCachingType, StringBuilder response = null)
        {
            ECachingType ret = CachingType;

            if (ret == ECachingType.Default)
                ret = defaultCachingType;

            if (response != null)
            {
                if (ret == ECachingType.Cacheable)
                {
                    GetCachedContents(key, defaultCachingType, response);
                }
                else
                {
                    response.Append(ToString());
                }
            }

            return ret == ECachingType.Cacheable && !(from e in Elements where !e.IsStaticResponse(null, ECachingType.Default, null) select false).Any();
        }
    }

    /// <summary>
    /// Represents an "a" element used for links
    /// </summary>
    public class HLink : HContainer
    {
        private readonly string _href, _onclick;

        /// <summary>
        /// Creates a new Link Element
        /// </summary>
        /// <param name="text">The Text of the Link</param>
        /// <param name="href">The URL this link points to</param>
        /// <param name="onclick">the Javasctipt action executed when clicking on this link</param>
        public HLink(string text = "", string href = "", string onclick = "") : base("a")
        {
            Text = text;
            _href = href;
            _onclick = onclick;
        }

        /// <summary>
        /// Creates a new Link Element
        /// </summary>
        /// <param name="element">The Element inside the Link</param>
        /// <param name="href">The URL this link points to</param>
        /// <param name="onclick">the Javasctipt action executed when clicking on this link</param>
        public HLink(HElement element, string href = "", string onclick = "") : base("a")
        {
            Elements.Add(element);
            _href = href;
            _onclick = onclick;
        }

        /// <summary>
        /// Creates a new Link Element
        /// </summary>
        /// <param name="href">The URL this link points to</param>
        /// <param name="elements">The Elements inside the Link</param>
        public HLink(string href = "", params HElement[] elements) : base("a")
        {
            Elements.AddRange(elements);
            _href = href;
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            if (additionalParams == null)
                additionalParams = "";
            else
                additionalParams += " ";

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                if (!string.IsNullOrWhiteSpace(_href))
                    additionalParams += "href='" + _href + "' ";

                if (!string.IsNullOrWhiteSpace(_onclick))
                    additionalParams += "onclick='" + _onclick + "' ";
            }
            else
            {
                Logger.LogExcept(new NotImplementedException($"The given SessionIdTransmissionType ({SessionContainer.SessionIdTransmissionType}) could not be handled in {GetType().ToString()}."));
            }

            return base.GetTagHead(additionalParams);
        }
    }

    /// <summary>
    /// A img element representing an image in html
    /// </summary>
    public class HImage : HSelectivelyCacheableElement
    {
        private readonly string _source;

        /// <summary>
        /// Additional attributes added to this tag
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// Creates an Image
        /// </summary>
        /// <param name="source">the URL where the image is located at</param>
        public HImage(string source = "")
        {
            this._source = source;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<img ";

            if (!string.IsNullOrWhiteSpace(_source))
                ret += "src='" + _source + "' ";

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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            return ret;
        }
    }

    /// <summary>
    /// A "p" tag, representing a textblock
    /// </summary>
    public class HText : HSelectivelyCacheableElement
    {
        /// <summary>
        /// Additional attributes to add to this HTML-Tag
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// The text to display
        /// </summary>
        public string Text;

        /// <summary>
        /// Constructs a TextBlock
        /// </summary>
        /// <param name="text">the Text displayed</param>
        public HText(string text = "")
        {
            this.Text = text;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            if (Text != null)
                ret += System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;");

            ret += "</p>";

            return ret;
        }
    }

    /// <summary>
    /// A "p" tag, representing a textblock. You can add HTexts seamlessly to an HTextBlock - only the text inside will be displayed
    /// </summary>
    public class HTextBlock : HContainer
    {
        /// <summary>
        /// Constructs a TextBlock
        /// </summary>
        /// <param name="text">the Text displayed</param>
        public HTextBlock(string text = "") : base("p")
        {
            this.Text = text;
        }

        /// <summary>
        /// Constructs a new TextBlock
        /// </summary>
        /// <param name="texts">will be a HText if string, will be itself if HElement, else will be HText of .ToString() text</param>
        public HTextBlock(params object[] texts) : base("p")
        {
            foreach (object text in texts)
            {
                if (text == null)
                    Elements.Add("null");
                else if(text is string)
                    Elements.Add(new HString((string)text));
                else if(text is HElement)
                    Elements.Add((HElement)text);
                else
                    Elements.Add(new HText(text.ToString()));
            }
        }
    }

    /// <summary>
    /// A "b" tag, representing bold text
    /// </summary>
    public class HBold : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The Text to display
        /// </summary>
        public string Text;

        /// <summary>
        /// Constructs a new HBold
        /// </summary>
        /// <param name="text">the text</param>
        public HBold(string text)
        {
            Text = text;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<b ";

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

            ret += ">" + System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</b>";

            return ret;
        }
    }

    /// <summary>
    /// A "i" tag, representing italic text
    /// </summary>
    public class HItalic : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The Text to display
        /// </summary>
        public string Text;

        /// <summary>
        /// Constructs a new HItalic
        /// </summary>
        /// <param name="text">the text</param>
        public HItalic(string text)
        {
            Text = text;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<i ";

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

            ret += ">" + System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</i>";

            return ret;
        }
    }

    /// <summary>
    /// A "del" tag, representing crossed out text
    /// </summary>
    public class HCrossedOut : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The Text to display
        /// </summary>
        public string Text;

        /// <summary>
        /// Constructs a new HBold
        /// </summary>
        /// <param name="text">the text</param>
        public HCrossedOut(string text)
        {
            Text = text;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<del ";

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

            ret += ">" + System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</del>";

            return ret;
        }
    }

    /// <summary>
    /// A "u" tag, representing underlined out text
    /// </summary>
    public class HUnderlined : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The Text to display
        /// </summary>
        public string Text;

        /// <summary>
        /// Constructs a new HBold
        /// </summary>
        /// <param name="text">the text</param>
        public HUnderlined(string text)
        {
            Text = text;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<u ";

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

            ret += ">" + System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</u>";

            return ret;
        }
    }

    /// <summary>
    /// A h(1-6) tag in html (h1 by default) representing a Headline
    /// </summary>
    public class HHeadline : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The Text displayed in this Headline
        /// </summary>
        public string Text;

        /// <summary>
        /// Additional attributes added to this element
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// The level of this headline (1-6)
        /// </summary>
        private readonly int _level;

        /// <summary>
        /// Constructs a new Headline
        /// </summary>
        /// <param name="text">the text of this headline</param>
        /// <param name="level">the level of this headline</param>
        public HHeadline(string text = "", int level = 1)
        {
            if (level > 6 || level < 1)
                throw new ArgumentOutOfRangeException("The level has to be between 1 and 6.");

            this.Text = text;
            this._level = level;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<h" + _level + " ";

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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">" + System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</h" + _level + ">";

            return ret;
        }
    }

    /// <summary>
    /// A input tag representing all kinds of Input Elements
    /// </summary>
    public class HInput : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The Type of the input element
        /// </summary>
        public EInputType InputType;

        /// <summary>
        /// The Value of the input element
        /// </summary>
        public string Value;

        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// Constructs a new Input Element
        /// </summary>
        /// <param name="inputType">the type of the input element</param>
        /// <param name="name">the Name of the HTML element</param>
        /// <param name="value">the predefined value of this input element</param>
        public HInput(EInputType inputType, string name, string value = "")
        {
            this.InputType = inputType;
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<input ";

            ret += "type='" + (InputType != EInputType.datetime_local ? InputType.ToString() : "datetime-local") + "' ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Value))
                ret += "value='" + Value + "' ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

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
    /// A list of radio-buttons of which only one can be selected at a time.
    /// </summary>
    public class HSingleSelector : HSelectivelyCacheableElement
    {
        /// <summary>
        /// Additional attributes to be added to the items
        /// </summary>
        public string DescriptionTags;

        private readonly int _selectedIndex = 0;
        private readonly List<Tuple<string, string>> _nameValuePairs;
        private readonly bool _newLineAfterSelection;

        /// <summary>
        /// Constructs a new HSingleSelector
        /// </summary>
        /// <param name="name">the name of the resulting value</param>
        /// <param name="nameValuePairs">a list of tuples of the selectableItems and their representative value</param>
        /// <param name="selectedIndex">the selected value of the radioButtons</param>
        /// <param name="newLineAfterSelection">shall there be a line after each option?</param>
        public HSingleSelector(string name, List<Tuple<string, string>> nameValuePairs, int selectedIndex = 0, bool newLineAfterSelection = true)
        {
            Name = name;
            _nameValuePairs = nameValuePairs;
            _selectedIndex = selectedIndex;
            _newLineAfterSelection = newLineAfterSelection;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string attribs = "";

            if (!string.IsNullOrWhiteSpace(ID))
                attribs += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Name))
                attribs += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                attribs += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                attribs += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Title))
                attribs += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                attribs += DescriptionTags + " ";

            string ret = "";

            int i = 0;

            foreach (Tuple<string, string> tuple in _nameValuePairs)
            {
                ret += "<label " + attribs + "> <input type=\"radio\" value=\"" + tuple.Item2 + "\" ";

                if (_selectedIndex == i++)
                {
                    ret += "checked ";
                }

                ret += attribs + ">" + System.Web.HttpUtility.HtmlEncode(tuple.Item1).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</label>";

                if (_newLineAfterSelection && i != _nameValuePairs.Count)
                    ret += "<br>";
            }

            return ret;
        }
    }

    /// <summary>
    /// A Text input field.
    /// </summary>
    public class HTextInput : HInput
    {
        private readonly string _placeholder;

        /// <inheritdoc />
        /// <param name="name">the name of the submitted value</param>
        /// <param name="value">the default value</param>
        /// <param name="placeholderText">the placeholder to display when no text has been entered.</param>
        public HTextInput(string name, string value = "", string placeholderText = "") : base(EInputType.text, name, value)
        {
            _placeholder = placeholderText;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<input ";

            ret += "type='" + (InputType != EInputType.datetime_local ? InputType.ToString() : "datetime-local") + "' ";

            if (!string.IsNullOrWhiteSpace(ID))
                ret += "id='" + ID + "' ";

            if (!string.IsNullOrWhiteSpace(Class))
                ret += "class='" + Class + "' ";

            if (!string.IsNullOrWhiteSpace(Style))
                ret += "style=\"" + Style + "\" ";

            if (!string.IsNullOrWhiteSpace(Name))
                ret += "name='" + Name + "' ";

            if (!string.IsNullOrWhiteSpace(Value))
                ret += "value='" + Value + "' ";

            if (!string.IsNullOrWhiteSpace(Title))
                ret += "title=\"" + Title + "\" ";

            if (!string.IsNullOrWhiteSpace(_placeholder))
                ret += "placeholder=\"" + _placeholder + "\" ";

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            return ret;
        }
    }

    /// <summary>
    /// A Password-Text input field.
    /// </summary>
    public class HPasswordInput : HTextInput
    {
        /// <inheritdoc />
        public HPasswordInput(string name, string placeholderText = "") : base(name, "", placeholderText)
        {
            base.InputType = EInputType.password;
        }
    }

    /// <summary>
    /// A simple Radiobutton.
    /// </summary>
    public class HRadioButton : HInput
    {
        private readonly string _text;
        private readonly bool _checked;

        /// <summary>
        /// Constructs a new HRadioButton.
        /// </summary>
        /// <param name="name">the name of the retrived value</param>
        /// <param name="value">the value to retrive</param>
        /// <param name="text">the displayed text (or null if none)</param>
        /// <param name="_checked">is it checked by default?</param>
        public HRadioButton(string name, string value, string text = null, bool _checked = true) : base(EInputType.radio, name, value)
        {
            _text = text;
            this._checked = _checked;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            if (_text == null)
            {
                string ret = "<input ";

                ret += "type='" + (InputType != EInputType.datetime_local ? InputType.ToString() : "datetime-local") + "' ";

                if (!string.IsNullOrWhiteSpace(ID))
                    ret += "id='" + ID + "' ";

                if (!string.IsNullOrWhiteSpace(Class))
                    ret += "class='" + Class + "' ";

                if (!string.IsNullOrWhiteSpace(Style))
                    ret += "style=\"" + Style + "\" ";

                if (!string.IsNullOrWhiteSpace(Name))
                    ret += "name='" + Name + "' ";

                if (!string.IsNullOrWhiteSpace(Value))
                    ret += "value='" + Value + "' ";

                if (!string.IsNullOrWhiteSpace(Title))
                    ret += "title=\"" + Title + "\" ";

                if (_checked)
                    ret += "checked ";

                if (!string.IsNullOrWhiteSpace(DescriptionTags))
                    ret += DescriptionTags;

                ret += ">";

                return ret;
            }
            else
            {
                string ret = "<label><input ";

                ret += "type='" + (InputType != EInputType.datetime_local ? InputType.ToString() : "datetime-local") + "' ";

                if (!string.IsNullOrWhiteSpace(ID))
                    ret += "id='" + ID + "' ";

                if (!string.IsNullOrWhiteSpace(Class))
                    ret += "class='" + Class + "' ";

                if (!string.IsNullOrWhiteSpace(Style))
                    ret += "style=\"" + Style + "\" ";

                if (!string.IsNullOrWhiteSpace(Name))
                    ret += "name='" + Name + "' ";

                if (!string.IsNullOrWhiteSpace(Value))
                    ret += "value='" + Value + "' ";

                if (!string.IsNullOrWhiteSpace(Title))
                    ret += "title=\"" + Title + "\" ";

                if (_checked)
                    ret += "checked ";

                if (!string.IsNullOrWhiteSpace(DescriptionTags))
                    ret += DescriptionTags;

                ret += ">" + System.Web.HttpUtility.HtmlEncode(_text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</label>";

                return ret;
            }
        }
    }

    /// <summary>
    /// A simple checkbox.
    /// </summary>
    public class HCheckBox : HRadioButton
    {
        /// <summary>
        /// Constructs a new HCheckbox.
        /// </summary>
        /// <param name="name">the name of the retrived value</param>
        /// <param name="value">the value to retrive</param>
        /// <param name="text">the displayed text (or null if none)</param>
        /// <param name="_checked">is it checked by default?</param>
        public HCheckBox(string name, string value, string text = null, bool _checked = true) : base(name, value, text, _checked)
        {
            InputType = EInputType.checkbox;
        }
    }

    /// <summary>
    /// A div element representing a container
    /// </summary>
    public class HContainer : HSelectivelyCacheableElement
    {
        /// <summary>
        /// Sets the HTML Tag.
        /// </summary>
        /// <param name="tag">the HTML Tag of this element.</param>
        protected HContainer(string tag)
        {
            Tag = tag;
        }

        /// <summary>
        /// Adds all listed objects into the container.
        /// </summary>
        /// <param name="elements">the elements to add</param>
        public HContainer(params HElement[] elements)
        {
            Tag = "div";
            Elements = elements.ToList();
        }

        /// <summary>
        /// Adds all listed objects into the container.
        /// </summary>
        /// <param name="elements">the elements to add</param>
        public HContainer(IEnumerable<HElement> elements)
        {
            Tag = "div";

            if (elements is List<HElement>)
                Elements = (List<HElement>)elements;
            else
                Elements = elements.ToList();
        }

        /// <summary>
        /// Adds all listed objects into the container.
        /// </summary>
        /// <param name="tag">the html tag of this element.</param>
        /// <param name="elements">the elements to add</param>
        protected HContainer(string tag, params HElement[] elements) : this(tag)
        {
            Tag = tag;
            Elements = elements.ToList();
        }

        /// <summary>
        /// A list of all contained elements
        /// </summary>
        public List<HElement> Elements = new List<HElement>();

        /// <summary>
        /// The text contained in this element
        /// </summary>
        public string Text;

        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// The HTML Tag of this Element.
        /// </summary>
        protected string Tag { get; }


        /// <summary>
        /// Adds an element to the element list
        /// </summary>
        /// <param name="element">the element</param>
        public void AddElement(HElement element)
        {
            Elements.Add(element);
        }

        /// <summary>
        /// Retrieves the Tag Head for the corresponding HTML element.
        /// </summary>
        /// <param name="additionalParams">additional things to add to the head.</param>
        /// <returns>Returns the Tag Head as string.</returns>
        protected virtual string GetTagHead(string additionalParams = null)
        {
            string ret = $"<{Tag} ";

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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags + " ";

            if (!string.IsNullOrWhiteSpace(additionalParams))
                ret += additionalParams;

            ret += ">";

            return ret;
        }


        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = GetTagHead();

            if (!string.IsNullOrWhiteSpace(Text))
                ret += System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;");

            for (int i = 0; i < Elements.Count; i++)
            {
                ret += Elements[i].GetContent(sessionData);
            }

            ret += $"</{Tag}>";

            return ret;
        }

        /// <summary>
        /// Adds a bunch of elements to the element list
        /// </summary>
        /// <param name="list">a list of elements</param>
        public void AddElements(IEnumerable<HElement> list)
        {
            foreach (var element in list)
            {
                AddElement(element);
            }
        }

        /// <summary>
        /// adds a bunch of elements to the elementlist
        /// </summary>
        /// <param name="list">a few elements</param>
        public void AddElements(params HElement[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                AddElement(list[i]);
            }
        }

        /// <summary>
        /// Retrieves cached contents for nested elements.
        /// </summary>
        /// <paramref name="key">The cache key of the container.</paramref>
        /// <paramref name="defaultCachingType">The default CachingType to refer to.</paramref>
        /// <paramref name="response">The StringBuilder to attatch the response to.</paramref>
        protected void GetCachedContents(string key, ECachingType defaultCachingType, StringBuilder response)
        {
            string preResponseString;

            if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/pre", out preResponseString))
            {
                response.Append(preResponseString);
            }
            else
            {
                string pre = GetTagHead();

                if (!string.IsNullOrWhiteSpace(Text))
                    pre += System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;");

                ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(key + "/pre", pre);
                response.Append(pre);
            }

            int firstSuccessfull = 0;

            for (int i = 0; i < Elements.Count; i++)
            {
                if (!Elements[i].IsStaticResponse(key + "/" + i, defaultCachingType))
                {
                    if(firstSuccessfull < i)
                    {
                        string responseString;

                        if (firstSuccessfull == i - 1)
                        {
                            if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/" + (i - 1), out responseString))
                                response.Append(responseString);
                            else
                                Elements[i - 1].IsStaticResponse(key + "/" + (i - 1), defaultCachingType, response);
                        }
                        else
                        {
                            if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/" + firstSuccessfull + "-" + (i - 1), out responseString))
                            {
                                response.Append(responseString);
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();

                                for (int j = firstSuccessfull; j < i; j++)
                                {
                                    sb.Append(Elements[j].ToString());
                                }

                                string s = sb.ToString();

                                ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(key + "/" + firstSuccessfull + "-" + (i - 1), s);
                                response.Append(s);
                            }
                        }
                    }

                    Elements[i].IsStaticResponse(key + "/" + i, ECachingType.Default, response); // <- Append the actual non-cacheable response.
                    
                    firstSuccessfull = i + 1;
                }
                else if(i + 1 == Elements.Count)
                {
                    if (firstSuccessfull == i)
                    {
                        string responseString;

                        if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(key + "/" + i, out responseString))
                            response.Append(responseString);
                        else
                            Elements[i].IsStaticResponse(key + "/" + i, defaultCachingType, response);
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();

                        for (int j = firstSuccessfull; j < Elements.Count; j++)
                        {
                            sb.Append(Elements[j].ToString());
                        }

                        string s = sb.ToString();

                        ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(key + "/" + firstSuccessfull + "-" + i, s);
                        response.Append(s);
                    }
                }
            }

            response.Append($"</{Tag}>");
        }

        /// <inheritdoc />
        public override bool IsStaticResponse(string key, ECachingType defaultCachingType, StringBuilder response = null)
        {
            ECachingType ret = CachingType;

            if (ret == ECachingType.Default)
                ret = defaultCachingType;

            if (response != null)
            {
                if (ret == ECachingType.Cacheable)
                {
                    GetCachedContents(key, defaultCachingType, response);
                }
                else
                {
                    response.Append(ToString());
                }
            }

            return ret == ECachingType.Cacheable && !(from e in Elements where !e.IsStaticResponse(null, ECachingType.Default, null) select false).Any();
        }
    }

    /// <summary>
    /// A container for inline elements - represented by a span-HTML tag
    /// </summary>
    public class HInlineContainer : HContainer
    {
        /// <summary>
        /// Constructs a new inline container containing the given elements.
        /// </summary>
        /// <param name="elements">the contained elements</param>
        public HInlineContainer(params HElement[] elements) : base("span", elements)
        {
            
        }
    }

    /// <summary>
    /// A 'blockquote' tag - representing a quote.
    /// </summary>
    public class HQuote : HContainer
    {
        /// <summary>
        /// The source of the Quote
        /// </summary>
        public string Source;

        /// <summary>
        /// Creates a new HQuote object
        /// </summary>
        /// <param name="text">the quoted text</param>
        /// <param name="source">the source of the quote</param>
        /// <param name="elements">the contained elements</param>
        public HQuote(string text, string source = null, params HElement[] elements) : base("blockquote", elements)
        {
            Text = text;
            Source = source;
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            if (additionalParams == null)
                additionalParams = "";
            else
                additionalParams += " ";

            if (!string.IsNullOrWhiteSpace(Source))
                additionalParams += "cite='" + Source + "' ";

            return base.GetTagHead(additionalParams);
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
        public string Action;

        private readonly bool _fixedAction;
        private readonly string _redirectTrue;
        private readonly string _redirectFalse;
        private readonly Func<SessionData, bool> _conditionalCode;

        /// <summary>
        /// Constructs a new Form pointing to the given action when submitted
        /// </summary>
        /// <param name="action">the URL to load when submitted</param>
        public HForm(string action) : base("form")
        {
            this.Action = action;
            _fixedAction = true;
        }

        /// <summary>
        /// redirects if the conditional code returns true and executes other code if the conditional code returns false
        /// </summary>
        /// <param name="redirectURLifTRUE">the url to redirect to if the conditionalCode returns true</param>
        /// <param name="redirectURLifFALSE">the url to redirect to if the conditionalCode returns false</param>
        /// <param name="conditionalCode">the conditional code</param>
        public HForm(string redirectURLifTRUE, string redirectURLifFALSE, Func<SessionData, bool> conditionalCode) : base("form")
        {
            _fixedAction = false;
            _redirectTrue = redirectURLifTRUE;
            _redirectFalse = redirectURLifFALSE;
            this._conditionalCode = conditionalCode;
        }

        /// <summary>
        /// creates a form containing a few values which are added to elements. It can also contain a submit button.
        /// </summary>
        /// <param name="action">the URL to load when submitted</param>
        /// <param name="addSubmitButton">shall there be a submit button?</param>
        /// <param name="buttontext">if yes: what should the text on the submit button say?</param>
        /// <param name="values">additional values to set in the form as invisible parameters</param>
        public HForm(string action, bool addSubmitButton, string buttontext = "", params Tuple<string, string>[] values) : this(action)
        {
            _fixedAction = true;
            this.Action = action;

            for (int i = 0; i < values.Length; i++)
            {
                Elements.Add(new HInput(HInput.EInputType.hidden, values[i].Item1, values[i].Item2));
            }

            if (addSubmitButton)
                Elements.Add(new HButton(buttontext, HButton.EButtonType.submit));
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            if (additionalParams == null)
                additionalParams = "";
            else
                additionalParams += " ";

            if (_fixedAction)
            {
                if (!string.IsNullOrWhiteSpace(Action))
                    additionalParams += "action='" + Action + "' ";
            }
            else
            {
                additionalParams += "action='" + InstantPageResponse.AddOneTimeConditionalRedirect(_redirectTrue, _redirectFalse, true, _conditionalCode) + "' ";
            }

            additionalParams += "method='POST' ";

            return base.GetTagHead(additionalParams);
        }
    }



    /// <summary>
    /// A 'fieldset' tag - a panel contining multiple inputs / elements
    /// </summary>
    public class HPanel : HContainer
    {
        /// <summary>
        /// The title of the panel.
        /// </summary>
        public string Legend;

        /// <summary>
        /// Creates a new HFieldSet object
        /// <param name="legend">the displayed name of the panel</param>
        /// <param name="elements">the contained elements</param>
        /// </summary>
        public HPanel(string legend = null, params HElement[] elements) : base("fieldset", elements)
        {
            Legend = legend;
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            string ret = base.GetTagHead(additionalParams);

            if (!string.IsNullOrWhiteSpace(Legend))
                ret += "<legend>" + Legend + "</legend>";

            return ret;
        }
    }

    /// <summary>
    /// A button tag representing a button
    /// </summary>
    public class HButton : HContainer
    {
        private readonly string _href;
        private readonly string _onclick;
        private readonly EButtonType _type;

        /// <summary>
        /// Creates a button. SUBMIT BUTTONS SHOULDN'T HAVE A HREF!
        /// </summary>
        /// <param name="text">the text of the button.</param>
        /// <param name="type">the button type according to http standards.</param>
        /// <param name="href">the destination of this button. SUBMIT BUTTONS SHOULDN'T HAVE A HREF!</param>
        /// <param name="onclick"></param>
        public HButton(string text, EButtonType type = EButtonType.button, string href = "", string onclick = "") : this(text, href, onclick)
        {
            this._type = type;
        }

        /// <summary>
        /// Creates a button.
        /// </summary>
        /// <param name="text">the text of the button.</param>
        /// <param name="href">the destination of this button. SUBMIT BUTTONS SHOULDN'T HAVE A HREF!</param>
        /// <param name="onclick">the executed javascript-code on clicking the button.</param>
        public HButton(string text, string href = "", string onclick = "") : base("button")
        {
            this.Text = text;
            this._href = href;
            this._onclick = onclick;
            this._type = EButtonType.button;
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            string ret = $"<{Tag} type='" + _type + "' ";

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

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                if (!string.IsNullOrWhiteSpace(_onclick))
                {
                    ret += "onclick='" + _onclick + ";";

                    if (!string.IsNullOrWhiteSpace(_href) && _type != EButtonType.submit)
                        ret += "location.href=\"" + _href + "\"'; ";
                    else
                        ret += "\' ";
                }
                else if (!string.IsNullOrWhiteSpace(_href) && _type != EButtonType.submit)
                {
                    ret += "onclick=\"location.href='" + _href + "'\" ";
                }
            }
            else
            {
                Logger.LogExcept("SessionIdTransmissionType is invalid or not supported in " + this.GetType().ToString() + ".");
            }

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags + " ";

            ret += ">";

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
    public class HDropDownMenu : HSelectivelyCacheableElement
    {
        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string DescriptionTags;

        private Tuple<string, string>[] options;

        /// <summary>
        /// The amount of entries displayed if not expanded
        /// </summary>
        public int Size = 1;

        /// <summary>
        /// does the dropdownmenu allow multiple selections?
        /// </summary>
        public bool MultipleSelectable = false;

        /// <summary>
        /// is the dropdownmenu disabled for the user?
        /// </summary>
        public bool Disabled = false;

        /// <summary>
        /// the selectedIndexes
        /// </summary>
        public List<int> SelectedIndexes = new List<int>() { 0 };

        /// <summary>
        /// Constructs a new DropDownMenu element
        /// </summary>
        /// <param name="name">the name of the element (for forms)</param>
        /// <param name="size">The amount of entries displayed if not expanded</param>
        /// <param name="multipleSelectable">does the dropdownmenu allow multiple selections?</param>
        /// <param name="textValuePairsToDisplay">All possibly selectable items as a tuple (Text displayed for the user, Value presented to form)</param>
        public HDropDownMenu(string name, int size, bool multipleSelectable, params Tuple<string, string>[] textValuePairsToDisplay)
        {
            this.Name = name;
            this.Size = size;
            this.MultipleSelectable = multipleSelectable;
            this.options = textValuePairsToDisplay;
        }

        /// <summary>
        /// Constructs a new DropDownMenu element
        /// </summary>
        /// <param name="name">the name of the element (for forms)</param>
        /// <param name="textValuePairsToDisplay">All possibly selectable items as a tuple (Text displayed for the user, Value presented to form)</param>
        public HDropDownMenu(string name, params Tuple<string, string>[] textValuePairsToDisplay)
        {
            this.Name = name;
            this.options = textValuePairsToDisplay;
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
            if (!MultipleSelectable)
                SelectedIndexes.Clear();

            for (int i = 0; i < options.Length; i++)
            {
                if(options[i].Item2 == value)
                {
                    this.SelectedIndexes.Add(i);
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
            if (!MultipleSelectable)
                SelectedIndexes.Clear();

            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].Item1 == text)
                {
                    this.SelectedIndexes.Add(i);
                }
            }

            return this;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
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

            ret += "size=\"" + Size + "\" ";

            if (MultipleSelectable)
                ret += "multiple=\"multiple\" ";

            if (Disabled)
                ret += "disabled=\"" + Disabled + "\" ";

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            if(options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    ret += "<option value=\"" + System.Web.HttpUtility.UrlEncode(options[i].Item2) + "\" ";

                    if (SelectedIndexes.Contains(i))
                        ret += "selected=\"selected\" ";

                    ret += ">" + System.Web.HttpUtility.HtmlEncode(options[i].Item1).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "</option>";
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
        private readonly EListType listType;

        /// <summary>
        /// If true adds "display: list-item;" at the start of every subitem Style property.
        /// </summary>
        public bool SetListStyleToElements = true;

        /// <summary>
        /// Constructs a new List Element
        /// </summary>
        /// <param name="listType">the type of the list</param>
        public HList(EListType listType) : base(listType == EListType.OrderedList ? "ol" : "ul")
        {
            this.listType = listType;
        }
        /// <summary>
        /// Constructs a new List Element
        /// </summary>
        /// <param name="listType">the type of the list</param>
        /// <param name="elements">the contents of the list</param>
        public HList(EListType listType, params object[] elements) : this(listType, (IEnumerable<object>)elements) { }

        /// <summary>
        /// Constructs a new List Element
        /// </summary>
        /// <param name="listType">the type of the list</param>
        /// <param name="elements">the contents of the list</param>
        public HList(EListType listType, IEnumerable<object> elements) : this(listType)
        {
            foreach (object o in elements)
            {
                if (o == null)
                    continue;
                else if (o is HElement)
                    Elements.Add((HElement)o);
                else if (o is string)
                    Elements.Add(new HText((string)o));
                else
                    Elements.Add(new HText(o.ToString()));
            }
        }

        /// <inheritdoc />
        protected override string GetTagHead(string additionalParams = null)
        {
            if (SetListStyleToElements && Elements != null)
                foreach (HElement element in Elements)
                    if (!element.Style.StartsWith("display: list-item;"))
                        element.Style = "display: list-item;" + element.Style;

            return base.GetTagHead(additionalParams);
        }

        /// <summary>
        /// Returns true if there are no elements in this List.
        /// </summary>
        /// <returns>Returns true if there are no elements in this List.</returns>
        public bool IsEmpty() => (Elements != null && Elements.Count == 0);


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
    public class HTable : HSelectivelyCacheableElement
    {
        private IEnumerable<IEnumerable<object>> _elements;

        /// <summary>
        /// The table header displayed on top of the table rows.
        /// </summary>
        public IEnumerable<object> TableHeader = null;

        /// <summary>
        /// Additional attributes to be added to this node
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// Constructs a new Table containing the given elements
        /// </summary>
        /// <param name="elements">the contained elements</param>
        public HTable(IEnumerable<IEnumerable<object>> elements)
        {
            if (elements == null || elements.Contains(null))
                throw new ArgumentNullException(nameof(elements));

            this._elements = elements;
        }

        /// <summary>
        /// Constructs a new Table containing the given data
        /// </summary>
        /// <param name="data">the contents of this table</param>
        public HTable(params IEnumerable<object>[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(_elements));

            if (data.Length == 1 && data[0] is IEnumerable<object>)
            {
                if (data[0] is IEnumerable<IEnumerable<object>>)
                {
                    _elements = (IEnumerable<IEnumerable<object>>)data[0];

                    return;
                }
                else
                {
                    foreach (var element in (IEnumerable<object>)data[0])
                        if (!(element is IEnumerable<object>))
                            goto NOT_ALREADY_LIST_OF_LISTS;

                    _elements = (from entry in data[0] select ((IEnumerable<object>)entry));

                    return;
                }
            }

            NOT_ALREADY_LIST_OF_LISTS:

            _elements = data;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            if(TableHeader != null)
            {
                ret += "<tr>";

                foreach (object header in TableHeader)
                    if(header is HElement)
                        ret += "<th>" + ((HElement)header).GetContent(sessionData) + "</th>";
                    else
                        ret += "<th>" + header?.ToString() + "</th>";

                ret += "</tr>";
            }

            if (_elements != null)
            {
                foreach (IEnumerable<object> outer in _elements)
                {
                    ret += "<tr>";

                    foreach (object element in outer)
                    {
                        if(element is HElement)
                            ret += "<td>" + ((HElement)element).GetContent(sessionData) + "</td>";
                        else
                            ret += "<td>" + element?.ToString() + "</td>";
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
    public class HTag : HSelectivelyCacheableElement
    {
        /// <summary>
        /// if false, the element won't have a start and end tag but will only consist of a single tag (like img)
        /// </summary>
        public bool HasContent;

        /// <summary>
        /// the name of the tag
        /// </summary>
        public string TagName;


        /// <summary>
        /// A list of all contained elements
        /// </summary>
        public List<HElement> Elements = new List<HElement>();

        /// <summary>
        /// The text contained in this element
        /// </summary>
        public string Text;

        /// <summary>
        /// Additional attributes added to the tag
        /// </summary>
        public string DescriptionTags;
        

        /// <summary>
        /// Adds an element to the element list
        /// </summary>
        /// <param name="element">the element</param>
        public void AddElement(HElement element)
        {
            Elements.Add(element);
        }

        /// <summary>
        /// Constructs a new custom tag
        /// </summary>
        /// <param name="tagName">the name of the custom tag</param>
        /// <param name="descriptionTags">Additional attributs</param>
        /// <param name="hasContent">if false, the element won't have a start and end tag but will only consist of a single tag (like img)</param>
        /// <param name="text">the contatined text in this element</param>
        public HTag(string tagName, string descriptionTags, bool hasContent, string text = "")
        {
            this.TagName = tagName;
            this.DescriptionTags = descriptionTags;
            this.HasContent = hasContent;
            this.Text = text;
        }

        /// <summary>
        /// Constructs a new custom tag
        /// </summary>
        /// <param name="tagName">the name of the custom tag</param>
        /// <param name="descriptionTags">Additional attributes</param>
        /// <param name="text">the contatined text in this element (or null if no content)</param>
        public HTag(string tagName, string descriptionTags, string text = null) : this(tagName, descriptionTags, text != null, text)
        {
            
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<" + TagName + " ";

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

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += ">";

            if (HasContent)
            {
                if (!string.IsNullOrWhiteSpace(Text))
                    ret += System.Web.HttpUtility.HtmlEncode(Text).Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;");

                for (int i = 0; i < Elements.Count; i++)
                {
                    ret += Elements[i].GetContent(sessionData);
                }

                ret += "</" + TagName + ">";
            }

            return ret;
        }
    }

    /// <summary>
    /// A script element representing embedded JavaScript-Code
    /// </summary>
    public class HScript : HSelectivelyCacheableElement
    {
        private object[] arguments;
        private bool dynamic;
        private string script;
        private ScriptCollection.ScriptFuction scriptFunction;
        
        /// <summary>
        /// generates a static script (not the ones that need ISessionIdentificator or the SSID)
        /// </summary>
        public HScript(string scriptText)
        {
            this.dynamic = false;
            this.script = scriptText;
        }

        /// <summary>
        /// generates a runtime defined script (like the ones, that need ISessionIdentificator or the SSID)
        /// </summary>
        public HScript(ScriptCollection.ScriptFuction scriptFunction, params object[] arguments)
        {
            this.dynamic = true;
            this.scriptFunction = scriptFunction;
            this.arguments = arguments;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            return "<script type=\"text/javascript\">" + (dynamic ? scriptFunction(sessionData, arguments) : script) + "</script>";
        }
    }

    /// <summary>
    /// Represents a script element pointing to a script-file which has to be loaded as well
    /// </summary>
    public class HScriptLink : HSelectivelyCacheableElement
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
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            return "<script type=\"text/javascript\" src=\"" + URL + "\"></script>";
        }
    }

    /// <summary>
    /// A canvas element used for complex rendering
    /// </summary>
    public class HCanvas : HSelectivelyCacheableElement
    {
        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
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
    public class HTextArea : HSelectivelyCacheableElement
    {
        /// <summary>
        /// The amount columns dispalyed
        /// </summary>
        public uint? Cols;

        /// <summary>
        /// The amount rows dispalyed
        /// </summary>
        public uint? Rows;

        /// <summary>
        /// The predefined value
        /// </summary>
        public string Value;

        /// <summary>
        /// Additional attributes added to this tag
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// Constructs a new textarea element
        /// </summary>
        /// <param name="value">the default value of this textarea</param>
        /// <param name="cols">the amount of columns displayed</param>
        /// <param name="rows">the amount of rows displayed</param>
        public HTextArea(string value = "", uint? cols = null, uint? rows = null)
        {
            this.Value = value;
            this.Cols = cols;
            this.Rows = rows;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
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

            if (Cols.HasValue)
                ret += "cols=\"" + Cols.Value + "\" ";
            
            if (Rows.HasValue)
                ret += "rows=\"" + Rows.Value + "\" ";

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            return ret + ">" + System.Web.HttpUtility.HtmlEncode(Value) + "</textarea>";
        }
    }

    /// <summary>
    /// A "p" tag, representing a textblock
    /// </summary>
    public class HIframe : HSelectivelyCacheableElement
    {
        /// <summary>
        /// Additional attributes to add to this HTML-Tag
        /// </summary>
        public string DescriptionTags;

        /// <summary>
        /// The source to display
        /// </summary>
        public string Source;

        /// <summary>
        /// The HTML-Content to display
        /// </summary>
        public string SourceHtml;

        /// <summary>
        /// The HTML5 sandbox attribute for iframes. null if nonexistent. SandboxMode.enabled no specific attribute.
        /// </summary>
        public SandboxMode? SandboxAttribute = null;

        /// <summary>
        /// Constructs a TextBlock
        /// </summary>
        /// <param name="text">the Text displayed</param>
        public HIframe(string src = "")
        {
            this.Source = src;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            string ret = "<iframe ";

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

            if (!string.IsNullOrWhiteSpace(Source))
                ret += "src=\"" + Source + "\" ";

            if (!string.IsNullOrWhiteSpace(SourceHtml))
                ret += "srcdoc=\"" + SourceHtml.Replace("\n", "").Replace("\t", "").Replace("\"", "&quot;") + "\" ";

            if (SandboxAttribute.HasValue)
            {
                if (SandboxAttribute == SandboxMode.enabled)
                    ret += "sandbox ";
                else
                    foreach (SandboxMode s in Enum.GetValues(typeof(SandboxMode)))
                        if (((int)SandboxAttribute & (int)s) != 0)
                            ret += $"sandbox=\"{s.ToString().Replace('_', '-')}\" ";
            }

            if (!string.IsNullOrWhiteSpace(DescriptionTags))
                ret += DescriptionTags;

            ret += "></iframe>";

            return ret;
        }

        /// <summary>
        /// Represents the HTML5 Sandbox attributes to an iframe.
        /// </summary>
        public enum SandboxMode
        {
            enabled = 0,
            allow_forms = 0x1,
            allow_pointer_lock = 0x2,
            allow_popups = 0x4,
            allow_same_origin = 0x8,
            allow_scripts = 0x10,
            allow_top_navigation = 0x20
        }
    }

    /// <summary>
    /// Non-static content, which is computed every request
    /// </summary>
    public class HRuntimeCode : HSelectivelyCacheableElement
    {
        /// <summary>
        /// the code to execute
        /// </summary>
        public Func<SessionData, string> RuntimeCode;

        /// <summary>
        /// Creates non-static content, which is computed every request
        /// </summary>
        /// <param name="runtimeCode">The code to execute every request</param>
        public HRuntimeCode(Func<SessionData, string> runtimeCode)
        {
            this.RuntimeCode = runtimeCode;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
        {
            return RuntimeCode.Invoke(sessionData);
        }

        /// <summary>
        /// returns a conditional non-static piece of code, which is computed every request if conditionalCode returns true, codeIfTRUE is executed, if it returns false, codeIfFALSE is executed
        /// </summary>
        /// <param name="codeIfTRUE">The code to execute if conditionalCode returns TRUE</param>
        /// <param name="codeIfFALSE">The code to execute if conditionalCode returns FALSE</param>
        /// <param name="conditionalCode">The Conditional code</param>
        /// <returns>returns a HRuntimeCode : HElement</returns>
        public static HRuntimeCode GetConditionalRuntimeCode(Func<SessionData, string> codeIfTRUE, Func<SessionData, string> codeIfFALSE, Func<SessionData, bool> conditionalCode)
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
        public static HRuntimeCode GetConditionalRuntimeCode(HElement elementIfTRUE, HElement elementIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            return new HRuntimeCode((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return elementIfTRUE == null ? "" : elementIfTRUE.GetContent(sessionData);

                return elementIfFALSE == null ? "" : elementIfFALSE.GetContent(sessionData);
            });
        }
    }

    /// <summary>
    /// Non-static content, which is computed every request AND SYNCRONIZED
    /// </summary>
    public class HSyncronizedRuntimeCode : HSelectivelyCacheableElement
    {
        /// <summary>
        /// the code to execute
        /// </summary>
        public Func<SessionData, string> runtimeCode;

        private System.Threading.Mutex mutex = new System.Threading.Mutex();

        /// <summary>
        /// Creates non-static content, which is computed every request AND SYNCRONIZED
        /// </summary>
        /// <param name="runtimeCode">The code to execute every request</param>
        public HSyncronizedRuntimeCode(Func<SessionData, string> runtimeCode)
        {
            this.runtimeCode = runtimeCode;
        }

        /// <summary>
        /// This Method parses the current element to string
        /// </summary>
        /// <param name="sessionData">the current ISessionIdentificator</param>
        /// <returns>the element as string</returns>
        public override string GetContent(SessionData sessionData)
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
                throw new Exception(e.SafeMessage(), e);
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
        public static HSyncronizedRuntimeCode getConditionalRuntimeCode(Func<SessionData, string> codeIfTRUE, Func<SessionData, string> codeIfFALSE, Func<SessionData, bool> conditionalCode)
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
                    return elementIfTRUE == null ? "" : elementIfTRUE.GetContent(sessionData);

                return elementIfFALSE == null ? "" : elementIfFALSE.GetContent(sessionData);
            });
        }
    }

    /// <summary>
    /// Provides functionality to dynamically cache HElements
    /// (if CachingType in HSelectivelyCacheableElement is set to ECachingType.Cacheable for all elements or subelements that should be cached).
    /// </summary>
    public class HCachePool : HSelectivelyCacheableElement
    {
        private HElement ContainedElement;
        private IURLIdentifyable CurrentResponse;
        private int CachePoolIndex;

        /// <summary>
        /// Constructs a new HCachePool which provides functionality to cache contained HElements easily
        /// (if CachingType in HSelectivelyCacheableElement is set to ECachingType.Cacheable for all elements or subelements that should be cached).
        /// </summary>
        /// <param name="containedElement">The contained Element to dynamically cache.</param>
        /// <param name="currentResponse">The current Page.</param>
        /// <param name="cachePoolIndex">The index of this HCachePool on this page (if you have multiple HCachePools on the same page).</param>
        public HCachePool(HElement containedElement, IURLIdentifyable currentResponse, int cachePoolIndex = 0)
        {
            if (containedElement == null)
                throw new NullReferenceException(nameof(containedElement));

            if (currentResponse == null)
                throw new NullReferenceException(nameof(currentResponse));

            ContainedElement = containedElement;
            CurrentResponse = currentResponse;
            CachePoolIndex = cachePoolIndex;
        }

        /// <inheritdoc />
        public override string GetContent(SessionData sessionData)
        {
            if (ContainedElement == null)
                throw new NullReferenceException(nameof(ContainedElement));

            if (ContainedElement.IsStaticResponse(CurrentResponse.URL + "#" + CachePoolIndex + "#", ECachingType.Default, null))
            {
                string responseString;

                if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(CurrentResponse.URL + "#" + CachePoolIndex + "#", out responseString))
                {
                    return responseString;
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    ContainedElement.IsStaticResponse(CurrentResponse.URL + "#" + CachePoolIndex + "#", ECachingType.Default, stringBuilder);

                    string ret = stringBuilder.ToString();

                    ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(CurrentResponse.URL + "#" + CachePoolIndex + "#", ret);

                    return ret;
                }
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();

                ContainedElement.IsStaticResponse(CurrentResponse.URL + "#" + CachePoolIndex + "#", ECachingType.Default, stringBuilder);

                return stringBuilder.ToString();
            }
        }

        /// <inheritdoc />
        public override bool IsStaticResponse(string key, ECachingType defaultCachingType, StringBuilder response = null)
        {
            ECachingType ret = CachingType;

            if (ret == ECachingType.Default)
                ret = defaultCachingType;

            if (response != null)
                response.Append(ToString());

            return ret == ECachingType.Cacheable && !ContainedElement.IsStaticResponse(key, ECachingType.Default, null);
        }
    }
}
