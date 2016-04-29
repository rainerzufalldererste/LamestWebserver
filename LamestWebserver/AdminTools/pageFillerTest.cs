using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LameNetHook;

namespace AdminTools
{
    public class pageFillerTest : LameNetHook.PageFiller
    {
        public pageFillerTest(string URL = "pftest.html") : base(URL) { }

        public override void processData(SessionData sessionData, ref string output)
        {
            setValue("head", sessionData.getValueHead("head"), ref output);
            setValue("last", "I AM REPLACED", ref output);
            setValue("middle", "let me be in the middle!", ref output);
        }
    }
}
