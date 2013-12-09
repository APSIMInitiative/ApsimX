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
    public class NullSummary : Model, ISummary
    {


        public void WriteMessage(string FullPath, string Message)
        {
        }

        public void WriteWarning(string FullPath, string Message)
        {
        }

        public void WriteError(string Message)
        {
        }

        public void WriteProperty(string Name, string Value)
        {
        }

        public string GetSummary(string apsimSummaryImageFileName)
        {
            return null;
        }

        public void CreateReportFile(bool baseline)
        {
        }

        public bool html
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
    }
}
