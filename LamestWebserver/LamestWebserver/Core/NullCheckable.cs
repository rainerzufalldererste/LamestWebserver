using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    /// <summary>
    /// The abstract NullCheckable class provides functionality for checking for null like in c/c++.
    /// </summary>
    public abstract class NullCheckable
    {
        /// <summary>
        /// Returns false if the object is null.
        /// </summary>
        /// <param name="obj">the object</param>
        public static implicit operator bool(NullCheckable obj) => !ReferenceEquals(obj, null);

        /// <summary>
        /// Returns true if the object is null.
        /// </summary>
        /// <param name="obj">the object</param>
        public static bool operator ! (NullCheckable obj) => ReferenceEquals(obj, null);
    }
}
