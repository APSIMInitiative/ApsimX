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
    [ValidParent(ParentType = typeof(Simulation))]
    public class NullSummary : Model, ISummary
    {
        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="model">The model writing the message</param>
        /// <param name="message">The message to write</param>
        public void WriteMessage(IModel model, string message)
        {
        }

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        /// <param name="model">The model writing the message</param>
        /// <param name="message">The message to write</param>
        public void WriteWarning(IModel model, string message)
        {
        }
    }
}
