using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Contains General Purpose Extention Methods.
    /// </summary>
    public static class ExtentionMethods
    {
        /// <summary>
        /// Decodes the characters of a HTML string.
        /// </summary>
        /// <param name="s">the string to decode</param>
        /// <returns>the decoded string</returns>
        public static string DecodeHtml(this string s) => System.Web.HttpUtility.HtmlDecode(s);

        /// <summary>
        /// Decodes the characters of a Url string.
        /// </summary>
        /// <param name="s">the string to decode</param>
        /// <returns>the decoded string</returns>
        public static string DecodeUrl(this string s) => System.Web.HttpUtility.UrlDecode(s);

        /// <summary>
        /// HTTP URL encodes a given input
        /// </summary>
        /// <param name="input">the input</param>
        /// <returns>the input encoded as HTTP URL</returns>
        public static string FormatToHttpUrl(this string input) => System.Web.HttpUtility.HtmlEncode(input);

        /// <summary>
        /// HTML encodes a given input
        /// </summary>
        /// <param name="text">the input</param>
        /// <returns>the input encoded as HTML</returns>
        public static string FormatToHtml(this string text) => new System.Web.HtmlString(text).ToHtmlString();



        /// <summary>
        /// Gets the index of an Element from a List
        /// </summary>
        /// <typeparam name="T">The Type of the List-Elements</typeparam>
        /// <param name="list">The List</param>
        /// <param name="value">The Value</param>
        /// <returns>Index or null if not contained or value is null</returns>
        public static int? GetIndex<T>(this List<T> list, T value)
        {
            if (value == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Equals(value))
                {
                    return i;
                }
            }

            return null;
        }

        internal static char[] HexToCharLookupTable = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        /// <summary>
        /// Converts a byte[] to a hex string
        /// </summary>
        /// <param name="bytes">the byte[]</param>
        /// <returns>the byte[] as hex string</returns>
        public static string ToHexString(this byte[] bytes)
        {
            char[] s = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                s[i * 2] = HexToCharLookupTable[bytes[i] & 0x0F];
                s[i * 2 + 1] = HexToCharLookupTable[(bytes[i] & 0xF0) >> 4];
            }

            return new string(s);
        }

        private static string ToBitString(this ulong value, int sizeOfType)
        {
            unsafe
            {
                ulong u = 1;
                int bits = sizeOfType << 3;
                char* pChars = stackalloc char[bits + 1];

                for (int i = bits - 1; i >= 0; i--)
                {
                    pChars[i] = ((value & u) != 0 ? '1' : '0');

                    u <<= 1;
                }

                return new string(pChars);
            }
        }

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this ulong value) => value.ToBitString(sizeof(ulong));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this uint value) => ((ulong)value).ToBitString(sizeof(uint));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this ushort value) => ((ulong)value).ToBitString(sizeof(ushort));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this byte value) => ((ulong)value).ToBitString(sizeof(byte));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this long value) => BitConverter.ToUInt64(BitConverter.GetBytes(value), 0).ToBitString(sizeof(long));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this int value) => BitConverter.ToUInt64(BitConverter.GetBytes(value), 0).ToBitString(sizeof(int));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this short value) => BitConverter.ToUInt64(BitConverter.GetBytes(value), 0).ToBitString(sizeof(short));

        /// <summary>
        /// Retrieves the bits of a given integer.
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <returns>The Bits as '1' and '0'.</returns>
        public static string ToBitString(this sbyte value) => BitConverter.ToUInt64(BitConverter.GetBytes(value), 0).ToBitString(sizeof(sbyte));

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1>(this Tuple<T1> tuple) => new object[] { tuple.Item1 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2>(this Tuple<T1, T2> tuple) => new object[] { tuple.Item1, tuple.Item2 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3>(this Tuple<T1, T2, T3> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5>(this Tuple<T1, T2, T3, T4, T5> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5, T6>(this Tuple<T1, T2, T3, T4, T5, T6> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5, T6, T7>(this Tuple<T1, T2, T3, T4, T5, T6, T7> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 };

        /// <summary>
        /// Casts a Tuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The tuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>(this Tuple<T1, T2, T3, T4, T5, T6, T7, T8> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Rest };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1>(this ValueTuple<T1> tuple) => new object[] { tuple.Item1 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2>(this ValueTuple<T1, T2> tuple) => new object[] { tuple.Item1, tuple.Item2 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3>(this ValueTuple<T1, T2, T3> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5, T6, T7>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7> tuple) => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 };

        /// <summary>
        /// Casts a ValueTuple to an IEnumerable.
        /// </summary>
        /// <param name="tuple">The valueTuple to cast to IEnumerable.</param>
        /// <returns>The elements in order as IEnumerable (object[]).</returns>
        public static IEnumerable ToEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> tuple) where T8 : struct => new object[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Rest };
    }
}
