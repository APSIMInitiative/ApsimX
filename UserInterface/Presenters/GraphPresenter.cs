using System;
using UserInterface.Views;
using Models.Graph;
using Models.Core;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Linq;
using Models.Factorial;
using UserInterface.Interfaces;
using System.Data;
using Models;
using UserInterface.EventArguments;
using Models.Soils;
using APSIM.Shared.Utilities;

namespace UserInterface.Presenters
{
    class GraphPresenter : IPresenter, IExportable
    {
        private IGraphView GraphView;
        private Graph Graph;
        private ExplorerPresenter ExplorerPresenter;
        private Models.DataStore DataStore;
        private IPresenter CurrentPresenter = null;
        private List<SeriesInfo> seriesMetadata = new List<SeriesInfo>();

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Graph = Model as Graph;
            GraphView = View as GraphView;
            ExplorerPresenter = explorerPresenter;

            GraphView.OnAxisClick += OnAxisClick;
            GraphView.OnPlotClick += OnPlotClick;
            GraphView.OnLegendClick += OnLegendClick;
            GraphView.OnTitleClick += OnTitleClick;
            GraphView.OnCaptionClick += OnCaptionClick;
            GraphView.OnHoverOverPoint += OnHoverOverPoint;
            ExplorerPresenter.CommandHistory.ModelChanged += OnGraphModelChanged;
            this.GraphView.AddContextAction("Copy graph XML to clipboard", CopyGraphXML);

            // Connect to a datastore.
            DataStore = new Models.DataStore(Graph);

            DrawGraph();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            if (CurrentPresenter != null)
            {
                CurrentPresenter.Detach();
            }
            GraphView.OnAxisClick -= OnAxisClick;
            GraphView.OnPlotClick -= OnPlotClick;
            GraphView.OnLegendClick -= OnLegendClick;
            GraphView.OnTitleClick -= OnTitleClick;
            GraphView.OnCaptionClick -= OnCaptionClick;
            GraphView.OnHoverOverPoint -= OnHoverOverPoint;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnGraphModelChanged;
            DataStore.Disconnect();
        }

        /// <summary>
        /// Draw the graph on the screen.
        /// </summary>
        public void DrawGraph()
        {
            GraphView.Clear();
            if (Graph != null && Graph.Series != null)
            {
                // Create all series.
                FillSeriesInfo();
                foreach (SeriesInfo seriesInfo in seriesMetadata)
                    seriesInfo.DrawOnView(this.GraphView);

                // Format the axes.
                foreach (Models.Graph.Axis A in Graph.Axes)
                    FormatAxis(A);

                // Add any regression lines if necessary.
                AddRegressionLines();

                // Format the legend.
                GraphView.FormatLegend(Graph.LegendPosition);

                // Format the title
                GraphView.FormatTitle(Graph.Title);

                //Format the footer
                GraphView.FormatCaption(Graph.Caption);

                // Remove series titles out of the graph disabled series list when
                // they are no longer valid i.e. not on the graph.
                IEnumerable<string> validSeriesTitles = this.seriesMetadata.Select(s => s.Title);
                List<string> seriesTitlesToKeep = new List<string>(validSeriesTitles.Intersect(this.Graph.DisabledSeries));
                this.Graph.DisabledSeries.Clear();
                this.Graph.DisabledSeries.AddRange(seriesTitlesToKeep);

                GraphView.Refresh();
            }
        }

        /// <summary>
        /// Add in regression lines if necessary.
        /// </summary>
        private void AddRegressionLines()
        {
            int seriesIndex = 0;
            if (this.Graph.ShowRegressionLine)
            {
                // Get all x and y values.
                List<double> x = new List<double>();
                List<double> y = new List<double>();
                foreach (SeriesInfo seriesInfo in seriesMetadata)
                {
                    if (seriesInfo.X != null && seriesInfo.Y != null)
                    {
                        foreach (double value in seriesInfo.X)
                            x.Add(value);
                        foreach (double value in seriesInfo.Y)
                            y.Add(value);
                    }
                }

                MathUtilities.RegrStats stats = MathUtilities.CalcRegressionStats(x, y);
                AddRegressionToGraph(this.GraphView, stats, Axis.AxisType.Bottom, Axis.AxisType.Left,
                                        Color.Black, seriesIndex);
                seriesIndex++;
            }

            foreach (SeriesInfo seriesInfo in seriesMetadata)
            {
                if (seriesInfo.X != null && seriesInfo.Y != null && seriesInfo.series.ShowRegressionLine)
                {
                    MathUtilities.RegrStats stats = MathUtilities.CalcRegressionStats(seriesInfo.X, seriesInfo.Y);
                    if (stats != null)
                    {
                        AddRegressionToGraph(this.GraphView, stats, seriesInfo.series.XAxis, seriesInfo.series.YAxis, seriesInfo.Colour, seriesIndex);
                        seriesIndex++;
                    }
                }
            }
        }

