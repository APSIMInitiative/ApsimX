using System.Collections.Generic;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// Interface for a row in an autodocs table.
    /// </summary>
    internal interface IDocumentationRow
    {
        /// <summary>
        /// Name of the row.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Cells in the row.
        /// </summary>
        /// <value></value>
        IEnumerable<IDocumentationCell> Cells { get; }
    }
}
