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
    /// This class inherits from ElementResponse, a prototype for responding UI Elements to the client.
    /// There should be always only one instance of this class.
    /// Whenever an instance is created, the current instance is the one registered as response at the server.
    /// </summary>
    public class MainPage : ElementResponse
    {
        /// <summary>
        /// Register this Page to be the default response of the server - located at the "/" URL
        /// You don't need to call this constructor anywhere if you are using Master.DiscoverPages() or the LamestWebserver Host Service.
        /// If you want to let your constructor be called automatically, please make sure, that it needs no parameters to be called - like this one.
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
            var page = new PageBuilder("LamestWebserver Reference"); // <- the title displayed in the browser window.

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
            container.AddElement(new HText("This is a guide and a tutorial on LamestWebserver at the same time."
                + " The code for every page of this website has a very indepth description on how everything is done."
                + " It might be helpful to browse the code while viewing this reference for better understanding."));

            // Add a new container with the class footer to the page, containing an image from the data directory ("/web") and the current filename and version
            page.AddElement(new HContainer
            {
                Class = "footer",
                Elements =
                {
                    new HImage("lwsfooter.png"),
                    new HText($"{nameof(MainPage)}.cs\nLamestWebserver Reference v{typeof(MainPage).Assembly.GetName().Version}")
                }
            });

            // Return the response.
            return page;
        }

        /// <summary>
        /// Let's just create a prototype of this layout, so we can use it more easily.
        /// Don't worry too much about the `HSelectivelyCacheableElement`. You'll learn more about that in the Caching tutorial.
        /// </summary>
        /// <param name="elements">the elements displayed on the page</param>
        /// <param name="filename">the filename to display</param>
        /// <returns>the page includig all layout elements</returns>
        internal static HSelectivelyCacheableElement GetPage(IEnumerable<HElement> elements, string filename)
        {
            // Create the page
            var page = new PageBuilder("LamestWebserver Reference") { StylesheetLinks = {"style.css"} };

            // Add the main-Container with all the elements and the footer
            page.AddElements(
                new HContainer()
                {
                    Class = "main",
                    Elements = elements.ToList(), 
                    
                    // We'll take a look at what this does in the Caching tutorial.
                    CachingType = LamestWebserver.Caching.ECachingType.Cacheable
                },
                new HContainer()
                {
                    Class = "footer",
                    Elements =
                    {
                        new HImage("lwsfooter.png"),
                        new HText(filename + "\nLamestWebserver Reference v" + typeof(MainPage).Assembly.GetName().Version)
                    },

                    // We'll take a look at what this does in the Caching tutorial.
                    CachingType = LamestWebserver.Caching.ECachingType.Cacheable
                });

            return page;
        }
    }
}
