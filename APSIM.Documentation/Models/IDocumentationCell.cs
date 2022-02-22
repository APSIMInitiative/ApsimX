using System.Collections.Generic;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// Interface for a cell in an autodocs table.
    /// </summary>
    internal interface IDocumentationCell
    {
        /// <summary>
        /// Files in the cell.
        /// </summary>
        IEnumerable<IDocumentationFile> Files { get; }
    }
}
