using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWIMFrame
{
    /// <summary>
    /// From http://www.dotnetperls.com/array-slice. Provides a simpler way of slicing arrays
    /// when converting from FORTRAN.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start and end indexes.
        /// </summary>
        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            start--; // Align with FORTRAN one-indexed arrays
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }
    }
}
