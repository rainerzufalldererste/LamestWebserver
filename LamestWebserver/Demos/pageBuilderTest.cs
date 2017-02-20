using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.UI;

namespace Demos
{
    public static class pageBuilderTest
    {
        public static void addLamePageBuilderTest()
        {
            PageBuilder pb = new PageBuilder("PageBuilder Testpage", "pbtest")
            {
                Text = "this is some string!\n\n",

                Elements = new List<HElement>()
                {
                    new HContainer() { Text = "hello world!" }
                }
            };
        }
    }
}
