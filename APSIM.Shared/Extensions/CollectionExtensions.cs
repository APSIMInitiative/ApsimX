using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.Shared.Extensions.Collections
{
    /// <summary>
    /// Extension methods for enumerable types.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Convert a non-generic IEnumerable to a generic IEnumerable.
        /// </summary>
        /// <param name="enumerable">The IEnumerable instance to be converted.</param>
        /// <returns></returns>
        public static IEnumerable<object> ToGenericEnumerable(this IEnumerable enumerable)
        {
            IEnumerator enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        /// <summary>
        /// Appndend a collection of items to another collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="appendee">The collection to be appended.</param>
        public static IEnumerable<T> AppendMany<T>(this IEnumerable<T> collection, IEnumerable<T> appendee)
        {
            foreach (T item in collection)
                yield return item;
            foreach (T item in appendee)
                yield return item;
        }
    }
}
