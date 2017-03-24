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

        public HLinkSearchBox(Func<AbstractSessionIdentificator, string, IEnumerable<Tuple<string, string>>> responseFunction, string responseUrl = null, string placeholder = null)
        {
            if (responseUrl == null)
                responseUrl = SessionContainer.GenerateUnusedHash();

            _pageUrl = responseUrl;
            _func = responseFunction;
            ContainerID = responseUrl;
            Placeholder = placeholder;

            if (responseUrl != null)
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
            input.onclick = JSFunctionCall.DisplayElementByID(container.ID);
            input.onfocus = input.onclick;
            input.oninput = input.SetInnerHTMLWithNameValueAsync(JSElement.GetByID(container.ID), _pageUrl, JSFunctionCall.DisplayElementByID(container.ID));
            input.onfocusout = JSFunctionCall.HideElementByID(container.ID);

            return input + container;
        }

        protected string GetResponse(AbstractSessionIdentificator sessionData)
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

            for (int i = 0; i < entries.Length; i++)
            {
                ret += new HLink(entries[i].Item1, entries[i].Item2)
                {
                    DescriptionTags = "onmousedown='" + new JScript(JSValue.CurrentBrowserURL.Set((JSStringValue) entries[i].Item2), JSElement.GetByID(ContainerID).Hide) + "'"
                };

                if (i + 1 < entries.Length)
                    ret += new HNewLine();
            }

            return ret;
        }
    }

    /// <summary>
    /// A button that cycles through multiple distinct states on click.
    /// </summary>
    public class HMultipleValuesButton : HElement
    {
        public string DescriptionTags = "";

        private List<Tuple<string, IEnumerable<HElement>>> Values;
        private string hiddenElementID = SessionContainer.GenerateHash();

        private string _pageURL;

        public HMultipleValuesButton(string name)
        {
            Name = name;
            ID = SessionContainer.GenerateHash();
            Values = new List<Tuple<string, IEnumerable<HElement>>>();
        }

        public HMultipleValuesButton(string name, params Tuple<string, string>[] values) : this(name)
        {
            foreach (Tuple<string, string> value in values)
            {
                Values.Add(new Tuple<string, IEnumerable<HElement>>(value.Item1, new[] {(HElement) new HString(value.Item2)}));
            }
        }

        public HMultipleValuesButton(string name, params Tuple<string, HElement>[] values) : this(name)
        {
            foreach (Tuple<string, HElement> value in values)
            {
                Values.Add(new Tuple<string, IEnumerable<HElement>>(value.Item1, new[] {value.Item2}));
            }
        }

        public HMultipleValuesButton(string name, params Tuple<string, IEnumerable<HElement>>[] values)
        {
            Name = name;
            ID = SessionContainer.GenerateHash();
            Values = values.ToList();
        }

        public void AddValue(string value, string text)
        {
            Values.Add(new Tuple<string, IEnumerable<HElement>>(value, new[] {(HElement) new HString(text)}));
        }

        public void AddValue(string value, params HElement[] element)
        {
            Values.Add(new Tuple<string, IEnumerable<HElement>>(value, element));
        }

        public string GetCurrentValue(AbstractSessionIdentificator sessionData)
        {
            if (!(sessionData is SessionData))
                return null;
            else
                return ((SessionData) sessionData).GetHttpPostValue(Name);
        }

        /// <inheritdoc />
        public override string GetContent(AbstractSessionIdentificator sessionData)
        {
            if (Values == null || Values.Count == 0)
                throw new ArgumentException($"No Values given to cycle through.");

            string defaultContents = "";

            foreach (var element in Values.First().Item2)
                defaultContents += element.GetContent(sessionData);

            var case0 = new JScript(JSElement.GetByID(hiddenElementID).Value.Set((JSStringValue) Values.First().Item1),
                JSElement.GetByID(ID).InnerHTML.Set((JSStringValue) defaultContents));

            var switchStatement = new JSSwitch(JSElement.GetByID(hiddenElementID).Value, case0, new Tuple<IJSValue, IJSPiece>((JSStringValue) Values.Last().Item1, case0));

            for (int i = 1; i < Values.Count; i++)
            {
                string contents = "";

                foreach (var element in Values[i].Item2)
                    contents += element.GetContent(sessionData);

                switchStatement.AddCase((JSStringValue) Values[i - 1].Item1,
                    new JScript(JSElement.GetByID(hiddenElementID).Value.Set((JSStringValue) Values[i].Item1), JSElement.GetByID(ID).InnerHTML.Set((JSStringValue) contents)));
            }
            return new HInput(HInput.EInputType.hidden, Name, Values.First().Item1) {ID = hiddenElementID} +
                   new HButton("", HButton.EButtonType.button, "", new JScript(switchStatement).ToString()) {Elements = Values.First().Item2.ToList(), ID = ID, Class = Class, Style = Style, DescriptionTags = DescriptionTags, Title = Title};
        }
    }
}
