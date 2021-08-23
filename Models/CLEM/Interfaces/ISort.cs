using System;
using System.ComponentModel;

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
}
