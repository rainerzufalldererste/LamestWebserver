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

            // You might as well just use .ToString();
            return page.ToString();

            // Or for all you fishy dudes out there there's also this alternative
            return page * sessionData;
        }

        /// <summary>
        /// This method retrieves all elements for the page
        /// </summary>
        private IEnumerable<HElement> GetElements()
        {
            // We're using yield return here so it's easier to add to the page.
            yield return new HHeadline("Tutorial 01: HElements");

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
                    new HBold("Some Headline")
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
                    new HText(nameof(HContainer)),
                    new HText("A Container for other elements."),
                    new HText("<div>"),
                    new HContainer(new HText("hello"), new HBold("yello"), new HNewLine(), new HButton("cello", HButton.EButtonType.button))
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
                    new HText(nameof(HInput)),
                    new HText("An input element."),
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
                    + new HInput(HInput.EInputType.image, "imageInput") { DescriptionTags = "src='lwsfooter.png'" } + new HNewLine()
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
                new List<HElement>()
                {
                    new HText(nameof(HTextArea)),
                    new HText("A mutliline text input."),
                    new HText("<textarea>"),
                    new HTextArea("this\n is\n  some\n   text", null, 5)
                },
                new List<HElement>()
                {
                    new HText(nameof(HForm)),
                    new HText("A collection of inputs that can be sent to another page via HTTP POST."),
                    new HText("<form>"),
                    new HForm("") { Elements = {new HInput(HInput.EInputType.text, "text", "insert your text here."), new HButton("submit", HButton.EButtonType.submit)}}
                },
                new List<HElement>()
                {
                    new HText(nameof(HRuntimeCode)),
                    new HText("Code that will be executed at runtime (mostly just to add code in a big HElement block like this one here)."),
                    new HText("-"),
                    new HRuntimeCode((innerSessionData) => // The code returns string and takes an AbstractSessionIdentificator
                    {
                        string result = (innerSessionData as SessionData)?.GetHttpPostValue("text"); // only SessionData can get Post Values - other AbstractSessionIdentificators could be initiated from Websockets.

                        if (result == null) // GetHttpPostValue returns null if the value could not be found.
                            return new HString("You can enter a text in the HForm example above and click on the submit button.").GetContent(innerSessionData);
                        else
                            return new HString($"You entered '{result}'").GetContent(innerSessionData);
                    })
                },
                new List<HElement>()
                {
                    new HText(nameof(HLine)),
                    new HText("A horizontal line."),
                    new HText("<hr>"),
                    new HLine()
                },
            });
        } 
    }
}
