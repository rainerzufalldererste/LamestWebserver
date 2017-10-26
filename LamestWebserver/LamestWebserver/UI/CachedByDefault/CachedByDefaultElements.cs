using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.UI;
using LamestWebserver.Caching;

namespace LamestWebserver.UI.CachedByDefault
{
    /// <inheritdoc />
    public class CPageBuilder : PageBuilder
    {
        /// <inheritdoc />
        public CPageBuilder(string pagetitle, string URL) : base(pagetitle, URL)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CPageBuilder(string title, string URL, string referalURL, Func<SessionData, bool> conditionalCode) : base(title, URL, referalURL, conditionalCode)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CPageBuilder(string title) : base(title)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CLine : HLine
    {
        /// <inheritdoc />
        public CLine() : base()
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CPlainText : HPlainText
    {
        /// <inheritdoc />
        public CPlainText(string text = "") : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CString : HString
    {
        /// <inheritdoc />
        public CString(string text = "") : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CLink : HLink
    {
        /// <inheritdoc />
        public CLink(string text = "", string href = "", string onclick = "") : base(text, href, onclick)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CImage : HImage
    {
        /// <inheritdoc />
        public CImage(string source) : base(source)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CText : HText
    {
        /// <inheritdoc />
        public CText(string text) : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CTextBlock : HTextBlock
    {
        /// <inheritdoc />
        public CTextBlock(string text = "") : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CTextBlock(params object[] texts) : base(texts)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CBold : HBold
    {
        /// <inheritdoc />
        public CBold(string text) : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CItalic : HItalic
    {
        /// <inheritdoc />
        public CItalic(string text) : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CCrossedOut : HCrossedOut
    {
        /// <inheritdoc />
        public CCrossedOut(string text) : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CUnderlined : HUnderlined
    {
        /// <inheritdoc />
        public CUnderlined(string text) : base(text)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CHeadline : HHeadline
    {
        /// <inheritdoc />
        public CHeadline(string text = "", int level = 1) : base(text, level)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CInput : HInput
    {
        /// <inheritdoc />
        public CInput(EInputType inputType, string name, string value = "") : base(inputType, name, value)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CSingleSelector : HSingleSelector
    {
        /// <inheritdoc />
        public CSingleSelector(string name, List<Tuple<string, string>> nameValuePairs, int selectedIndex = 0, bool newLineAfterSelection = true) : base(name, nameValuePairs, selectedIndex, newLineAfterSelection)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CTextInput : HTextInput
    {
        /// <inheritdoc />
        public CTextInput(string name, string value = "", string placeholderText = "") : base(name, value, placeholderText)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CPasswordInput : HPasswordInput
    {
        /// <inheritdoc />
        public CPasswordInput(string name, string placeholderText = "") : base(name, placeholderText)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CRadioButton : HRadioButton
    {
        /// <inheritdoc />
        public CRadioButton(string name, string value, string text = null, bool _checked = true) : base(name, value, text, _checked)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CCheckBox : HCheckBox
    {
        /// <inheritdoc />
        public CCheckBox(string name, string value, string text = null, bool _checked = true) : base(name, value, text, _checked)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CContainer : HContainer
    {
        /// <inheritdoc />
        public CContainer(params HElement[] elements) : base(elements)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CContainer(IEnumerable<HElement> elements) : base(elements)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CInlineContainer : HInlineContainer
    {
        /// <inheritdoc />
        public CInlineContainer(params HElement[] elements) : base(elements)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CQuote : HQuote
    {
        /// <inheritdoc />
        public CQuote(string text, string source = null, params HElement[] elements) : base(text, source, elements)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CForm : HForm
    {
        /// <inheritdoc />
        public CForm(string action) : base(action)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CForm(string action, bool addSubmitButton, string buttontext = "", params Tuple<string, string>[] values) : base(action, addSubmitButton, buttontext, values)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CPanel : HPanel
    {
        /// <inheritdoc />
        public CPanel(string legend = null, params HElement[] elements) : base(legend, elements)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CButton : HButton
    {
        /// <inheritdoc />
        public CButton(string text, EButtonType type = EButtonType.button, string href = "", string onclick = "") : base(text, type, href, onclick)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CButton(string text, string href = "", string onclick = "") : base(text, href, onclick)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CDropDownMenu : HDropDownMenu
    {
        /// <inheritdoc />
        public CDropDownMenu(string name, int size, bool multipleSelectable, params Tuple<string, string>[] textValuePairsToDisplay) : base(name, size, multipleSelectable, textValuePairsToDisplay)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CDropDownMenu(string name, params Tuple<string, string>[] textValuePairsToDisplay) : base(name, textValuePairsToDisplay)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CList : HList
    {
        /// <inheritdoc />
        public CList(EListType listType) : base(listType)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CList(EListType listType, IEnumerable<string> input) : base(listType, input)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CList(EListType listType, params HElement[] elements) : base(listType, elements)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CList(EListType listType, IEnumerable<HElement> elements) : base(listType, elements)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CTable : HTable
    {
        /// <inheritdoc />
        public CTable(IEnumerable<IEnumerable<object>> elements) : base(elements)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CTable(params IEnumerable<object>[] data) : base(data)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CScript : HScript
    {
        /// <inheritdoc />
        public CScript(string scriptText) : base(scriptText)
        {
            CachingType = ECachingType.Cacheable;
        }

        /// <inheritdoc />
        public CScript(ScriptCollection.ScriptFuction scriptFunction, params object[] arguments) : base(scriptFunction, arguments)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CScriptLink : HScriptLink
    {
        /// <inheritdoc />
        public CScriptLink(string URL) : base(URL)
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CCanvas : HCanvas
    {
        /// <inheritdoc />
        public CCanvas() : base()
        {
            CachingType = ECachingType.Cacheable;
        }
    }

    /// <inheritdoc />
    public class CTextArea : HTextArea
    {
        /// <inheritdoc />
        public CTextArea(string value = "", uint? cols = null, uint? rows = null) : base(value, cols, rows)
        {
            CachingType = ECachingType.Cacheable;
        }
    }
}
