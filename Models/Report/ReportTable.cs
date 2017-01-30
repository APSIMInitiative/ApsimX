// -----------------------------------------------------------------------
// <copyright file="ReportTable.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;

    /// <summary>A class for holding data for a .db table.</summary>
    [Serializable]
    public class ReportTable
    {
        /// <summary>The file name</summary>
        public string FileName;
        /// <summary>The simulation name</summary>
        public string SimulationName;
        /// <summary>The table name</summary>
        public string TableName;
        /// <summary>The data</summary>
        public List<IReportColumn> Data = new List<IReportColumn>();
    }
}
