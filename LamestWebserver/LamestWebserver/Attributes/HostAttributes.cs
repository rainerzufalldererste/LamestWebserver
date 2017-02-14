using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Attributes
{
    public class ExecuteOnLoad : Attribute
    {
        public object[] Args;

        public ExecuteOnLoad(params object[] args)
        {
            Args = args;
        }
    }
    
    public class HostIgnore : Attribute
    {
        
    }

    public class ExecuteOnUnload : Attribute
    {
        public object[] Args;

        public ExecuteOnUnload(params object[] args)
        {
            Args = args;
        }
    }
}
