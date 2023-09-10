using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using System;
using System.Collections;
using System.ComponentModel;
using Models.Interfaces;

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

        /// <summary>A description of the Data set and how it is used</summary>
        public Model Model { get; set; }

        /// <summary>The data table.</summary>
        public DataTable Data
        {
            get { return (Model as IGridTable).ConvertModelToDisplay(GetData()); }
            set { SetData((Model as IGridTable).ConvertDisplayToModel(value)); }
        }

        /// <summary>Constructor</summary>
        /// <param name="nameOfData">Name of tabular data.</param>
        /// <param name="columns">Properties containing array variables. One for each column.</param>
        /// <param name="model">The the model being shown in this grid table</param>
        public GridTable(string nameOfData, IEnumerable<Column> columns, Model model)
        {
            Name = nameOfData;
            Description = "";
            Model = model;
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
            if (data.Rows.Count == 0)
                return;

            //if the first row is units, delete it
            if (this.HasUnits())
                data.Rows.RemoveAt(0);

            //if there is a depth column, don't let person add more rows
            if (data != null && data.Columns.Count > 0 &&
                data.Columns[0].ColumnName == "Depth")
            {
                string[] depths = DataTableUtilities.GetColumnAsStrings(data, "Depth", CultureInfo.CurrentCulture);
                var numLayers = depths.TrimEnd().Count;
                while (data.Rows.Count > numLayers)
                    data.Rows.RemoveAt(data.Rows.Count - 1); // remove bottom row.
            }

            //if the last row is empty in all spaces, remove it
            if (data.Rows.Count > 0)
            {
                DataRow row = data.Rows[data.Rows.Count - 1];
                bool blank = true;
                foreach (var value in row.ItemArray)
                    if (!String.IsNullOrEmpty(value.ToString()))
                            blank = false;

                if (blank)
                    data.Rows.RemoveAt(data.Rows.Count - 1); // remove bottom row.
            }

            //copy values into properties
            foreach (var column in columns)
                if (!column.IsReadOnly)
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
                if (!String.IsNullOrEmpty(column.Units))
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
                if (properties != null && IsReadOnly == false)
                    IsReadOnly = properties.First().IsReadOnly;

                this.units = units;
                if (properties != null && this.units == null)
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
                            else if (property.DataType == typeof(DateTime[]))
                                values = ((DateTime[])propertyValue).Select(v => v.ToString("yyyy/MM/dd")).ToArray();

                            DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Length);
                        }
                        else if (propertyValue is IEnumerable<object> list)
                        {
                            List<string> values = new List<string>();

                            PropertyInfo propInfo = null;
                            FieldInfo fieldInfo = null;
                            foreach (object obj in list)
                            {
                                object val = null;
                                if (VariableIsPrimitive(obj))
                                {
                                    val = obj;
                                }
                                else
                                {
                                    if (propInfo == null)
                                        propInfo = obj.GetType().GetProperty(Name);
                                    if (fieldInfo == null)
                                        fieldInfo = obj.GetType().GetField(Name);

                                    if (propInfo != null)
                                        val = propInfo.GetValue(obj);
                                    else if (fieldInfo != null)
                                        val = fieldInfo.GetValue(obj);
                                }
                                
                                if (val is double && double.IsNaN((double)val))
                                    values.Add(string.Empty);
                                else
                                    values.Add(val.ToString());
                            }
                            DataTableUtilities.AddColumn(data, Name, values, startingRow, values.Count);
                        }
                    }
                }
            }

            /// <summary>Setting model data. Called by GUI.</summary>
            /// <param name="data"></param>'
            public void Set(DataTable data)
            {
                if (properties != null)
                {
                    Type propertyType = properties.First().DataType;
                    if (propertyType == typeof(string[]) || propertyType == typeof(double[]) || propertyType == typeof(DateTime[]))
                    {
                        int numRows = data.Rows.Count;
                        foreach (var property in properties)
                        {
                            if (!property.IsReadOnly)
                            {
                                if (property.DataType == typeof(string[]))
                                    property.Value = DataTableUtilities.GetColumnAsStrings(data, Name, numRows, 0, CultureInfo.CurrentCulture);
                                else if (property.DataType == typeof(double[]))
                                    property.Value = DataTableUtilities.GetColumnAsDoubles(data, Name, numRows, 0, CultureInfo.CurrentCulture);
                                else if (property.DataType == typeof(DateTime[]))
                                    property.Value = DataTableUtilities.GetColumnAsDates(data, Name); //todo: add numRows/startRow option for dates
                            }
                        }
                    }
                    else if (propertyType.FullName.Contains("System.Collections.Generic.List"))
                    {
                        Model model = properties.First().Object as Model;
                        PropertyInfo fieldInfo = model.GetType().GetProperty(properties.First().Name);
                        IEnumerable<object> list = fieldInfo.GetValue(model) as IEnumerable<object>;

                        //make a new list
                        if (list != null)
                        {
                            Type elementType = list.GetType().GetGenericArguments()[0];
                            Type listType = typeof(List<>).MakeGenericType(new[] { elementType });
                            IList newList = (IList)Activator.CreateInstance(listType);

                            if (TypeIsPrimitive(elementType))
                            {
                                for (int i = 0; i < data.Rows.Count; i++)
                                {
                                    TypeConverter typeConverter = TypeDescriptor.GetConverter(elementType);
                                    object propValue = typeConverter.ConvertFromString(data.Rows[i][Name].ToString());
                                    newList.Add(propValue);
                                }

                                //Set the Model to use our modified list
                                fieldInfo.SetValue(model, newList);
                            }
                            else
                            {
                                //each column send it's own update event
                                //so we need to use the existing object's value to avoid overwriting them
                                //but we only add as many rows as required by the new table
                                foreach (object obj in list)
                                    if (newList.Count < data.Rows.Count)
                                        newList.Add(obj);
                                //on the first column that runs, it must add additional entries for any new lines
                                for (int i = 0; i < data.Rows.Count - list.Count(); i++)
                                    newList.Add(Activator.CreateInstance(elementType));
                                //once we have a list of the right length, we then write in our cells one at a time.
                                for (int i = 0; i < data.Rows.Count; i++)
                                {
                                    string value = data.Rows[i][Name].ToString();
                                    ApplyChangesToListData(newList[i], Name, value);
                                }
                                //Set the Model to use our modified list
                                fieldInfo.SetValue(model, newList);
                            }
                        }
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
                        double valueAsDouble = double.NaN;
                        if (!String.IsNullOrEmpty(value))
                            valueAsDouble = Convert.ToDouble(value);

                        propInfo.SetValue(obj, valueAsDouble);
                    }
                    else if (propInfo.PropertyType == typeof(bool))
                    {
                        bool? valueAsBoolen = null;
                        if (!String.IsNullOrEmpty(value))
                            valueAsBoolen = Convert.ToBoolean(value);
                        propInfo.SetValue(obj, valueAsBoolen);
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
                        double valueAsDouble = double.NaN;
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

            private bool VariableIsPrimitive(object obj)
            {
                return TypeIsPrimitive(obj.GetType());
            }

            private bool TypeIsPrimitive(Type type)
            {
                if (type.IsPrimitive || type == typeof(string) || type == typeof(System.String))
                    return true;
                else
                    return false;
            }
        }

    }
}
