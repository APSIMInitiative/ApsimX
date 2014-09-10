//-----------------------------------------------------------------------
// <copyright file="PropertyPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using Models.Core;
    using Views;
    using Models;
    using Interfaces;
    using EventArguments;
    using Classes;

    /// <summary>
    /// <para>
    /// This presenter displays properties of a Model in an IGridView.
    /// The properties must be public, read/write and have a [Description]
    /// attribute. Array properties are supported if they are integer, double
    /// or string arrays.
    /// </para>
    /// <para>
    /// There is also a method (RemoveProperties) for excluding properties from 
    /// the PropertyGrid. This is important when a PropertyGrid is embedded on
    /// a ProfileGrid and the ProfileGrid is displaying some properties as well.
    /// We don't want properties to be on both the ProfileGrid and the PropertyGrid.
    /// </para>
    /// </summary>
    public class PropertyPresenter : IPresenter
    {
        /// <summary>
        /// The underlying grid control to work with.
        /// </summary>
        private IGridView grid;

        /// <summary>
        /// The model we're going to examine for properties.
        /// </summary>
        private Model model;

        /// <summary>
        /// The parent ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// A list of all properties found in the Model.
        /// </summary>
        private List<VariableProperty> properties = new List<VariableProperty>();

        /// <summary>
        /// Gets a value indicating whether the grid is empty (i.e. no rows).
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return this.grid.RowCount == 0;
            }
        }

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to</param>
        /// <param name="view">The view to connect to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.grid = view as IGridView;
            this.model = model as Model;
            this.explorerPresenter = explorerPresenter;

            this.FindAllProperties();
            this.PopulateGrid(this.model);
            this.grid.CellsChanged += this.OnCellValueChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.grid.CellsChanged -= this.OnCellValueChanged;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGrid(Model model)
        {
            this.model = model;
            DataTable table = new DataTable();
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Value", typeof(object));

            this.FillTable(table);
            this.grid.DataSource = table;
            this.FormatGrid();
        }
        
        /// <summary>
        /// Remove the specified properties from the grid.
        /// </summary>
        /// <param name="propertysToRemove">The names of all properties to remove</param>
        public void RemoveProperties(IEnumerable<VariableProperty> propertysToRemove)
        {
            foreach (VariableProperty property in propertysToRemove)
            {
                // Try and find the description in our list of properties.
                int i = this.properties.FindIndex(p => p.Description == property.Description);

                // If found then remove the property.
                if (i != -1)
                {
                    this.properties.RemoveAt(i);
                }
            }

            this.PopulateGrid(this.model);
        }
        
        /// <summary>
        /// Find all properties from the model and fill this.properties.
        /// </summary>
        private void FindAllProperties()
        {
            this.properties.Clear();
            if (this.model != null)
            {
                foreach (PropertyInfo property in this.model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    // Properties must have a [Description], not be called Name, and be read/write.
                    bool hasDescription = property.IsDefined(typeof(DescriptionAttribute), false);
                    bool includeProperty = hasDescription &&
                                           property.Name != "Name" &&
                                           property.CanRead &&
                                           property.CanWrite;

                    // Only allow lists that are double[], int[] or string[]
                    if (includeProperty && property.PropertyType.GetInterface("IList") != null)
                    {
                        includeProperty = property.PropertyType == typeof(double[]) ||
                                          property.PropertyType == typeof(int[]) ||
                                          property.PropertyType == typeof(string[]);
                    }

                    if (includeProperty)
                    {
                        Attribute descriptionAttribute = Utility.Reflection.GetAttribute(property, typeof(DescriptionAttribute), true);
                        this.properties.Add(new VariableProperty(this.model, property));
                    }
                }
            }
        }

        /// <summary>
        /// Fill the specified table with columns and rows based on this.Properties
        /// </summary>
        /// <param name="table">The table that needs to be filled</param>
        private void FillTable(DataTable table)
        {
            foreach (VariableProperty property in this.properties)
            {
                table.Rows.Add(new object[] { property.Description, property.ValueWithArrayHandling });
            }
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        private void FormatGrid()
        {
            for (int i = 0; i < this.properties.Count; i++)
            {
                IGridCell cell = this.grid.GetCell(1, i);
                        
                if (this.properties[i].DisplayType == DisplayAttribute.DisplayTypeEnum.TableName)
                {
                    DataStore dataStore = this.model.Find(typeof(DataStore)) as DataStore;
                    if (dataStore != null)
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = dataStore.TableNames;
                    }
                }
                else if (this.properties[i].DisplayType == DisplayAttribute.DisplayTypeEnum.CultivarName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    ICrop crop = GetCrop(properties);
                    if (crop != null)
                    {
                        cell.DropDownStrings = crop.CultivarNames;
                    }
                    
                }
                else
                {
                    object cellValue = this.properties[i].ValueWithArrayHandling;
                    if (cellValue is DateTime)
                    {
                        cell.EditorType = EditorTypeEnum.DateTime;
                    }
                    else if (cellValue is bool)
                    {
                        cell.EditorType = EditorTypeEnum.Boolean;
                    }
                    else if (cellValue.GetType().IsEnum)
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = Utility.String.EnumToStrings(cellValue);
                    }
                    else if (cellValue.GetType() == typeof(ICrop))
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        List<string> cropNames = new List<string>();
                        foreach (Model crop in this.model.FindAll(typeof(ICrop)))
                        {
                            cropNames.Add(crop.Name);
                        }
                        cell.DropDownStrings = cropNames.ToArray();
                    }
                    else
                    {
                        cell.EditorType = EditorTypeEnum.TextBox;
                    }
                }
            }

            IGridColumn descriptionColumn = this.grid.GetColumn(0);
            descriptionColumn.Width = -1;
            descriptionColumn.ReadOnly = true;

            IGridColumn valueColumn = this.grid.GetColumn(1);
            valueColumn.Width = -1;
        }

        /// <summary>
        /// Go find a crop property in the specified list of properties or if not
        /// found, find the first crop in scope.
        /// </summary>
        /// <param name="properties">The list of properties to look through.</param>
        /// <returns>The found crop or null if none found.</returns>
        private ICrop GetCrop(List<VariableProperty> properties)
        {
            foreach (VariableProperty property in properties)
            {
                if (property.DataType == typeof(ICrop))
                {
                    return this.model.Get(property.Value.ToString()) as ICrop;
                }
            }

            // Not found so look for one in scope.
            return this.model.Find(typeof(ICrop)) as ICrop;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="col">The column index of the cell that has changed</param>
        /// <param name="row">The row index of the cell that has changed</param>
        /// <param name="oldValue">The cell value before the user changed it</param>
        /// <param name="newValue">The cell value the user has entered</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            foreach (IGridCell cell in e.ChangedCells)
            {
                this.SetPropertyValue(this.properties[cell.RowIndex], cell.Value);
            }
            
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }



        /// <summary>
        /// Set the value of the specified property
        /// </summary>
        /// <param name="property">The property to set the value of</param>
        /// <param name="value">The value to set the property to</param>
        private void SetPropertyValue(VariableProperty property, object value)
        {
            if (property.DataType.IsArray)
            {
                string[] stringValues = value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (property.DataType == typeof(double[]))
                {
                    value = Utility.Math.StringsToDoubles(stringValues);
                }
                else if (property.DataType == typeof(int[]))
                {
                    value = Utility.Math.StringsToDoubles(stringValues);
                }
                else if (property.DataType == typeof(string[]))
                {
                    value = stringValues;
                }
                else
                {
                    throw new ApsimXException(this.model, "Invalid property type: " + property.DataType.ToString());
                }
            }

            Commands.ChangeProperty cmd = new Commands.ChangeProperty(this.model, property.Name, value);
            this.explorerPresenter.CommandHistory.Add(cmd, true);
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == this.model)
            {
                this.PopulateGrid(this.model);
            }
        }
    }
}
