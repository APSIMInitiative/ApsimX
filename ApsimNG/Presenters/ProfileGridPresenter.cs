using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;
using UserInterface.Commands;
using UserInterface.EventArguments;
using UserInterface.Interfaces;

namespace UserInterface.Presenters
{
    public class ProfileGridPresenter : GridPresenter
    {
        /// <summary>
        /// List of properties shown in the grid.
        /// </summary>
        private List<VariableProperty> properties;

        /// <summary>
        /// Model whose properties are being shown.
        /// </summary>
        private IModel model;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to</param>
        /// <param name="view">The view to connect to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            base.Attach(model, view, explorerPresenter);
            this.model = model as IModel;

            // No intellisense in this grid.

            // if the model is Testable, run the test method.
            if (model is ITestable test)
            {
                test.Test(false, true);
                grid.ReadOnly = true;
            }

            grid.NumericFormat = "N3";

            PopulateGrid(this.model);

            grid.CellsChanged += OnCellsChanged;
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public override void Detach()
        {
            try
            {
                base.Detach();
                grid.CellsChanged -= OnCellsChanged;
                presenter.CommandHistory.ModelChanged -= OnModelChanged;
            }
            catch (NullReferenceException)
            {
                // to keep Neil happy
            }
        }

        /// <summary>
        /// Properties displayed by this presenter.
        /// </summary>
        public VariableProperty[] Properties
        {
            get
            {
                return properties.ToArray();
            }
        }

        /// <summary>
        /// Populates the grid view with data, or refreshes the grid if
        /// it already contains data.
        /// </summary>
        /// <param name="model">The model to examine for properties.</param>
        private void PopulateGrid(IModel model)
        {
            // After refreshing the grid, we want the selected cell
            // to still be selected.
            IGridCell selectedCell = grid.GetCurrentCell;
            this.model = model;

            properties = FindAllProperties(this.model);
            DataTable table = CreateGrid();
            FillTable(table);
            grid.DataSource = table;
            FormatGrid();

            if (selectedCell != null)
                grid.GetCurrentCell = selectedCell;
        }
        
        private List<VariableProperty> FindAllProperties(IModel model)
        {
            List<VariableProperty> properties = new List<VariableProperty>();

            // When user clicks on a SoilCrop, there is no thickness column. In this
            // situation get thickness column from parent model.
            if (this.model is SoilCrop && model.Parent is Physical water)
            {
                PropertyInfo thickness = water.GetType().GetProperty("Thickness");
                properties.Add(new VariableProperty(water, thickness));
            }

            foreach (PropertyInfo property in model.GetType().GetProperties())
            {
                Attribute description = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), false);
                if (property.PropertyType.IsArray && description != null)
                    properties.Add(new VariableProperty(model, property));
            }

            foreach (SoilCrop crop in Apsim.Children(model, typeof(SoilCrop)))
                properties.AddRange(FindAllProperties(crop));

