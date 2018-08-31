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
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Interfaces;
    using Models;
    using Models.Core;
    using Models.Surface;
    using Utility;
    using Views;

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
    public class PropertyPresenter : GridPresenter
    {
        /// <summary>
        /// Linked storage reader
        /// </summary>
        [Link]
        private IStorageReader storage = null;

        /// <summary>
        /// The model we're going to examine for properties.
        /// </summary>
        private Model model;

        /// <summary>
        /// A list of all properties found in the Model.
        /// </summary>
        private List<IVariable> properties = new List<IVariable>();

        /// <summary>
        /// The completion form.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to</param>
        /// <param name="view">The view to connect to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public override void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            base.Attach(model, view, explorerPresenter);
            grid.ContextItemsNeeded += GetContextItems;
            grid.CanGrow = false;
            this.model = model as Model;
            intellisense = new IntellisensePresenter(grid as ViewBase);

            // The grid does not have control-space intellisense (for now).
            intellisense.ItemSelected += (sender, e) => grid.InsertText(e.ItemSelected);
            // if the model is Testable, run the test method.
            ITestable testModel = model as ITestable;
            if (testModel != null)
            {
                testModel.Test(false, true);
                grid.ReadOnly = true;
            }

            grid.NumericFormat = "G6"; 
            FindAllProperties(this.model);
            if (grid.DataSource == null)
            {
                PopulateGrid(this.model);
            }
            else
            {
                FormatTestGrid();
            }

            grid.CellsChanged += OnCellValueChanged;
            grid.ButtonClick += OnFileBrowseClick;
            this.presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Gets a value indicating whether the grid is empty (i.e. no rows).
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return grid.RowCount == 0;
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public override void Detach()
        {
            base.Detach();
            grid.EndEdit();
            grid.CellsChanged -= OnCellValueChanged;
            grid.ButtonClick -= OnFileBrowseClick;
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            intellisense.Cleanup();
        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGrid(Model model)
        {
            IGridCell selectedCell = grid.GetCurrentCell;
            this.model = model;
            DataTable table = new DataTable();
            bool hasData = properties.Count > 0;
            table.Columns.Add(hasData ? "Description" : "No values are currently available", typeof(string));
            table.Columns.Add(hasData ? "Value" : " ", typeof(object));

            FillTable(table);
            FormatGrid();
            if (selectedCell != null)
            {
                grid.GetCurrentCell = selectedCell;
            }
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
                int i = properties.FindIndex(p => p.Description == property.Description);

                // If found then remove the property.
                if (i != -1)
                {
                    properties.RemoveAt(i);
                }
            }

            PopulateGrid(model);
        }
        
        /// <summary>
        /// Find all properties from the model and fill this.properties.
        /// </summary>
        /// <param name="model">The mode object</param>
        public void FindAllProperties(Model model)
        {
            this.model = model;
            properties.Clear();
            if (this.model != null)
            {
                var members = model.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public).ToList();
                members.RemoveAll(m => !Attribute.IsDefined(m, typeof(DescriptionAttribute)));
                var orderedMembers = members.OrderBy(m => ((DescriptionAttribute)m.GetCustomAttribute(typeof(DescriptionAttribute), true)).LineNumber);

                foreach (MemberInfo member in orderedMembers)
                {
                    IVariable property = null;
                    if (member is PropertyInfo)
                        property = new VariableProperty(this.model, member as PropertyInfo);
                    else if (member is FieldInfo)
                        property = new VariableField(this.model, member as FieldInfo);

                    if (property != null && property.Description != null && property.Writable)
                    {
                        // Only allow lists that are double[], int[] or string[]
                        bool includeProperty = true;
                        if (property.DataType.GetInterface("IList") != null)
                        {
                            includeProperty = property.DataType == typeof(double[]) ||
                                              property.DataType == typeof(int[]) ||
                                              property.DataType == typeof(string[]);
                        }

                        if (Attribute.IsDefined(member, typeof(SeparatorAttribute)))
                        {
                            SeparatorAttribute separator = Attribute.GetCustomAttribute(member, typeof(SeparatorAttribute)) as SeparatorAttribute;
                            properties.Add(new VariableObject(separator.ToString()));  // use a VariableObject for separators
                        }
                        if (includeProperty)
                            properties.Add(property);

                        if (property.DataType == typeof(DataTable))
                            grid.DataSource = property.Value as DataTable;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the lists of Cultivar and Field names in the model.
        /// This is used when the model has been changed. For example, when a 
        /// new crop has been selecled.
        /// </summary>
        /// <param name="model">The new model</param>
        public void UpdateModel(Model model)
        {
            this.model = model;
            if (this.model != null)
            {
                IGridCell curCell = grid.GetCurrentCell;
                for (int i = 0; i < properties.Count; i++)
                {
                    IGridCell cell = grid.GetCell(1, i);
                    if (curCell != null && cell.RowIndex == curCell.RowIndex && cell.ColumnIndex == curCell.ColumnIndex)
                    {
                        continue;
                    }

                    if (properties[i].Display != null &&
                        properties[i].Display.Type == DisplayType.CultivarName)
                    {
                        IPlant crop = GetCrop(properties);
                        if (crop != null)
                        {
                            cell.DropDownStrings = GetCultivarNames(crop);
                        }
                    }
                    else if (properties[i].Display != null &&
                             properties[i].Display.Type == DisplayType.FieldName)
                    {
                        string[] fieldNames = GetFieldNames();
                        if (fieldNames != null)
                        {
                            cell.DropDownStrings = fieldNames;
                        }
                    }
                }
            }
        }
        
        private void GetContextItems(object o, NeedContextItemsArgs e)
        {
            try
            {
                if (intellisense.GenerateGridCompletions(e.Code, e.Offset, model, true, false, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.Item1, e.Coordinates.Item2);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            
        }

        /// <summary>
        /// Fill the specified table with columns and rows based on this.Properties
        /// </summary>
        /// <param name="table">The table that needs to be filled</param>
        private void FillTable(DataTable table)
        {
            foreach (IVariable property in properties)
            {
                if (property is VariableObject)
                    table.Rows.Add(new object[] { property.Value , null });
                else if (property.Value is IModel)
                    table.Rows.Add(new object[] { property.Description, Apsim.FullPath(property.Value as IModel)});
                else
                    table.Rows.Add(new object[] { property.Description, property.ValueWithArrayHandling });
            }

            grid.DataSource = table;
        }

        /// <summary>
        /// Format the grid when displaying Tests.
        /// </summary>
        private void FormatTestGrid()
        {
            int numCols = grid.DataSource.Columns.Count;

            for (int i = 0; i < numCols; i++)
            {
                grid.GetColumn(i).Format = "F4";
            }
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        private void FormatGrid()
        {
            for (int i = 0; i < properties.Count; i++)
            {
                IGridCell cell = grid.GetCell(1, i);
                    
                if (properties[i] is VariableObject)
                {
                    cell.EditorType = EditorTypeEnum.TextBox;

                    grid.SetRowAsSeparator(i, true);
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.TableName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    cell.DropDownStrings = storage.TableNames.ToArray();
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.CultivarName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    IPlant crop = GetCrop(properties);
                    if (crop != null)
                    {
                        cell.DropDownStrings = GetCultivarNames(crop);
                    }
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.FileName)
                {
                    cell.EditorType = EditorTypeEnum.Button;
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.FieldName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = GetFieldNames();
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.ResidueName &&
                         model is SurfaceOrganicMatter)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = GetResidueNames();
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.Model)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;

                    string[] modelNames = GetModelNames(properties[i].Display.ModelType);
                    if (modelNames != null)
                        cell.DropDownStrings = modelNames;
                }
                else
                {
                    object cellValue = properties[i].ValueWithArrayHandling;
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
                        cell.DropDownStrings = VariableProperty.EnumToStrings(cellValue);
                        Enum cellValueAsEnum = cellValue as Enum;
                        if (cellValueAsEnum != null)
                            cell.Value = VariableProperty.GetEnumDescription(cellValueAsEnum);
                    }
                    else if (cellValue.GetType() == typeof(IPlant))
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        List<string> cropNames = new List<string>();
                        foreach (Model crop in Apsim.FindAll(model, typeof(IPlant)))
                        {
                            cropNames.Add(crop.Name);
                        }

                        cell.DropDownStrings = cropNames.ToArray();
                    }
                    else if (properties[i].DataType == typeof(IPlant))
                    {
                        List<string> plantNames = Apsim.FindAll(model, typeof(IPlant)).Select(m => m.Name).ToList();
                        cell.EditorType = EditorTypeEnum.DropDown;
                        cell.DropDownStrings = plantNames.ToArray();
                    }
                    else
                    {
                        cell.EditorType = EditorTypeEnum.TextBox;
                    }
                }
            }

            IGridColumn descriptionColumn = grid.GetColumn(0);
            descriptionColumn.Width = -1;
            descriptionColumn.ReadOnly = true;

            IGridColumn valueColumn = grid.GetColumn(1);
            valueColumn.Width = -1;
        }

        public void SetCellReadOnly(IGridCell cell)
        {

        }

        /// <summary>Get a list of cultivars for crop.</summary>
        /// <param name="crop">The crop.</param>
        /// <returns>A list of cultivars.</returns>
        private string[] GetCultivarNames(IPlant crop)
        {
            if (crop.CultivarNames.Length == 0)
            {
                Simulations simulations = Apsim.Parent(crop as IModel, typeof(Simulations)) as Simulations;
                Replacements replacements = Apsim.Child(simulations, typeof(Replacements)) as Replacements;
                if (replacements != null)
                {
                    IPlant replacementCrop = Apsim.Child(replacements, (crop as IModel).Name) as IPlant;
                    if (replacementCrop != null)
                    {
                        return replacementCrop.CultivarNames;
                    }
                }
            }
            else
            {
                return crop.CultivarNames;
            }

            return new string[0];
        }

        /// <summary>Get a list of database fieldnames. 
        /// Returns the names associated with the first table name in the property list
        /// </summary>
        /// <returns>A list of fieldnames.</returns>
        private string[] GetFieldNames()
        {
            string[] fieldNames = null;
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].Display.Type == DisplayType.TableName)
                {
                    IGridCell cell = grid.GetCell(1, i);
                    if (cell.Value != null && cell.Value.ToString() != string.Empty)
                    {
                        string tableName = cell.Value.ToString();
                        DataTable data = null;
                        if (storage.TableNames.Contains(tableName))
                            data = storage.RunQuery("SELECT * FROM " + tableName + " LIMIT 1");
                        if (data != null)
                            fieldNames = DataTableUtilities.GetColumnNames(data);
                    }
                }
            }

            return fieldNames;
        }

        /// <summary>
        /// Go find a crop property in the specified list of properties or if not
        /// found, find the first crop in scope.
        /// </summary>
        /// <param name="properties">The list of properties to look through.</param>
        /// <returns>The found crop or null if none found.</returns>
        private IPlant GetCrop(List<IVariable> properties)
        {
            foreach (IVariable property in properties)
            {
                if (property.DataType == typeof(IPlant))
                {
                    IPlant plant = property.Value as IPlant;
                    if (plant != null)
                    {
                        return plant;
                    }
                }
            }

            // Not found so look for one in scope.
            return Apsim.Find(model, typeof(IPlant)) as IPlant;
        }

        private string[] GetResidueNames()
        {
            if (model is SurfaceOrganicMatter)
            {
                List<string> names = new List<string>();
                names = (this.model as SurfaceOrganicMatter).ResidueTypeNames();
                names.Sort();
                return names.ToArray();
            }
            return null;
        }

        private string[] GetModelNames(Type t)
        {
            List<IModel> models;
            if (t == null)
                models = Apsim.FindAll(model);
            else
                models = Apsim.FindAll(model, t);

            List<string> modelNames = new List<string>();
            foreach (IModel model in models)
                modelNames.Add(Apsim.FullPath(model));
            return modelNames.ToArray();
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event parameters</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;

            foreach (IGridCell cell in e.ChangedCells)
            {
                try
                {
                    if (e.InvalidValue)
                        throw new Exception("The value you entered was not valid for its datatype.");
                    if (cell.RowIndex < properties.Count)
                        SetPropertyValue(properties[cell.RowIndex], cell.Value);
                }
                catch (Exception ex)
                {
                    presenter.MainPresenter.ShowError(ex);
                }
            }
            
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Set the value of the specified property
        /// </summary>
        /// <param name="property">The property to set the value of</param>
        /// <param name="value">The value to set the property to</param>
        private void SetPropertyValue(IVariable property, object value)
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
                    throw new ApsimXException(model, "Invalid property type: " + property.DataType.ToString());
                }
            }
            else if (typeof(IPlant).IsAssignableFrom(property.DataType))
            {
                value = Apsim.Find(model, value.ToString()) as IPlant;
            }
            else if (property.DataType == typeof(DateTime))
            {
                value = Convert.ToDateTime(value);
            }
            else if (property.DataType.IsEnum)
            {
                value = VariableProperty.ParseEnum(property.DataType, value.ToString());
            }
            else if (property.Display != null &&
                     property.Display.Type == DisplayType.Model)
            {
                value = Apsim.Get(model, value.ToString());
            }

            Commands.ChangeProperty cmd = new Commands.ChangeProperty(model, property.Name, value);
            presenter.CommandHistory.Add(cmd, true);
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
            {
                PopulateGrid(model);
            }
        }

        /// <summary>
        /// Called when user clicks on a file name.
        /// </summary>
        /// <remarks>
        /// Does creation of the dialog belong here, or in the view?
        /// </remarks>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnFileBrowseClick(object sender, GridCellsChangedArgs e)
        {
            IFileDialog fileChooser = new FileDialog()
            {
                Action = FileDialog.FileActionType.Open,
                Prompt = "Select file path",
                InitialDirectory = e.ChangedCells[0].Value.ToString()
            };
            string fileName = fileChooser.GetFile();
            if (fileName != null && fileName != e.ChangedCells[0].Value.ToString())
            {
                e.ChangedCells[0].Value = fileName;
                OnCellValueChanged(sender, e);
                PopulateGrid(model);
            }
        }
    }
}
