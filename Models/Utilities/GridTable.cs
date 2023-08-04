using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Models.Core;
using System;

namespace Models.Utilities
{
    /// <summary>
    /// Provides a way to generate a DataTable while storing the data with additional data such as names,
    /// descriptions, units and other meta data.
    /// Data is not stored within a DataTable, but is passed in and out as one.
    /// Used by a number of classes to visualise additional information for displaying in the GUI.
    /// </summary>
    public class GridTable
    {
        // List of all properties (name, property)
        private List<Column> columns = new List<Column>();

        /// <summary>Name of the data set</summary>
        public string Name { get; set; }

        /// <summary>A description of the Data set and how it is used</summary>
        public string Description { get; set; }

        /// <summary>The data table.</summary>
        public DataTable Data
        {
            get { return GetData(); }
            set { SetData(value); }
        }

        /// <summary>Constructor</summary>
        /// <param name="nameOfData">Name of tabular data.</param>
        /// <param name="columns">Properties containing array variables. One for each column.</param>
        public GridTable(string nameOfData, IEnumerable<Column> columns)
        {
            Name = nameOfData;
            Description = "";
            this.columns.AddRange(columns);
        }

        /// <summary>Get tabular data as a DataTable. Called by GUI.</summary>
        public DataTable GetData()
        {
            var data = new DataTable(Name);

            // Add columns to data table.
            foreach (var column in columns)
                data.Columns.Add(new DataColumn(column.Name));

            // Add units to data table as row 1.
            //only show if one of the columns has units
            bool hasUnits = HasUnits();
            if (hasUnits)
            {
                var unitsRow = data.NewRow();
                foreach (var column in columns)
                    unitsRow[column.Name] = column.Units;
                data.Rows.Add(unitsRow);
            }                

            // Add values to data table.
            foreach (var column in columns)
                column.AddColumnToDataTable(data, hasUnits);

            // Set readonly flag on each column in data.
            foreach (var column in columns)
                data.Columns[column.Name].ReadOnly = column.IsReadOnly;

            return data;
        }

        /// <summary>Setting tabular data. Called by GUI.</summary>
        /// <param name="data"></param>
        public void SetData(DataTable data)
        {
            // If Depth is the first column then use the number of values in
            // that column to resize the other columns.
            if (data != null && data.Columns.Count > 0 &&
                data.Columns[0].ColumnName == "Depth")
            {
                string[] depths = DataTableUtilities.GetColumnAsStrings(data, "Depth", CultureInfo.CurrentCulture);
                var numLayers = depths.TrimEnd().Count;
                while (data.Rows.Count > numLayers)
                    data.Rows.RemoveAt(data.Rows.Count - 1); // remove bottom row.
            }

            foreach (var column in columns)
                column.Set(data);
        }

        /// <summary>
        /// Get possible units for a given column.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <returns></returns>
        public IEnumerable<string> GetUnits(int columnIndex)
        {
            if (columnIndex < columns.Count)
                return columns[columnIndex].AllowableUnits;
            else
                return new string[0];
        }

        /// <summary>
        /// Returns true if any column has units assigned
        /// </summary>
        /// <returns></returns>
        public bool HasUnits()
        {
            foreach (var column in columns)
                if (column.Units != null)
                    return true;
            return false;
        }

        /// <summary>
        /// Set the units for a column.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="units"></param>
        public void SetUnits(int columnIndex, string units)
        {
            if (columnIndex < columns.Count)
                columns[columnIndex].Units = units;
        }

        /// <summary>Encapsulates a column of data.</summary>
        public class Column
        {
            /// <summary>Column units.</summary>
            private string units;

            /// <summary>Column Vales</summary>
            private IEnumerable<object> list;

