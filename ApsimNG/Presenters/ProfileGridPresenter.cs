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
using UserInterface.EventArguments;

namespace UserInterface.Presenters
{
    public class ProfileGridPresenter : PropertyPresenter
    {
        /// <summary>
        /// Properties displayed by this presenter.
        /// </summary>
        public IVariable[] Properties
        {
            get
            {
                return properties.ToArray();
            }
        }

        protected override void FindAllProperties(IModel model)
        {
            // When user clicks on a SoilCrop, there is no thickness column. In this
            // situation get thickness column from parent model.
            if (model is SoilCrop && model.Parent is Physical water && properties.Count == 0)
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
                FindAllProperties(crop);
        }

        protected override DataTable CreateGrid()
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
        protected override void FillTable(DataTable table)
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
                    table.Rows[j][i] = GetCellValue(property, j, i);
            }
        }

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

        protected override void FormatGrid()
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

        protected override IVariable GetProperty(int row, int column)
        {
            return properties[column];
        }

        protected override object GetCellValue(IVariable property, int row, int column)
        {
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
            return value;
        }

        /// <summary>
        /// Gets the new value of the cell from a string containing the
        /// cell's new contents.
        /// </summary>
        /// <param name="cell">Cell which has been changed.</param>
        protected override object GetNewPropertyValue(IVariable property, GridCellChangedArgs cell)
        {
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

        protected override void UpdateReadOnlyProperties()
        {
            for (int i = 0; i < properties.Count; i++)
            {
                VariableProperty property = properties[i] as VariableProperty;
                if (property.IsReadOnly && property.DataType.IsArray)
                {
                    Array value = property.Value as Array;
                    for (int j = 0; j < value.Length; j++)
                    {
                        grid.DataSource.Rows[j][i] = value.GetValue(j).ToString();
                    }
                }
            }
        }
    }
}
