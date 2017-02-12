using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;

namespace Demos
{
    public class pageFillerTest : LamestWebserver.PageFiller
    {
        public pageFillerTest() : base("pftest.html") { }

        public override void processData(SessionData sessionData, ref string output)
        {
            setValue("head", sessionData.GetHttpHeadValue("head"), ref output);
            setValue("last", "I AM REPLACED", ref output);
            setValue("middle", "let me be in the middle!", ref output);
            setValue("content", new HNewLine().getContent(sessionData) + new HButton("button one", "pftest.html").getContent(sessionData) + new HButton("button two", "#").getContent(sessionData), ref output);

            if(!sessionData.KnownUser)
                sessionData.RegisterUser("");
        }
    }
}
