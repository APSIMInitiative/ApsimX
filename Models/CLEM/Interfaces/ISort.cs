using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Enables a model to be sorted
    /// </summary>
    public interface ISort
    {
        /// <summary>
        /// The direction of sorting to apply
        /// </summary>
        ListSortDirection SortDirection { get; }

        /// <summary>
        /// An expression defining how the items should be sorted
        /// </summary>
        /// <typeparam name="T">The type of item to be sorted</typeparam>
        /// <param name="item">An instance of a sortable item</param>
        /// <returns>Any object, which will be ordered using the default comparer</returns>
        object OrderRule<T>(T item);
    }

    /// <summary>
    /// Contains extension methods for ISort
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Order a collection based on the given sorting parameters
        /// </summary>
        /// <param name="source">The items to sort</param>
        /// <param name="sorts">The parameters to sort by</param>
        /// <param name="randomiseBeforeSort">enforces a random sort before custom sorts to remove any inherant herd order</param>
        /// <returns></returns>
        public static IOrderedEnumerable<T> Sort<T>(this IEnumerable<T> source, IEnumerable<ISort> sorts, bool randomiseBeforeSort = false)
        {
            var sorted = source.OrderBy(i => ((randomiseBeforeSort) ? RandomNumberGenerator.Generator.NextDouble() : 1));

            if (!sorts.Any())
                return sorted;

            foreach (ISort sort in sorts)
                sorted = (sort.SortDirection == System.ComponentModel.ListSortDirection.Ascending) ? sorted.ThenBy(sort.OrderRule) : sorted.ThenByDescending(sort.OrderRule);

            return sorted;
        }
    }
}
