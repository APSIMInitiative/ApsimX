
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
    using Models.CLEM;
    using Models.CLEM.Resources;
    using Utility;
    using Views;
    using Models.Storage;
    using System.Globalization;
    using Models.LifeCycle;

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
    /// <para>
    /// This is similar to the <see cref="PropertyPresenter"/>, except that this
    /// presenter shows properties for all children that have the same type in a single
    /// table in the grid view. The rows will be the different properties, and the
    /// columns will be the different values of that same property for each child.
    /// </para>
    /// </summary>
    public class PropertyTablePresenter : IPresenter
    {
        /// <summary>
        /// Linked storage reader
        /// </summary>
        [Link]
        private IDataStore storage = null;

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
        /// A list of all the children that have the same type so that they will all have the same properties
        /// so that we can turn them all into a single table in the grid view.
        /// The rows will be the different properties 
        /// The columns will be the different values of that same property for each child. 
        /// </summary>
        private List<IModel> childrenWithSameType = new List<IModel>();

        /// <summary>
        /// A List of lists of all properties found in the Model.
        /// Each list is the list of all the properties in each child of this model.
        /// </summary>
        private List<List<VariableProperty>> properties = new List<List<VariableProperty>>();

        /// <summary>
        /// The category name to filter for on the Category Attribute for the properties
        /// </summary>
        public string CategoryFilter { get; set; }

        /// <summary>
        /// The subcategory name to filter for on the Category Attribute for the properties
        /// </summary>
        public string SubcategoryFilter { get; set; }

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

            this.grid.NumericFormat = "G6";
            grid.CanGrow = false;
            this.childrenWithSameType = this.GetChildModelsWithSameType(this.model);
            this.FindAllPropertiesForChildren();
            if (this.grid.DataSource == null)
            {
                this.PopulateGrid(this.model);
            }
            else
            {
                this.FormatTestGrid();
            }

            this.grid.CellsChanged += this.OnCellValueChanged;
            this.grid.ButtonClick += this.OnFileBrowseClick;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

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

            if (this.childrenWithSameType != null)
            {
                foreach (IModel child in this.childrenWithSameType)
                {
                    //child names become the column names
                    table.Columns.Add(child.Name, typeof(object));
                }
            }
            else
            {
                //if there are no children
                table.Columns.Add("Value", typeof(string));
            }

            this.FillTable(table);
            this.FormatGrid();
            if (selectedCell != null)
            {
                this.grid.GetCurrentCell = selectedCell;
            }     
        }

       /// <summary>
       /// Gets all the child models that have the same type as the first child model.
       /// </summary>
       /// <param name="model"></param>
       /// <returns></returns>
        private List<IModel> GetChildModelsWithSameType(Model model)
        {
            if (model != null)
            {
                Model firstChild = model.Children.FirstOrDefault();
                if (firstChild != null)
                {
                    List<IModel> sameTypeChildren = Apsim.Children(model, firstChild.GetType());
                    return sameTypeChildren;
                }
            }
            return null;
        }

        private void FindAllPropertiesForChildren()
        {
            this.properties.Clear();
            if (this.childrenWithSameType != null)
            {
                foreach (IModel ichild in this.childrenWithSameType)
                {
                    Model child = ichild as Model;
                    this.properties.Add(this.FindAllProperties(child));
                }
            }
        }

        /// <summary>
        /// Find all properties from the model and fill this.properties.
        /// </summary>
        /// <param name="model">The mode object</param>
        private List<VariableProperty> FindAllProperties(Model model)
        {
            List<VariableProperty> result = new List<VariableProperty>();
            this.model = model;
            bool filterByCategory = !((this.CategoryFilter == "") || (this.CategoryFilter == null));
            bool filterBySubcategory = !((this.SubcategoryFilter == "") || (this.SubcategoryFilter == null));

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

                    //If the above conditions have been met and,
                    //If a CategoryFilter has been specified. 
                    //filter only those properties with a [Catagory] attribute that matches the filter.

                    if (includeProperty && filterByCategory)
                    {
                        bool hasCategory = property.IsDefined(typeof(CategoryAttribute), false);
                        if (hasCategory)
                        {
                            CategoryAttribute catAtt = (CategoryAttribute)property.GetCustomAttribute(typeof(CategoryAttribute));
                            if (catAtt.Category == this.CategoryFilter)
                            {
                                if (filterBySubcategory)
                                {
                                    //the catAtt.Subcategory is by default given a value of 
                                    //"Unspecified" if the Subcategory is not assigned in the Category Attribute.
                                    //so this line below will also handle "Unspecified" subcategories.
                                    includeProperty = (catAtt.Subcategory == this.SubcategoryFilter);
                                }
                                else
                                {
                                    includeProperty = true;
                                }
                            } 
                            else
                            {
                                includeProperty = false;
                            }
                        }
                        else
                        {
                            //if we are filtering on "Unspecified" category then there is no Category Attribute
                            // just a Description Attribute on the property in the model.
                            //So we still may need to include it in this case.
                            if (this.CategoryFilter == "Unspecified")
                            {
                                includeProperty = true;
                            }
                            else
                            {
                                includeProperty = false;
                            }
                        }
                    }

                    if (includeProperty)
                    {
                        result.Add(new VariableProperty(this.model, property));
                    }
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Updates the model (All columns)
        /// </summary>
        /// <param name="model"></param>
        public void UpdateModel(Model model)
        {
            this.model = model;
            if (this.model != null)
            {
                int propListIndex = 0;
                foreach (List<VariableProperty> propList in this.properties)
                {
                    this.UpdateModel(propListIndex);
                    propListIndex++;
                }
            }
        }

        /// <summary>
        /// Updates the model (Just one column)
        /// Updates the lists of Cultivar, LifeCycle Phases and Field names in the model.
        /// This is used when the model has been changed. For example, when a 
        /// new crop has been selecled.
        /// </summary>
        /// <param name="model">The new model</param>
        private void UpdateModel(int propListIndex)
        {
            IGridCell curCell = this.grid.GetCurrentCell;
            for (int i = 0; i < this.properties[propListIndex].Count; i++)
            {
                IGridCell cell = this.grid.GetCell(propListIndex+1, i); //add 1 because of Description column
                if (curCell != null && cell.RowIndex == curCell.RowIndex && cell.ColumnIndex == curCell.ColumnIndex)
                {
                    continue;
                }

                if (this.properties[propListIndex][i].Display.Type == DisplayType.CultivarName)
                {
                    IPlant crop = this.GetCrop(this.properties[propListIndex]);
                    if (crop != null)
                    {
                        cell.DropDownStrings = this.GetCultivarNames(crop);
                    }
                }
                else if (this.properties[propListIndex][i].Display.Type == DisplayType.LifeCycleName)
                {
                    Zone zone = Apsim.Find(model,typeof(Zone)) as Zone;
                    if (zone != null)
                    {
                        cell.DropDownStrings = this.GetLifeCycleNames(zone);
                    }
                }
                else if (this.properties[propListIndex][i].Display.Type == DisplayType.LifePhaseName)
                {
                    LifeCycle lifeCycle = this.GetLifeCycle(this.properties[propListIndex]);
                    if (lifeCycle != null)
                    {
                        cell.DropDownStrings = this.GetPhaseNames(lifeCycle);
                    }
                }
                else if (this.properties[propListIndex][i].Display.Type == DisplayType.FieldName)
                {
                    string[] fieldNames = this.GetFieldNames(propListIndex);
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
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
            int propIndex = 0;
            if(this.properties.Count == 0)
            {
                return;
            }
            foreach (VariableProperty property in this.properties[0])
            {
                //set the number of columns to the number of lists
                // plus 1 for the description column
                object[] newrow = new object[this.properties.Count+1];
                newrow[0] = property.Description;
                int colIndex = 1;
     
                foreach (List<VariableProperty> propList in this.properties)
                {
                    newrow[colIndex] = propList[propIndex].ValueWithArrayHandling;
                    colIndex++;
                }

                table.Rows.Add(newrow);
                propIndex++;
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
        /// Format the grid. (All columns)
        /// </summary>
        private void FormatGrid()
        {
            int propListIndex = 0;
            foreach (List<VariableProperty> propList in this.properties)
            {
                this.FormatGrid(propListIndex);
                propListIndex++;
            }
        }

        /// <summary>
        /// Format the grid. (Just one column)
        /// </summary>
        private void FormatGrid(int propListIndex)
        {
            for (int i = 0; i < this.properties[propListIndex].Count; i++)
            {
                IGridCell cell = this.grid.GetCell(propListIndex+1, i); //add one because of the Description column
                        
                if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.TableName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    cell.DropDownStrings = this.storage.Reader.TableNames.ToArray();
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.CultivarName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    IPlant crop = this.GetCrop(this.properties[propListIndex]);
                    if (crop != null)
                    {
                        cell.DropDownStrings = this.GetCultivarNames(crop);
                    }
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.LifeCycleName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    Zone zone = Apsim.Find(model, typeof(Zone)) as Zone;
                    if (zone != null)
                    {
                        cell.DropDownStrings = this.GetLifeCycleNames(zone);
                    }
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.LifePhaseName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    LifeCycle lifeCycle = this.GetLifeCycle(this.properties[propListIndex]);
                    if (lifeCycle != null)
                    {
                        cell.DropDownStrings = this.GetPhaseNames(lifeCycle);
                    }
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.FileName)
                {
                    cell.EditorType = EditorTypeEnum.Button;
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.FieldName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = this.GetFieldNames(propListIndex);
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.ResidueName &&
                         this.model is SurfaceOrganicMatter)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;
                    string[] fieldNames = this.GetResidueNames();
                    if (fieldNames != null)
                    {
                        cell.DropDownStrings = fieldNames;
                    }
                }
                else if (this.properties[propListIndex][i].Display != null && this.properties[propListIndex][i].Display.Type == DisplayType.CLEMResourceName)
                {
                    cell.EditorType = EditorTypeEnum.DropDown;

                    List<string> fieldNames = new List<string>();
                    fieldNames.AddRange(this.GetCLEMResourceNames(this.properties[propListIndex][i].Display.CLEMResourceNameResourceGroups) );

                    // add any extras elements provided to the list.
                    if(this.properties[propListIndex][i].Display.CLEMExtraEntries != null)
                    {
                        fieldNames.AddRange(this.properties[propListIndex][i].Display.CLEMExtraEntries);
                    }

                    if (fieldNames.Count != 0)
                    {
                        cell.DropDownStrings = fieldNames.ToArray();
                    }
                }
                else
                {
                    object cellValue = this.properties[propListIndex][i].ValueWithArrayHandling;
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
                    else if (this.properties[propListIndex][i].DataType == typeof(IPlant))
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

        /// <summary>Get a list of life cycles in the zone.</summary>
        /// <param name="zone">The zone.</param>
        /// <returns>A list of life cycles.</returns>
        private string[] GetLifeCycleNames(Zone zone)
        {
            List<IModel> LifeCycles = Apsim.Children(zone, typeof(LifeCycle));
            if (LifeCycles.Count > 0)
            {
                string[] Namelist = new string[LifeCycles.Count];
                int i = 0;
                foreach (IModel LC in LifeCycles)
                {
                    Namelist[i] = LC.Name;
                    i++;
                }
                return Namelist;
            }
            return new string[0];
        }

        /// <summary>Get a list of Phase Names for life Cycle</summary>
        /// <param name="crop">The crop.</param>
        /// <returns>A list of Phase Names.</returns>
        private string[] GetPhaseNames(LifeCycle lifeCycle)
        {
            if (lifeCycle.LifeCyclePhaseNames.Length == 0)
            {
                Simulations simulations = Apsim.Parent(lifeCycle as IModel, typeof(Simulations)) as Simulations;
                Replacements replacements = Apsim.Child(simulations, typeof(Replacements)) as Replacements;
                if (replacements != null)
                {
                    LifeCycle replacementLifeCycle = Apsim.Child(replacements, (lifeCycle as IModel).Name) as LifeCycle;
                    if (replacementLifeCycle != null)
                    {
                        return replacementLifeCycle.LifeCyclePhaseNames;
                    }
                }
            }
            else
            {
                return lifeCycle.LifeCyclePhaseNames;
            }

            return new string[0];
        }

        /// <summary>Get a list of database fieldnames. 
        /// Returns the names associated with the first table name in the property list
        /// </summary>
        /// <returns>A list of fieldnames.</returns>
        private string[] GetFieldNames(int propListIndex)
        {
            string[] fieldNames = null;
            for (int i = 0; i < this.properties.Count; i++)
            {
                if (this.properties[propListIndex][i].Display.Type == DisplayType.TableName)
                {
                    IGridCell cell = this.grid.GetCell(1, i);
                    if (cell.Value != null && cell.Value.ToString() != string.Empty)
                    {
                        string tableName = cell.Value.ToString();
                        if (storage.Reader.TableNames.Contains(tableName))
                            fieldNames = storage.Reader.ColumnNames(tableName).ToArray<string>();
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
        private IPlant GetCrop(List<VariableProperty> properties)
        {
            foreach (VariableProperty property in properties)
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

        /// <summary>
        /// Go find a LifeCycle property in the specified list of properties or if not
        /// found, find the Life cycle in scope.
        /// </summary>
        /// <param name="properties">The list of properties to look through.</param>
        /// <returns>The found crop or null if none found.</returns>
        private LifeCycle GetLifeCycle(List<VariableProperty> properties)
        {
            foreach (VariableProperty property in properties)
            {
                if (property.DataType == typeof(LifeCycle))
                {
                    LifeCycle lifeCycle = property.Value as LifeCycle;
                    if (lifeCycle != null)
                    {
                        return lifeCycle;
                    }
                }
            }

            // Not found so look for one in scope.
            return Apsim.Find(this.model, typeof(LifeCycle)) as LifeCycle;
        }

        private string[] GetResidueNames()
        {
            if (this.model is SurfaceOrganicMatter)
            {
                List<string> names = new List<string>();
                names = (this.model as SurfaceOrganicMatter).ResidueTypeNames();
                names.Sort();
                return names.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Gets the names of all the items for each ResourceGroup whose items you want to put into a dropdown list.
        /// eg. "AnimalFoodStore,HumanFoodStore,ProductStore"
        /// Will create a dropdown list with all the items from the AnimalFoodStore, HumanFoodStore and ProductStore.
        /// A blank list of ResourceNameResourceGroups will result in all available resources types being created in the list
        /// 
        /// To help uniquely identify items in the dropdown list will need to add the ResourceGroup name to the item name.
        /// eg. The names in the drop down list will become AnimalFoodStore.Wheat, HumanFoodStore.Wheat, ProductStore.Wheat, etc. 
        /// </summary>
        /// <returns>Will create a string array with all the items from the AnimalFoodStore, HumanFoodStore and ProductStore.
        /// to help uniquely identify items in the dropdown list will need to add the ResourceGroup name to the item name.
        /// eg. The names in the drop down list will become AnimalFoodStore.Wheat, HumanFoodStore.Wheat, ProductStore.Wheat, etc. </returns>
        private string[] GetCLEMResourceNames(Type[] resourceNameResourceGroups)
        {
            List<string> result = new List<string>();
            ZoneCLEM zoneCLEM = Apsim.Parent(this.model, typeof(ZoneCLEM)) as ZoneCLEM;
            ResourcesHolder resHolder = Apsim.Child(zoneCLEM, typeof(ResourcesHolder)) as ResourcesHolder;

            foreach (Type resGroupType in resourceNameResourceGroups)
            {
                IModel resGroup = Apsim.Child(resHolder, resGroupType);
                if (resGroup != null)  //see if this group type is included in this particular simulation.
                {
                    foreach (IModel item in resGroup.Children)
                    {
                        result.Add(resGroup.Name + "." + item.Name);
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event parameters</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            foreach (GridCellChangedArgs cell in e.ChangedCells)
            {
                try
                {
                    //need to subtract one for column index of the cell due to description column
                    Model childmodel = this.childrenWithSameType[cell.ColIndex - 1] as Model;
                    VariableProperty property = properties[cell.ColIndex - 1][cell.RowIndex];

                    object newValue = GetNewCellValue(property, cell.NewValue);
                    this.SetPropertyValue(childmodel, property, newValue);
                    if (newValue.GetType().IsEnum)
                        newValue = VariableProperty.GetEnumDescription(newValue as Enum);
                    grid.DataSource.Rows[cell.RowIndex][cell.ColIndex] = newValue;

                }
                catch (Exception ex)
                {
                    explorerPresenter.MainPresenter.ShowError(ex);
                }
            }
            
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Gets the new value of the cell from a string containing the
        /// cell's new contents.
        /// </summary>
        /// <param name="cell">Cell which has been changed.</param>
        private object GetNewCellValue(IVariable property, string newValue)
        {
            if (typeof(IPlant).IsAssignableFrom(property.DataType))
                return Apsim.Find(property.Object as IModel, newValue);

            if (property.Display != null && property.Display.Type == DisplayType.Model)
                return Apsim.Get(property.Object as IModel, newValue);

            try
            {
                return ReflectionUtilities.StringToObject(property.DataType, newValue, CultureInfo.CurrentCulture);
            }
            catch (FormatException err)
            {
                throw new Exception($"Value '{newValue}' is invalid for property '{property.Name}' - {err.Message}.");
            }
        }

        /// <summary>
        /// Set the value of the specified property
        /// </summary>
        /// <param name="property">The property to set the value of</param>
        /// <param name="value">The value to set the property to</param>
        private void SetPropertyValue(Model childmodel, VariableProperty property, object value)
        {
            Commands.ChangeProperty cmd = new Commands.ChangeProperty(childmodel, property.Name, value);
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
        private void OnFileBrowseClick(object sender, GridCellChangedArgs e)
        {
            IFileDialog fileChooser = new FileDialog()
            {
                Action = FileDialog.FileActionType.Open,
                Prompt = "Select file path",
                InitialDirectory = e.OldValue
            };
            string fileName = fileChooser.GetFile();

            if (!string.IsNullOrWhiteSpace(fileName) && fileName != e.OldValue)
            {
                e.NewValue = fileName;
                OnCellValueChanged(sender, new GridCellsChangedArgs(e));
                PopulateGrid(model);
            }
        }
    }
}
