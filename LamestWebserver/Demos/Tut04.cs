using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.UI;
using LamestWebserver.JScriptBuilder;

namespace Demos
{
    public class Tut04 : DirectoryElementResponse
    {
        public Tut04() : base(nameof(Tut04)) { }

        /// <summary>
        /// This method is executed whenever the directory-page has been requested.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <param name="subUrl">The suburl that this page has been called with.</param>
        /// <returns>The response page as HElement.</returns>
        protected override HElement GetElement(SessionData sessionData, string subUrl)
        {
            return MainPage.GetPage(GetPageContents(sessionData, subUrl), nameof(Tut04) + ".cs");
        }

        private IEnumerable<HElement> GetPageContents(SessionData sessionData, string subUrl)
        {
            yield return new HHeadline("Directoy Responses");

            if(string.IsNullOrEmpty(subUrl))
            {
                yield return new HText($"Directoy Responses are called for all request targeting a specific sub-directory like '{URL}/<suburl>' for this {nameof(DirectoryElementResponse)}.");
                yield return new HText($"Enter a suburl to visit:");

                // Create a text-field that changes the sub-url, a button should go to on click.
                JSVariable suburl = new JSVariable();
                JSButton button = new JSButton("Go to Sub-URL");
                button.onclick = new JScript(JSValue.CurrentBrowserURL.Set(new JSStringValue($"{URL}/") + suburl.Name));

                JSInput input = new JSInput(HInput.EInputType.text, "suburl");
                input.onchange = new JScript(suburl.Set(input.GetInnerValue()));

                yield return new HContainer(new HScript(suburl.GetJsCode(sessionData)), input, button) { Class = "fit" };
            }
            else
            {
                yield return new HHeadline($"Sub-URL '{subUrl}' has been called.", 3);
                yield return new HTextBlock($"You can return to the main-page of this {nameof(DirectoryElementResponse)} by clicking ", new HLink("here", "/" + URL) { Style="display: initial;" }, ".");
            }
        }
    }
}
