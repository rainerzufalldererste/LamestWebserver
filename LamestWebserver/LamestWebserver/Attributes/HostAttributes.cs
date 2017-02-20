using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Attributes
{
    /// <summary>
    /// Mark a public static Method to be executed on load by the LamestWebserver Host Service
    /// </summary>
    public class ExecuteOnLoad : Attribute
    {
        /// <summary>
        /// The arguments to start the method with
        /// </summary>
        public object[] Args;

        /// <summary>
        /// Mark a public static Method to be executed on load by the LamestWebserver Host Service
        /// </summary>
        /// <param name="args">The arguments to start the method with</param>
        public ExecuteOnLoad(params object[] args)
        {
            Args = args;
        }
    }
    
    /// <summary>
    /// Mark this class to not be added automatically when discovering pages.
    /// </summary>
    public class IgnoreDiscovery : Attribute
    {
        
    }

    /// <summary>
    /// Mark a public static Method to be executed on unload by the LamestWebserver Host Service
    /// </summary>
    public class ExecuteOnUnload : Attribute
    {
        /// <summary>
        /// The arguments to start the method with
        /// </summary>
        public object[] Args;

        /// <summary>
        /// Mark a public static Method to be executed on unload by the LamestWebserver Host Service
        /// </summary>
        /// <param name="args">The arguments to start the method with</param>
        public ExecuteOnUnload(params object[] args)
        {
            Args = args;
        }
    }
}
