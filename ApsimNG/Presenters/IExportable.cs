// -----------------------------------------------------------------------
// <copyright file="IExportable.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    /// <summary>
    /// Defines an interface for Views that are printable.
    /// </summary>
    public interface IExportable
    {
        /// <summary>
        /// Convert the node to HTML and return the HTML string. Returns
        /// null if not convertable. The 'folder' is where any images
        /// should be created.
        /// </summary>
        /// <param name="folder">The folder name</param>
        /// <returns>HTML text</returns>
        string ConvertToHtml(string folder);
    }
}
