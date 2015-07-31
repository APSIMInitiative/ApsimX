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
    using System.Linq;
    using System.Reflection;
    using EventArguments;
    using Interfaces;
    using Models;
    using Models.Core;
    using APSIM.Shared.Utilities;

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
            string[] split;
            this.grid = view as IGridView;
            this.model = model as Model;
            this.explorerPresenter = explorerPresenter;

            this.FindAllProperties();
            this.PopulateGrid(this.model);
            this.grid.CellsChanged += this.OnCellValueChanged;
            this.grid.ButtonClick += OnFileBrowseClick;
            this.grid.ResizeControls();
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
            if (model != null)
            {
                split = this.model.GetType().ToString().Split('.');
                this.grid.ModelName = split[split.Length - 1];
                this.grid.LoadImage();
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.grid.CellsChanged -= this.OnCellValueChanged;
            this.grid.ButtonClick -= OnFileBrowseClick;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGrid(Model model)
        {
            IGridCell selectedCell = this.grid.GetCurrentCell;
            this.model = model;
            DataTable table = new DataTable();
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Value", typeof(object));

            this.FillTable(table);
            this.grid.DataSource = table;
            this.FormatGrid();
            if (selectedCell != null)
                this.grid.GetCurrentCell = selectedCell;
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
                        Attribute descriptionAttribute = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), true);
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
            string[] fieldNames = null;
            for (int i = 0; i < this.properties.Count; i++)
            {
                IGridCell cell = this.grid.GetCell(1, i);
                        
                if (this.properties[i].DisplayType == DisplayAttribute.DisplayTypeEnum.TableName)
                {
                    DataStore dataStore = new DataStore(this.model);
                    cell.EditorType = EditorTypeEnum.DropDown;
                    cell.DropDownStrings = dataStore.TableNames;
                    if (cell.Value != null && cell.Value.ToString() != string.Empty)
                    {
                        DataTable data = dataStore.RunQuery("SELECT * FROM " + cell.Value.ToString() + " LIMIT 1");
                        if (data != null)
                            fieldNames = DataTableUtilities.GetColumnNames(data);
                    }
                    dataStore.Disconnect();
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
                else if (this.properties[i].DisplayType == DisplayAttribute.DisplayTypeEnum.FileName)
                {
                    cell.EditorType = EditorTypeEnum.Button;
                }
                else if (this.properties[i].DisplayType == DisplayAttribute.DisplayTypeEnum.FieldName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    if (fieldNames != null)
                        cell.DropDownStrings = fieldNames;
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
                        cell.DropDownStrings = StringUtilities.EnumToStrings(cellValue);
                    }
                    else if (cellValue.GetType() == typeof(ICrop))
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        List<string> cropNames = new List<string>();
                        foreach (Model crop in Apsim.FindAll(this.model, typeof(ICrop)))
                        {
                            cropNames.Add(crop.Name);
                        }
                        cell.DropDownStrings = cropNames.ToArray();
                    }
                    else if (this.properties[i].DataType == typeof(ICrop))
                    {
                        List<string> plantNames = Apsim.FindAll(this.model, typeof(ICrop)).Select(m => m.Name).ToList();
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = plantNames.ToArray();
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
                    ICrop plant = property.Value as ICrop;
                    if (plant != null)
                        return plant;
                }
            }

            // Not found so look for one in scope.
            return Apsim.Find(this.model, typeof(ICrop)) as ICrop;
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
            if (property.DataType.IsArray && value != null)
            {
                string[] stringValues = value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (property.DataType == typeof(double[]))
                {
                    value = MathUtilities.StringsToDoubles(stringValues);
                }
                else if (property.DataType == typeof(int[]))
                {
                    value = MathUtilities.StringsToDoubles(stringValues);
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
            else if (typeof(ICrop).IsAssignableFrom(property.DataType))
            {
                value = Apsim.Find(this.model, value.ToString()) as ICrop;
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

        /// <summary>
        /// Called when user clicks on a file name.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnFileBrowseClick(object sender, GridCellsChangedArgs e)
        {
            string fileName = explorerPresenter.AskUserForFile("Select file");
            if (fileName != null)
            {
                e.ChangedCells[0].Value = fileName;
                OnCellValueChanged(sender, e);
                PopulateGrid(model);
            }
        }
    }
}