        /// <summary>
        /// This method fills the series info list from the graph series.
        /// </summary>
        private void FillSeriesInfo()
        {
            seriesMetadata.Clear();
            foreach (Models.Graph.Series S in Graph.Series)
            {
                if (S.X != null && S.Y != null)
                    this.FillSeriesInfoFromSeries(S);
            }
        }

        /// <summary>
        /// Fill series information list from the specified series.
        /// </summary>
        /// <param name="series">The series to use</param>
        private void FillSeriesInfoFromSeries(Series series)
        {
            Simulation parentSimulation = Apsim.Parent(this.Graph, typeof(Simulation)) as Simulation;
            if (this.Graph.Parent is Soil)
            {
                FillSeriesInfoFromRawSeries(series);
                return;
            }

            // See if graph is inside a simulation. If so then graph the simulation.
            if (parentSimulation != null)
            {
                FillSeriesInfoFromSimulations(new string[] { parentSimulation.Name }, series);
                return;
            }

            // See if graph is inside an experiment. If so then graph all series in experiment.
            Experiment parentExperiment = Apsim.Parent(this.Graph, typeof(Experiment)) as Experiment;
            if (parentExperiment != null)
            {
                FillSeriesInfoFromSimulations(parentExperiment.Names(), series);
                return;
            }

            // Must be in a folder or at top level.
            IModel[] experiments;
            IModel[] simulations;

            if (this.Graph.Parent is Simulations)
            {
                simulations = Apsim.ChildrenRecursively(this.Graph.Parent, typeof(Simulation)).ToArray();
                experiments = Apsim.ChildrenRecursively(this.Graph.Parent, typeof(Experiment)).ToArray();
            }
            else
            {
                experiments = Apsim.FindAll(this.Graph, typeof(Experiment)).ToArray();
                simulations = Apsim.FindAll(this.Graph, typeof(Simulation)).ToArray();
            }

            if (experiments.Length > 0)
            {
                FillSeriesInfoFromExperiments(experiments, series);
                for (int i = 0; i < this.seriesMetadata.Count; i++)
                    this.seriesMetadata[i].Title = experiments[i].Name;
            }

            if (simulations.Length > 0)
            {
                int i = this.seriesMetadata.Count;
                IEnumerable<string> simulationNames = simulations.Select(s => s.Name);
                FillSeriesInfoFromSimulations(simulationNames.ToArray(), series);
                int j = i;
                while (i < this.seriesMetadata.Count)
                {
                    seriesMetadata[i].Title = simulations[i - j].Name;
                    i++;
                }
            }

        }

        /// <summary>
        /// Fill series information list from a list of simulation names.
        /// </summary>
        /// <param name="simulationNames">List of simulation names</param>
        /// <param name="series">Associated series</param>
        private void FillSeriesInfoFromSimulations(string[] simulationNames, Series series)
        {
            int seriesIndex = 0;
            foreach (string simulationName in simulationNames)
            {
                string[] zones = GetZones(series.X.TableName, simulationName);

                int numSeries;
                if (zones.Length > 1)
                    numSeries = zones.Length;
                else
                    numSeries = simulationNames.Length;

                foreach (string zoneName in zones)
                {
                    string filter = "Name = '" + simulationName + "'";
                    if (zones.Length > 1)
                        filter += " and ZoneName = '" + zoneName + "'";

                    string title;
                    if (zones.Length > 1)
                        title = zoneName;
                    else
                        title = simulationName;

                    SeriesInfo info = new SeriesInfo(
                        graph: Graph,
                        dataStore: DataStore,
                        series: series,
                        title: title,
                        filter: filter,
                        seriesIndex: seriesIndex,
                        numSeries: numSeries);

                    if (!(this.Graph.Parent is Soil) && numSeries == 1)
                        info.Title = series.Y.FieldName;

                    seriesMetadata.Add(info);
                    seriesIndex++;
                }
            }
        }

