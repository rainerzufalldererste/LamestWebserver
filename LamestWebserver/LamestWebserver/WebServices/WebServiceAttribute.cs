using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.WebServices
{
    public abstract class WebServiceAttribute : Attribute
    {

    }

    public class WebServiceIgnore : WebServiceAttribute
    {

    }
}
