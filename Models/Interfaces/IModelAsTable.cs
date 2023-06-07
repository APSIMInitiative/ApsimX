using System.Collections.Generic;
using System.Data;

namespace Models.Interfaces
{

    /// <summary>This interface describes the way a grid presenter talks to a model via a data table.</summary>
    public interface IModelAsTable
    {
        /// <summary>
        /// Gets or sets the tables of values.
        /// </summary>
        List<DataTable> Tables { get; }
    }
}
