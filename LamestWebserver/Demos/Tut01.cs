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
                    new HTextBlock("Regular", new HBold("bold"), new HItalic("italic"), new HText("HText"))
                },
                new List<HElement>()
                {
                    new HText(nameof(HPlainText)),
                    new HText("Copies an unencoded string to the final result."),
                    new HText("-"),
                    new HPlainText("&amp; vs. &<b>bold</b>")
                },
                new List<HElement>()
                {
                    new HText(nameof(HHeadline)),
                    new HText("A Headline."),
                    new HText("<h1> <h2> ..."),
                    new HContainer(new HHeadline("h1"), new HHeadline("h2", 2), new HHeadline("h3", 3))
                }
            });
        } 
    }
}
