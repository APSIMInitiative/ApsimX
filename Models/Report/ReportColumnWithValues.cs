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
        /// <summary>Name of column</summary>
        public string Name { get; private set; }

        /// <summary>The values</summary>
        public List<object> Values { get; set; }

        /// <summary>Constructor for a report column that has simple values.</summary>
        /// <param name="columnName">The column name to write to the output</param>
        public ReportColumnWithValues(string columnName)
        {
            Name = columnName;
            Values = new List<object>();
        }

        /// <summary>Constructor for a report column that has simple values.</summary>
        /// <param name="columnName">The column name to write to the output</param>
        /// <param name="initialValues">Values for column - used for testing.</param>
        public ReportColumnWithValues(string columnName, object[] initialValues)
        {
            Name = columnName;
            Values = new List<object>();
            Values.AddRange(initialValues);
        }

        /// <summary>Add a value.</summary>
        /// <param name="value">The value to add</param>
        public void Add(object value)
        {
            Values.Add(value);
        }

        /// <summary>Return the number of values</summary>
        public int NumRows { get { return Values.Count; } }


        /// <summary>Return the names and type of columns</summary>
        public void GetNamesAndTypes(List<string> columnNames, List<Type> columnTypes)
        {
            if (!columnNames.Contains(Name))
            {
                columnNames.Add(Name);
                if (Values.Count == 0)
                    columnTypes.Add(typeof(int));
                else
                    columnTypes.Add(Values[0].GetType());
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
            if (rowIndex < Values.Count)
            {
                int valueIndex = names.IndexOf(Name);
                if (valueIndex != -1)
                    dataValues[valueIndex] = Values[rowIndex];
            }
        }
    }
}
