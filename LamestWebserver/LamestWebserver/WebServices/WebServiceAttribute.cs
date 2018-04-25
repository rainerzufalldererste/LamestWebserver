using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.WebServices
{
    /// <summary>
    /// An Attribute Associated with the WebService Host.
    /// </summary>
    public abstract class WebServiceAttribute : Attribute
    {

    }

    /// <summary>
    /// This Attribute specifies to Ignore a specific public method, property or field.
    /// </summary>
    public class WebServiceIgnore : WebServiceAttribute
    {

    }
}
