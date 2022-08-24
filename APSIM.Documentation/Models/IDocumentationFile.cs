using System.Collections.Generic;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// Interface for an auto-generated documentation file.
    /// </summary>
    internal interface IDocumentationFile
    {
        /// <summary>
        /// Display name of the file.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The output name of the file.
        /// </summary>
        string OutputFileName { get; }

        /// <summary>
        /// Generate the auto-documentation at the given output path.
        /// </summary>
        /// <param name="path">Path to which the file will be generated.</param>
        void Generate(string path);
    }
}
