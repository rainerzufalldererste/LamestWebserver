using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.UI;

namespace Demos
{
    /// <summary>
    /// A PageResponse is just another example of a Page in the server - this one returns a string in the GetContents method
    /// </summary>
    public class Tut01 : PageResponse
    {
        /// <summary>
        /// Again, just an empty constructor for auto-discovery.
        /// </summary>
        public Tut01() : base(nameof(Tut01)) // <- The nameof(Tut01) is just used to name the URL just like this class is called
        {
        }

        protected override string GetContents(SessionData sessionData)
        {
            // Get the default layout around the elements retrieved by GetElements()
            HElement page = MainPage.GetPage(GetElements(), nameof(Tut01) + ".cs");

            // To get the HTML-string of an HElement, call GetContent with the current session data.
            return page.GetContent(sessionData);

#pragma warning disable CS0162
            // You might as well just use .ToString();
            return page.ToString();

            // Or for all you fishy dudes out there: there's also this alternative
            return page * sessionData;
#pragma warning restore CS0162
        }

        /// <summary>
        /// This method retrieves all elements for the page
        /// </summary>
        private IEnumerable<HElement> GetElements()
        {
            // We're using yield return here so it's easier to add to the page.
            yield return new HHeadline("Tutorial 01: HElements");

            yield return new HText("Constructed Pages in LamestWebserver are built out of HElement objects, which are mostly a direct representation of the HTML-Tag counterparts. You can find these Elements in the Namespace 'LamestWebserver.UI'.");

            yield return new HHeadline("Basic Elements", 2);

            yield return new HTable(new List<List<HElement>>()
            {
                new List<HElement>()
                {
                    new HBold("Class"),
                    new HBold("Purpose"),
                    new HBold("HTML-Tag"),
                    new HBold("Example")
                },
                new List<HElement>()
                {
                    new HText(nameof(HText)),
                    new HText("Represents a piece of text."),
                    new HText("<p>"),
                    new HText("Hello World!")
                },
                new List<HElement>()
                {
                    new HText(nameof(HBold)),
                    new HText("A bold text."),
                    new HText("<b>"),
                    new HBold("Some bold text.")
                },
                new List<HElement>()
                {
                    new HText(nameof(HItalic)),
                    new HText("An italic text."),
                    new HText("<i>"),
                    new HItalic("Pizza Calzone")
                },
                new List<HElement>()
                {
                    new HText(nameof(HUnderlined)),
                    new HText("An underlined text."),
                    new HText("<u>"),
                    new HUnderlined("Offer of the week")
                },
                new List<HElement>()
                {
                    new HText(nameof(HCrossedOut)),
                    new HText("An underlined text."),
                    new HText("<del>"),
                    new HCrossedOut("Free Pasta!")
                },
                new List<HElement>()
                {
                    new HText(nameof(HTextBlock)),
                    new HText("Can combine text with e.g. bold and italic text. (Every HElement)"),
                    new HText("<p>"),
                    new HTextBlock("Regular", new HBold("Bold"), new HItalic("Italic"), new HText("HText"))
                },
                new List<HElement>()
                {
                    new HText(nameof(HPlainText)),
                    new HText("Copies an unencoded string to the final result."),
                    new HText("-"),
                    new HPlainText("&amp; vs. & / <b>bold</b>")
                },
                new List<HElement>()
                {
                    new HText(nameof(HString)),
                    new HText("Copies a string to the final result like HPlainText but the string is automatically encoded before."),
                    new HText("-"),
                    new HString("&amp; vs. & / <b>bold</b>")
                },
                new List<HElement>()
                {
                    new HText(nameof(HMultipleElements)),
                    new HText("Contains multiple HElements without a surrounding element."),
                    new HText("-"),
                    new HMultipleElements(new HBold("bold+") + new HItalic("italic+"), new HText("regular"))
                },
                new List<HElement>()
                {
                    new HText(nameof(HNewLine)),
                    new HText("A line break"),
                    new HText("<br>"),
                    new HText("one line") + new HNewLine() + new HText("another line")
                },
                new List<HElement>()
                {
                    new HText(nameof(HHeadline)),
                    new HText("A Headline."),
                    new HText("<h1> <h2> ..."),
                    new HHeadline("h1") + new HHeadline("h2", 2) + new HHeadline("h3", 3)
                },
                new List<HElement>()
                {
                    new HText(nameof(HImage)),
                    new HText("An image."),
                    new HText("<img>"),
                    new HImage("lwsfooter.png")
                },
                new List<HElement>()
                {
                    new HText(nameof(HLine)),
                    new HText("A horizontal line."),
                    new HText("<hr>"),
                    new HLine()
                },
                new List<HElement>()
                {
                    new HText(nameof(HQuote)),
                    new HText("A quote."),
                    new HText("<blockquote>"),
                    new HQuote("Sources shall be quoted.\n- Some Guy on the Internet", "http://some.source.com/")
                },
                new List<HElement>()
                {
                    new HText(nameof(HCanvas)),
                    new HText("A HTML-Canvas for drawing on using JavaScript code."),
                    new HText("<canvas>"),
                    new HCanvas() {Style = "width:100px; height:100px; background-color:#fb905f;"}
                },
                new List<HElement>()
                {
                    new HText(nameof(HTag)),
                    new HText("A custom html tag."),
                    new HText("custom"),
                    new HTag("a", "href=\"\"", true, "I am a Link")
                    + new HTag("img", "src=\"lwsfooter.png\"")
                },
            });

            yield return new HHeadline("Container Elements", 2);

            yield return new HTable(new List<List<HElement>>()
            {
                new List<HElement>()
                {
                    new HBold("Class"),
                    new HBold("Purpose"),
                    new HBold("HTML-Tag"),
                    new HBold("Example")
                },
                new List<HElement>()
                {
                    new HText(nameof(HContainer)),
                    new HText("A Container for other elements."),
                    new HText("<div>"),
                    new HContainer(new HText("hello"), new HBold("yello")) + new HContainer(new HButton("cello", HButton.EButtonType.button))
                },
                new List<HElement>()
                {
                    new HText(nameof(HInlineContainer)),
                    new HText("An Inline-Container for other elements and text."),
                    new HText("<span>"),
                    new HInlineContainer(new HText("hello"), new HBold("yello")) + new HInlineContainer(new HButton("cello", HButton.EButtonType.button))
                },
                new List<HElement>()
                {
                    new HText(nameof(HPanel)),
                    new HText("A grouped collection of elements."),
                    new HText("<fieldset>"),
                    new HPanel(null, new HButton("button", HButton.EButtonType.submit))
                     + new HPanel("Your Title could be here", new HButton("button2", HButton.EButtonType.submit))
                },
            });

            yield return new HHeadline("Interactive Elements", 2);

            yield return new HTable(new List<List<HElement>>()
            {
                new List<HElement>()
                {
                    new HBold("Class"),
                    new HBold("Purpose"),
                    new HBold("HTML-Tag"),
                    new HBold("Example")
                },
                new List<HElement>()
                {
                    new HText(nameof(HLink)),
                    new HText("A link"),
                    new HText("<a>"),
                    new HLink("Go back to the MainPage", "/")
                },
                new List<HElement>()
                {
                    new HText(nameof(HButton)),
                    new HText("A Button."),
                    new HText("<button>"),
                    new HButton("button", HButton.EButtonType.button, "")
                },
                new List<HElement>()
                {
                    new HText(nameof(HForm)),
                    new HText("A collection of inputs that can be sent to another page via HTTP POST."),
                    new HText("<form>"),
                    new HForm("") {Elements = {new HTextInput("text", "", "insert your text here."), new HButton("submit", HButton.EButtonType.submit)}}
                },
                new List<HElement>()
                {
                    new HText(nameof(HTextInput)),
                    new HText("A simple text input."),
                    new HText("<input>"),
                    new HTextInput("textInput4", "", "placeholder text")
                },
                new List<HElement>()
                {
                    new HText(nameof(HTextArea)),
                    new HText("A mutliline text input."),
                    new HText("<textarea>"),
                    new HTextArea("this\n is\n  some\n   text", null, 5)
                },
                new List<HElement>()
                {
                    new HText(nameof(HPasswordInput)),
                    new HText("A simple password input."),
                    new HText("<input>"),
                    new HPasswordInput("passwordInput2", "placeholder text")
                },
                new List<HElement>()
                {
                    new HText(nameof(HCheckBox)),
                    new HText("A simple checkbox input."),
                    new HText("<input> <label>"),
                    new HCheckBox("checkBox1", "checkBox1Value", null, true) + new HNewLine()
                    + new HCheckBox("checkBox2", "checkBox2Value", "Check Box Text", true) + new HNewLine()
                    + new HCheckBox("checkBox3", "checkBox3Value", null, false) + new HNewLine()
                    + new HCheckBox("checkBox4", "checkBox4Value", "Next Check Box text", false)
                },
                new List<HElement>()
                {
                    new HText(nameof(HRadioButton)),
                    new HText("A simple radio button input."),
                    new HText("<input> <label>"),
                    new HRadioButton("radioButton1", "radioButton1Value", null, true) + new HNewLine()
                    + new HRadioButton("radioButton2", "radioButton2Value", "Radio Button Text", true) + new HNewLine()
                    + new HRadioButton("radioButton3", "radioButton3Value", null, false) + new HNewLine()
                    + new HRadioButton("radioButton4", "radioButton4Value", "Next Radio Button text", false)
                },
                new List<HElement>()
                {
                    new HText(nameof(HDropDownMenu)),
                    new HText("A dropdown menu."),
                    new HText("<select>"),
                    new HDropDownMenu("dropDown", new Tuple<string, string>("Some", "first"), new Tuple<string, string>("Thing", "second"))
                    + new HNewLine()
                    + new HNewLine()
                    + new HDropDownMenu("dropDown", 3, true, new Tuple<string, string>("Some", "first"), new Tuple<string, string>("Thing", "second"),
                        new Tuple<string, string>("With", "third"), new Tuple<string, string>("More", "fourth"),
                        new Tuple<string, string>("Concurrent", "fifth"), new Tuple<string, string>("Options", "sixth"))
                },
                new List<HElement>()
                {
                    new HText(nameof(HSingleSelector)),
                    new HText("A list of radio-buttons, in which only one can be selected once."),
                    new HText("<input> <label>"),
                    new HSingleSelector("groupedRadioButtons", new List<Tuple<string, string>>()
                    {
                        new Tuple<string, string>("Zero", "zerothItem"),
                        new Tuple<string, string>("First", "firstItem"),
                        new Tuple<string, string>("Second", "secondItem"),
                        new Tuple<string, string>("Third", "thirdItem"),
                        new Tuple<string, string>("Fourth", "fourthItem"),
                        new Tuple<string, string>("Fifth", "fifthItem")
                    }, 2, true)
                },
                new List<HElement>()
                {
                    new HText(nameof(HInput)),
                    new HText("A custom input element."),
                    new HText("<input>"),
                    new HInput(HInput.EInputType.text, "textInput", "textInput") + new HNewLine()
                    + new HInput(HInput.EInputType.button, "buttonInput", "buttonInput") + new HNewLine()
                    + new HInput(HInput.EInputType.checkbox, "checkBoxInput") + new HNewLine()
                    + new HInput(HInput.EInputType.color, "colorInput") + new HNewLine()
                    + new HInput(HInput.EInputType.date, "dateInput", "dateInput") + new HNewLine()
                    + new HInput(HInput.EInputType.datetime, "dateTimeInput") + new HNewLine()
                    + new HInput(HInput.EInputType.datetime_local, "dateTimeLocalInput") + new HNewLine()
                    + new HInput(HInput.EInputType.email, "emailInput", "emailInput") + new HNewLine()
                    + new HInput(HInput.EInputType.file, "fileInput") + new HNewLine()
                    + new HInput(HInput.EInputType.hidden, "hiddenInput", "hidden") + new HItalic("<- Hidden Input") + new HNewLine()
                    + new HInput(HInput.EInputType.image, "imageInput") {DescriptionTags = "src='lwsfooter.png'"} + new HNewLine()
                    + new HInput(HInput.EInputType.month, "monthInput", "monthInput") + new HNewLine()
                    + new HInput(HInput.EInputType.number, "numberInput", "1337") + new HNewLine()
                    + new HInput(HInput.EInputType.password, "passwordInput", "password") + new HNewLine()
                    + new HInput(HInput.EInputType.radio, "radioInput") + new HNewLine()
                    + new HInput(HInput.EInputType.range, "rangeInput", "5.5") {DescriptionTags = "min='0' max='10' step='0.5'"} + new HNewLine()
                    + new HInput(HInput.EInputType.reset, "resetInput", "resetInput") + new HNewLine()
                    + new HInput(HInput.EInputType.search, "searchInput", "searchInput") + new HNewLine()
                    + new HInput(HInput.EInputType.submit, "submitInput", "submitInput") + new HNewLine()
                    + new HInput(HInput.EInputType.tel, "telInput", "telInput") + new HNewLine()
                    + new HInput(HInput.EInputType.time, "timeInput", "timeInput") + new HNewLine()
                    + new HInput(HInput.EInputType.url, "urlInput", "urlInput") + new HNewLine()
                    + new HInput(HInput.EInputType.week, "weekInput", "weekInput") + new HNewLine()
                },
            });
            
            yield return new HHeadline("Structural Elements", 2);

            yield return new HTable(new List<List<HElement>>()
            {
                new List<HElement>()
                {
                    new HBold("Class"),
                    new HBold("Purpose"),
                    new HBold("HTML-Tag"),
                    new HBold("Example")
                },
                new List<HElement>()
                {
                    new HText(nameof(HList)),
                    new HText("List of elements."),
                    new HText("<ol> <ul>"),
                    new HList(HList.EListType.OrderedList, new string[] {"this", "is", "an", "ordered", "list" })
                    +
                    new HList(HList.EListType.UnorderedList, new HInput(HInput.EInputType.text, "textInput2", "Hello!"), "this", "is", "an", "unordered", "list",
                        new HImage("lwsfooter.png"))
                },
                new List<HElement>()
                {
                    new HText(nameof(HTable)),
                    new HText("A table."),
                    new HText("<table>"),
                    new HTable(new List<List<HElement>>()
                    {
                        new List<HElement>
                        {
                            new HText("Everything"),
                            new HItalic("in this table"),
                            new HInput(HInput.EInputType.text, "textInput3", "speaking about HElements")
                        },
                        new List<HElement>
                        {
                            new HText("has been created"),
                            new HBold("just like this"),
                            new HImage("lwsfooter.png")
                        }
                    })
                },
            });

            yield return new HHeadline("Runtime-Dependent Elements", 2);

            yield return new HTable(new List<List<HElement>>()
            {
                new List<HElement>()
                {
                    new HBold("Class"),
                    new HBold("Purpose"),
                    new HBold("HTML-Tag"),
                    new HBold("Example")
                },
                new List<HElement>()
                {
                    new HText(nameof(HRuntimeCode)),
                    new HText("Code that will be executed at runtime (mostly just to add code in a big HElement block like this one here)."),
                    new HText("-"),
                    new HRuntimeCode((innerSessionData) => // The code returns string and takes an AbstractSessionIdentificator
                    {
                        string result = innerSessionData.GetHttpPostValue("text");
                        
                        if (result == null) // GetHttpPostValue returns null if the value could not be found.
                            return new HString("You can enter a text in the HForm example above and click on the submit button.").GetContent(innerSessionData);
                        else
                            return new HString($"You entered '{result}'").GetContent(innerSessionData);
                    })
                },
            });

            yield return new HHeadline("JavaScript Embedding Elements", 2);

            yield return new HTable(new List<List<HElement>>()
            {
                new List<HElement>()
                {
                    new HBold("Class"),
                    new HBold("Purpose"),
                    new HBold("HTML-Tag"),
                    new HBold("Example")
                },
                new List<HElement>()
                {
                    new HText(nameof(HScript)),
                    new HText("A script embedded in the webpage."),
                    new HText("<script>"),
                    new HScript("")
                    + new HScript(ScriptCollection.GetPageReloadInMilliseconds, new object[] {60000000})
                    // the first parameter is a delegate, the second one is the parameters expected for this delegate to work
                    // the delegates in ScriptCollection specify the parameters in their comments
                    // In this case the parameters are obviously the milliseconds to wait.
                },
                new List<HElement>()
                {
                    new HText(nameof(HScriptLink)),
                    new HText("A linked script file embedded in the webpage."),
                    new HText("<script>"),
                    new HScriptLink("script.js")
                },
            });
        }
    }
}
