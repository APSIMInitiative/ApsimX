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
    }
}