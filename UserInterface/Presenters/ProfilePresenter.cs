//-----------------------------------------------------------------------
// <copyright file="ProfilePresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using EventArguments;
    using Interfaces;
    using Models.Core;
    using Models.Graph;
    using Models.Soils;
    using Views;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// <para>
    /// This presenter talks to a ProfileView to display profile (layered) data in a grid.
    /// It uses reflection to look for public properties that are read/write, have a
    /// [Description] attribute and return either double[] or string[].
    /// </para>
    /// <para>
    /// For each property found it will
    ///   1. optionally look for units via a units attribute:
    ///         [Units("kg/ha")]
    ///   2. optionally look for settable units via the presence of these properties/methods:
    ///         {Property}Units { get; set; }
    ///         {Property}UnitsToString(Units)
    ///         {Property}UnitsSet(ToUnits)
    ///         where {Property} is the name of the property being examined.
    ///   3. optionally look for a metadata property named:
    ///     {Property}Metadata { get; set; }
    /// </para>
    /// </summary>
    public class ProfilePresenter : IPresenter
    {
        /// <summary>
        /// The underlying model that this presenter is to work with.
        /// </summary>
        private Model model;

        /// <summary>
        /// The underlying view that this presenter is to work with.
        /// </summary>
        private IProfileView view;

        /// <summary>
        /// A reference to the parent 'explorerPresenter'
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// A reference to the 'graphPresenter' responsible for our graph.
        /// </summary>
        private GraphPresenter graphPresenter;

        /// <summary>
        /// A reference to our 'propertyPresenter'
        /// </summary>
        private PropertyPresenter propertyPresenter;

        /// <summary>
        /// A list of all properties in the profile grid.
        /// </summary>
        private List<VariableProperty> propertiesInGrid = new List<VariableProperty>();

        /// <summary>
        /// Our graph.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// The parent zone of our model.
        /// </summary>
        private IModel parentForGraph;

        /// <summary>
        /// When the user right clicks a column header, this field contains the index of that column
        /// </summary>
        private int indexOfClickedVariable;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The underlying model we are to use</param>
        /// <param name="view">The underlying view we are to attach to</param>
        /// <param name="explorerPresenter">Our parent explorerPresenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.view = view as IProfileView;
            this.explorerPresenter = explorerPresenter;

            // Setup the property presenter and view. Hide the view if there are no properties to show.
            this.propertyPresenter = new PropertyPresenter();
            this.propertyPresenter.Attach(this.model, this.view.PropertyGrid, this.explorerPresenter);

            // Create a list of profile (array) properties. Create a table from them and 
            // hand the table to the profile grid.
            this.FindAllProperties(this.model);

            // Populate the grid
            this.PopulateGrid();

            // Populate the graph.
            this.graph = Utility.Graph.CreateGraphFromResource(model.GetType().Name + "Graph");
            if (this.graph == null)
            {
                this.view.ShowGraph(false);
            }
            else
            {
                parentForGraph = this.model.Parent as IModel;
                if (this.parentForGraph != null)
                {
                    this.parentForGraph.Children.Add(this.graph);
                    this.graph.Parent = this.parentForGraph;
                    this.view.ShowGraph(true);
                    int padding = (this.view as ProfileView).Width / 2 / 2;
                    this.view.Graph.LeftRightPadding = padding;
                    this.graphPresenter = new GraphPresenter();
                    this.graphPresenter.Attach(this.graph, this.view.Graph, this.explorerPresenter);
                }
            }

            // Trap the invoking of the ProfileGrid 'CellValueChanged' event so that
            // we can save the contents.
            this.view.ProfileGrid.CellsChanged += this.OnProfileGridCellValueChanged;

            // Trap the right click on column header so that we can potentially put
            // units on the context menu.
            this.view.ProfileGrid.ColumnHeaderClicked += this.OnColumnHeaderClicked;

            // Trap the model changed event so that we can handle undo.
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
            
            this.view.ProfileGrid.ResizeControls();
            this.view.PropertyGrid.ResizeControls();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.view.ProfileGrid.CellsChanged -= this.OnProfileGridCellValueChanged;
            this.view.ProfileGrid.ColumnHeaderClicked -= this.OnColumnHeaderClicked;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
       
            if (this.parentForGraph != null && this.graph != null)
            {
                this.parentForGraph.Children.Remove(this.graph);
            }
        }

        /// <summary>
        /// Populate the grid with data and formatting.
        /// </summary>
        private void PopulateGrid()
        {
            DataTable table = this.CreateTable();
            this.view.ProfileGrid.DataSource = table;

            // Remove, from the PropertyGrid, the properties being displayed in the ProfileGrid.
            this.propertyPresenter.RemoveProperties(this.propertiesInGrid);
            this.view.ShowPropertyGrid(!this.propertyPresenter.IsEmpty);

            // Format the profile grid.
            this.FormatGrid(table);
        }

        /// <summary>
        /// Format the grid based on the data in the specified table.
        /// </summary>
        /// <param name="table">The table to use to format the grid.</param>
        private void FormatGrid(DataTable table)
        {
            Color[] cropColors = { Color.FromArgb(173, 221, 142), Color.FromArgb(247, 252, 185) };
            Color[] predictedCropColors = { Color.FromArgb(233, 191, 255), Color.FromArgb(244, 226, 255) };

            int cropIndex = 0;
            int predictedCropIndex = 0;

            Color foregroundColour = Color.Black;
            Color backgroundColour = Color.White;

            for (int col = 0; col < this.propertiesInGrid.Count; col++)
            {
                VariableProperty property = this.propertiesInGrid[col];

                string columnName = property.Description;

                // crop colours
                if (property.CropName != null)
                {
                    if (property.Metadata.Contains("Estimated"))
                    {
                        backgroundColour = predictedCropColors[predictedCropIndex];
                        foregroundColour = Color.Gray;
                        if (columnName.Contains("XF"))
                        {
                            predictedCropIndex++;
                        }

                        if (predictedCropIndex >= predictedCropColors.Length)
                        {
                            predictedCropIndex = 0;
                        }
                    }
                    else
                    {
                        backgroundColour = cropColors[cropIndex];
                        if (columnName.Contains("XF"))
                        {
                            cropIndex++;
                        }

                        if (cropIndex >= cropColors.Length)
                        {
                            cropIndex = 0;
                        }
                    }
                }

                // tool tips
                string[] toolTips = null;
                if (property.IsReadOnly)
                {
                    foregroundColour = Color.Gray;
                    toolTips = StringUtilities.CreateStringArray("Calculated", this.view.ProfileGrid.RowCount);
                }
                else
                {
                    foregroundColour = Color.Black;
                    toolTips = property.Metadata;
                }

                string format = property.Format;
                if (format == null)
                {
                    format = "N3";
                }

                IGridColumn gridColumn = this.view.ProfileGrid.GetColumn(col);
                gridColumn.Format = format;
                gridColumn.BackgroundColour = backgroundColour;
                gridColumn.ForegroundColour = foregroundColour;
                gridColumn.ReadOnly = property.IsReadOnly;
                for (int rowIndex = 0; rowIndex < toolTips.Length; rowIndex++)
                {
                    IGridCell cell = this.view.ProfileGrid.GetCell(col, rowIndex);
                    cell.ToolTip = toolTips[rowIndex];
                }

                // colour the column headers of total columns.
                if (!double.IsNaN(property.Total))
                {
                    gridColumn.HeaderForegroundColour = Color.Red;
                }
            }

            this.view.ProfileGrid.RowCount = 100;
        }

        /// <summary>
        /// Setup the profile grid based on the properties in the model.
        /// </summary>
        /// <param name="model">The underlying model we are to use to find the properties</param>
        private void FindAllProperties(Model model)
        {
            // Properties must be public with a getter and a setter. They must also
            // be either double[] or string[] type.
            foreach (PropertyInfo property in model.GetType().GetProperties())
            {
                bool hasDescription = property.IsDefined(typeof(DescriptionAttribute), false);
                if (hasDescription && property.CanRead)
                {
                    if (this.model.Name == "Water" &&
                        property.Name == "Depth" &&
                        typeof(ISoilCrop).IsAssignableFrom(model.GetType()))
                    {
                    }
                    else if (property.PropertyType == typeof(double[]) || 
                        property.PropertyType == typeof(string[]))
                    {
                        this.propertiesInGrid.Add(new VariableProperty(model, property));
                    }
                    else if (property.PropertyType.FullName.Contains("SoilCrop"))
                    {
                        List<ISoilCrop> crops = property.GetValue(model, null) as List<ISoilCrop>;
                        if (crops != null)
                        {
                            foreach (ISoilCrop crop in crops)
                            {
                                this.FindAllProperties(crop as Model);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Setup the profile grid based on the properties in the model.
        /// The column index of the cell that has changed.
        /// </summary>
        /// <returns>The filled data table. Never returns null.</returns>
        private DataTable CreateTable()
        {
            DataTable table = new DataTable();

            foreach (VariableProperty property in this.propertiesInGrid)
            {
                string columnName = property.Description;
                if (property.Units != null)
                {
                    columnName += "\r\n(" + property.Units + ")";
                }

                // add a total to the column header if necessary.
                double total = property.Total;
                if (!double.IsNaN(total))
                {
                    columnName = columnName + "\r\n" + total.ToString("N1");
                }

                Array values = property.Value as Array;

                if (table.Columns.IndexOf(columnName) == -1)
                {
                    table.Columns.Add(columnName, property.DataType.GetElementType());
                }
                else
                {

                }

                DataTableUtilities.AddColumnOfObjects(table, columnName, values);
            }

            return table;
        }

        /// <summary>
        /// User has changed the value of one or more cells in the profile grid.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnProfileGridCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            this.SaveGrid();

            // Refresh all calculated columns.
            this.RefreshCalculatedColumns();

            // Refresh the graph.
            if (this.graph != null)
            {
                this.graphPresenter.DrawGraph();
            }
        }

        /// <summary>
        /// Save the grid back to the model.
        /// </summary>
        private void SaveGrid()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
       
            // Get the data source of the profile grid.
            DataTable data = this.view.ProfileGrid.DataSource;

            // Maintain a list of all property changes that we need to make.
            List<Commands.ChangeProperty.Property> properties = new List<Commands.ChangeProperty.Property>();

            // Loop through all non-readonly properties, get an array of values from the data table
            // for the property and then set the property value.
            for (int i = 0; i < this.propertiesInGrid.Count; i++)
            {
                // If this property is NOT readonly then set its value.
                if (!this.propertiesInGrid[i].IsReadOnly)
                {
                    // Get an array of values for this property.
                    Array values;
                    if (this.propertiesInGrid[i].DataType.GetElementType() == typeof(double))
                    {
                        values = DataTableUtilities.GetColumnAsDoubles(data, data.Columns[i].ColumnName);
                        if (!MathUtilities.ValuesInArray((double[])values))
                            values = null;
                        else
                            values = MathUtilities.RemoveMissingValuesFromBottom((double[])values);
                    }
                    else
                    {
                        values = DataTableUtilities.GetColumnAsStrings(data, data.Columns[i].ColumnName);
                        values = MathUtilities.RemoveMissingValuesFromBottom((string[])values);
                    }

                    // Is the value any different to the former property value?
                    bool changedValues;
                    if (this.propertiesInGrid[i].DataType == typeof(double[]))
                    {
                        changedValues = !MathUtilities.AreEqual((double[])values, (double[])this.propertiesInGrid[i].Value);
                    }
                    else
                    {
                        changedValues = !MathUtilities.AreEqual((string[])values, (string[])this.propertiesInGrid[i].Value);
                    }

                    if (changedValues)
                    {
                        // Store the property change.
                        Commands.ChangeProperty.Property property = new Commands.ChangeProperty.Property();
                        property.Name = this.propertiesInGrid[i].Name;
                        property.Obj = this.propertiesInGrid[i].Object;
                        property.NewValue = values;
                        properties.Add(property);
                    }
                }
            }

            // If there are property changes pending, then commit the changes in a block.
            if (properties.Count > 0)
            {
                Commands.ChangeProperty command = new Commands.ChangeProperty(properties);
                this.explorerPresenter.CommandHistory.Add(command);
            }

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Refresh the values of all calculated columns in the profile grid.
        /// </summary>
        private void RefreshCalculatedColumns()
        {
            // Loop through all calculated properties, get an array of values from the property
            // a give to profile grid.
            for (int i = 0; i < this.propertiesInGrid.Count; i++)
            {
                if (this.propertiesInGrid[i].IsReadOnly && i > 0)
                {
                    VariableProperty property = this.propertiesInGrid[i];
                    int col = i;
                    int row = 0;
                    foreach (object value in property.Value as IEnumerable<double>)
                    {
                        object valueForCell = value;
                        bool missingValue = (double)value == MathUtilities.MissingValue || double.IsNaN((double)value);

                        if (missingValue)
                        {
                            valueForCell = null;
                        }

                        IGridCell cell = this.view.ProfileGrid.GetCell(col, row);
                        cell.Value = valueForCell;

                        row++;
                    }

                    // add a total to the column header if necessary.
                    double total = property.Total;
                    if (!double.IsNaN(total))
                    {
                        string columnName = property.Description;
                        if (property.Units != null)
                        {
                            columnName += "\r\n(" + property.Units + ")";
                        }

                        columnName = columnName + "\r\n" + total.ToString("N1") + " mm";

                        IGridColumn column = this.view.ProfileGrid.GetColumn(col);
                        column.HeaderText = columnName;
                    }
                }
            }
        }

        /// <summary>
        /// The model has changed probably because of an undo.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            this.PopulateGrid();
            this.RefreshCalculatedColumns();
            if (this.graphPresenter != null)
            {
                this.graphPresenter.DrawGraph();
            }
        }

        /// <summary>
        /// The column header has been clicked
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnColumnHeaderClicked(object sender, GridHeaderClickedArgs e)
        {
            if (e.RightClick)
            {
                this.view.ProfileGrid.ClearContextActions();
                this.indexOfClickedVariable = e.Column.ColumnIndex;
                VariableProperty property = this.propertiesInGrid[this.indexOfClickedVariable];
                foreach (string unit in property.AllowableUnits)
                    this.view.ProfileGrid.AddContextAction(unit, this.OnUnitClick);                     
            }
        }

        /// <summary>
        /// The unit menu item has been clicked by user.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnUnitClick(object sender, EventArgs e)
        {
            VariableProperty property = this.propertiesInGrid[this.indexOfClickedVariable];
            property.Units = (sender as System.Windows.Forms.ToolStripDropDownItem).Text;
            this.OnModelChanged(this.model);
        }
    }
}