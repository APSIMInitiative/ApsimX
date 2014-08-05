// -----------------------------------------------------------------------
// <copyright file="NullSummary.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models
{
    using System;
    using Models.Core;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    public class NullSummary : Model, ISummary
    {
        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="fullPath">The full path of the model writing the message</param>
        /// <param name="message">The message to write</param>
        public void WriteMessage(string fullPath, string message)
        {
        }

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="fullPath">The full path of the model writing the message</param>
        /// <param name="message">The message to write</param>
        public void WriteWarning(string fullPath, string message)
        {
        }
    }
}
