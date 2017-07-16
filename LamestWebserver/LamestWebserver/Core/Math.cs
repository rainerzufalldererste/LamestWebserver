using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Contains essential mathematic extention methods.
    /// </summary>
    public static class Math
    {
        /// <summary>
        /// Clamps a variable or an object between min and max.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="val">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            // Source: https://stackoverflow.com/questions/2683442/where-can-i-find-the-clamp-function-in-net

            if (val.CompareTo(min) < 0)
                return min;
            else if (val.CompareTo(max) > 0)
                return max;
            else
                return val;
        }
    }
}
