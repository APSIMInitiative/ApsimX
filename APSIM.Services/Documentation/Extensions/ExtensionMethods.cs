using System.Collections.Generic;

namespace APSIM.Services.Documentation.Extensions
{
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Create an IEnumerable<T> instance containing a single item.
        /// </summary>
        /// <param name="item">The item which the enumerable should contain.</param>
        /// <typeparam name="T">The item's type.</typeparam>
        internal static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }
    }
}
