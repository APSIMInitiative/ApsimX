// -----------------------------------------------------------------------
// <copyright file="ReportColumn.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;

    /// <summary>An interface for a column in a report table.</summary>
    public interface IReportColumn
    {
        /// <summary>Name of column.</summary>
        string Name { get; }

        /// <summary>Values for each row of this column.</summary>
        List<object> Values { get; }
    }
}