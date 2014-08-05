// -----------------------------------------------------------------------
// <copyright file="ISummary.cs" company="APSIM Initiative">
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
    }
}