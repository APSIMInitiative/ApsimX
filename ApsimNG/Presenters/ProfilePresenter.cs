namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Commands;
    using Models.Core;
    using Models.Graph;
    using Models.Soils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using Views;

    /// <summary>
    /// This presenter talks to a ProfileView to display profile (layered) data in a grid.
    /// It uses reflection to look for public properties that are read/write, and have a
    /// [Description] attribute.
    /// </summary>
    /// <remarks>
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
    /// </remarks>

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
        private List<PropertyColumn> propertiesInGrid = new List<PropertyColumn>();

        /// <summary>
        /// Presenter for the profile grid.
        /// </summary>
        private GridPresenter profileGrid = new GridPresenter();

        /// <summary>
        /// Our graph.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// The parent zone of our model.
        /// </summary>
        private IModel parentForGraph;

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
            profileGrid.Attach(model, this.view.ProfileGrid, explorerPresenter);
            this.explorerPresenter = explorerPresenter;

            this.view.ShowView(false);

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
                this.view.ShowGraph(false);
            else
            {
                // The graph's series contain many variables such as [Soil].LL. We now replace
                // these relative paths with absolute paths.
                foreach (Series series in Apsim.Children(graph, typeof(Series)))
                {
                    series.XFieldName = series.XFieldName?.Replace("[Soil]", Apsim.FullPath(this.model.Parent));
                    series.X2FieldName = series.X2FieldName?.Replace("[Soil]", Apsim.FullPath(this.model.Parent));
                    series.YFieldName = series.YFieldName?.Replace("[Soil]", Apsim.FullPath(this.model.Parent));
                    series.Y2FieldName = series.Y2FieldName?.Replace("[Soil]", Apsim.FullPath(this.model.Parent));
                }

                this.parentForGraph = this.model.Parent as IModel;
                if (this.parentForGraph != null)
                {
                    this.parentForGraph.Children.Add(this.graph);
                    this.graph.Parent = this.parentForGraph;
                    this.view.ShowGraph(true);
                    int padding = (this.view as ProfileView).MainWidget.Allocation.Width / 2 / 2;
                    this.view.Graph.LeftRightPadding = padding;
                    this.graphPresenter = new GraphPresenter();
                    for (int col = 0; col < this.propertiesInGrid.Count; col++)
                    {
                        string columnName = propertiesInGrid[col].ColumnName;

                        if (columnName.Contains("\r\n"))
                            StringUtilities.SplitOffAfterDelimiter(ref columnName, "\r\n");

                        // crop colours
                        if (columnName.Contains("LL"))
                        {
                            Series cropLLSeries = new Series();
                            cropLLSeries.Name = columnName;
                            cropLLSeries.Colour = ColourUtilities.ChooseColour(this.graph.Children.Count);
                            cropLLSeries.Line = LineType.Solid;
                            cropLLSeries.Marker = MarkerType.None;
                            cropLLSeries.Type = SeriesType.Scatter;
                            cropLLSeries.ShowInLegend = true;
                            cropLLSeries.XAxis = Axis.AxisType.Top;
                            cropLLSeries.YAxis = Axis.AxisType.Left;
                            cropLLSeries.YFieldName = (parentForGraph is Soil ? Apsim.FullPath(parentForGraph) : "[Soil]") + ".DepthMidPoints";
                            cropLLSeries.XFieldName = Apsim.FullPath((propertiesInGrid[col].ObjectWithProperty as Model)) + "." + propertiesInGrid[col].PropertyName;
                            //cropLLSeries.XFieldName = Apsim.FullPath(property.Object as Model) + "." + property.Name;
                            cropLLSeries.Parent = this.graph;

                            this.graph.Children.Add(cropLLSeries);
                        }
                    }

                    explorerPresenter.ApsimXFile.Links.Resolve(graphPresenter);
                    this.graphPresenter.Attach(this.graph, this.view.Graph, this.explorerPresenter);
                }
            }

            // Trap the invoking of the ProfileGrid 'CellValueChanged' event so that
            // we can save the contents.
            this.view.ProfileGrid.CellsHaveChanged += this.OnProfileGridCellValueChanged;

            // Trap the right click on column header so that we can potentially put
            // units on the context menu.
            this.view.ProfileGrid.ColumnMenuClicked += this.OnColumnMenuItemClicked;

            // Trap the model changed event so that we can handle undo.
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            this.view.ShowView(true);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.ProfileGrid.EndEdit();
            SaveGrid();

            this.view.ProfileGrid.CellsHaveChanged -= this.OnProfileGridCellValueChanged;
            this.view.ProfileGrid.ColumnMenuClicked -= this.OnColumnMenuItemClicked;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            propertyPresenter.Detach();
            if (this.graphPresenter != null)
                this.graphPresenter.Detach();

            if (this.parentForGraph != null && this.graph != null)
                this.parentForGraph.Children.Remove(this.graph);
        }

        /// <summary>
        /// Populate the grid with data and formatting.
        /// </summary>
        private void PopulateGrid()
        {
            view.ProfileGrid.SetColumns(propertiesInGrid.Cast<GridColumnMetaData>().ToList());

            // Remove, from the PropertyGrid, the properties being displayed in the ProfileGrid.
            propertyPresenter.RemoveProperties(propertiesInGrid.Select(property => property.PropertyName));
            view.ShowPropertyGrid(!propertyPresenter.IsEmpty);
        }

        /// <summary>Find all properties to display in the grid.</summary>
        /// <param name="model">The underlying model we are to use to find the properties.</param>
        private void FindAllProperties(Model model)
        {
            // When user clicks on a SoilCrop, there is no thickness column. In this
            // situation get thickness column from parent model.
            if (model is SoilCrop && propertiesInGrid.Count == 0)
            {
                var thicknessProperty = model.Parent.GetType().GetProperty("Thickness");
                propertiesInGrid.Add(new ThicknessColumn(thicknessProperty, model.Parent));
            }

            foreach (PropertyInfo property in model.GetType().GetProperties())
            {
                var description = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), false);
                if (property.PropertyType.IsArray && description != null)
                {
                    PropertyColumn column;

                    if (property.Name == "Thickness")
                        column = new ThicknessColumn(property, model);
                    else
                        column = new PropertyColumn(property, model);

                    if (model is SoilCrop)
                        FormatSoilCropColumn(column);

                    propertiesInGrid.Add(column);

                    if (property.Name == "XF")
                        propertiesInGrid.Add(new PAWCColumn(model, propertiesInGrid));
                }
            }

            foreach (var soilCrop in model.Children.FindAll(child => child is SoilCrop))
                FindAllProperties(soilCrop);
        }

        /// <summary>Format the SoilCrop column.</summary>
        /// <param name="column">The column to format.</param>
        private void FormatSoilCropColumn(PropertyColumn column)
        {
            var soilCrop = column.ObjectWithProperty as SoilCrop;

            column.ColumnName = soilCrop.Name.Replace("Soil", "") + " " + column.ColumnName;

            // Colour the crop column.
            var crops = soilCrop.Parent.Children.Where(child => child is SoilCrop).ToList();
            int cropIndex = crops.IndexOf(soilCrop);
            int colourIndex = cropIndex % ColourUtilities.Colours.Length;
            column.ForegroundColour = ColourUtilities.Colours[colourIndex];

            // Make the soil crop columns wider to fit the crop name in column title.
            column.Width = 90;
        }

        /// <summary>
        /// User has changed the value of one or more cells in the profile grid.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnProfileGridCellValueChanged(object sender, EventArgs e)
        {
            // Refresh the graph.
            if (this.graph != null)
                this.graphPresenter.DrawGraph();
        }

        /// <summary>
        /// Save the grid back to the model.
        /// </summary>
        private void SaveGrid()
        {
            try
            {
                explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

                // Maintain a list of all property changes that we need to make.
                var properties = new List<ChangeProperty.Property>();

                // Loop through all changed properties and set the property value.
                foreach (var column in propertiesInGrid.Where(column => column.ValuesHaveChanged))
                    properties.Add(column.GetChangeProperty());

                // If there are property changes pending, then commit the changes in a block.
                if (properties.Count > 0)
                {
                    var command = new ChangeProperty(properties);
                    explorerPresenter.CommandHistory.Add(command);
                }

                explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
            }
            catch (Exception e)
            {
                explorerPresenter.MainPresenter.ShowError(e);
            }
        }

        /// <summary>
        /// The model has changed probably because of an undo.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            this.PopulateGrid();
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
        private void OnColumnMenuItemClicked(object sender, GridCellColumnMenuClickedArgs e)
        {
            var newColumnName = e.ColumnClicked.ColumnName;
            int posOpenBracket = newColumnName.IndexOf('(');
            int posCloseBracket = newColumnName.IndexOf(')');
            newColumnName = newColumnName.Remove(posOpenBracket, posCloseBracket - posOpenBracket + 1);
            newColumnName = newColumnName.Insert(posOpenBracket, "(" + e.MenuNameClicked + ")");
            e.ColumnClicked.ColumnName = newColumnName;
            e.ColumnClicked.ValuesHaveChanged = true;
        }

        /// <summary>Encapsulates metadata about a column on the grid.</summary>
        private class PropertyColumn : GridColumnMetaData
        {
            public object ObjectWithProperty;
            public string PropertyName;

            /// <summary>Constructor.</summary>
            /// <param name="property">The property.</param>
            /// <param name="obj">The instance containing the property.</param>
            public PropertyColumn(PropertyInfo property, object obj)
            {
                if (property != null)
                {
                    ObjectWithProperty = obj;
                    PropertyName = property.Name;
                    ColumnDataType = property.PropertyType.GetElementType();

                    var description = ReflectionUtilities.GetAttribute(property, typeof(DescriptionAttribute), false);
                    if (description == null)
                        ColumnName = property.Name;
                    else
                        ColumnName += description.ToString();

                    // Add units to column name.
                    var units = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false);
                    if (units != null)
                        ColumnName += "\r\n" + units.ToString();

                    // Add display attributes.
                    var display = ReflectionUtilities.GetAttribute(property, typeof(DisplayAttribute), false) as DisplayAttribute;
                    if (display != null)
                    {
                        Format = display.Format;
                        AddTotalToColumnName = display.ShowTotal;
                    }

                    Values = property.GetValue(obj) as IEnumerable;
                    IsReadOnly = !property.CanWrite;
                    Width = -1;
                }
            }

            /// <summary>Returns a Property set command.</summary>
            public virtual ChangeProperty.Property GetChangeProperty()
            {
                return new ChangeProperty.Property(ObjectWithProperty, PropertyName, Values);
            }
        }

        /// <summary>Encapsulates a thickness column.</summary>
        private class ThicknessColumn : PropertyColumn
        {
            /// <summary>Constructor.</summary>
            /// <param name="property">The property.</param>
            /// <param name="obj">The instance containing the property.</param>
            public ThicknessColumn(PropertyInfo property, object obj) 
                : base(property, obj)
            {
                ColumnName = "Depth\r\n(mm)";
                ColumnDataType = typeof(string);
                Values = APSIM.Shared.APSoil.SoilUtilities.ToDepthStrings((double[])property.GetValue(obj));
            }

            /// <summary>Returns a Property set command.</summary>
            public override ChangeProperty.Property GetChangeProperty()
            {
                var thickness = APSIM.Shared.APSoil.SoilUtilities.ToThickness((string[]) Values);
                return new ChangeProperty.Property(ObjectWithProperty, PropertyName, thickness);
            }
        }

        /// <summary>Encapsulates a PAWC column.</summary>
        private class PAWCColumn : PropertyColumn
        {
            /// <summary>Constructor.</summary>
            /// <param name="property">The property.</param>
            /// <param name="obj">The instance containing the property.</param>
            public PAWCColumn(IModel soilCrop,
                              List<PropertyColumn> propertiesInGrid)
                : base(null, null)
            {
                ColumnName = soilCrop.Name.Replace("Soil", "") + " PAWC\r\n(mm)";
                ColumnDataType = typeof(double);
                IsReadOnly = true;
                AddTotalToColumnName = true;
                Width = 100;
                ForegroundColour = Color.Red;

                var cropName = soilCrop.Name.Replace("Soil", "");
                var thicknessColumn = propertiesInGrid.Find(prop => prop.ColumnName.StartsWith("Depth"));
                var llColumn = propertiesInGrid.Find(prop => prop.ColumnName.StartsWith(cropName + " LL"));
                var dulColumn = propertiesInGrid.Find(prop => prop.ColumnName.StartsWith("DUL"));
                var xfColumn = propertiesInGrid.Find(prop => prop.ColumnName.StartsWith(cropName + " XF"));

                // When user clicks on a SoilCrop, there is no DUL column. In
                // this situation get dul from the parent model.
                double[] dul;
                if (dulColumn == null)
                    dul = (soilCrop.Parent as Physical).DUL;
                else
                    dul = dulColumn.Values as double[];

                var thickness = APSIM.Shared.APSoil.SoilUtilities.ToThickness((string[])thicknessColumn.Values);
                var pawcVolumetric = Soil.CalcPAWC(thickness, llColumn.Values as double[], dul, xfColumn.Values as double[]);
                Values = MathUtilities.Multiply(pawcVolumetric, thickness);
            }

            /// <summary>Returns a Property set command.</summary>
            public override ChangeProperty.Property GetChangeProperty()
            {
                return null;
            }
        }
    }
}