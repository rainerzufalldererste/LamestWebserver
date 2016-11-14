using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using System.Threading;

namespace Demos
{
    public static class CardGame
    {
        private static string stylesheet = "body{font-family: \"Segoe UI\", sans-serif; background-color: #444444; background-image: url(\"/cgame/card.png\"); background-repeat: repeat; background-size: 125px;} div{font-family: \"Segoe UI\",sans-serif;width: 70 %;max-width: 800px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;padding-top: 22px;padding-bottom: 22px;}";

        public static void register()
        {
            new LoginScreen();
            new Lobby();
        }

        public class LoginScreen : PageBuilder
        {
            private static List<string> secretKeys = new List<string>();

            public LoginScreen() : base("CardGame - Login", "cgame/")
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

                            new HForm(InstantPageResponse.addOneTimeConditionalRedirect("/cgame/lobby", "/cgame/?failed", false, (SessionData sessionData) => 
                                {
                                    string userName = sessionData.getHTTP_POST_Value("user");
                                    string key = sessionData.getHTTP_POST_Value("key");

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
                                            sessionData.setUserVariable("cycles", (int?)6);
                                            return true;
                                        }

                                        return false;
                                    }
                                    else
                                    {
                                        sessionData.registerUser(userName);
                                        secretKeys.Add(key);
                                        sessionData.setUserVariable("cycles", (int?)6);
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

        public class Lobby : PageBuilder
        {
            private List<int> searchingPlayers = new List<int>();
            private List<int> findingPlayers = new List<int>();
            private string nextGameHash = "";
            private GameHandler currentGame;
            private Mutex mutex = new Mutex();

            public Lobby() : base("CardGame - Lobby", "cgame/lobby")
            {

                stylesheetCode = stylesheet;

                addElement(
                    HRuntimeCode.getConditionalRuntimeCode(
                        new HContainer() { elements = new List<HElement>()
                            {
                                new HScript(ScriptCollection.getPageReloadWithFullPOSTInMilliseconds, 2500),
                                new HText("Searching for a lobby... <i>(The Page might reload a couple of times)</i>"),
                                new HSyncronizedRuntimeCode((SessionData sessionData) => 
                                    {
                                        int? cycles = sessionData.getUserVariable<int?>(nameof(cycles));

                                        if(cycles.HasValue)
                                        {
                                            if(cycles < 2 && !findingPlayers.Contains(sessionData.userID.Value))
                                            {
                                                // make sure your next game doesn't start with cycle < 2.
                                                sessionData.setUserVariable<object>(nameof(cycles), null); // clears the variable space for "cycles" & removes it.
                                                searchingPlayers.Remove(sessionData.userID.Value);

                                                return new HScript(
                                                    ScriptCollection.getPageReferalToX,
                                                    InstantPageResponse.addOneTimeTimedRedirect("/cgame/", "Sorry.\nWe couldn't find a game for you.", 5000, true)) * sessionData; // <- operator overloading can be fishy. this executes HScript.getContent(sessionData);
                                            }
                                            else
                                            {
                                                if(cycles == 6)
                                                {
                                                    searchingPlayers.Add(sessionData.userID.Value);
                                                }
                                                
                                                if (findingPlayers.Contains(sessionData.userID.Value))
                                                {
                                                    if(nextGameHash == "")
                                                    {
                                                        registerNextGame();
                                                        currentGame.setSize(findingPlayers.Count);
                                                    }
                                                    
                                                    findingPlayers.Remove(sessionData.userID.Value);
                                                    searchingPlayers.Remove(sessionData.userID.Value);
                                                    sessionData.setUserVariable<object>(nameof(cycles), null);

                                                    string lastgamehash = nextGameHash;

                                                    if(findingPlayers.Count == 0)
                                                    {
                                                        nextGameHash = "";
                                                    }

                                                    return new HScript(ScriptCollection.getPageReferalToX, lastgamehash).getContent(sessionData); // <- without operator overloading
                                                }
                                                else if(searchingPlayers.Contains(sessionData.userID.Value))
                                                {
                                                    if(searchingPlayers.Count >= cycles)
                                                    {
                                                        for (int i = searchingPlayers.Count - 1; i > -1; i--)
                                                        {
                                                            findingPlayers.Add(searchingPlayers[i]);
                                                            searchingPlayers.RemoveAt(i);
			                                            }
                                                    }
                                                    else
                                                    {
                                                        cycles--;
                                                        sessionData.setUserVariable(nameof(cycles), cycles);
                                                    }
                                                }
                                                else
                                                {
                                                    cycles = 5;
                                                    sessionData.setUserVariable(nameof(cycles), cycles);
                                                    searchingPlayers.Add(sessionData.userID.Value);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            cycles = 6;
                                            sessionData.setUserVariable(nameof(cycles), cycles);
                                        }

                                        return "[ " + cycles + " | " + sessionData.userID.Value + " ]" + new HTable( findingPlayers.Cast<object>(), searchingPlayers.Cast<object>() ).getContent(sessionData);
                                    }),
                            }
                        },
                        new HContainer() { elements = new List<HElement>()
                        {
                            new HScript(ScriptCollection.getPageReferalToXInMilliseconds, "/cgame/", 2500),
                            new HLink("you have to login first. click here to log in.", "/cgame/") } }
                        ,
                        (SessionData sessionData) => sessionData.knownUser ));
            }

            private void registerNextGame()
            {
                nextGameHash = SessionContainer.generateUnusedHash(); // generates a new hash
                currentGame = new GameHandler(nextGameHash);
                nextGameHash = "/" + nextGameHash;
            }
        }

        public class GameHandler : SyncronizedPageResponse
        {
            public enum CardType : byte
            {
                Eichenblatt, Löwenzahn, Sonnenblume, Feder, Knoblauch, Pilz, Rabe, Mistel, Kristall
            }

            public class Card
            {
                public CardType[] pictures = new CardType[3];
                public byte specality = 0;
                public CardType special;

                public static Random rand = new Random();

                public Card()
                {
                    double d;

                    for (int i = 0; i < 3; i++)
                    {
                        d = rand.NextDouble() * 100d;

                        if(d < 16)
                        {
                            pictures[i] = CardType.Eichenblatt;
                        }
                        else if(d < 32)
                        {
                            pictures[i] = CardType.Löwenzahn;
                        }
                        else if (d < 48)
                        {
                            pictures[i] = CardType.Sonnenblume;
                        }
                        else if(d < 60)
                        {
                            pictures[i] = CardType.Feder;
                        }
                        else if (d < 70)
                        {
                            pictures[i] = CardType.Knoblauch;
                        }
                        else if(d < 77.5d)
                        {
                            pictures[i] = CardType.Pilz;
                        }
                        else if(d < 85)
                        {
                            pictures[i] = CardType.Rabe;
                        }
                        else if (d < 92.5d)
                        {
                            pictures[i] = CardType.Mistel;
                        }
                        else if (d < 97.5d)
                        {
                            pictures[i] = CardType.Kristall;
                        }
                    }

                    if (pictures[0] == pictures[1])
                    {
                        specality++;
                        special = pictures[0];
                    }

                    if (pictures[0] == pictures[2])
                    {
                        specality++;
                        special = pictures[0];
                    }

                    if (pictures[1] == pictures[2])
                    {
                        specality++;
                        special = pictures[1];
                    }
                }

                public bool cardPlayable(Card c)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (c.pictures[0] == pictures[i]) return true;
                        if (c.pictures[1] == pictures[i]) return true;
                        if (c.pictures[2] == pictures[i]) return true;
                    }

                    return false;
                }

                public override string ToString()
                {
                    string output = "";

                    for (int i = 0; i < 3; i++)
                    {
                        output += pictures[i] + "<br>";
                    }

                    return output;
                }
            }

            public class Player
            {
                public List<Card> cards = new List<Card>();
            }

            private List<int> joinedUserIDs = new List<int>();
            private int size = -1;
            private int activePlayer = 0;
            private int gameState = 0;
            private List<Player> players = new List<Player>();

            private Card topCard = new Card();

            public GameHandler(string hashURL) : base(hashURL)
            {

            }

            protected override string getContents(SessionData sessionData)
            {
                if (!sessionData.knownUser)
                    return "YOU ARE NOT LOGGED IN!";

                if (joinedUserIDs.Count == 0)
                    activePlayer = 0;

                if (!joinedUserIDs.Contains(sessionData.userID.Value))
                {
                    joinedUserIDs.Add(sessionData.userID.Value);
                    players.Add(new Player() { cards = new List<Card>() { new Card(), new Card(), new Card(), new Card(), new Card(), new Card(), new Card(), new Card() } });
                }

                int thisplayer = -1;

                for (int i = 0; i < joinedUserIDs.Count; i++)
                {
                    if(joinedUserIDs[i] == sessionData.userID.Value)
                    {
                        thisplayer = i;
                        break;
                    }
                }

                if (thisplayer == -1)
                    return "THIS IS NOT YOUR GAME!";

                string postvalue = sessionData.getHTTP_POST_Value("card");

                if(players.Count <= 1 && joinedUserIDs.Count > 1)
                {
                    removeFromServer();

                    return new HScript(ScriptCollection.getPageReferalToX, InstantPageResponse.addOneTimeTimedRedirect("/cgame/lobby", "You have lost the game!", 2500, true)) * sessionData;
                }

                if (thisplayer == activePlayer && postvalue != null)
                {
                    int card_num = -1;

                    int.TryParse(postvalue, out card_num);

                    if(card_num >= 0 && card_num < players[thisplayer].cards.Count && topCard.cardPlayable(players[thisplayer].cards[card_num]))
                    {
                        topCard = players[thisplayer].cards[card_num];
                        players[thisplayer].cards.RemoveAt(card_num);

                        if(players[thisplayer].cards.Count == 0)
                        {
                            players.RemoveAt(thisplayer);

                            activePlayer++;

                            if (activePlayer >= players.Count)
                            {
                                activePlayer = 0;
                            }

                            return new HScript(ScriptCollection.getPageReferalToX, InstantPageResponse.addOneTimeTimedRedirect("/cgame/lobby", "You have won the game!", 2500, true)) * sessionData;
                        }

                        activePlayer++;

                        if(activePlayer >= players.Count)
                        {
                            activePlayer = 0;
                        }

                        return new HScript(ScriptCollection.getPageReloadInMilliseconds, 0) * sessionData;
                    }
                }

                postvalue = sessionData.getHTTP_POST_Value("draw");

                if(thisplayer == activePlayer && postvalue != null && postvalue == "1")
                {
                    players[thisplayer].cards.Add(new Card());
                    activePlayer++;

                    if (activePlayer >= players.Count)
                    {
                        activePlayer = 0;
                    }

                    return new HScript(ScriptCollection.getPageReloadInMilliseconds, 0) * sessionData;
                }

                string output =
                    "<div style='position: relative;text-align: center;padding: 20;width: 180;min-width: 100;border-style: solid;font-family: \"Georgia\", \"Times New Roman\", serif, sans-serif;color: #BFB39E;background: #544E44;'>" + topCard.ToString() + "</div><br><br>" +
                    (
                        thisplayer == activePlayer ?
                        new HForm("")
                        {
                            descriptionTags = "style='position: relative;text-align: right;'",
                            elements = new List<HElement>()
                            {
                                new HInput(HInput.EInputType.hidden, "draw", "1"), new HButton("Draw a card", HButton.EButtonType.submit)
                                {
                                    descriptionTags = "style='background: #EADDC6; border-style: solid; border-color: #A09580; border-radius: 10px; width: 150px; height: 40px;font-size: 15px;margin: 10px;font-family: sans-serif;font-weight: bold;color: #544E44;'"
                                }
                            }
                        } 
                        : (HElement)new HScript(ScriptCollection.getPageReloadInMilliseconds, 1000)
                    ) * sessionData +  "<div style='overflow:scroll;'>";

                for (int i = 0; i < players[thisplayer].cards.Count; i++)
                {
                    output += new HContainer()
                    {
                        descriptionTags = "style='display: table-cell;padding: 20;width: 180;min-width: 100;height: 220; border-style: solid;font-family: \"Georgia\", \"Times New Roman\", serif, sans-serif;color: #BFB39E;background: #544E44;'",
                        elements = new List<HElement>()
                        {
                            new HPlainText(players[thisplayer].cards[i].ToString()),
                            (thisplayer == activePlayer && topCard.cardPlayable(players[thisplayer].cards[i]) ? 
                                new HForm("")
                                {
                                    elements = new List<HElement>() { new HNewLine(), new HInput(HInput.EInputType.hidden, "card", i.ToString()), new HButton("play this card", HButton.EButtonType.submit) }
                                } 
                                : (HElement)new HPlainText())
                        }
                    } * sessionData;
                }

                output += "</div>";

                return "wow, dude, i'm a game! (" + sessionData.ssid + ")" + new HNewLine() * sessionData + "[" + sessionData.userID.Value + ": (" + joinedUserIDs.Count + "/" + size + ")] " + new HTable(joinedUserIDs.Cast<object>()) * sessionData + "<hr>" + output;
            }

            internal void setSize(int size)
            {
                this.size = size;
            }
        }
    }
}
