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
        /// <summary>The number of rows</summary>
        int NumRows { get; }

        /// <summary>Get the names and types of all row values.</summary>
        void GetNamesAndTypes(List<string> columnNames, List<Type> columnTypes);

        /// <summary>
        /// Insert values into the dataValues array for the specified row.
        /// </summary>
        /// <param name="rowIndex">The index of the row to return values for.</param>
        /// <param name="names">The names of each value to provide a value for.</param>
        /// <param name="dataValues">The values for the specified row.</param>
        void InsertValuesForRow(int rowIndex, List<string> names, object[] dataValues);
    }
}