            return properties;
        }

        /// <summary>
        /// Creates the skeleton data table with columns but no data.
        /// </summary>
        private DataTable CreateGrid()
        {
            DataTable table = new DataTable();
            for (int i = 0; i < properties.Count; i++)
            {
                VariableProperty property = properties[i] as VariableProperty;

                // Each property represents a column of data.
                // todo - do we want to use correct element type for this column?
                // e.g. double type if property is a double array.
                table.Columns.Add(new DataColumn(GetColumnName(property), typeof(string)));
            }

            return table;
        }

        /// <summary>
        /// Fill the specified table with columns and rows based on this.Properties
        /// </summary>
        /// <param name="table">The table that needs to be filled</param>
        private void FillTable(DataTable table)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                // Skip this property if it's not an array. This should never
                // happen because we don't add non-array properties to the list
                // of properties.
                VariableProperty property = properties[i] as VariableProperty;
                if (!property.DataType.IsArray)
                    continue;

                // Ensure that we have enough rows to display all items in this array.
                Array array = property.Value as Array;
                if (array == null)
                    continue;

                while (table.Rows.Count < array.Length)
                    table.Rows.Add(table.NewRow());

                // Now add the items in this array to the rows in the i-th column.
                // This will break if there are any non-array properties in the 
                // list of properties, because i will be greater than the number
                // of columns.
                for (int j = 0; j < array.Length; j++)
                    table.Rows[j][i] = GetCellValue(j, i);
            }
        }

        /// <summary>
        /// Figures out the appropriate name for a column. This is only
        /// necessary because the columns for soil crop properties need
        /// to contain the soil crop's name (e.g. Wheat LL).
        /// </summary>
        private string GetColumnName(VariableProperty property)
        {
            string columnName = property.Name;
            if (property.Name == "Thickness")
                columnName = "Depth";
            else if (property.Object is SoilCrop crop)
            {
                // This column represents an array property of a SoilCrop.
                // Column name by default would be something like XF but we
                // want the column to be called 'Wheat XF'.
                columnName = crop.Name.Replace("Soil", "") + " " + property.Name;
            }
            if (property.Units != null)
                columnName += $" \n({property.Units})";

            return columnName;
        }

        /// <summary>
        /// Formats the GridView. Sets colours, spacing, locks the
        /// depth column, etc.
        /// </summary>
        private void FormatGrid()
        {
            for (int i = 0; i < properties.Count; i++)
            {
                VariableProperty property = properties[i] as VariableProperty;
                if (!(property.Object is SoilCrop))
                    continue;

                SoilCrop crop = property.Object as SoilCrop;
                int index = Apsim.Children(crop.Parent, typeof(SoilCrop)).IndexOf(crop);
                Color foreground = ColourUtilities.ChooseColour(index);
                if (property.IsReadOnly)
                    foreground = Color.Red;

                grid.GetColumn(i).ForegroundColour = foreground;
                grid.GetColumn(i).MinimumWidth = 70;
                grid.GetColumn(i).ReadOnly = property.IsReadOnly;
                // Make the soil crop columns wider to fit the crop name in column title.
                grid.GetColumn(i).Width = 90;
            }
            grid.LockLeftMostColumns(1);
        }

        /// <summary>
        /// Gets a formatted value for a cell in the grid. This is
        /// necessary because the grid uses the string data type for
        /// everything, so we need to convert thicknesses to depths
        /// and format the numbers correctly, (# of decimal places, and
        /// show nothing instead of NaN).
        /// </summary>
        /// <param name="row">Row index of the cell.</param>
        /// <param name="column">Column index of the cell.</param>
        private object GetCellValue(int row, int column)
        {
            VariableProperty property = properties[column];
            if (property.Name == "Thickness")
            {
                string[] depths = APSIM.Shared.APSoil.SoilUtilities.ToDepthStrings((double[])property.Value);
                return depths[row];
            }
            object value = (property.Value as Array)?.GetValue(row);
            if (value == null)
                return null;

            Type dataType = property.DataType.GetElementType();
            if (dataType == typeof(double) && double.IsNaN((double)value))
                return "";
            if (dataType == typeof(float) && double.IsNaN((float)value))
                return "";

            if (dataType == typeof(double))
                return ((double)value).ToString(grid.NumericFormat);
            if (dataType == typeof(float))
                return ((float)value).ToString(grid.NumericFormat);

            return value;
        }

        /// <summary>
        /// Gets the new value of the property which will be passed
        /// into the model.
        /// </summary>
        /// <param name="cell">Cell which has been changed.</param>
        private object GetNewPropertyValue(GridCellChangedArgs cell)
        {
            VariableProperty property = properties[cell.ColIndex];
            if (typeof(IPlant).IsAssignableFrom(property.DataType))
                return Apsim.Find(property.Object as IModel, cell.NewValue);

            if (property.Display != null && property.Display.Type == DisplayType.Model)
                return Apsim.Get(property.Object as IModel, cell.NewValue);

            try
            {
                if (property.Name == "Thickness")
                {
                    double[] thickness = (double[])property.Value;
                    string[] depths = APSIM.Shared.APSoil.SoilUtilities.ToDepthStrings(thickness);
                    depths[cell.RowIndex] = cell.NewValue;
                    return APSIM.Shared.APSoil.SoilUtilities.ToThickness(depths);
                }
                object value = ReflectionUtilities.StringToObject(property.DataType.GetElementType(), cell.NewValue, CultureInfo.CurrentCulture);

                // We now have the value of a single element of the
                // array. We need to copy the actual array stored in
                // the model and change the appropriate element.
                Array array;
                if (property.Value == null)
                {
                    // Can't clone null - setup array and fill with NaN.
                    array = new double[cell.RowIndex + 1]; // fixme
                    for (int i = 0; i < array.Length; i++)
                        array.SetValue(double.NaN, i);
                }
                else
                {
                    // Get a deep copy of the model's array property.
                    double[] arr = ReflectionUtilities.Clone(property.Value) as double[];
                    int n = arr.Length;
                    if (n <= cell.RowIndex)
                        Array.Resize(ref arr, cell.RowIndex + 1);
                    for (int i = n; i < arr.Length; i++)
                        arr[i] = double.NaN;
                    array = arr;
                }

                array.SetValue(value, cell.RowIndex);

                if (!MathUtilities.ValuesInArray(array))
                    array = null;

                return array;
            }
            catch (FormatException err)
            {
                throw new Exception($"Value '{cell.NewValue}' is invalid for property '{property.Name}' - {err.Message}.");
            }
        }

        /// <summary>
        /// Update read-only (calculated) properties in the grid.
        /// </summary>
        private void UpdateReadOnlyProperties()
        {
            for (int i = 0; i < properties.Count; i++)
            {
                VariableProperty property = properties[i] as VariableProperty;
                if (property.IsReadOnly && property.DataType.IsArray)
                {
                    Array value = property.Value as Array;
                    for (int j = 0; j < value.Length; j++)
                        grid.DataSource.Rows[j][i] = GetCellValue(j, i);
                }
            }
        }

        /// <summary>
        /// Set the value of the specified property
        /// </summary>
        /// <param name="property">The property to set the value of</param>
        /// <param name="value">The value to set the property to</param>
        private void SetPropertyValue(IVariable property, object value)
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            try
            {
                ChangeProperty cmd = new ChangeProperty(property.Object, property.Name, value);
                presenter.CommandHistory.Add(cmd);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// User has changed the value of a cell. Validate the change
        /// apply the change.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="args">Event parameters</param>
        private void OnCellsChanged(object sender, GridCellsChangedArgs args)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            foreach (GridCellChangedArgs cell in args.ChangedCells)
            {
                if (cell.NewValue == cell.OldValue)
                    continue; // silently fail

                // If there are multiple changed cells, each change will be
                // individually undoable.
                IVariable property = properties[cell.ColIndex];
                if (property == null)
                    continue;

                // Parse the input string to 
                object newValue = GetNewPropertyValue(cell);

                // Update the value of the model's property.
                SetPropertyValue(property, newValue);

                // Update the value shown in the grid.
                grid.DataSource.Rows[cell.RowIndex][cell.ColIndex] = GetCellValue(cell.RowIndex, cell.ColIndex);
            }

            UpdateReadOnlyProperties();
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
                PopulateGrid(model);
        }
    }
}
