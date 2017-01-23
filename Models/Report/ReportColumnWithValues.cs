// -----------------------------------------------------------------------
// <copyright file="ReportColumnForFactorValue.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;

    /// <summary>A class for containing values for a report column</summary>
    [Serializable]
    public class ReportColumnWithValues : IReportColumn
    {
        /// <summary>The column name for the constant</summary>
        private string name;

        /// <summary>The values</summary>
        private List<object> values = new List<object>();

        /// <summary>Constructor for a report column that has simple values.</summary>
        /// <param name="columnName">The column name to write to the output</param>
        public ReportColumnWithValues(string columnName)
        {
            name = columnName;
        }

        /// <summary>Add a value.</summary>
        /// <param name="value">The value to add</param>
        public void Add(object value)
        {
            values.Add(value);
        }

        /// <summary>Return the number of values</summary>
        public int NumRows { get { return values.Count; } }

        /// <summary>Return the names and type of columns</summary>
        public void GetNamesAndTypes(List<string> columnNames, List<Type> columnTypes)
        {
            if (!columnNames.Contains(name))
            {
                columnNames.Add(name);
                if (values.Count == 0)
                    columnTypes.Add(typeof(int));
                else
                    columnTypes.Add(values[0].GetType());
            }
        }

        /// <summary>Return the row values</summary>
        public void GetRowValues(int rowIndex, List<object> dataValues)
        {
            if (rowIndex < values.Count)
                dataValues.Add(values[rowIndex]);
            else
                dataValues.Add(null);
        }
    }
}
