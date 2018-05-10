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
    public class PropertyPresenter : IPresenter
    {
        /// <summary>
        /// Linked storage reader
        /// </summary>
        [Link]
        private IStorageReader storage = null;

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
        private List<IVariable> properties = new List<IVariable>();

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
            this.grid.ContextItemsNeeded += GetContextItems;
            this.model = model as Model;
            this.explorerPresenter = explorerPresenter;

            // if the model is Testable, run the test method.
            ITestable testModel = model as ITestable;
            if (testModel != null)
            {
                testModel.Test(false, true);
                this.grid.ReadOnly = true;
            }

            string[] split;

            this.grid.NumericFormat = "G6"; 
            this.FindAllProperties(this.model);
            if (this.grid.DataSource == null)
            {
                this.PopulateGrid(this.model);
            }
            else
            {
                this.grid.ResizeControls();
                this.FormatTestGrid();
            }

            this.grid.CellsChanged += this.OnCellValueChanged;
            this.grid.ButtonClick += this.OnFileBrowseClick;
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
            this.grid.EndEdit();
            this.grid.CellsChanged -= this.OnCellValueChanged;
            this.grid.ButtonClick -= this.OnFileBrowseClick;
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

            this.grid.PropertyMode = true;
            this.FillTable(table);
            this.FormatGrid();
            if (selectedCell != null)
            {
                this.grid.GetCurrentCell = selectedCell;
            }

            this.grid.ResizeControls();
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
        /// <param name="model">The mode object</param>
        public void FindAllProperties(Model model)
        {
            this.model = model;
            this.properties.Clear();
            if (this.model != null)
            {
                var members = from member in this.model.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public)
                                 where Attribute.IsDefined(member, typeof(DescriptionAttribute)) &&
                                       (member is PropertyInfo || member is FieldInfo)
                                 orderby ((DescriptionAttribute)member
                                           .GetCustomAttributes(typeof(DescriptionAttribute), false)
                                           .Single()).LineNumber
                                 select member;

                foreach (MemberInfo member in members)
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
                            properties.Add(new VariableObject(property.Description));  // use a VariableObject for separators

                        if (includeProperty)
                            this.properties.Add(property);

                        if (property.DataType == typeof(DataTable))
                            this.grid.DataSource = property.Value as DataTable;
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
                IGridCell curCell = this.grid.GetCurrentCell;
                for (int i = 0; i < this.properties.Count; i++)
                {
                    IGridCell cell = this.grid.GetCell(1, i);
                    if (curCell != null && cell.RowIndex == curCell.RowIndex && cell.ColumnIndex == curCell.ColumnIndex)
                    {
                        continue;
                    }

                    if (this.properties[i].Display != null &&
                        this.properties[i].Display.Type == DisplayType.CultivarName)
                    {
                        IPlant crop = this.GetCrop(this.properties);
                        if (crop != null)
                        {
                            cell.DropDownStrings = this.GetCultivarNames(crop);
                        }
                    }
                    else if (this.properties[i].Display != null &&
                             this.properties[i].Display.Type == DisplayType.FieldName)
                    {
                        string[] fieldNames = this.GetFieldNames();
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
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(model, e.ObjectName, true, true, false));
        }

        /// <summary>
        /// Fill the specified table with columns and rows based on this.Properties
        /// </summary>
        /// <param name="table">The table that needs to be filled</param>
        private void FillTable(DataTable table)
        {
            foreach (IVariable property in this.properties)
            {
                if (property is VariableObject)
                    table.Rows.Add(new object[] { "###### " + property.Value , "###############" });
                else if (property.Value is IModel)
                    table.Rows.Add(new object[] { property.Description, Apsim.FullPath(property.Value as IModel)});
                else
                    table.Rows.Add(new object[] { property.Description, property.ValueWithArrayHandling });
            }

            this.grid.DataSource = table;
        }

        /// <summary>
        /// Format the grid when displaying Tests.
        /// </summary>
        private void FormatTestGrid()
        {
            int numCols = this.grid.DataSource.Columns.Count;

            for (int i = 0; i < numCols; i++)
            {
                this.grid.GetColumn(i).Format = "F4";
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
                    
                if (this.properties[i] is VariableObject)
                {
                    cell.EditorType = EditorTypeEnum.TextBox;

                    //IGridCell cell1 = grid.GetCell(0, i);
                    //cell1.SetBackgroundColour(System.Drawing.Color.LightGray);
                    //cell1.SetReadOnly();

                    //IGridCell cell2 = grid.GetCell(1, i);
                    //cell2.SetBackgroundColour(System.Drawing.Color.LightGray);
                    //cell2.SetReadOnly();
                }
                else if (this.properties[i].Display != null && 
                         this.properties[i].Display.Type == DisplayType.TableName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    cell.DropDownStrings = this.storage.TableNames.ToArray();
                }
                else if (this.properties[i].Display != null && 
                         this.properties[i].Display.Type == DisplayType.CultivarName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    IPlant crop = this.GetCrop(this.properties);
                    if (crop != null)
                    {
                        cell.DropDownStrings = this.GetCultivarNames(crop);
                    }
                }
                else if (this.properties[i].Display != null && 
                         this.properties[i].Display.Type == DisplayType.FileName)
                {
                    cell.EditorType = EditorTypeEnum.Button;
                }
                else if (this.properties[i].Display != null && 
                         this.properties[i].Display.Type == DisplayType.FieldName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = this.GetFieldNames();
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (this.properties[i].Display != null && 
                         this.properties[i].Display.Type == DisplayType.ResidueName &&
                         this.model is Models.SurfaceOM.SurfaceOrganicMatter)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = GetResidueNames();
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (this.properties[i].Display != null && 
                         properties[i].Display.Type == DisplayType.Model)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;

                    string[] modelNames = GetModelNames(properties[i].Display.ModelType);
                    if (modelNames != null)
                        cell.DropDownStrings = modelNames;
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
                    else if (cellValue.GetType() == typeof(IPlant))
                    {
                        cell.EditorType = EditorTypeEnum.DropDown;
                        List<string> cropNames = new List<string>();
                        foreach (Model crop in Apsim.FindAll(this.model, typeof(IPlant)))
                        {
                            cropNames.Add(crop.Name);
                        }

                        cell.DropDownStrings = cropNames.ToArray();
                    }
                    else if (this.properties[i].DataType == typeof(IPlant))
                    {
                        List<string> plantNames = Apsim.FindAll(this.model, typeof(IPlant)).Select(m => m.Name).ToList();
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
            for (int i = 0; i < this.properties.Count; i++)
            {
                if (this.properties[i].Display.Type == DisplayType.TableName)
                {
                    IGridCell cell = this.grid.GetCell(1, i);
                    if (cell.Value != null && cell.Value.ToString() != string.Empty)
                    {
                        string tableName = cell.Value.ToString();
                        DataTable data = null;
                        if (storage.TableNames.Contains(tableName))
                            data = this.storage.RunQuery("SELECT * FROM " + tableName + " LIMIT 1");
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
            return Apsim.Find(this.model, typeof(IPlant)) as IPlant;
        }

        private string[] GetResidueNames()
        {
            if (this.model is Models.SurfaceOM.SurfaceOrganicMatter)
            {
                List<Models.SurfaceOM.SurfaceOrganicMatter.ResidueType> types = (this.model as Models.SurfaceOM.SurfaceOrganicMatter).ResidueTypes.residues;
                string[] result = new string[types.Count];
                for (int i = 0; i < types.Count; i++)
                    result[i] = types[i].fom_type;
                Array.Sort(result, StringComparer.InvariantCultureIgnoreCase);

                return result;
            }
            return null;
        }

        private string[] GetModelNames(Type t)
        {
            List<IModel> models;
            if (t == null)
                models = Apsim.FindAll(this.model);
            else
                models = Apsim.FindAll(this.model, t);

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
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            foreach (IGridCell cell in e.ChangedCells)
            {
                if (e.invalidValue)
                {
                    this.explorerPresenter.MainPresenter.ShowMsgDialog("The value you entered was not valid for its datatype", "Invalid entry", Gtk.MessageType.Warning, Gtk.ButtonsType.Ok);
                }
                try
                {
                    this.SetPropertyValue(this.properties[cell.RowIndex], cell.Value);
                }
                catch (Exception ex)
                {
                    explorerPresenter.MainPresenter.ShowError(ex);
                }
            }
            
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
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
                    throw new ApsimXException(this.model, "Invalid property type: " + property.DataType.ToString());
                }
            }
            else if (typeof(IPlant).IsAssignableFrom(property.DataType))
            {
                value = Apsim.Find(this.model, value.ToString()) as IPlant;
            }
            else if (property.DataType == typeof(DateTime))
            {
                value = Convert.ToDateTime(value);
            }
            else if (property.DataType.IsEnum)
            {
                value = Enum.Parse(property.DataType, value.ToString());
            }
            else if (property.Display != null &&
                     property.Display.Type == DisplayType.Model)
            {
                value = Apsim.Get(this.model, value.ToString());
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
        /// Does creation of the dialog belong here, or in the view?
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnFileBrowseClick(object sender, GridCellsChangedArgs e)
        {
            string fileName = ViewBase.AskUserForFileName("Select file path", string.Empty, Gtk.FileChooserAction.Open, e.ChangedCells[0].Value.ToString());
            if (fileName != null && fileName != e.ChangedCells[0].Value.ToString())
            {
                e.ChangedCells[0].Value = fileName;
                this.OnCellValueChanged(sender, e);
                this.PopulateGrid(this.model);
            }
        }
    }
}
