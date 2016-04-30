using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LameNetHook;

namespace Demos
{
    public static class pageBuilderTest
    {
        public static void addLamePageBuilderTest()
        {
            PageBuilder pb = new PageBuilder("PageBuilder Testpage", "pbtest")
            {
                text = "this is some string!\n\n",

                elements = new List<HElement>()
                {
                    new HContainer() { text = "hello world!" }
                }
            };
        }
    }
}
