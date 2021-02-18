namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Commands;
    using Models.Core;
    using Models;
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
            this.graph = Utility.Graph.CreateGraphFromResource("ApsimNG.Resources.WaterGraph.xml");
            graph.Name = "";
            if (this.graph == null)
                this.view.ShowGraph(false);
            else
            {
                // The graph's series contain many variables such as [Soil].LL. We now replace
                // these relative paths with absolute paths.
                foreach (Series series in graph.FindAllChildren<Series>())
                {
                    series.XFieldName = series.XFieldName?.Replace("[Soil]", this.model.Parent.FullPath);
                    series.X2FieldName = series.X2FieldName?.Replace("[Soil]", this.model.Parent.FullPath);
                    series.YFieldName = series.YFieldName?.Replace("[Soil]", this.model.Parent.FullPath);
                    series.Y2FieldName = series.Y2FieldName?.Replace("[Soil]", this.model.Parent.FullPath);
                }

                this.parentForGraph = this.model.Parent as IModel;
                if (this.parentForGraph != null)
                {
                    // Don't add the graph as a child of the soil. This causes problems
                    // (see bug #4622), and adding the soil as a parent is sufficient.
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
                            if (profileGrid.Properties[i].Object is SoilCrop)
                            {
                                string soilCropName = (profileGrid.Properties[i].Object as SoilCrop).Name;
                                string cropName = soilCropName.Replace("Soil", "");
                                columnName = cropName + " " + columnName;
                            }

                            Series cropLLSeries = new Series();
                            cropLLSeries.Name = columnName;
                            cropLLSeries.Colour = ColourUtilities.ChooseColour(this.graph.Children.Count);
                            cropLLSeries.Line = LineType.Solid;
                            cropLLSeries.Marker = MarkerType.None;
                            cropLLSeries.Type = SeriesType.Scatter;
                            cropLLSeries.ShowInLegend = true;
                            cropLLSeries.XAxis = Axis.AxisType.Top;
                            cropLLSeries.YAxis = Axis.AxisType.Left;
                            cropLLSeries.YFieldName = (parentForGraph is Soil ? parentForGraph.FullPath : "[Soil]") + ".Physical.DepthMidPoints";
                            cropLLSeries.XFieldName = ((profileGrid.Properties[i].Object as IModel)).FullPath + "." + profileGrid.Properties[i].Name;
                            //cropLLSeries.XFieldName = ((property.Object as Model)).FullPath + "." + property.Name;
                            cropLLSeries.Parent = this.graph;

                            this.graph.Children.Add(cropLLSeries);
                        }
                    }

                    this.graph.LegendPosition = Graph.LegendPositionType.RightTop;
                    explorerPresenter.ApsimXFile.Links.Resolve(graphPresenter);
                    this.graphPresenter.Attach(this.graph, this.view.Graph, this.explorerPresenter);
                    graphPresenter.LegendInsideGraph = false;
                }
            }

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
    }
}