using System.Collections.Generic;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A row in an autodocs table, made up of multiple cells.
    /// </summary>
    internal class DocumentationRow : IDocumentationRow
    {
        /// <summary>
        /// Name of the row.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Cells in the row.
        /// </summary>
        public IEnumerable<IDocumentationCell> Cells { get; private set; }

        /// <summary>
        /// Create a new <see cref="DocumentationRow"/> instance.
        /// </summary>
        /// <param name="name">Name of the row.</param>
        /// <param name="cells">Cells in the row.</param>
        public DocumentationRow(string name, IEnumerable<IDocumentationCell> cells)
        {
            Name = name;
            Cells = cells;
        }
    }
}
