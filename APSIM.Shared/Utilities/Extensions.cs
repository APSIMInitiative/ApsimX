using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSIM.Shared.Utilities
{
    /// <summary>A collection of useful extensions.</summary>
    public static class Extensions
    {
        /// <summary>Enclose a collection of strings within a prefix and suffix.</summary>
        /// <param name="strings">The strings to enclose.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="suffix">The suffix.</param>
        /// <returns>The new collection that was created.</returns>
        public static IEnumerable<string> Enclose(this IEnumerable<string> strings, string prefix, string suffix)
        {
            foreach (var st in strings)
                yield return $"{prefix}{st}{suffix}";
        }

        /// <summary>Join a collection of strings together with a delimiter between each.</summary>
        /// <param name="strings">The collection of strings.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        /// <returns>The new collection that was created.</returns>
        public static string Join<T>(this IEnumerable<T> strings, string delimiter)
        {
            var writer = new StringBuilder();
            foreach (var st in strings)
            {
                if (writer.Length > 0)
                    writer.Append(delimiter);
                writer.Append(st);
            }
            return writer.ToString();
        }

        /// <summary>
        /// Remove trailing blank strings from collection of values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IList<string> TrimEnd(this IList<string> values)
        {
            int i;
            for (i = values.Count - 1; i >= 0; i--)
                if (!string.IsNullOrEmpty(values[i]))
                    break;
            return values.Take(i+1).ToList();
        }


    }
}
