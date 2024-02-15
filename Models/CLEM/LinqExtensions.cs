using System;
using System.Collections.Generic;

namespace Models.CLEM
{
    /// <summary>
    /// Additional linq extensions
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Method to extend linq and allow DistinctBy for unions
        /// Provided by MoreLinQ
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
         (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

    }

}