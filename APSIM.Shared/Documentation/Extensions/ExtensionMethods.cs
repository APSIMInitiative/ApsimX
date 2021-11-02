using System.Collections;
using System.Collections.Generic;

namespace APSIM.Shared.Documentation.Extensions
{
    /// <summary>
    /// Extension methods for enumerable types.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Create an <see cref="IEnumerable{T}"/> instance containing a single item.
        /// </summary>
        /// <param name="item">The item which the enumerable should contain.</param>
        /// <typeparam name="T">The item's type.</typeparam>
        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Count the number of items in a non-generic IEnumerable collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public static int Count(this IEnumerable collection)
        {
            int result = 0;
            foreach (object _ in collection)
                result++;
            return result;
        }
    }
}
