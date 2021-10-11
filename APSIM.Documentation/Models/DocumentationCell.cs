using System.Collections.Generic;
using System;
using APSIM.Shared.Documentation.Extensions;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A cell in an autodocs table. Contains multiple files.
    /// </summary>
    internal class DocumentationCell : IDocumentationCell
    {
        /// <summary>
        /// Files in the cell.
        /// </summary>
        public IEnumerable<IDocumentationFile> Files { get; private set; }

        /// <summary>
        /// Create a new <see cref="DocumentationCell"/> instance which holds
        /// a single file.
        /// </summary>
        /// <param name="file">The file to show in the cell.</param>
        public DocumentationCell(IDocumentationFile file) : this(file.ToEnumerable())
        {
        }

        /// <summary>
        /// Create a new <see cref="DocumentationCell"/> instance which holds
        /// the given files.
        /// </summary>
        /// <param name="files">Files in the cell.</param>
        public DocumentationCell(IEnumerable<IDocumentationFile> files)
        {
            Files = files;
        }
    }
}
