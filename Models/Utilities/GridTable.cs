using System;
using System.Collections.Generic;
using System.Data;
using Models.Core;
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
        private List<GridTableColumn> columns = new List<GridTableColumn>();

        /// <summary>Name of the data set</summary>
        public string Name { get; set; }

        /// <summary>A description of the Data set and how it is used</summary>
        public string Description { get; set; }

        /// <summary>A description of the Data set and how it is used</summary>
        public Model Model { get; set; }

        /// <summary>The data table.</summary>
        public DataTable Data
        {
            get { return (Model as IGridModel).ConvertModelToDisplay(GetData()); }
            set { SetData((Model as IGridModel).ConvertDisplayToModel(value)); }
        }

        /// <summary>Constructor</summary>
        /// <param name="nameOfData">Name of tabular data.</param>
        /// <param name="columns">Properties containing array variables. One for each column.</param>
        /// <param name="model">The the model being shown in this grid table</param>
        public GridTable(string nameOfData, IEnumerable<GridTableColumn> columns, Model model)
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

            //if the last row is empty in all spaces, remove it
            for (int i = data.Rows.Count - 1; i >= 0; i--)
            {
                DataRow row = data.Rows[i];
                bool blank = true;
                foreach (var value in row.ItemArray)
                    if (!String.IsNullOrEmpty(value.ToString()))
                        blank = false;

                if (blank)
                    data.Rows.RemoveAt(i); //remove blank row
            }

            //copy values into properties
            foreach (var column in columns)
                if (!column.IsReadOnly)
                    column.Set(data, HasUnits());
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

        /// <summary>
        /// Get possible units for a given column.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <returns></returns>
        public List<bool> GetIsCalculated(int columnIndex)
        {
            if (columnIndex < columns.Count)
                return columns[columnIndex].IsCalculated;
            else
                return null;
        }        
    }
}
