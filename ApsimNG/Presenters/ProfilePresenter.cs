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
        private IModel model;

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
        /// Presenter for the profile grid.
        /// </summary>
        private ProfileGridPresenter profileGrid = new ProfileGridPresenter();

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
            propertyPresenter.ScalarsOnly = true;
            // Populate the grid
            this.PopulateGrid();

            // Populate the graph.
            this.graph = Utility.Graph.CreateGraphFromResource("WaterGraph");

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
                    for (int i = 0; i < this.profileGrid.Properties.Length; i++)
                    {
                        string columnName = profileGrid.Properties[i].Name;

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
                            cropLLSeries.XFieldName = Apsim.FullPath((profileGrid.Properties[i].Object as IModel)) + "." + profileGrid.Properties[i].Name;
                            //cropLLSeries.XFieldName = Apsim.FullPath(property.Object as Model) + "." + property.Name;
                            cropLLSeries.Parent = this.graph;

                            this.graph.Children.Add(cropLLSeries);
                        }
                    }

                    explorerPresenter.ApsimXFile.Links.Resolve(graphPresenter);
                    this.graphPresenter.Attach(this.graph, this.view.Graph, this.explorerPresenter);
                }
            }
            /*
            // Trap the invoking of the ProfileGrid 'CellValueChanged' event so that
            // we can save the contents.
            this.view.ProfileGrid.CellsHaveChanged += this.OnProfileGridCellValueChanged;

            // Trap the right click on column header so that we can potentially put
            // units on the context menu.
            this.view.ProfileGrid.ColumnMenuClicked += this.OnColumnMenuItemClicked;
            */
            // Trap the model changed event so that we can handle undo.
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            this.view.ShowView(true);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            propertyPresenter.Detach();
            profileGrid.Detach();
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
            // Remove, from the PropertyGrid, the properties being displayed in the ProfileGrid.
            //propertyPresenter.RemoveProperties(propertiesInGrid.Select(property => property.PropertyName)); // fixme
            view.ShowPropertyGrid(!propertyPresenter.IsEmpty);
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
        /*
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
        */
    }
}