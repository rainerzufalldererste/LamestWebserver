using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.UI
{
    /// <summary>
    /// Contains Extention Methods for LamestWebserver.UI.
    /// </summary>
    public static class ExtentionMethods
    {
        /// <summary>
        /// Wrapps a string inside a HString.
        /// </summary>
        /// <param name="s">the string</param>
        /// <returns>the string as HElement</returns>
        public static HElement ToHElement(this string s)
        {
            return new HString(s);
        }

        /// <summary>
        /// Casts an int to string contained in a HString.
        /// </summary>
        /// <param name="i">the int</param>
        /// <returns>the int as HElement</returns>
        public static HElement ToHElement(this int i)
        {
            return new HPlainText(i.ToString());
        }

        /// <summary>
        /// Casts an object to string contained in a HString.
        /// </summary>
        /// <param name="obj">the object.</param>
        /// <returns>the object as HElement</returns>
        public static HElement ToHElement(this object obj)
        {
            return new HPlainText(obj.ToString());
        }
    }
}
