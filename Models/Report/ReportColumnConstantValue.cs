// -----------------------------------------------------------------------
// <copyright file="ReportColumnForFactorValue.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using System;
    using System.Collections.Generic;

    /// <summary>A class for outputting a constant value in a report column.</summary>
    [Serializable]
    public class ReportColumnConstantValue : IReportColumn
    {
        /// <summary>The column name for the constant</summary>
        private string name;

        /// <summary>The constant value</summary>
        private object value;

        /// <summary>
        /// Constructor for a plain report variable.
        /// </summary>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="constantValue">The constant value</param>
        public ReportColumnConstantValue(string columnName, object constantValue)
        {
            name = columnName;
            this.value = constantValue;
        }

        /// <summary>Return the number of values</summary>
        public int NumRows { get { return 1; } }

        /// <summary>Return the names and type of columns</summary>
        public void GetNamesAndTypes(List<string> columnNames, List<Type> columnTypes)
        {
            if (!columnNames.Contains(name))
            {
                columnNames.Add(name);
                columnTypes.Add(value.GetType());
            }
        }

        /// <summary>
        /// Insert values into the dataValues array for the specified row.
        /// </summary>
        /// <param name="rowIndex">The index of the row to return values for.</param>
        /// <param name="names">The names of each value to provide a value for.</param>
        /// <param name="dataValues">The values for the specified row.</param>
        public void InsertValuesForRow(int rowIndex, List<string> names, object[] dataValues)
        {
            int valueIndex = names.IndexOf(name);
            if (valueIndex != -1)
                dataValues[valueIndex] = value;
        }
    }
}
