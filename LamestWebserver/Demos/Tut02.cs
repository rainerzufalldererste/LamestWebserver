using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.UI;

namespace Demos
{
    public class Tut02 : ElementResponse
    {
        /// <inheritdoc />
        public Tut02() : base(nameof(Tut02))
        {
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            return MainPage.GetPage(GetContents(), nameof(Tut02) + ".cs");
        }

        private HLinkSearchBox hLinkSearchBox = new HLinkSearchBox((identificator, s) =>
        {
            List<Tuple<string, string>> list = new List<Tuple<string, string>>();
            for (int i = 0; i < s.Length; i++)
            {
                list.Add(Tuple.Create(s.Substring(0, i + 1), "/#" + s[i].ToString()));
            }
            return list;
        });

        private  HMultipleValuesButton hMultipleValuesButton = new HMultipleValuesButton("multiple_", 
            new Tuple<string, HElement>("one", new HString("1")), 
            new Tuple<string, HElement>("two", new HString("2")), 
            new Tuple<string, HElement>("lws", new HImage("lwsfooter.png")));

        private IEnumerable<HElement> GetContents()
        {
            yield return new HHeadline("Advanced Interactive Elements");

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
                    new HText(nameof(HLinkSearchBox)),
                    new HText("A search box retrieving links."),
                    new HText("<input>"),
                    hLinkSearchBox
                },
                new List<HElement>()
                {
                    new HText(nameof(HMultipleValuesButton)),
                    new HText("A button cycling through multiple values."),
                    new HText("<button> <input>"),
                    hMultipleValuesButton
                },
            });
        }
    }
}
