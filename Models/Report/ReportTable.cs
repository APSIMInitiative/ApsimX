// -----------------------------------------------------------------------
// <copyright file="ReportTable.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Report
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

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

        /// <summary>
        /// Return all data.
        /// </summary>
        /// <param name="store">Datastore</param>
        /// <returns>A list of all data columns.</returns>
        public List<IReportColumn> GetAllColumns(DataStore store)
        {
            if (Data.Count > 0)
            {
                int numRows = Data[0].NumRows;
                int simulationID = store.GetSimulationID(SimulationName);
                List<IReportColumn> allColumns = new List<Models.Report.IReportColumn>();
                //allColumns.Add(new ReportColumnConstantValue("SimulationID", simulationID, numRows));
                //allColumns.AddRange(Data);
                return allColumns;
            }
            return null;
        }

        /// <summary>Merge (copy all columns and values) into the specified table.</summary>
        /// <param name="table">The table to merge into.</param>
        public void MergeInto(ReportTable table)
        {
            if (table.TableName != TableName)
                throw new Exception("Cannot merge report tables. The table names don't match");

            // Merge columns that match.
            foreach (IReportColumn column in Data)
            {
                IReportColumn existingColumn = table.Data.Find(col => col.Name.Equals(column.Name, StringComparison.CurrentCultureIgnoreCase));
                if (existingColumn != null)
                    existingColumn.Values.AddRange(column.Values);
            }

            // Standardise the number of values in each column
            int numRows = 0;
            table.Data.ForEach(col => numRows = Math.Max(numRows, col.Values.Count));

            // Fill columns that don't have enough rows with nulls.
            foreach (IReportColumn column in table.Data)
            {
                object valueToAdd = null;
                if (column is ReportColumnConstantValue)
                    valueToAdd = column.Values[0];
                while (column.Values.Count < numRows)
                    column.Values.Add(valueToAdd);
            }

            // Add new columns to the end of the specified table.
            foreach (IReportColumn column in Data)
            {
                IReportColumn existingColumn = table.Data.Find(col => col.Name.Equals(column.Name, StringComparison.CurrentCultureIgnoreCase));
                if (existingColumn == null)
                {
                    // Move values down in new colulmn so that the number of values matches
                    // the number of rows.
                    while (column.Values.Count < numRows)
                        column.Values.Insert(0, null);
                    table.Data.Add(column);
                }
            }
        }

        /// <summary>Flatten the table i.e. turn arrays and structures into a flat table.</summary>
        public List<IReportColumn> Flatten()
        {
            List<IReportColumn> flattenedColumns = new List<Models.Report.IReportColumn>();
            foreach (IReportColumn column in Data)
            {
                for (int rowIndex = 0; rowIndex < column.Values.Count; rowIndex++)
                    FlattenValue(column.Name, column.Values[rowIndex], rowIndex, flattenedColumns);
            }

            return flattenedColumns;
        }

        /// <summary>
        /// 'Flatten' the object passed in, into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        /// <param name="name">Column name where the value came from.</param>
        /// <param name="value">The object to be analyzed and flattened</param>
        /// <param name="rowIndex">The row index of the specified value.</param>
        /// <param name="flattenedColumns">The returned flattened columns.</param>
        /// <returns>The list of values that can be written to a data table</returns>
        private static void FlattenValue(string name, object value, int rowIndex, List<IReportColumn> flattenedColumns)
        {
            if (value.GetType().IsArray)
            {
                // Array
                Array array = value as Array;

                for (int columnIndex = 0; columnIndex < array.Length; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array.GetValue(columnIndex);
                    FlattenValue(heading, arrayElement, rowIndex, flattenedColumns);  // recursion
                }
            }
            else if (value.GetType().GetInterface("IList") != null)
            {
                IList array = value as IList;
                for (int columnIndex = 0; columnIndex < array.Count; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array[columnIndex];
                    FlattenValue(heading, arrayElement, rowIndex, flattenedColumns);  // recursion
                }
            }
            else if (value.GetType() == typeof(DateTime) || value.GetType() == typeof(string) || !value.GetType().IsClass)
            {
                // Scalar
                IReportColumn flattenedColumn = flattenedColumns.Find(col => col.Name == name);
                if (flattenedColumn == null)
                {
                    flattenedColumn = new ReportColumnWithValues(name);
                    InsertColumn(flattenedColumn, flattenedColumns);
                }

                // Ensure all columns have the correct number of values.
                foreach (IReportColumn column in flattenedColumns)
                {
                    while (column.Values.Count <= rowIndex)
                        column.Values.Add(null);
                }

                flattenedColumn.Values[rowIndex] = value;
            }
            else
            {
                // A struct or class
                foreach (PropertyInfo property in ReflectionUtilities.GetPropertiesSorted(value.GetType(), BindingFlags.Instance | BindingFlags.Public))
                {
                    string heading = name + "." + property.Name;
                    object classElement = property.GetValue(value, null);
                    FlattenValue(heading, classElement, rowIndex, flattenedColumns);
                }
            }
        }

        /// <summary>
        /// Insert, at the correct location, the specified column into the columns list
        /// Need to ensure that array columns are kept in order.
        /// </summary>
        /// <param name="column">The column to insert.</param>
        /// <param name="flattenedColumns">The returned flattened columns.</param>
        private static void InsertColumn(IReportColumn column, List<IReportColumn> flattenedColumns)
        {
            int indexOfBracket = column.Name.IndexOf('(');
            if (indexOfBracket != -1)
            {
                // find the last column that has a name is identical up to bracketed value.
                string namePrefixToMatch = column.Name.Substring(0, indexOfBracket);
                int indexToInsertAfter = flattenedColumns.FindLastIndex(col => col.Name.StartsWith(namePrefixToMatch));
                if (indexToInsertAfter != -1)
                {
                    flattenedColumns.Insert(indexToInsertAfter + 1, column);
                    return;
                }
            }
            flattenedColumns.Add(column);
        }
    }
}
