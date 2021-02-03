namespace UserInterface.Presenters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Interfaces;
    using Models.Core;
    using Models;
    using Models.Storage;
    using Views;
    
    /// <summary>
    /// A presenter for a graph.
    /// </summary>
    public class GraphPresenter : IPresenter, IExportable
    {
        /// <summary>
        /// The storage object
        /// </summary>
        [Link]
        private IDataStore storage = null;
        
        /// <summary>The graph view</summary>
        private IGraphView graphView;

        /// <summary>The graph</summary>
        private Graph graph;

        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The series definitions to show on graph.</summary>
        public IEnumerable<SeriesDefinition> SeriesDefinitions { get; set; } = new List<SeriesDefinition>();

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Attach(model, view, explorerPresenter, null);
        }

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        /// <param name="cache">Cached definitions to be used.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter, List<SeriesDefinition> cache)
        {
            this.graph = model as Graph;
            this.graphView = view as GraphView;
            this.explorerPresenter = explorerPresenter;

            graphView.OnAxisClick += OnAxisClick;
            graphView.OnLegendClick += OnLegendClick;
            graphView.OnCaptionClick += OnCaptionClick;
            graphView.OnHoverOverPoint += OnHoverOverPoint;
            explorerPresenter.CommandHistory.ModelChanged += OnGraphModelChanged;
            this.graphView.AddContextAction("Copy graph to clipboard", CopyGraphToClipboard);
            this.graphView.AddContextOption("Include in auto-documentation?", IncludeInDocumentationClicked, graph.IncludeInDocumentation);

            if (cache == null)
                DrawGraph();
            else
            {
                SeriesDefinitions = cache;
                DrawGraph(cache);
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnGraphModelChanged;
            if (CurrentPresenter != null)
            {
                CurrentPresenter.Detach();
            }

            graphView.OnAxisClick -= OnAxisClick;
            graphView.OnLegendClick -= OnLegendClick;
            graphView.OnCaptionClick -= OnCaptionClick;
            graphView.OnHoverOverPoint -= OnHoverOverPoint;
        }

        public void DrawGraph()
        {
            graphView.Clear();
            if (storage == null)
                storage = graph.FindInScope<IDataStore>();

            // Get a list of series definitions.
            try
            {
                SeriesDefinitions = graph.GetDefinitionsToGraph(storage?.Reader, SimulationFilter).ToArray();
            }
            catch (SQLiteException e)
            {
                explorerPresenter.MainPresenter.ShowError(new Exception("Error obtaining data from database: ", e));
            }
            catch (FirebirdException e)
            {
                explorerPresenter.MainPresenter.ShowError(new Exception("Error obtaining data from database: ", e));
            }

            DrawGraph(SeriesDefinitions);
        }

        /// <summary>Draw the graph on the screen.</summary>
        public void DrawGraph(IEnumerable<SeriesDefinition> definitions)
        {
            graphView.Clear();
            if (storage == null)
                storage = graph.FindInScope<IDataStore>();
            if (graph != null && graph.Series != null)
            {
                foreach (SeriesDefinition definition in definitions)
                {
                    DrawOnView(definition);
                }

                // Update axis maxima and minima
                graphView.UpdateView();

                // Format the axes.
                foreach (Models.Axis a in graph.Axis)
                    FormatAxis(a);

                // Get a list of series annotations.
                DrawOnView(graph.GetAnnotationsToGraph());

                // Format the legend.
                graphView.FormatLegend(graph.LegendPosition, graph.LegendOrientation);

                // Format the title
                graphView.FormatTitle(graph.Name);

                // Format the footer
                if (string.IsNullOrEmpty(graph.Caption))
                {
                    graphView.FormatCaption("Double click to add a caption", true);
                }
                else
                {
                    graphView.FormatCaption(graph.Caption, false);
                }

                // Remove series titles out of the graph disabled series list when
                // they are no longer valid i.e. not on the graph.
                if (graph.DisabledSeries == null)
                    graph.DisabledSeries = new List<string>();
                IEnumerable<string> validSeriesTitles = definitions.Select(s => s.Title);
                List<string> seriesTitlesToKeep = new List<string>(validSeriesTitles.Intersect(this.graph.DisabledSeries));
                this.graph.DisabledSeries.Clear();
                this.graph.DisabledSeries.AddRange(seriesTitlesToKeep);
                graphView.LegendInsideGraph = !graph.LegendOutsideGraph;

                graphView.Refresh();
            }
        }

        /// <summary>Export the contents of this graph to the specified file.</summary>
        /// <param name="folder">The folder.</param>
        /// <returns>The file name</returns>
        public string ExportToPNG(string folder)
        {
            // The rectange numbers below are optimised for generation of PDF document
            // on a computer that has its display settings at 100%.
            Rectangle r = new Rectangle(0, 0, 600, 450);
            Bitmap img = new Bitmap(r.Width, r.Height);

            graphView.Export(ref img, r, true);

            string path = graph.FullPath.Replace(".Simulations.", string.Empty);
            string fileName = Path.Combine(folder, path + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return fileName;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns>A list of series names.</returns>
        public string[] GetSeriesNames()
        {
            return SeriesDefinitions.Select(s => s.Title).ToArray();
        }

        public List<string> SimulationFilter { get; set; }

        /// <summary>The current presenter</summary>
        public IPresenter CurrentPresenter { get; set; }

        /// <summary>
        /// Iff set to true, the legend will appear inside the graph boundaries.
        /// </summary>
        public bool LegendInsideGraph
        {
            get
            {
                return graphView.LegendInsideGraph;
            }
            set
            {
                graphView.LegendInsideGraph = value;
            }
        }

        /// <summary>Draws the specified series definition on the view.</summary>
        /// <param name="definition">The definition.</param>
        private void DrawOnView(SeriesDefinition definition)
        {
            if (graph.DisabledSeries == null ||
                !graph.DisabledSeries.Contains(definition.Title))
            {
                try
                {
                    Color colour = definition.Colour;
                    // If dark theme is active, and colour is black, use white instead.
                    // This won't help at all if the colour is a dark grey.
                    if (Utility.Configuration.Settings.DarkTheme && colour.R == 0 && colour.G == 0 && colour.B == 0)
                        colour = Color.White;
                    // Create the series and populate it with data.
                    if (definition.Type == SeriesType.Bar)
                    {
                        graphView.DrawBar(
                                          definition.Title,
                                          definition.X,
                                          definition.Y,
                                          definition.XAxis,
                                          definition.YAxis,
                                          colour,
                                          definition.ShowInLegend);
                    }
                    else if (definition.Type == SeriesType.Scatter)
                    {
                        graphView.DrawLineAndMarkers(
                                                    definition.Title,
                                                    definition.X,
                                                    definition.Y,
                                                    definition.XFieldName,
                                                    definition.YFieldName,
                                                    definition.XError,
                                                    definition.YError,
                                                    definition.XAxis,
                                                    definition.YAxis,
                                                    colour,
                                                    definition.Line,
                                                    definition.Marker,
                                                    definition.LineThickness,
                                                    definition.MarkerSize,
                                                    definition.MarkerModifier,
                                                    definition.ShowInLegend);
                    }
                    else if (definition.Type == SeriesType.Region)
                    {
                        graphView.DrawRegion(
                                            definition.Title,
                                            definition.X,
                                            definition.Y,
                                            definition.X2,
                                            definition.Y2,
                                            definition.XAxis,
                                            definition.YAxis,
                                            colour,
                                            definition.ShowInLegend);
                    }
                    else if (definition.Type == SeriesType.Area)
                    {
                        graphView.DrawArea(
                            definition.Title,
                            definition.X,
                            definition.Y,
                            definition.XAxis,
                            definition.YAxis,
                            colour,
                            definition.ShowInLegend);
                    }
                    else if (definition.Type == SeriesType.StackedArea)
                    {
                        graphView.DrawStackedArea(
                            definition.Title,
                            definition.X.Cast<object>().ToArray(),
                            definition.Y.Cast<double>().ToArray(),
                            definition.XAxis,
                            definition.YAxis,
                            colour,
                            definition.ShowInLegend);
                    }
                    else if (definition.Type == SeriesType.Box)
                    {
                        graphView.DrawBoxPLot(definition.Title,
                            definition.X.Cast<object>().ToArray(),
                            definition.Y.Cast<double>().ToArray(),
                            definition.XAxis,
                            definition.YAxis,
                            definition.Colour,
                            definition.ShowInLegend,
                            definition.Line,
                            definition.Marker,
                            definition.LineThickness);
                    }
                }
                catch (Exception err)
                {
                    throw new Exception($"Unable to draw graph {graph.FullPath}", err);
                }
            }
        }

        /// <summary>Draws the specified series definition on the view.</summary>
        /// <param name="annotations">The list of annotations</param>
        private void DrawOnView(IEnumerable<IAnnotation> annotations)
        {
            double minimumX = graphView.AxisMinimum(Axis.AxisType.Bottom) * 1.01;
            double maximumX = graphView.AxisMaximum(Axis.AxisType.Bottom);
            double minimumY = graphView.AxisMinimum(Axis.AxisType.Left);
            double maximumY = graphView.AxisMaximum(Axis.AxisType.Left);
            double lowestAxisScale = Math.Min(minimumX, minimumY);
            double largestAxisScale = Math.Max(maximumX, maximumY);
            for (int i = 0; i < annotations.Count(); i++)
            {
                IAnnotation annotation = annotations.ElementAt(i);
                if (annotation is TextAnnotation textAnnotation)
                {
                    if (textAnnotation.x is double && ((double)textAnnotation.x) == double.MinValue)
                    {
                        double interval = (largestAxisScale - lowestAxisScale) / 8; // fit 10 annotations on graph.

                        double yPosition = largestAxisScale - (i * interval);
                        graphView.DrawText(
                                            textAnnotation.text, 
                                            minimumX, 
                                            yPosition,
                                            textAnnotation.leftAlign, 
                                            textAnnotation.textRotation,
                                            Axis.AxisType.Bottom, 
                                            Axis.AxisType.Left,
                                            Utility.Configuration.Settings.DarkTheme ? Color.White : textAnnotation.colour);
                    }
                    else
                    {
                        graphView.DrawText(
                                            textAnnotation.text, 
                                            textAnnotation.x, 
                                            textAnnotation.y,
                                            textAnnotation.leftAlign, 
                                            textAnnotation.textRotation,
                                            Axis.AxisType.Bottom, 
                                            Axis.AxisType.Left,
                                            Utility.Configuration.Settings.DarkTheme ? Color.White : textAnnotation.colour);
                    }
                }
                else if (annotation is LineAnnotation lineAnnotation)
                {
                    graphView.DrawLine(
                                        lineAnnotation.x1, 
                                        lineAnnotation.y1,
                                        lineAnnotation.x2, 
                                        lineAnnotation.y2,
                                        lineAnnotation.type, 
                                        lineAnnotation.thickness,
                                        Utility.Configuration.Settings.DarkTheme ? Color.White : lineAnnotation.colour,
                                        lineAnnotation.InFrontOfSeries,
                                        lineAnnotation.ToolTip);
                }
                else
                    throw new Exception($"Unknown annotation type {annotation.GetType()}");
            }
        }

        /// <summary>Format the specified axis.</summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxis(Models.Axis axis)
        {
            string title = axis.Title;
            if (axis.Title == null || axis.Title == string.Empty)
            {
                // Work out a default title by going through all series and getting the
                // X or Y field name depending on whether 'axis' is an x axis or a y axis.
                HashSet<string> titles = new HashSet<string>();
                HashSet<string> variableNames = new HashSet<string>();
                foreach (SeriesDefinition definition in SeriesDefinitions)
                {
                    if (definition.X != null && definition.XAxis == axis.Type && definition.XFieldName != null)
                    {
                        IEnumerator enumerator = definition.X.GetEnumerator();
                        if (enumerator.MoveNext())
                            axis.DateTimeAxis = enumerator.Current.GetType() == typeof(DateTime);
                        string xName = definition.XFieldName;
                        if (!variableNames.Contains(xName))
                        {
                            variableNames.Add(xName);
                            if (definition.XFieldUnits != null)
                                xName = xName + " (" + definition.XFieldUnits + ")";

                            titles.Add(xName);
                        }
                    }

                    if (definition.Y != null && definition.YAxis == axis.Type && definition.YFieldName != null)
                    {
                        IEnumerator enumerator = definition.Y.GetEnumerator();
                        if (enumerator.MoveNext())
                            axis.DateTimeAxis = enumerator.Current.GetType() == typeof(DateTime);
                        string yName = definition.YFieldName;
                        if (!variableNames.Contains(yName))
                        {
                            variableNames.Add(yName);
                            if (definition.YFieldUnits != null)
                                yName = yName + " (" + definition.YFieldUnits + ")";

                            titles.Add(yName);
                        }
                    }
                }

                // Create a default title by appending all 'names' together.
                title = StringUtilities.BuildString(titles.ToArray(), Environment.NewLine);
            }

            graphView.FormatAxis(axis.Type, title, axis.Inverted, axis.Minimum, axis.Maximum, axis.Interval, axis.CrossesAtZero);
        }
        
        /// <summary>The graph model has changed.</summary>
        /// <param name="model">The model.</param>
        private void OnGraphModelChanged(object model)
        {
            if (model == graph || graph.FindAllDescendants().Contains(model) || graph.Axis.Contains(model))
                DrawGraph();
        }

        /// <summary>User has clicked an axis.</summary>
        /// <param name="axisType">Type of the axis.</param>
        private void OnAxisClick(Axis.AxisType axisType)
        {
            if (CurrentPresenter != null)
            {
                CurrentPresenter.Detach();
            }

            AxisPresenter axisPresenter = new AxisPresenter();
            CurrentPresenter = axisPresenter;
            AxisView a = new AxisView(graphView as GraphView);
            string dimension = (axisType == Axis.AxisType.Left || axisType == Axis.AxisType.Right) ? "Y" : "X";
            graphView.ShowEditorPanel(a.MainWidget, dimension + "-Axis options");
            axisPresenter.Attach(GetAxis(axisType), a, explorerPresenter);
        }

        /// <summary>User has clicked a footer.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCaptionClick(object sender, EventArgs e)
        {
            if (CurrentPresenter != null)
            {
                CurrentPresenter.Detach();
            }

            TitlePresenter titlePresenter = new TitlePresenter();
            CurrentPresenter = titlePresenter;
            titlePresenter.ShowCaption = true;

            TitleView t = new TitleView(graphView as GraphView);
            graphView.ShowEditorPanel(t.MainWidget, "Title options");
            titlePresenter.Attach(graph, t, explorerPresenter);
        }

        /// <summary>Get an axis</summary>
        /// <param name="axisType">Type of the axis.</param>
        /// <returns>Return the axis</returns>
        /// <exception cref="System.Exception">Cannot find axis with type:  + axisType.ToString()</exception>
        private object GetAxis(Axis.AxisType axisType)
        {
            foreach (Axis a in graph.Axis)
            {
                if (a.Type.ToString() == axisType.ToString())
                {
                    return a;
                }
            }

            throw new Exception("Cannot find axis with type: " + axisType.ToString());
        }

        /// <summary>The axis has changed</summary>
        /// <param name="axis">The axis.</param>
        private void OnAxisChanged(Axis axis)
        {
            DrawGraph();
        }

        /// <summary>User has clicked the legend.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnLegendClick(object sender, LegendClickArgs e)
        {
            if (CurrentPresenter != null)
            {
                CurrentPresenter.Detach();
            }

            LegendPresenter presenter = new LegendPresenter(this);
            CurrentPresenter = presenter;

            LegendView view = new LegendView(graphView as GraphView);
            graphView.ShowEditorPanel(view.MainWidget, "Legend options");
            presenter.Attach(graph, view, explorerPresenter);
        }

        /// <summary>User has hovered over a point on the graph.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnHoverOverPoint(object sender, EventArguments.HoverPointArgs e)
        {
            // Find the correct series.
            foreach (SeriesDefinition definition in SeriesDefinitions)
            {
                if (definition.Title == e.SeriesName)
                {
                    e.HoverText = GetSimulationNameForPoint(e.X, e.Y);
                    if (e.HoverText == null)
                    {
                        e.HoverText = e.SeriesName;
                    }

                    return;
                }
            }
        }

        /// <summary>User has clicked "copy graph" menu item.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void CopyGraphToClipboard(object sender, EventArgs e)
        {
            graphView.ExportToClipboard();
        }

        /// <summary>User has clicked "Include In Documentation" menu item.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void IncludeInDocumentationClicked(object sender, EventArgs e)
        {
            graph.IncludeInDocumentation = !graph.IncludeInDocumentation; // toggle
            this.graphView.AddContextOption("Include in auto-documentation?", IncludeInDocumentationClicked, graph.IncludeInDocumentation);
        }

        /// <summary>
        /// Look for the row in data that has the specified x and y. 
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <returns>The simulation name of the row</returns>
        private string GetSimulationNameForPoint(double x, double y)
        {
            foreach (SeriesDefinition definition in SeriesDefinitions)
            {
                if (definition.SimulationNamesForEachPoint != null)
                {
                    IEnumerator xEnum = definition.X.GetEnumerator();
                    IEnumerator yEnum = definition.Y.GetEnumerator();
                    IEnumerator simNameEnum = definition.SimulationNamesForEachPoint.GetEnumerator();

                    while (xEnum.MoveNext() && yEnum.MoveNext() && simNameEnum.MoveNext())
                    {
                        object rowX = xEnum.Current;
                        object rowY = yEnum.Current;

                        if (rowX is double && rowY is double &&
                            MathUtilities.FloatsAreEqual(x, (double)rowX) &&
                            MathUtilities.FloatsAreEqual(y, (double)rowY))
                        {
                            object simulationName = simNameEnum.Current;
                            if (simulationName != null)
                            {
                                return simulationName.ToString();
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
