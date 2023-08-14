using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections;
using System.Xml.Linq;

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

            //clear all rows that only have null, blank strings and 0s
            for(int i = data.Rows.Count-1; i >= 0; i--)
            {
                DataRow row = data.Rows[i];
                bool hasValues = false;
                foreach (var item in row.ItemArray) {
                    string value = item.ToString();
                    if (!String.IsNullOrEmpty(value) && value != "0")
                    {
                        hasValues = true;
                    }
                }
                if (!hasValues)
                {
                    data.Rows.RemoveAt(i);
                    //foreach (var column in columns)
                    columns.First().Remove(i);
                }
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

            /// <summary>A collection of properties that need to be kept in sync i.e. when one changes they all get changed. e.g. 'Depth'</summary>
            private IEnumerable<VariableProperty> properties { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Name of the column. Used for property name in Lists</param>
            /// <param name="property">The PropertyInfo instance</param>
            /// <param name="readOnly">Is the column readonly?</param>
            /// <param name="units">The units of the column.</param>
            public Column(string name, object property, bool readOnly = false, string units = null)
            {
                Name = name;

                //This is a merger of the old DataTables and the TabularData systems.
                //If an property array is provided, it uses the VariableProperty system
                //If a list is provided, it stores the list for use.

                if (property is VariableProperty props)
                {
                    properties = new VariableProperty[] { property as VariableProperty };
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
                        if (propertyValue is Array)
                        {
                            string[] values = null;
                            if (property.DataType == typeof(string[]))
                                values = ((string[])propertyValue).Select(v => v.ToString()).ToArray();
                            else if (property.DataType == typeof(double[]))
                                values = ((double[])propertyValue).Select(v => double.IsNaN(v) ? string.Empty : v.ToString("F3")).ToArray();

                            DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Length);
                        }
                        else if (propertyValue is IEnumerable<object> list)
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
                }
            }

            /// <summary>Setting model data. Called by GUI.</summary>
            /// <param name="data"></param>
            public void Set(DataTable data)
            {
                if (properties != null)
                {
                    var propertyValue = properties.First().Value;
                    if (propertyValue is Array)
                    {
                        int numRows = data.Rows.Count;
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
                    else if (propertyValue is IEnumerable<object>)
                    {
                        Model model = properties.First().Object as Model;
                        PropertyInfo fieldInfo = model.GetType().GetProperty("Parameters");
                        IEnumerable<object> list = fieldInfo.GetValue(model) as IEnumerable<object>;
                        while (data.Rows.Count > list.Count())
                        {
                            //make a new list
                            Type elementType = list.GetType().GetGenericArguments()[0];
                            Type listType = typeof(List<>).MakeGenericType(new[] { elementType });
                            IList newList = (IList)Activator.CreateInstance(listType);
                            //copy in the existing values
                            foreach (object element in list)
                            {
                                newList.Add(element);
                            }
                            //make a new instance of an element
                            object obj = Activator.CreateInstance(elementType);
                            //add it to the list
                            newList.Add(obj);

                            //Set the Model to use our modified list
                            fieldInfo.SetValue(model, newList);
                            list = newList as IEnumerable<object>;
                        }
                        for (int i = 0; i < data.Rows.Count; i++)
                        {
                            string value = data.Rows[i][Name].ToString();
                            int count = 0;
                            if (i < list.Count())
                            {
                                foreach (object obj in list)
                                {
                                    if (i == count)
                                    {
                                        ApplyChangesToListData(obj, Name, value);
                                    }
                                    count += 1;
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>Removes a row from a </summary>
            /// <param name="index"></param>
            public void Remove(int index)
            {
                if (properties != null)
                {
                    var propertyValue = properties.First().Value;
                    if (propertyValue is Array)
                    {
                    } 
                    else if (propertyValue is IEnumerable<object>)
                    {
                        Model model = properties.First().Object as Model;
                        PropertyInfo fieldInfo = model.GetType().GetProperty("Parameters");
                        IEnumerable<object> list = fieldInfo.GetValue(model) as IEnumerable<object>;
                        //make a new list
                        Type elementType = list.GetType().GetGenericArguments()[0];
                        Type listType = typeof(List<>).MakeGenericType(new[] { elementType });
                        IList newList = (IList)Activator.CreateInstance(listType);

                        //copy in the existing values
                        int i = 0;
                        foreach (object element in list)
                        {
                            if (i != index)
                                newList.Add(element);
                            i += 1;
                        }

                        //Set the Model to use our modified list
                        fieldInfo.SetValue(model, newList);
                    }
                }
            }

            private static void ApplyChangesToListData(object obj, string name, string value)
            {
                PropertyInfo propInfo = obj.GetType().GetProperty(name);
                FieldInfo fieldInfo = obj.GetType().GetField(name);

                if (propInfo != null)
                {
                    if (propInfo.PropertyType == typeof(double))
                    {
                        double valueAsDouble = 0;
                        if (!String.IsNullOrEmpty(value))
                            valueAsDouble = Convert.ToDouble(value);

                        propInfo.SetValue(obj, valueAsDouble);
                    }
                    else
                    {
                        propInfo.SetValue(obj, value);
                    }
                }
                    
                else if (fieldInfo != null)
                {
                    if (fieldInfo.FieldType == typeof(double))
                    {
                        double valueAsDouble = 0;
                        if (!String.IsNullOrEmpty(value))
                            valueAsDouble = Convert.ToDouble(value);

                        fieldInfo.SetValue(obj, valueAsDouble);
                    }
                    else
                    {
                        fieldInfo.SetValue(obj, value);
                    }
                }
                    
            }
        }

    }
}
