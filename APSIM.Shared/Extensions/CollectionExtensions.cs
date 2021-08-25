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
    }
}