        /// <summary>
        /// Get a list of zone names for the specified simulation and table.
        /// </summary>
        /// <param name="tableName">The table name in the DataStore to search.</param>
        /// <param name="simulationName">The simulation name.</param>
        /// <returns>A list of zone names.</returns>
        private string[] GetZones(string tableName, string simulationName)
        {
            // Get a list of zones in this simulation.
            List<string> zones = null;
            DataTable simulationData = DataStore.GetData(simulationName, tableName);
            if (simulationData != null && simulationData.Columns.Contains("ZoneName"))
            {
                zones = DataTableUtilities.GetDistinctValues(simulationData, "ZoneName");
            }

            if (zones == null || zones.Count == 0)
                return new string[] { "*" };
            else
                return zones.ToArray();
        }

        /// <summary>
        /// Fill series information list from a list of simulation names.
        /// </summary>
        /// <param name="simulationNames">List of simulation names</param>
        /// <param name="series">Associated series</param>
        private void FillSeriesInfoFromExperiments(IModel[] experiments, Series series)
        {
            foreach (Experiment experiment in experiments)
            {
                string filter = "NAME IN " + "(" + StringUtilities.Build(experiment.Names(), delimiter: ",", prefix: "'", suffix: "'") + ")";

                int seriesIndex = Array.IndexOf(experiments, experiment);

                SeriesInfo info = new SeriesInfo(
                    graph: Graph,
                    dataStore: DataStore,
                    series: series,
                    title: experiment.Name,
                    filter: filter,
                    seriesIndex: seriesIndex,
                    numSeries: experiments.Length);

                if (experiments.Length == 1)
                {
                    info.Title = series.Y.FieldName;
                }
                seriesMetadata.Add(info);
            }
        }

        /// <summary>
        /// Fill series information list from just the raw series
        /// </summary>
        /// <param name="simulationNames">List of simulation names</param>
        /// <param name="series">Associated series</param>
        private void FillSeriesInfoFromRawSeries(Series series)
        {
            SeriesInfo info = new SeriesInfo(
                graph: Graph,
                dataStore: DataStore,
                series: series,
                title: series.Title,
                filter: null,
                seriesIndex: 1,
                numSeries: 1);
            seriesMetadata.Add(info);
        }

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxis(Models.Graph.Axis axis)
        {
            string title = axis.Title;
            if (axis.Title == null || axis.Title == string.Empty)
            {
                // Work out a default title by going through all series and getting the
                // X or Y field name depending on whether 'axis' is an x axis or a y axis.
                HashSet<string> names = new HashSet<string>();

                foreach (Series series in Graph.Series)
                {
                    if (series.X != null && series.XAxis == axis.Type)
                    {
                        names.Add(series.X.FieldName);
                    }
                    if (series.Y != null && series.YAxis == axis.Type)
                    {
                        names.Add(series.Y.FieldName);
                    }
                }

                // Create a default title by appending all 'names' together.
                title = StringUtilities.BuildString(names.ToArray(), ", ");
            }
            GraphView.FormatAxis(axis.Type, title, axis.Inverted, axis.Minimum, axis.Maximum, axis.Interval);
        }

        /// <summary>
        /// Return a list of simulations or experiments in scope to graph
        /// </summary>
        /// <returns>The list of simulations or experiments</returns>
        private IModel[] FindSomethingToGraph()
        {
            // See if graph is inside an experiment. If so return it.
            IModel parentExperiment = Apsim.Parent(this.Graph, typeof(Experiment)) as Experiment;
            if (parentExperiment != null)
                return new IModel[] { parentExperiment };

            // See if graph is inside a simulation. If so return it.
            IModel parentSimulation = Apsim.Parent(this.Graph, typeof(Simulation)) as Simulation;
            if (parentSimulation != null)
                return new IModel[] { parentSimulation };
            
            // Must be at top level so look for experiments first.
            IModel[] experiments = Apsim.FindAll(this.Graph, typeof(Experiment)).ToArray();
            if (experiments.Length > 0)
                return experiments;

            // If we get this far then simply graph every simulation we can find.
            return Apsim.FindAll(this.Graph, typeof(Simulation)).ToArray();
        }

