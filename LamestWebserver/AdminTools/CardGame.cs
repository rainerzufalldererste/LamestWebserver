using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LameNetHook;

namespace Demos
{
    public static class CardGame
    {
        public class loginScreen : PageBuilder
        {
            public loginScreen() : base("CardGame - Login", "cgame/")
            {
                descriptionTags = "style='font-family: sans-serif; background-color: #444444;'";

                addElement(
                    new HForm("lobby")
                    {
                        descriptionTags = "style='width: 80%; margin: 5em auto; padding: 50px; background-color: #fff; border-radius: 1em;'",
                        elements = new List<HElement>()
                        {
                            new HHeadline("Login:", 1),
                            new HText("Pick a UserName"),
                            new HInput(HInput.EInputType.text, "user"),
                            new HButton("Login", HButton.EButtonType.submit)
                        }
                    });
            }
        }
    }
}
