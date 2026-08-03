using System.Data;

namespace Models.Core
{
    /// <summary>
    /// An interface for a model which provides a list of values for display
    /// </summary>
    public interface IListValues : IModel
    {
        /// <summary>
        /// Returns a datatable to display
        /// </summary>
        DataTable Rows { get; }
    }
}
