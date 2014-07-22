// -----------------------------------------------------------------------
// <copyright file="NullSummary.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Models.Core;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    public class NullSummary : Model, ISummary
    {


        public void WriteMessage(string FullPath, string Message)
        {
        }

        public void WriteWarning(string FullPath, string Message)
        {

        }

        /// <summary>
        /// Write the summary report to a file
        /// </summary>
        /// <param name="baseline">Indicates whether the baseline datastore should be used.</param>
        public void WriteReportToFile(bool baseline)
        {
        }

        public bool Html
        {
            get
            {
                return false;
            }
            set
            {
                
            }
        }

        public bool AutoCreate
        {
            get
            {
                return false;
            }
            set
            {
                
            }
        }

        public bool StateVariables
        {
            get
            {
                return false;
            }
            set
            {
                
            }
        }

        /// <summary>
        /// Write the summary report to the specified writer.
        /// </summary>
        /// <param name="writer">Text writer to write to</param>
        /// <param name="apsimSummaryImageFileName">The png file name for the apsim logo</param>
        /// <param name="baseline">Read from the baseline datastore?</param>
        public void WriteReport(System.IO.TextWriter writer, string apsimSummaryImageFileName, bool baseline)
        {

        }
        
        
    }
}
