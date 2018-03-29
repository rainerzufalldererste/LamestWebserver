using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.CodeDom;
using System.Net;
using System.Reflection;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;
using LamestWebserver.WebServices.Generators;

namespace LamestWebserver.WebServices
{
    public abstract class IWebService : IURLIdentifyable
    {
        public string URL { get; }

        protected IWebService()
        {
            URL = this.GetType().FullName;
        }
    }
}
