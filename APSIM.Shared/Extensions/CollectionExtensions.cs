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

        /// <summary>
        /// Dequeue a chunk of items from the front of a queue.
        /// </summary>
        /// <remarks>
        /// If the queue contains less elements than chunkSize, the
        /// entire queue will be returned.
        /// </remarks>
        /// <param name="queue">The queue.</param>
        /// <param name="chunkSize">Chunk size (number of items to dequeue).</param>
        public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, uint chunkSize)
        {
            for (int i = 0; i < chunkSize && queue.Any(); i++)
                yield return queue.Dequeue();
        }

        /// <summary>
        /// Sum an array of arrays.
        /// </summary>
        /// <param name="array">The array to sum.</param>
        /// <returns>Always returns a double[].</returns>
        public static double[] Sum(this IEnumerable<IReadOnlyList<double>> array)
        {
            if (!array.Any())
                return Array.Empty<double>();

            double[] values = null;

            foreach (double[] vals in array)
            {
                Array.Resize(ref values, vals.Length);
                for (int i = 0; i < vals.Length; i++)
                    values[i] += vals[i];
            }
            return values;
        }

        /// <summary>
        /// Copy the elements of a read only list to an array.
        /// </summary>
        /// <param name="list">The list to copy from.</param>
        /// <param name="array">The array to copy to.</param>
        public static void CopyTo<T>(this IReadOnlyList<T> list, T[] array)
        {
            for (int i = 0; i < list.Count; i++)
                array[i] = list[i];
        }
    }
}
