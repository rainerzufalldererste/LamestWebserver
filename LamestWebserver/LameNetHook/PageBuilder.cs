﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public class PageBuilder : HContainer
    {
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
        public string URL = "";

        /// <summary>
        /// Path to the stylesheets. Prefer strings. Else: toString() will be used
        /// </summary>
        public List<Object> stylesheetLinks = new List<object>();

        /// <summary>
        /// javascript code directly bound into the page code
        /// </summary>
        public List<string> scripts = new List<string>();

        /// <summary>
        /// additional lines added to the "<head>" segment of the page
        /// </summary>
        public string additionalHeadLines;

        /// <summary>
        /// The icon to display
        /// </summary>
        public string favicon = null;

        /// <summary>
        /// CSS code directly bound into the page code
        /// </summary>
        public string stylesheetCode;

        public PageBuilder(string title, string URL)
        {
            this.title = title;
            this.URL = URL;
            getContentMethod = buildContent;

            register();
        }

        protected string buildContent(SessionData sessionData)
        {
            string ret = "<html>\n<head>\n<title>" + title + "</title>\n";

            if (!string.IsNullOrWhiteSpace(favicon))
                ret += "<link rel=\"shortcut icon\" href='" + favicon + "'>\n";

            for (int i = 0; i < stylesheetLinks.Count; i++)
            {
                ret += "<link rel=\"stylesheet\" href='" + stylesheetLinks[i] + "'>\n";
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                ret += "<script type=\"text/javascript\">" + scripts[i] + "</script>\n";
            }

            if (!string.IsNullOrWhiteSpace(stylesheetCode))
                ret += "<style type=\"text/css\">" + stylesheetCode + "</style>";

            if (!string.IsNullOrWhiteSpace(additionalHeadLines))
                ret += additionalHeadLines;

            ret += "</head>\n<body ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < base.elements.Count; i++)
            {
                ret += base.elements[i].getContent(sessionData);
            }

            ret += "</body>\n</html>";

            return ret;
        }

        public string getContents(SessionData sessionData)
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
            Master.addFuntionToServer(URL, getContents);
        }

        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }
    }

    public abstract class HElement
    {
        public string id = "";
        public string name = "";

        public abstract string getContent(SessionData sessionData);
    }

    public class HNewLine : HElement
    {
        public override string getContent(SessionData sessionData)
        {
            return "\n<br>\n";
        }
    }

    public class HLine : HElement
    {
        public override string getContent(SessionData sessionData)
        {
            return "\n<hr>\n";
        }
    }

    public class HPlainText : HElement
    {
        public string text;

        public HPlainText(string text)
        {
            this.text = text;
        }

        public override string getContent(SessionData sessionData)
        {
            return text;
        }
    }

    public class HLink : HElement
    {
        string href, onclick, text, descriptionTags;

        public HLink(string text = "", string href = "", string onclick = "")
        {
            this.text = text;
            this.href = href;
            this.onclick = onclick;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<a ";

            if (href.Length > 0)
            {
                if (href[0] == '#')
                    ret += "href='" + href + "' ";
                else
                    ret += "href='#' ";

                string hash = SessionContainer.generateHash();
                string add = ";var f_"
                    + hash + "=document.createElement('form');f_"
                    + hash + ".setAttribute('method','POST');f_"
                    + hash + ".setAttribute('action','"
                        + href + "');f_"
                    + hash + ".setAttribute('enctype','text/html');var i_"
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

                ret += " onclick='" + onclick + add + "'";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(onclick))
                    ret += "onclick='" + onclick + "' ";
            }

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            ret += "</a>\n";

            return ret;
        }
    }

    public class HImage : HElement
    {
        string source, descriptionTags;

        public HImage(string source = "")
        {
            this.source = source;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<img ";

            if (!string.IsNullOrWhiteSpace(source))
                ret += "src='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            return ret;
        }
    }

    public class HText : HElement
    {
        string text, descriptionTags;

        public HText(string text = "")
        {
            this.text = text;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<p ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n" + text.Replace("\n","<br>") + "\n</p>\n";

            return ret;
        }
    }
    public class HHeadline : HElement
    {
        string text, descriptionTags;
        int level;

        public HHeadline(string text = "", int level = 1)
        {
            this.text = text;
            this.level = level;

            if (level > 6 || level < 1)
                throw new Exception("the level has to be between 1 and 6!");
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<h" + level + " ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n" + text.Replace("\n", "<br>") + "\n</h" + level + ">\n";

            return ret;
        }
    }

    public class HInput : HElement
    {
        public EInputType inputType;
        public string value;
        public string descriptionTags;

        public HInput(EInputType inputType, string name, string value = "")
        {
            this.inputType = inputType;
            this.name = name;
            this.value = value;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<input ";

            ret += "type='" + (inputType != EInputType.datetime_local ? inputType.ToString() : "datetime-local") + "' ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(value))
                ret += "value='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            return ret;
        }

        public enum EInputType : byte
        {
            button,
            checkbox,
            color,
            date,
            datetime,
            datetime_local,
            email,
            file,
            hidden,
            image,
            month,
            number,
            password,
            radio,
            range,
            reset,
            search,
            submit,
            tel,
            text,
            time,
            url,
            week,
        }
    }

    public class HContainer : HElement
    {
        public List<HElement> elements = new List<HElement>();
        public string text;
        public string descriptionTags;

        public void addElement(HElement element)
        {
            elements.Add(element);
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<div ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "'";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "'";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData);
            }

            ret += "\n</div>\n";

            return ret;
        }
    }

    public class HForm : HContainer
    {
        public string action;

        public HForm(string action)
        {
            this.action = action;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<form ";

            if (!string.IsNullOrWhiteSpace(action))
                ret += "action='" + action + "' ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += "method='POST' ";

            ret += ">\n<input type='hidden' name='ssid' value='" + sessionData.ssid + "'>\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData);
            }

            ret += "\n</form>\n";

            return ret;
        }
    }

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

        public override string getContent(SessionData sessionData)
        {
            string ret = "<button ";

            if (type != EButtonType.button)
                ret += "type='" + type + "' ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (href.Length > 0 && type != EButtonType.submit)
            {
                if (href[0] == '#')
                    ret += "href='" + href + "' ";
                else
                    ret += "href='#' ";

                string hash = SessionContainer.generateHash();
                string add = ";var f_"
                    + hash + "=document.createElement('form');f_"
                    + hash + ".setAttribute('method','POST');f_"
                    + hash + ".setAttribute('action','"
                        + href + "');f_"
                    + hash + ".setAttribute('enctype','text/html');var i_"
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

                ret += " onclick='" + onclick + add + "'";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(onclick))
                    ret += "onclick='" + onclick + "' ";
            }

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i].getContent(sessionData);
            }

            ret += "\n</button>\n";

            return ret;
        }

        public enum EButtonType : byte
        {
            button, reset, submit
        }
    }

    public class HList : HContainer
    {
        private EListType listType;

        public HList(EListType listType)
        {
            this.listType = listType;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<" + (listType == EListType.OrderedList ? "ol" : "ul") + " ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < elements.Count; i++)
            {
                ret += "<li>\n" + elements[i].getContent(sessionData) + "</li>\n";
            }

            ret += "</" + (listType == EListType.OrderedList ? "ol" : "ul") + ">\n";

            return ret;
        }

        public enum EListType : byte
        {
            OrderedList, UnorderedList
        }
    }

    public class HTable : HElement
    {
        private ICollection<ICollection<HElement>> data;
        public string descriptionTags;

        public HTable(ICollection<ICollection<HElement>> data)
        {
            this.data = data;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<table ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            foreach(ICollection<HElement> outer in data)
            {
                ret += "<tr>\n";

                foreach(HElement element in outer)
                {
                    ret += "<td>\n" + element.getContent(sessionData) + "</td>\n";
                }

                ret += "</tr>\n";
            }

            ret += "</table>\n";

            return ret;
        }

        public enum EListType : byte
        {
            OrderedList, UnorderedList
        }
    }

    public class HTag : HContainer
    {
        public bool hasContent;
        public string tagName;

        public HTag(string tagName, string descriptionTags, bool hasContent = false, string text = "")
        {
            this.tagName = tagName;
            this.descriptionTags = descriptionTags;
            this.hasContent = hasContent;
            this.text = text;
        }

        public override string getContent(SessionData sessionData)
        {
            string ret = "<" + tagName + " ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "'";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "'";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (hasContent)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    ret += text.Replace("\n", "<br>");

                for (int i = 0; i < elements.Count; i++)
                {
                    ret += elements[i].getContent(sessionData);
                }

                ret += "\n</" + tagName  + ">\n";
            }

            return ret;
        }
    }

    /// <summary>
    /// Non-static content, which is computed every request
    /// </summary>
    public class HRuntimeCode : HElement
    {
        public Master.getContents runtimeCode;

        /// <summary>
        /// Creates non-static content, which is computed every request
        /// </summary>
        /// <param name="runtimeCode">The code to execute every request</param>
        public HRuntimeCode(Master.getContents runtimeCode)
        {
            this.runtimeCode = runtimeCode;
        }

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
}