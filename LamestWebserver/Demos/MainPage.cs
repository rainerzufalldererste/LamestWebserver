using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.UI;

namespace Demos
{
    public class MainPage : ElementResponse
    {
        /// <summary>
        /// Register this Page to be the default response of the server - located at the "/" URL
        /// </summary>
        public MainPage() : base("/")
        {
        }

        /// <summary>
        /// This method retrieves the page for the user
        /// </summary>
        /// <param name="sessionData">the sessionData for the current user</param>
        /// <returns>the response</returns>
        protected override HElement GetElement(SessionData sessionData)
        {
            // Create a new Page outline for the browser. 
            var page = new PageBuilder("LamestWebserver Tutorial"); // <- the title displayed in the browser window.

            // Add the stylesheet to be referenced in the page.
            page.StylesheetLinks.Add("style.css");

            // Create a div-element - a container - with the class "main" (used in css stylesheet).
            var container = new HContainer()
            {
                Class = "main"
            };

            // Add the container to the page
            page.AddElement(container);

            // Add a Headline and a Text to the page.
            container.AddElement(new HHeadline("LamestWebserver Tutorial / Reference"));
            container.AddElement(new HText("This is a guide and a tutorial on LamestWebserver at the same time. The code for every page of this website has very indepth description on how things are working."));

            // Add a new container with the class footer to the page, containing an image from the data directory ("/web") and the current filename and version
            page.AddElement(new HContainer
            {
                Class = "footer",
                Elements =
                {
                    new HImage("lwsfooter.png"),
                    new HText($"{typeof(MainPage).Name}.cs\nLamestWebserver Reference v{typeof(MainPage).Assembly.GetName().Version}")
                }
            });

            // Return the response.
            return page;
        }
    }
}
