using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public class PageBuilder : HContainer
    {
        public Func<SessionData, string> getContentMethod;
        public string title;
        public string URL = "";

        /// <summary>
        /// Path to the stylesheets. Prefer strings. Else: toString() will be used
        /// </summary>
        public List<Object> stylesheets = new List<object>();

        public List<string> scripts = new List<string>();
        public string additionalHeadLines;
        public string favicon = null;

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
                ret += "<link rel='shortcut icon' href='" + favicon + "'>\n";

            for (int i = 0; i < stylesheets.Count; i++)
            {
                ret += "<link rel='stylesheet' href='" + stylesheets[i] + "'>\n";
            }

            for (int i = 0; i < scripts.Count; i++)
            {
                ret += "<script type='text / javascript'>" + scripts[i] + "</script>\n";
            }

            if (!string.IsNullOrWhiteSpace(additionalHeadLines))
                ret += additionalHeadLines;

            ret += "</head>\n<body";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < base.elements.Count; i++)
            {
                ret += base.elements[i];
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
                ret = "<b>An Error occured while processing the output</b><br>" + e.ToString().Replace("\r\n", "<br>");
            }

            return ret;
        }

        public void register(string URL = null)
        {
            if (URL == null)
                URL = this.URL;

            Master.callAddFunctionEvent(URL, getContents);
        }
    }

    public abstract class HElement
    {
        public string id = "";
        public string name = "";

        public abstract override string ToString();
    }

    public class HNewLine : HElement
    {
        public override string ToString()
        {
            return "\n<br>\n";
        }
    }

    public class HLine : HElement
    {
        public override string ToString()
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

        public override string ToString()
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

        public override string ToString()
        {
            string ret = "<a ";

            if (!string.IsNullOrWhiteSpace(href))
                ret += "href='" + href + "' ";

            if (!string.IsNullOrWhiteSpace(onclick))
                ret += "onclick='" + onclick + "' ";

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

        public override string ToString()
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

        public override string ToString()
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

    public class HContainer : HElement
    {
        public List<HElement> elements = new List<HElement>();
        public string text;
        public string descriptionTags;

        public void addElement(HElement element)
        {
            elements.Add(element);
        }

        public override string ToString()
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
                ret += elements[i];
            }

            ret += "\n</div>\n";

            return ret;
        }
    }

    public class HForm : HContainer
    {
        private SessionData sdata;

        public HForm(SessionData sessionData)
        {
            sdata = sessionData;
        }

        public override string ToString()
        {
            string ret = "<form ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += "method='POST' ";

            ret += ">\n<input type='hidden' name='ssid' value='" + sdata.ssid + "'>\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i];
            }

            ret += "\n</form>\n";

            return ret;
        }
    }

    public class HButton : HContainer
    {
        string href, onclick;
        EButtonType type;

        public HButton(string text = "", string href = "", EButtonType type = EButtonType.button, string onclick = "")
        {
            this.text = text;
            this.href = href;
            this.onclick = onclick;
            this.type = type;
        }

        public override string ToString()
        {
            string ret = "<button ";

            if (type != EButtonType.button)
                ret += "type='" + type + "' ";

            if (!string.IsNullOrWhiteSpace(id))
                ret += "id='" + id + "' ";

            if (!string.IsNullOrWhiteSpace(name))
                ret += "name='" + name + "' ";

            if (!string.IsNullOrWhiteSpace(href))
                ret += "href='" + href + "' ";

            if (!string.IsNullOrWhiteSpace(onclick))
                ret += "onclick='" + onclick + "' ";

            if (!string.IsNullOrWhiteSpace(descriptionTags))
                ret += descriptionTags;

            ret += ">\n";

            if (!string.IsNullOrWhiteSpace(text))
                ret += text.Replace("\n", "<br>");

            for (int i = 0; i < elements.Count; i++)
            {
                ret += elements[i];
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

        public override string ToString()
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
                ret += "<li>\n" + elements[i] + "</li>\n";
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

        public override string ToString()
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
                    ret += "<td>\n" + element + "</td>\n";
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
}
