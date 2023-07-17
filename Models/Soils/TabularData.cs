using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// Encapsulates tabular data that can be visualised and changed via a grid control.
    /// </summary>
    public class TabularData
    {
        // Name of data set.
        private string name;

        // List of all properties (name, property)
        private List<Column> columns = new List<Column>();

        /// <summary>Constructor</summary>
        /// <param name="nameOfData">Name of tabular data.</param>
        /// <param name="columns">Properties containing array variables. One for each column.</param>
        public TabularData(string nameOfData, IEnumerable<Column> columns)
        {
            name = nameOfData;
            this.columns.AddRange(columns);
        }

        /// <summary>The data table.</summary>
        public DataTable Data
        {
            get => GetData();
            set => SetData(value);
        }

        /// <summary>Get tabular data as a DataTable. Called by GUI.</summary>
        public DataTable GetData()
        {
            var data = new DataTable(name);

            // Add columns to data table.
            foreach (var column in columns)
                data.Columns.Add(new DataColumn(column.Name));

            // Add units to data table as row 1.
            var unitsRow = data.NewRow();
            foreach (var column in columns)
                unitsRow[column.Name] = column.Units;
            data.Rows.Add(unitsRow);

            // Add values to data table.
            foreach (var column in columns)
                column.AddColumnToDataTable(data);

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

            /// <summary>Name of column.</summary>
            public string Name { get; }

            /// <summary>A collection of properties that need to be kept in sync i.e. when one changes they all get changed. e.g. 'Depth'</summary>
            public IEnumerable<VariableProperty> Properties { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Name of the column.</param>
            /// <param name="property">The PropertyInfo instance.</param>
            /// <param name="readOnly">Is the column readonly?</param>
            /// <param name="units">The units of the column.</param>
            public Column(string name, VariableProperty property, bool readOnly = false, string units = null)
            {
                Name = name;
                Properties = new VariableProperty[] { property };
                IsReadOnly = readOnly || Properties.First().IsReadOnly;
                if (units == null)
                    this.units = Properties.First().Units;
                else
                    this.units = units;
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Name of the column.</param>
            /// <param name="properties">The collection of PropertyInfo instances that are all related.</param>
            public Column(string name, IEnumerable<VariableProperty> properties)
            {
                Name = name;
                Properties = properties;
            }

            /// <summary>Column units.</summary>
            public string Units
            {
                get => units;
                set
                {
                    Properties.First().Units = value;
                    units = value;
                }
            }

            /// <summary>Allowable column units.</summary>
            public IEnumerable<string> AllowableUnits => Properties.First().AllowableUnits.Select(au => au.Name);

            /// <summary>Is the column readonly?</summary>
            public bool IsReadOnly { get; }

            /// <summary>Add a column to a DataTable.</summary>
            /// <param name="data">The DataTable.</param>
            public void AddColumnToDataTable(DataTable data)
            {
                var property = Properties.First();
                var propertyValue = property.Value;
                if (propertyValue != null)
                {
                    string[] values = null;
                    if (property.DataType == typeof(string[]))
                        values = ((string[])propertyValue).Select(v => v.ToString()).ToArray();
                    else if (property.DataType == typeof(double[]))
                        values = ((double[])propertyValue).Select(v => double.IsNaN(v) ? string.Empty : v.ToString("F3")).ToArray();

                    DataTableUtilities.AddColumn(data, Name, values, 1, values.Length);
                }
            }

            /// <summary>Setting tabular data. Called by GUI.</summary>
            /// <param name="data"></param>
            public void Set(DataTable data)
            {
                int numLayers = data.Rows.Count - 1;

                foreach (var property in Properties)
                {
                    if (!property.IsReadOnly)
                    {
                        if (numLayers == -1)
                            property.Value = null;
                        else
                        {
                            if (property.DataType == typeof(string[]))
                                property.Value = DataTableUtilities.GetColumnAsStrings(data, Name, numLayers, 1, CultureInfo.CurrentCulture);
                            else if (property.DataType == typeof(double[]))
                            {
                                var values = DataTableUtilities.GetColumnAsDoubles(data, Name, numLayers, 1, CultureInfo.CurrentCulture);
                                property.Value = values;
                            }
                        }
                    }
                }
            }
        }

    }
}
