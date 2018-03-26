using LamestWebserver.Core;
using System;

namespace LamestWebserver.Caching
{
    /// <summary>
    /// Specifies whether an element or a page should be cached.
    /// </summary>
    public enum ECachingType : byte
    {
        /// <summary>
        /// The element or page can be cached (does not include dynamic content).
        /// </summary>
        Cacheable,
        
        /// <summary>
        /// The element or page can not be cached (does include dynamic content).
        /// </summary>
        NotCacheable,

        /// <summary>
        /// The element or page will take the default value for Cachable or NotCacheable from it's ancestor or the global Default.
        /// </summary>
        Default
    }
}