        /// <summary>
        /// Export the contents of this graph to the specified file.
        /// </summary>
        public string ConvertToHtml(string folder)
        {
            Rectangle r = new Rectangle(0, 0, 800, 500);
            Bitmap img = new Bitmap(r.Width, r.Height);

            GraphView.Export(img);

            string fileName = Path.Combine(folder, Graph.Name + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            string html = "<img class=\"graph\" src=\"" + fileName + "\"/>";
            if (this.Graph.Caption != null)
                html += "<p>" + this.Graph.Caption + "</p>";
            return html;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns></returns>
        public string[] GetSeriesNames()
        {
            IEnumerable<string> validSeriesTitles = this.seriesMetadata.Select(s => s.Title);

            return validSeriesTitles.ToArray();
        }

        /// <summary>
        /// The graph model has changed.
        /// </summary>
        private void OnGraphModelChanged(object Model)
        {
            DrawGraph();
        }

        /// <summary>
        /// User has clicked an axis.
        /// </summary>
        private void OnAxisClick(Axis.AxisType axisType)
        {
            AxisPresenter AxisPresenter = new AxisPresenter();
            CurrentPresenter = AxisPresenter;
            AxisView A = new AxisView();
            GraphView.ShowEditorPanel(A);
            AxisPresenter.Attach(GetAxis(axisType), A, ExplorerPresenter);
        }

        /// <summary>
        /// User has clicked the plot area.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnPlotClick(object sender, EventArgs e)
        {
            SeriesPresenter SeriesPresenter = new SeriesPresenter();
            CurrentPresenter = SeriesPresenter; 
            SeriesView SeriesView = new SeriesView();
            GraphView.ShowEditorPanel(SeriesView);
            SeriesPresenter.Attach(Graph, SeriesView, ExplorerPresenter);
        }

        /// <summary>
        /// User has clicked a title.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnTitleClick(object sender, EventArgs e)
        {
            TitlePresenter titlePresenter = new TitlePresenter();
            CurrentPresenter = titlePresenter; 
            
            TitleView t = new TitleView();
            GraphView.ShowEditorPanel(t);
            titlePresenter.Attach(Graph, t, ExplorerPresenter);
        }

        /// <summary>
        /// User has clicked a footer.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCaptionClick(object sender, EventArgs e)
        {
            TitlePresenter titlePresenter = new TitlePresenter();
            CurrentPresenter = titlePresenter;
            titlePresenter.ShowCaption = true;

            TitleView t = new TitleView();
            GraphView.ShowEditorPanel(t);
            titlePresenter.Attach(Graph, t, ExplorerPresenter);
        }

        /// <summary>
        /// Get an axis 
        /// </summary>
        private object GetAxis(Axis.AxisType axisType)
        {
            foreach (Axis A in Graph.Axes)
                if (A.Type.ToString() == axisType.ToString())
                    return A;
            throw new Exception("Cannot find axis with type: " + axisType.ToString());
        }

        /// <summary>
        /// The axis has changed 
        /// </summary>
        private void OnAxisChanged(Axis Axis)
        {
            DrawGraph();
        }

        /// <summary>
        /// User has clicked the legend.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnLegendClick(object sender, LegendClickArgs e)
        {
            
            LegendPresenter presenter = new LegendPresenter(this);
            CurrentPresenter = presenter;

            LegendView view = new LegendView();
            GraphView.ShowEditorPanel(view);
            presenter.Attach(Graph, view, ExplorerPresenter);
    }

        /// <summary>
        /// User has hovered over a point on the graph.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnHoverOverPoint(object sender, EventArguments.HoverPointArgs e)
        {
            // Find the correct series.
            foreach (SeriesInfo series in this.seriesMetadata)
            {
                if (series.Title == e.SeriesName)
                {
                    e.HoverText = series.GetSimulationNameForPoint(e.X, e.Y);
                    return;
                }
            }
        }

        /// <summary>
        /// User has clicked "copy graph xml" menu item.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void CopyGraphXML(object sender, EventArgs e)
        {
            // Set the clipboard text.
            System.Windows.Forms.Clipboard.SetText(Apsim.Serialise(this.Graph));
        }

        /// <summary>
        /// Add regression line and stats to graph
        /// </summary>
        /// <param name="graphView">The graph to add line and stats to</param>
        /// <param name="stats">The regression stats</param>
        /// <param name="xAxis">The associated x axis</param>
        /// <param name="yAxis">The associated y axis</param>
        /// <param name="colour">The color to use</param>
        /// <param name="seriesIndex">The series index</param>
        private static void AddRegressionToGraph(IGraphView graphView, 
                                                 MathUtilities.RegrStats stats,
                                                 Axis.AxisType xAxis, Axis.AxisType yAxis,
                                                 Color colour,
                                                 int seriesIndex)
        {
            if (stats != null)
            {
                double minimumX = graphView.AxisMinimum(xAxis);
                double maximumX = graphView.AxisMaximum(xAxis);
                double minimumY = graphView.AxisMinimum(yAxis);
                double maximumY = graphView.AxisMaximum(yAxis);

                double[] regressionX = new double[] { minimumX, maximumX };
                double[] regressionY = new double[] { stats.m * minimumX + stats.c, stats.m * maximumX + stats.c };
                graphView.DrawLineAndMarkers("", regressionX, regressionY,
                                             xAxis, yAxis, colour,
                                             Series.LineType.Solid, Series.MarkerType.None, true);

                // Show the 1:1 line
                double lowestAxisScale = Math.Min(minimumX, minimumY);
                double largestAxisScale = Math.Max(maximumX, maximumY);
                double[] oneToOne = new double[] { lowestAxisScale, largestAxisScale };
                graphView.DrawLineAndMarkers("", oneToOne, oneToOne,
                                             xAxis, yAxis, colour,
                                             Series.LineType.Dash, Series.MarkerType.None, true);

                // Draw the equation.
                double interval = (largestAxisScale - lowestAxisScale) / 13;
                double yPosition = largestAxisScale - seriesIndex * interval;

                string equation = string.Format("y = {0:F2} x + {1:F2}, r2 = {2:F2}, n = {3:F0}\r\n" +
                                                "NSE = {4:F2}, ME = {5:F2}, MAE = {6:F2}\r\n" +
                                                "RSR = {7:F2}, RMSD = {8:F2}",
                                                new object[] {stats.m,
                                                                          stats.c,
                                                                          stats.R2,
                                                                          stats.n,
                                                                          stats.NSE,
                                                                          stats.ME,
                                                                          stats.MAE,
                                                                          stats.RSR,
                                                                          stats.RMSD});
                graphView.DrawText(equation, lowestAxisScale, yPosition, xAxis, yAxis, colour);
            }
        }

        /// <summary>
        /// A class for holding series information and data.
        /// </summary>
        private class SeriesInfo
        {
            /// <summary>
            /// The graph model
            /// </summary>
            private Graph graph;

            /// <summary>
            /// The data store
            /// </summary>
            private DataStore dataStore;

            /// <summary>
            /// The series index
            /// </summary>
            private int seriesIndex;
            
            /// <summary>
            /// The number of series.
            /// </summary>
            private int numSeries;

            /// <summary>
            /// Gets or set the filter used to get the data.
            /// </summary>
            private string filter;

            /// <summary>
            /// Gets the data for the series.
            /// </summary>
            private DataTable data;

            /// <summary>
            /// Initializes a new instance of the <see cref="SeriesInfo" /> class.
            /// </summary>
            public SeriesInfo(
                Graph graph,
                DataStore dataStore,
                Series series,
                string title, 
                string filter,
                int seriesIndex, int numSeries)
            {
                this.graph = graph;
                this.dataStore = dataStore;
                this.series = series;
                this.Title = title;
                this.filter = filter;
                this.seriesIndex = seriesIndex;
                this.numSeries = numSeries;
            }

            /// <summary>
            /// The associated graph series.
            /// </summary>
            public Series series { get; set; }

            /// <summary>
            /// Gets the series title.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// Gets the series colour.
            /// </summary>
            public Color Colour 
            { 
                get
                {
                    // Colour from a palette?
                    if (numSeries > 1)
                    {
                        return Utility.Colour.ChooseColour(seriesIndex);
                    }
                    else
                    {
                        return series.Colour;
                    }
                }
            }

            /// <summary>
            /// Get the x values
            /// </summary>
            public IEnumerable X
            {
                get
                {
                    return GetData(series.X);
                }
            }

            /// <summary>
            /// Get the y values
            /// </summary>
            public IEnumerable Y
            {
                get
                {
                    IEnumerable data = GetData(series.Y);
                    if (series.Cumulative)
                    {
                        data = MathUtilities.Cumulative((IEnumerable<double>)data);
                    }

                    return data;
                }
            }

            /// <summary>
            /// Get the x2 values
            /// </summary>
            public IEnumerable X2
            {
                get
                {
                    return GetData(series.X2);
                }
            }

            /// <summary>
            /// Get the y2 values
            /// </summary>
            public IEnumerable Y2
            {
                get
                {
                    return GetData(series.Y2);
                }
            }

            /// <summary>
            /// Draw this series on the view
            /// </summary>
            public void DrawOnView(IGraphView graphView)
            {
                if (series.Type == Series.SeriesType.Line)
                    series.Type = Series.SeriesType.Scatter;

                string title = Title;
                if (series.Title != null && !series.Title.StartsWith("Series") && numSeries == 1)
                    title = series.Title;

                if (!graph.DisabledSeries.Contains(title))
                {
                    // Create the series and populate it with data.
                    if (series.Type == Models.Graph.Series.SeriesType.Bar)
                    {
                        graphView.DrawBar(title, X, Y, series.XAxis, series.YAxis, Colour, series.ShowInLegend);
                    }

                    else if (series.Type == Series.SeriesType.Scatter)
                    {
                        graphView.DrawLineAndMarkers(title, X, Y, series.XAxis, series.YAxis, Colour,
                                                     series.Line, series.Marker, series.ShowInLegend);
                    }
                    else if (X2 != null && Y2 != null)
                    {
                        graphView.DrawArea(title, X, Y, X2, Y2, series.XAxis, series.YAxis, Colour, series.ShowInLegend);
                    }
                }
            }

            /// <summary>
            /// Look for the row in data that has the specified x and y. 
            /// </summary>
            /// <param name="x">The x coordinate</param>
            /// <param name="y">The y coordinate</param>
            /// <returns>The simulation name of the row</returns>
            public string GetSimulationNameForPoint(double x, double y)
            {
                if (this.data != null)
                {
                    foreach (DataRow row in this.data.Rows)
                    {
                        object rowX = row[series.X.FieldName];
                        object rowY = row[series.Y.FieldName];

                        if (rowX is double && rowY is double)
                        {
                            if (MathUtilities.FloatsAreEqual(x, (double)rowX) &&
                                MathUtilities.FloatsAreEqual(y, (double)rowY))
                            {
                                object simulationName = row["SimulationName"];
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

            /// <summary>
            /// Gets the data table for this series.
            /// </summary>
            private IEnumerable GetData(GraphValues graphValues)
            {
                if (graphValues != null && graphValues.TableName == null && graphValues.FieldName != null)
                {
                    // Use reflection to access a property.
                    return graphValues.GetData(this.graph);
                }
                else if (graphValues != null && graphValues.TableName != null && graphValues.FieldName != null)
                {
                    // Create the data if we haven't already
                    if (this.data == null && this.dataStore.TableExists(graphValues.TableName))
                    {
                        this.data = this.dataStore.GetFilteredData(graphValues.TableName, filter);
                    }
                    
                    // If the field exists in our data table then return it.
                    if (this.data != null && graphValues.FieldName != null && this.data.Columns[graphValues.FieldName] != null)
                    {
                        if (this.data.Columns[graphValues.FieldName].DataType == typeof(DateTime))
                        {
                            return DataTableUtilities.GetColumnAsDates(this.data, graphValues.FieldName);
                        }
                        else if (this.data.Columns[graphValues.FieldName].DataType == typeof(string))
                        {
                            return DataTableUtilities.GetColumnAsStrings(this.data, graphValues.FieldName);
                        }
                        else
                        {
                            return DataTableUtilities.GetColumnAsDoubles(this.data, graphValues.FieldName);
                        }
                    }
                }

                return null;
            }
        }
    }
}