            /// <summary>A collection of properties that need to be kept in sync i.e. when one changes they all get changed. e.g. 'Depth'</summary>
            private IEnumerable<VariableProperty> properties { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Name of the column. Used for property name in Lists</param>
            /// <param name="property">The PropertyInfo instance or a List of Objects</param>
            /// <param name="readOnly">Is the column readonly?</param>
            /// <param name="units">The units of the column.</param>
            public Column(string name, object property, bool readOnly = false, string units = null)
            {
                Name = name;

                //This is a merger of the old DataTables and the TabularData systems.
                //If an property array is provided, it uses the VariableProperty system
                //If a list is provided, it stores the list for use.

                if (property is VariableProperty)
                {
                    properties = new VariableProperty[] { property as VariableProperty };
                }
                else if (property is IEnumerable<object>)
                {
                    list = property as IEnumerable<object>;
                }

                IsReadOnly = readOnly;
                if (properties != null)
                    IsReadOnly = properties.First().IsReadOnly;

                this.units = units;
                if (properties != null)
                    this.units = properties.First().Units;
            }

            /// <summary>Name of column.</summary>
            public string Name { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Name of the column.</param>
            /// <param name="properties">The collection of PropertyInfo instances that are all related.</param>
            public Column(string name, IEnumerable<VariableProperty> properties)
            {
                Name = name;
                this.properties = properties;
            }

            /// <summary>Column units.</summary>
            public string Units
            {
                get => units;
                set
                {
                    properties.First().Units = value;
                    units = value;
                }
            }

            /// <summary>Allowable column units.</summary>
            public IEnumerable<string> AllowableUnits => properties.First().AllowableUnits.Select(au => au.Name);

            /// <summary>Is the column readonly?</summary>
            public bool IsReadOnly { get; }

            /// <summary>Add a column to a DataTable.</summary>
            /// <param name="data">The DataTable.</param>
            /// <param name="hasUnits">Does the Table have a Units row</param>
            public void AddColumnToDataTable(DataTable data, bool hasUnits)
            {
                int startingRow = 0;
                if (hasUnits)
                    startingRow = 1;

                if (properties != null)
                {
                    var property = properties.First();
                    var propertyValue = property.Value;
                    if (propertyValue != null)
                    {
                        string[] values = null;
                        if (property.DataType == typeof(string[]))
                            values = ((string[])propertyValue).Select(v => v.ToString()).ToArray();
                        else if (property.DataType == typeof(double[]))
                            values = ((double[])propertyValue).Select(v => double.IsNaN(v) ? string.Empty : v.ToString("F3")).ToArray();
                        
                        DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Length);
                    }
                } 
                else if (list != null)
                {
                    List<string> values = new List<string>();

                    PropertyInfo propInfo = null;
                    FieldInfo fieldInfo = null;
                    foreach (object obj in list)
                    {
                        if (propInfo == null)
                            propInfo = obj.GetType().GetProperty(Name);
                        if (fieldInfo == null)
                            fieldInfo = obj.GetType().GetField(Name);

                        if (propInfo != null)
                            values.Add(propInfo.GetValue(obj).ToString());
                        else if (fieldInfo != null)
                            values.Add(fieldInfo.GetValue(obj).ToString());
                    }
                    DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Count);
                }
                
            }

            /// <summary>Setting model data. Called by GUI.</summary>
            /// <param name="data"></param>
            public void Set(DataTable data)
            {
                int numRows = data.Rows.Count - 1;

                if (properties != null)
                {
                    foreach (var property in properties)
                    {
                        if (!property.IsReadOnly)
                        {
                            if (numRows == -1)
                                property.Value = null;
                            else
                            {
                                if (property.DataType == typeof(string[]))
                                    property.Value = DataTableUtilities.GetColumnAsStrings(data, Name, numRows, 1, CultureInfo.CurrentCulture);
                                else if (property.DataType == typeof(double[]))
                                {
                                    var values = DataTableUtilities.GetColumnAsDoubles(data, Name, numRows, 1, CultureInfo.CurrentCulture);
                                    property.Value = values;
                                }
                            }
                        }
                    }
                }
                else if (list != null)
                {
                    PropertyInfo propInfo = null;
                    FieldInfo fieldInfo = null;
                    for (int i = 0; i < data.Rows.Count; i++) {
                        string value = data.Rows[i][Name].ToString();
                        int count = 0;
                        foreach (object obj in list)
                        {
                            if (i == count)
                            {
                                if (propInfo == null)
                                    propInfo = obj.GetType().GetProperty(Name);
                                if (fieldInfo == null)
                                    fieldInfo = obj.GetType().GetField(Name);

                                if (propInfo != null)
                                    if (propInfo.PropertyType == typeof(double))
                                        propInfo.SetValue(obj, Convert.ToDouble(value));
                                    else
                                        propInfo.SetValue(obj, value);
                                else if (fieldInfo != null)
                                    if (fieldInfo.FieldType == typeof(double))
                                        fieldInfo.SetValue(obj, Convert.ToDouble(value));
                                    else
                                        fieldInfo.SetValue(obj, value);
                            }
                            count += 1;
                        }
                    }
                }
            }
        }

    }
}
