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
        private static List<string> secretKeys = new List<string>();
        private static string stylesheet = "body{font-family: \"Segoe UI\", sans-serif; background-color: #444444; background-image: url(\"/cgame/card.png\"); background-repeat: repeat; background-size: 125px;} div{font-family: \"Segoe UI\",sans-serif;width: 70 %;max-width: 800px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;padding-top: 22px;padding-bottom: 22px;}";

        public static void register()
        {
            new loginScreen();
            new lobby();
        }

        public class loginScreen : PageBuilder
        {
            public loginScreen() : base("CardGame - Login", "cgame/")
            {
                stylesheetCode = stylesheet;

                addElement(
                    new HContainer()
                    {
                        elements = new List<HElement>()
                        {
                            HRuntimeCode.getConditionalRuntimeCode(
                                null,
                                new HContainer()
                                {
                                    descriptionTags = "style='color: #aa5555;background-color:#FFEBEB;margin: 0;'",
                                    text = "This Username has already been taken <i>(or you forgot your secret key)</i>."
                                }, (SessionData sessionData) =>
                                {
                                    if (sessionData.getHTTP_HEAD_Value("failed") != null)
                                        return false;

                                    return true;
                                }),

                            new HHeadline("Login:", 1),

                            new HForm(InstantPageResponse.addOneTimeConditionalRedirect("/cgame/lobby", "/cgame/?failed", (SessionData sessionData) => 
                                {
                                    string userName = sessionData.getHTTP_POST_value("user");
                                    string key = sessionData.getHTTP_POST_value("key");

                                    if(string.IsNullOrWhiteSpace(userName))
                                        return false;
                                    else if(string.IsNullOrWhiteSpace(key))
                                        return false;

                                    int? id = sessionData.getUserIndex(userName);
                                    
                                    if (id.HasValue)
                                    {
                                        if(secretKeys[id.Value] == key)
                                        {
                                            sessionData.registerUser(userName);
                                            return true;
                                        }

                                        return false;
                                    }
                                    else
                                    {
                                        sessionData.registerUser(userName);
                                        secretKeys.Add(key);
                                        return true;
                                    }
                                }))
                            {
                                elements = new List<HElement>()
                                {
                                    new HText("Pick a UserName"),
                                    new HInput(HInput.EInputType.text, "user"),
                                    new HNewLine(),
                                    new HText("Enter your secret key<br><i>(If you are a new user, the secret key you enter will be your secret key for the future)</i>"),
                                    new HInput(HInput.EInputType.password, "key"),
                                    new HNewLine(),
                                    new HNewLine(),
                                    new HButton("Login", HButton.EButtonType.submit)
                                }
                            }
                        }
                    });
            }
        }

        public class lobby : PageBuilder
        {
            public lobby() : base("CardGame - Lobby", "cgame/lobby")
            {
                stylesheetCode = stylesheet;

                addElement(
                    HRuntimeCode.getConditionalRuntimeCode(
                        new HContainer() { elements = new List<HElement>()
                            {
                                new HScript(ScriptCollection.getPageReloadInMilliseconds, 5000),
                                new HText("Searching for a lobby... <i>(The Page might reload a couple of times)</i>"),
                                new HRuntimeCode((SessionData sessionData) => 
                                    {
                                        int? cycles = sessionData.getUserFileVariable<int?>(nameof(cycles));

                                        if(cycles.HasValue)
                                        {
                                            if(cycles < 2)
                                            {
                                                // make sure your next game doesn't start with cycle < 2.
                                                sessionData.setUserFileVariable<object>(nameof(cycles), null); // clears the variable space for "cycles"

                                                return new HScript(
                                                    ScriptCollection.getPageReferalToX,
                                                    InstantPageResponse.addOneTimeTimedRedirect("/cgame/", "Sorry.\nWe couldn't find a game for you.", 5000, true)) * sessionData; // <- operator overloading can be fishy. this executes HScript.getContent(sessionData);
                                            }
                                            else
                                            {
                                                // TODO: find a nice game for you.

                                                cycles--;
                                                sessionData.setUserFileVariable(nameof(cycles), cycles);
                                            }
                                        }
                                        else
                                        {
                                            cycles = 6;
                                            sessionData.setUserFileVariable(nameof(cycles), cycles);
                                        }

                                        return "";
                                    }),
                            }},
                        new HContainer() { elements = new List<HElement>()
                        {
                            new HScript(ScriptCollection.getPageReferalToXInMilliseconds, "/cgame/", 2500),
                            new HLink("you have to login first. click here to log in.", "/cgame/") } }
                        ,
                        (SessionData sessionData) => sessionData.knownUser ));
            }
        }
    }
}
