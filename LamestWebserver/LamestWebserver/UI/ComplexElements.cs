using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.JScriptBuilder;

namespace LamestWebserver.UI
{
    public class HLinkSearchBox : HElement
    {
        public string Value = "";
        public string Placeholder = "";
        private readonly string _pageUrl;
        private readonly Func<AbstractSessionIdentificator, string, IEnumerable<Tuple<string, string>>> _func;
        public string ContainerID;

        public HLinkSearchBox(string responseUrl, Func<AbstractSessionIdentificator, string, IEnumerable<Tuple<string, string>>> responseFunction, string placeholder = null)
        {
            _pageUrl = responseUrl;
            _func = responseFunction;
            ContainerID = responseUrl;
            Placeholder = placeholder;

            if(responseUrl != null)
                InstantPageResponse.AddInstantPageResponse(_pageUrl, GetResponse);
        }

        /// <inheritdoc />
        public override string GetContent(AbstractSessionIdentificator sessionData)
        {
            var input = new JSInput(HInput.EInputType.text, Name, Value) {Class = Class, Style = Style, Title = Title};

            if (!string.IsNullOrWhiteSpace(Placeholder))
                input.DescriptionTags = "placeholder='" + Placeholder + "' ";

            if (!string.IsNullOrWhiteSpace(ID))
                input.ID = ID;
            else
                input.ID = SessionContainer.GenerateHash();

            var container = new HContainer() {ID = ContainerID, Style = "display:none;"};
            input.onfocus = input.SetInnerHTMLWithNameValueAsync(JSElement.GetByID(container.ID), _pageUrl, JSFunctionCall.DisplayElementByID(container.ID));
            input.onclick = input.onfocus;
            input.oninput = input.onfocus;
            input.onfocusout = JSFunctionCall.HideElementByID(container.ID);

            return input + container;
        }

        public string GetResponse(AbstractSessionIdentificator sessionData)
        {
            string param = "";

            if (sessionData is SessionData)
            {
                param = ((SessionData) sessionData).GetHttpHeadValue("value");
            }

            if (param == null)
                param = "";

            var entries = _func(sessionData, param).ToArray();

            string ret = "";

            for(int i = 0; i < entries.Length; i++)
            {
                ret += new HLink(entries[i].Item1, entries[i].Item2) {DescriptionTags = "onmousedown='" + new JScript(JSValue.CurrentBrowserURL.Set((JSStringValue)entries[i].Item2), JSElement.GetByID(ContainerID).Hide) + "'"};

                if (i + 1 < entries.Length)
                    ret += new HNewLine();
            }

            return ret;
        }
    }
}
