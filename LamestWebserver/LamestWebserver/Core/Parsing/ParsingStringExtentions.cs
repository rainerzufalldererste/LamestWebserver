using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Parsing
{
    /// <summary>
    /// Provides Extention Methods for string operations.
    /// </summary>
    public static class ParsingStringExtentions
    {
        /// <summary>
        /// Returns the string between two substrings of this string.
        /// </summary>
        /// <param name="s">this string.</param>
        /// <param name="before">the string before the requested string.</param>
        /// <param name="after">the string after the requested string.</param>
        /// <returns>the string inbetween or null.</returns>
        public static string FindBetween(this string s, string before, string after)
        {
            int beforeIndex, afterIndex;

            if (s.FindString(before, out beforeIndex))
                if (s.Substring(beforeIndex + before.Length).FindString(after, out afterIndex))
                    return s.Substring(beforeIndex + before.Length, afterIndex);

            return null;
        }

        /// <summary>
        /// Searches for a Substring and returns it's start index.
        /// </summary>
        /// <param name="s">this string.</param>
        /// <param name="find">the substring to find.</param>
        /// <param name="index">the index where the substring begins.</param>
        /// <returns>returns true if the string could be found. otherwise false.</returns>
        public static bool FindString(this string s, string find, out int index)
        {
            int[] findIndexes = find.GetKMP();

            for (int i = 0; i < s.Length; i++)
            {
                if (find.Length > s.Length - i)
                {
                    index = i;
                    return false;
                }

                int length = find.Length;

                for (int j = 0; j < length; j++)
                {
                    if (s[i + j] != find[j])
                    {
                        i += findIndexes[j];
                        break;
                    }

                    if (j + 1 == length)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = s.Length;
            return false;
        }

        /// <summary>
        /// Returns the indexes of the Knuth–Morris–Pratt algorithm of a given string.
        /// </summary>
        /// <param name="s">this string.</param>
        /// <returns>the Knuth–Morris–Pratt algorithm indexes.</returns>
        public static int[] GetKMP(this string s)
        {
            int[] ret = new int[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                ret[i] = -1;

                for (int j = 1; j < System.Math.Min(i, s.Length / 2); j++)
                {
                    if (s.Substring(i - j, j) == s.Substring(0, j))
                        ret[i] = j;
                }

                if (ret[i] < 0)
                    ret[i] = 0;
            }

            return ret;
        }

        /// <summary>
        /// Parses a string like Split(...) but keeps the splitting strings as separate entries.
        /// </summary>
        /// <param name="s">this string.</param>
        /// <param name="delimiters">the delimiters to split at.</param>
        /// <returns>A list of the splitted string parts without empty entries.</returns>
        public static List<string> Parse(this string s, params string[] delimiters) => Parse(s, true, delimiters);

        /// <summary>
        /// Parses a string like Split(...) but keeps the splitting strings as separate entries.
        /// </summary>
        /// <param name="s">this string.</param>
        /// <param name="removeEmptyEntries">shall empty strings be removed from the returned list?</param>
        /// <param name="delimiters">the delimiters to split at.</param>
        /// <returns>A list of the splitted string parts.</returns>
        public static List<string> Parse(this string s, bool removeEmptyEntries, params string[] delimiters)
        {
            List<string> ret = new List<string>();

            for (int i = 0; i < s.Length; i++)
            {
                for (int j = 0; j < delimiters.Length; j++)
                {
                    if (delimiters[j].Length > s.Length- i)
                        continue;

                    for (int k = 0; k < delimiters[j].Length; k++)
                    {
                        if (s[i + k] != delimiters[j][k])
                            break;

                        if(k + 1 == delimiters[j].Length)
                        {
                            if(i > 0)
                                ret.Add(s.Substring(0, i));

                            ret.Add(delimiters[j]);
                            s = s.Substring(i + delimiters[j].Length);

                            i = -1;
                            goto NEXT_CHAR;
                        }
                    }
                }

                NEXT_CHAR:;
            }

            if(s.Length > 0)
                ret.Add(s);

            if(removeEmptyEntries)
                for (int i = ret.Count - 1; i >= 0; i--)
                    if (string.IsNullOrWhiteSpace(ret[i]))
                        ret.RemoveAt(i);

            return ret;
        }
    }
}
