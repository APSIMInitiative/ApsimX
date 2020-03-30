using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
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
                try
                {
                    RemoveEmptyRows();
                }
                catch (Exception err)
                {
                    presenter.MainPresenter.ShowError(err);
                }

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
            // After refreshing the grid, we want the selected cell to
            // still be selected.
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
        
        /// <summary>
        /// Finds all array properties with a description attribute of
        /// a given model and all child models of type SoilCrop.
        /// </summary>
        /// <param name="model">Model to examine.</param>
        private List<VariableProperty> FindAllProperties(IModel model)
        {
            List<VariableProperty> properties = new List<VariableProperty>();

            // When user clicks on a SoilCrop, there is no thickness column. In this
            // situation get thickness column from parent model.
            if (this.model is SoilCrop)
            {
                Physical water = model.Parent as Physical;
                if (water == null)
                {
                    // Parent model is not a Physical model. This can happen if the soil
                    // crop is a factor or under replacements. If under replacements, all
                    // bets are off. Otherwise, we find an ancestor which is a simulation
                    // generator (experiment, simulation, morris, etc.) and search for
                    // a physical node somewhere under the simulation generator.
                    IModel parent = Apsim.Parent(model, typeof(ISimulationDescriptionGenerator));
                    if (parent != null)
                        water = Apsim.ChildrenRecursively(parent, typeof(Physical)).FirstOrDefault() as Physical;
                }
                if (water != null)
                {
                    PropertyInfo depth = water.GetType().GetProperty("Depth");
                    properties.Add(new VariableProperty(water, depth));
                }
            }

            // Get all properties of the model which have a description attribute.
            foreach (PropertyInfo property in model.GetType().GetProperties())
            {
                Attribute description = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), false);
                if (property.PropertyType.IsArray && description != null)
                    properties.Add(new VariableProperty(model, property));
            }

            // Get properties of all child models of type SoilCrop.
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
            if (property.Object is SoilCrop crop)
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
                // fixme - ugly hack to work around last column being very wide
                if (i != properties.Count - 1)
                    grid.GetColumn(i).LeftJustification = false;

                grid.GetColumn(i).HeaderLeftJustification = false;
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
            Array arr = property.Value as Array;
            if (arr == null || arr.Length <= row)
                return null;

            object value = arr.GetValue(row);
            if (value == null)
                return null;

            Type dataType = property.DataType.GetElementType();
            // Fixme!
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

            try
            {
                // Parse the new string to an object of the appropriate type.
                object value = ReflectionUtilities.StringToObject(property.DataType.GetElementType(), cell.NewValue, CultureInfo.CurrentCulture);

                // Clone the array stored in the model. This is necessary
                // because we need to modify an element of property.Value,
                // which is a shallow copy of the actual property value.
                Array array = Clone(property.Value as Array, property.DataType.GetElementType());

                // Resize the array if necessary.
                if (cell.RowIndex >= array.Length)
                    Resize(ref array, cell.RowIndex + 1);

                // Change the appropriate element in the array.
                array.SetValue(value, cell.RowIndex);

                if (!MathUtilities.ValuesInArray(array))
                    return null;

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
        private void SetPropertyValue(ChangeProperty changedProperty)
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            presenter.CommandHistory.Add(changedProperty);
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Checks the lengths of all array properties. Resizes any
        /// array which is too short and fills new elements with NaN.
        /// This is needed when the user enters a new row of data.
        /// </summary>
        /// <param name="cell"></param>
        private List<ChangeProperty.Property> CheckArrayLengths(GridCellChangedArgs cell)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            foreach (VariableProperty property in properties)
            {
                // If the property is not an array or if it's readonly,
                // ignore it.
                if (!property.DataType.IsArray || property.IsReadOnly)
                    continue;

                // If the property is the property which has just been
                // changed by the user, ignore it - we don't want to
                // modify it twice.
                if (property == properties[cell.ColIndex])
                    continue;

                // If the property value is null, ignore it.
                Array arr = property.Value as Array;
                if (arr == null)
                    continue;

                // If array is already long enough, ignore it.
                int n = arr?.Length ?? 0;
                if (n > cell.RowIndex)
                    continue;

                // Array is too short - need to resize it. However,
                // this array is a reference to the value of the
                // property stored in the model, so we need to clone it
                // before making any changes, otherwise the changes
                // won't be undoable.
                Type elementType = property.DataType.GetElementType();
                arr = Clone(arr, elementType);
                Resize(ref arr, cell.RowIndex + 1);

                // Unless this property is thickness, fill the new
                // values (if any) with NaN as per conversation with
                // Dean (blame him!).
                if ((elementType == typeof(double) || elementType == typeof(float))
                    && property.Name != "Thickness")
                {
                    object nan = null;
                    if (elementType == typeof(double))
                        nan = double.NaN;
                    else if (elementType == typeof(float))
                        nan = float.NaN;

                    for (int i = n; i < arr.Length; i++)
                        arr.SetValue(nan, i);
                }

                changes.Add(new ChangeProperty.Property(property.Object, property.Name, arr));
            }
            return changes;
        }

        /// <summary>
        /// Clones an array. Never returns null.
        /// </summary>
        /// <param name="array"></param>
        private Array Clone(Array array, Type elementType)
        {
            if (array == null)
                return Array.CreateInstance(elementType, 0);

            return ReflectionUtilities.Clone(array) as Array;
        }

        /// <summary>
        /// Resizes a generic array.
        /// </summary>
        /// <param name="array">Array to be resized.</param>
        /// <param name="newSize">New size.</param>
        private void Resize(ref Array array, int newSize)
        {
            Type elementType = array.GetType().GetElementType();
            Array newArray = Array.CreateInstance(elementType, newSize);
            Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
            array = newArray;
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

            // If all cells are being set to null, and the number of changed
            // cells is a multiple of the number of properties, we could be
            // deleting an entire row (or rows). In this case, we need
            // different logic, to resize all of the arrays.
            bool deletedRow = false;
            if (args.ChangedCells.All(c => c.NewValue == null) &&
                args.ChangedCells.Count % properties.Where(p => !p.IsReadOnly).Count() == 0)
            {
                // Get list of distinct rows which have been changed.
                int[] changedRows = args.ChangedCells.Select(c => c.RowIndex).Distinct().ToArray();
                List<int> rowsToDelete = new List<int>();
                foreach (int row in changedRows)
                {
                    // Get columns which have been changed in this row.
                    var changesInRow = args.ChangedCells.Where(c => c.RowIndex == row);
                    int[] columns = changesInRow.Select(c => c.ColIndex).ToArray();

                    // If all non-readonly properties have been set to null in this row,
                    // delete the row.
                    bool deleteRow = true;
                    for (int i = 0; i < properties.Count; i++)
                        if (!properties[i].IsReadOnly && !columns.Contains(i))
                            deleteRow = false;

                    if (deleteRow)
                    {
                        // We can't delete the row now - what if the user has deleted
                        // multiple rows at once (this is possible via shift-clicking).
                        // We need one change property command per property. Otherwise,
                        // only the last command will have an effect.
                        deletedRow = true;
                        rowsToDelete.Add(row);
                    }
                }

                if (rowsToDelete.Count > 0)
                {
                    // This assumes that only consecutive rows can be deleted together.
                    // ie the user can shift click multiple rows and hit delete to delete
                    // more than 1 row. They cannot ctrl click to select non-adjacent rows.
                    int from = rowsToDelete.Min();
                    int to = rowsToDelete.Max();
                    changes.AddRange(DeleteRows(from, to));

                    // Remove cells in deleted rows from list of changed cells,
                    // as we've already dealt with them.
                    args.ChangedCells = args.ChangedCells.Where(c => !rowsToDelete.Contains(c.RowIndex)).ToList();
                }
            }

            foreach (var column in args.ChangedCells.GroupBy(c => c.ColIndex))
            {
                VariableProperty property = properties[column.Key];
                if (property == null || property.IsReadOnly)
                    continue;

                // Get a deep copy of the property value.
                Array newArray = property.Value as Array;
                if (newArray == null && property.Value != null)
                    continue;
                newArray = Clone(newArray, property.DataType.GetElementType());

                // It's possible to change multiple values in the same column
                // simultaneously via multi-selection. If we just add a change
                // property command for each individual change, later changes
                // would overwrite the earlier changes. We need to merge all
                // changes to a single column into a single command then move
                // onto the next column.
                foreach (GridCellChangedArgs change in column)
                {
                    if (change.NewValue == change.OldValue)
                        continue; // silently fail

                    if (change.RowIndex >= newArray.Length && string.IsNullOrEmpty(change.NewValue))
                        continue;

                    // If the user has entered data into a new row, we will need to
                    // resize all of the array properties.
                    changes.AddRange(CheckArrayLengths(change));

                    // Need to convert user input to a string using the current
                    // culture.
                    object element = ReflectionUtilities.StringToObject(property.DataType.GetElementType(), change.NewValue, CultureInfo.CurrentCulture);
                    if (newArray.Length <= change.RowIndex)
                        Resize(ref newArray, change.RowIndex + 1);

                    newArray.SetValue(element, change.RowIndex);
                }
                changes.Add(new ChangeProperty.Property(property.Object, property.Name, newArray));
            }

            // Apply all changes to the model in a single undoable command.
            SetPropertyValue(new ChangeProperty(changes));

            // Update the value shown in the grid. This needs to happen after
            // we have applied changes to the model for obvious reasons.
            foreach (GridCellChangedArgs cell in args.ChangedCells)
            {
                // Add new rows to the view's grid if necessary.
                while (grid.RowCount <= cell.RowIndex + 1)
                    grid.RowCount++;
                grid.DataSource.Rows[cell.RowIndex][cell.ColIndex] = GetCellValue(cell.RowIndex, cell.ColIndex);
            }

            // If the user deleted an entire row, do a full refresh of the
            // grid. Otherwise, only refresh read-only columns (PAWC).
            if (deletedRow)
                PopulateGrid(model);
            else
                UpdateReadOnlyProperties();
        }

        /// <summary>
        /// Remove all empty rows - that is, resize all property arrays
        /// to remove elements where each array has NaN at a given
        /// index.
        /// 
        /// E.g. if each array has NaN at the 3rd and 4th indices,
        /// the arrays will all be resized to remove these elements.
        /// </summary>
        /// <returns></returns>
        private bool RemoveEmptyRows()
        {
            int numRows = properties.Max(p => (p.Value as Array)?.Length ?? 0);

            // First, check which rows need to be deleted.
            List<int> rowsToDelete = new List<int>();
            for (int i = 0; i < numRows; i++)
                if (IsEmptyRow(i))
                    rowsToDelete.Add(i);

            if (rowsToDelete == null || rowsToDelete.Count < 1)
                return false;

            // Ideally, the entire change would be undoable in a single
            // click. Unfortunately, this is not possible if the rows
            // to be deleted are not all contiguous - e.g. if we want
            // to delete rows 3, 4, 6, and 7. We can't just delete rows
            // 3-7 because we want to keep row 5. We also can't delete
            // rows 3-4, then 6-7 because the array indices will all
            // change after we delete rows 3 and 4.
            int from = rowsToDelete[0];
            int numDeleted = 0;
            for (int i = 0; i < rowsToDelete.Count; i++)
            {
                // Delete this batch of rows iff we've reached the last
                // row to be deleted or there's a gap before the next
                // row (ie next row to be deleted is not the row
                // immediately below this one).
                if (i == rowsToDelete.Count - 1 || rowsToDelete[i] + 1 != rowsToDelete[i + 1])
                {
                    // Need to subtract number of rows already deleted
                    // from the to/from indices. E.g. if we've deleted
                    // rows 3-4, the rows which were originally at
                    // indices 6-7 will now be at indices 4-5.
                    int to = rowsToDelete[i];
                    SetPropertyValue(new ChangeProperty(DeleteRows(from - numDeleted, to - numDeleted)));
                    numDeleted += to - from;
                    if (i < rowsToDelete.Count - 1)
                        from = rowsToDelete[i + 1];
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true iff a given row is empty.
        /// </summary>
        /// <param name="row">The row to check.</param>
        private bool IsEmptyRow(int row)
        {
            // Iterate over columns (each property is shown in its own column).
            for (int j = 0; j < properties.Count; j++)
                if (!string.IsNullOrEmpty(GetCellValue(row, j)?.ToString()))
                    return false;

            return true;
        }

        /// <summary>
        /// Deletes a row of data from all properties.
        /// Note that this method does not actually perform the
        /// deletion, but instead returns a list of change property
        /// objects which may be passed into the ChangeProperty
        /// constructor and executed to be undoable in a single click.
        /// </summary>
        /// <param name="from">Index of the first row to be deleted.</param>
        /// <param name="to">Index of the last row to be deleted.</param>
        private List<ChangeProperty.Property> DeleteRows(int from, int to)
        {
            if (from > to)
            {
                int tmp = to;
                to = from;
                from = tmp;
            }

            // if deleting from 4 to 8, num rows to delete = 5.
            int numRowsToDelete = to - from + 1;

            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            foreach (VariableProperty property in properties)
            {
                Array array = Clone(property.Value as Array, property.DataType.GetElementType());
                if (array == null || array.Length < from + 1 || property.IsReadOnly)
                    continue;

                // Move each element after the start row to be deleted back one index.
                // e.g. array[0] = array[1], etc. Then resize the array and remove
                // the last element.
                for (int i = from; i < array.Length - numRowsToDelete; i++)
                    array.SetValue(array.GetValue(i + numRowsToDelete), i);

                Resize(ref array, array.Length - numRowsToDelete);
                changes.Add(new ChangeProperty.Property(property.Object, property.Name, array));
            }
            return changes;
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
