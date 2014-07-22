// -----------------------------------------------------------------------
// <copyright file="ISummary.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Core
{
    /// <summary>
    /// A summary model interface for writing to the summary file.
    /// </summary>
    public interface ISummary
    {
        /// <summary>
        /// Gets or sets a value indicating whether the report should be in HTML format
        /// </summary>
        bool Html { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the summary file should be
        /// created every time the simulation is run.
        /// </summary>
        bool AutoCreate { get; set; }
        
        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="fullPath">The full path of the model writing the message</param>
        /// <param name="message">The message to write</param>
        void WriteMessage(string fullPath, string message);

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="fullPath">The full path of the model writing the message</param>
        /// <param name="message">The message to write</param>
        void WriteWarning(string fullPath, string message);

        /// <summary>
        /// Write the summary report to a file
        /// </summary>
        /// <param name="baseline">Indicates whether the baseline data store should be used.</param>
        void WriteReportToFile(bool baseline);
    }
}