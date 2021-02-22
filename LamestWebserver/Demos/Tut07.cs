using System;
using LamestWebserver;

namespace Demos
{
    public class Tut07 : JsonResponse
    {
        public Tut07() : base(nameof(Tut07)) { }

        protected override object GetResponse(HttpSessionData sessionData)
        {
            return new { this_is = "a json response as object", easy_as = "pie" };
        }
    }
}
