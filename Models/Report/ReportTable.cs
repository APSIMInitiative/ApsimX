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
    using System.Xml.Serialization;

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
        public List<IReportColumn> Columns = new List<IReportColumn>();
        /// <summary>
        ///  map of column name to units of measurement
        /// </summary>
        public Dictionary<String, String> colUnitsMap = new Dictionary<String, String>();

        /// <summary>Flatten the table i.e. turn arrays and structures into a flat table.</summary>
        public void Flatten()
        {
            List<IReportColumn> origColumns = Columns;
            Columns = new List<IReportColumn>();
            colUnitsMap.Clear();
            foreach (IReportColumn column in origColumns)
            {
                if (column is ReportColumnConstantValue)
                    Columns.Add(column);
                else
                    for (int rowIndex = 0; rowIndex < column.Values.Count; rowIndex++)
                        FlattenValue(column.Name, column.Units, column.Values[rowIndex], rowIndex);
            }
        }

        /// <summary>
        /// 'Flatten' the object passed in, into a list of columns ready to be added
        /// to a data table.
        /// </summary>
        /// <param name="name">Column name where the value came from.</param>
        /// <param name="units">Units of measurement; null if not applicable</param>
        /// <param name="value">The object to be analyzed and flattened</param>
        /// <param name="rowIndex">The row index of the specified value.</param>
        /// <returns>The list of values that can be written to a data table</returns>
        private void FlattenValue(string name, string units, object value, int rowIndex)
        {
            Type type = value.GetType();
            if (value == null || type == typeof(DateTime) || type == typeof(string) || !type.IsClass)
            {
                // Scalar
                IReportColumn flattenedColumn = Columns.Find(col => col.Name == name);
                if (flattenedColumn == null)
                {
                    flattenedColumn = new ReportColumnWithValues(name);
                    InsertColumn(flattenedColumn);
                    if (units != null)
                        colUnitsMap.Add(name, units);
                }

                // Ensure all columns have the correct number of values.
                foreach (IReportColumn column in Columns)
                {
                    if (column is ReportColumnConstantValue)
                    { }
                    else
                        while (column.Values.Count <= rowIndex)
                            column.Values.Add(null);
                }

                flattenedColumn.Values[rowIndex] = value;
            }
            else if (value.GetType().IsArray)
            {
                // Array
                Array array = value as Array;

                for (int columnIndex = 0; columnIndex < array.Length; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array.GetValue(columnIndex);
                    FlattenValue(heading, units, arrayElement, rowIndex);  // recursion
                }
            }
            else if (value.GetType().GetInterface("IList") != null)
            {
                // List
                IList array = value as IList;
                for (int columnIndex = 0; columnIndex < array.Count; columnIndex++)
                {
                    string heading = name;
                    heading += "(" + (columnIndex + 1).ToString() + ")";

                    object arrayElement = array[columnIndex];
                    FlattenValue(heading, units, arrayElement, rowIndex);  // recursion
                }
            }
            else
            {
                // A struct or class
                foreach (PropertyInfo property in ReflectionUtilities.GetPropertiesSorted(value.GetType(), BindingFlags.Instance | BindingFlags.Public))
                {
                    object[] attrs = property.GetCustomAttributes(true);
                    string propUnits = null;
                    bool ignore = false;
                    foreach (object attr in attrs)
                    {
                        if (attr is XmlIgnoreAttribute)
                        {
                            ignore = true;
                            continue;
                        }
                        Core.UnitsAttribute unitsAttr = attr as Core.UnitsAttribute;
                        if (unitsAttr != null)
                            propUnits = unitsAttr.ToString();
                    }
                    if (ignore)
                        continue;
                    string heading = name + "." + property.Name;
                    object classElement = property.GetValue(value, null);
                    FlattenValue(heading, propUnits, classElement, rowIndex);
                }
            }
        }

        /// <summary>
        /// Insert, at the correct location, the specified column into the columns list
        /// Need to ensure that array columns are kept in order.
        /// </summary>
        /// <param name="column">The column to insert.</param>
        private void InsertColumn(IReportColumn column)
        {
            int indexOfBracket = column.Name.IndexOf('(');
            if (indexOfBracket != -1)
            {
                // find the last column that has a name is identical up to bracketed value.
                string namePrefixToMatch = column.Name.Substring(0, indexOfBracket);
                int indexToInsertAfter = Columns.FindLastIndex(col => col.Name.StartsWith(namePrefixToMatch));
                if (indexToInsertAfter != -1)
                {
                    Columns.Insert(indexToInsertAfter + 1, column);
                    return;
                }
            }
            Columns.Add(column);
        }
    }
}
