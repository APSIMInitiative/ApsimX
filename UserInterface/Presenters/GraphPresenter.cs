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
                FillSeriesInfo();

                foreach (SeriesInfo seriesInfo in seriesMetadata)
                {
                    seriesInfo.DrawOnView(this.GraphView);
                }

                // Format the axes.
                foreach (Models.Graph.Axis A in Graph.Axes)
                    FormatAxis(A);

                // Format the legend.
                GraphView.FormatLegend(Graph.LegendPosition);

                // Format the title
                GraphView.FormatTitle(Graph.Title);

                GraphView.Refresh();
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
                {
                    // We need to handle the case where the series needs to be duplicated
                    // for each simulation or experiment in scope.
                    bool duplicateForEachSimulation = S.SplitOn == "Simulation";
                    bool duplicateForEachExperiment = S.SplitOn == "Experiment";
                    if (duplicateForEachSimulation)
                    {
                        // get all simulation names in scope.
                        string[] simulationNamesInScope = FindSimulationNamesInScope();

                        foreach (string simulationName in simulationNamesInScope)
                        {
                            int seriesIndex = Array.IndexOf(simulationNamesInScope, simulationName);
                            
                            SeriesInfo info = new SeriesInfo(
                                graph: Graph,
                                dataStore: DataStore,
                                series: S,
                                title: simulationName, 
                                filter: "Name = '" + simulationName + "'",
                                seriesIndex: seriesIndex,
                                numSeries: simulationNamesInScope.Length);

                            if (simulationNamesInScope.Length == 1)
                            {
                                info.Title = S.Y.FieldName;
                            }
                            seriesMetadata.Add(info);
                        }
                    }
                    else if (duplicateForEachExperiment)
                    {
                            
                        // Find all experiments.
                        Experiment[] experimentsInScope = FindExperimentsInScope();

                        foreach (Experiment experiment in experimentsInScope)
                        {
                            int seriesIndex = Array.IndexOf(experimentsInScope, experiment);
                                
                            SeriesInfo info = new SeriesInfo(
                                graph: Graph,
                                dataStore: DataStore,
                                series: S, 
                                title: experiment.Name,
                                filter: "NAME IN " +
                                        "(" + Utility.String.Build(experiment.Names(), delimiter: ",", prefix: "'", suffix: "'") + ")",
                                seriesIndex: seriesIndex,
                                numSeries: experimentsInScope.Length);
                            seriesMetadata.Add(info);
                        }
                    }
                    else
                    {
                        SeriesInfo info = new SeriesInfo(
                                graph: Graph,
                                dataStore: DataStore,
                                series: S,
                                title: S.Title,
                                filter: null,
                                seriesIndex: 1,
                                numSeries: 1);
                        seriesMetadata.Add(info);
                    }
                }
            }
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
                title = Utility.String.BuildString(names.ToArray(), ", ");
            }
            GraphView.FormatAxis(axis.Type, title, axis.Inverted);
        }

        /// <summary>
        /// Return a list of simulation names in scope.
        /// </summary>
        /// <returns></returns>
        /// <param name="splitOn">The axis to format</param>
        private string[] FindSimulationNamesInScope()
        {
            Type[] parentTypes = new Type[] { typeof(Simulation), /*typeof(Folder),*/ typeof(Experiment), typeof(Simulations) };

            Model parent = FindParent(parentTypes);
            if (parent is Experiment)
                return (parent as Experiment).Names();
            else if (parent is Simulation)
                return new string[1] { parent.Name };
            //else if (parent is Folder)
            //{
            //    List<string> names = new List<string>();
            //    foreach (Model model in parent.Children.AllRecursively)
            //    {
            //        if (model is Simulation)
            //        {
            //            names.Add(model.Name);
            //        }
            //        else if (model is Experiment)
            //        {
            //            names.AddRange((model as Experiment).Names());
            //        }
            //    }
            //    return names.ToArray();
            //}
            else
                return Graph.DataStore.SimulationNames;
        }

        /// <summary>
        /// Find all experiments in scope
        /// </summary>
        /// <returns></returns>
        private Experiment[] FindExperimentsInScope()
        {
            Type[] parentTypes = new Type[] { typeof(Experiment), typeof(Simulations) };
            Model parent = FindParent(parentTypes);
            if (parent is Experiment)
            {
                return new Experiment[] { parent as Experiment };
            }
            else
            {
                List<Experiment> experiments = new List<Experiment>();
                foreach (Experiment experiment in parent.Children.AllRecursivelyMatching(typeof(Experiment)))
                {
                    experiments.Add(experiment);
                }

                return experiments.ToArray();
            }
        }

        /// <summary>
        /// Find a parent model of one of the specified types
        /// </summary>
        /// <param name="types">The types to look for</param>
        /// <returns>The found parent model</returns>
        private Model FindParent(Type[] types)
        {
            Model parent = Graph;
            while (parent != null && Array.IndexOf(types, parent.GetType()) == -1)
                parent = parent.Parent;
            return parent;
        }


        /// <summary>
        /// Export the contents of this graph to the specified file.
        /// </summary>
        public string ConvertToHtml(string folder)
        {
            Rectangle r = new Rectangle(0, 0, 600, 600);
            Bitmap img = new Bitmap(r.Width, r.Height);

            GraphView.Export(img);

            string fileName = Path.Combine(folder, Graph.Name + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return "<img class=\"graph\" src=\"" + Graph.Name + ".png" + "\"/>";
        }

        /// <summary>
        /// The graph model has changed.
        /// </summary>
        private void OnGraphModelChanged(object Model)
        {
            if (Graph.Axes.Count >= 2 &&
                (Model == Graph || Model == Graph.Axes[0] || Model == Graph.Axes[1] || Model is Series))
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
        private void OnLegendClick(object sender, EventArgs e)
        {
            LegendPresenter presenter = new LegendPresenter();
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
            System.Windows.Forms.Clipboard.SetText(this.Graph.Serialise());
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

                // If showInLegend is false then blank the series title.
                if (!series.ShowInLegend)
                {
                    Title = string.Empty;
                }
            }

            /// <summary>
            /// The associated graph series.
            /// </summary>
            private Series series;

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
                    return GetData(series.Y);
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
                // Create the series and populate it with data.
                if (series.Type == Models.Graph.Series.SeriesType.Bar)
                {
                    graphView.DrawBar(Title, X, Y, series.XAxis, series.YAxis, Colour);
                }

                else if (series.Type == Series.SeriesType.Line || series.Type == Series.SeriesType.Scatter)
                {
                    graphView.DrawLineAndMarkers(Title, X, Y, series.XAxis, series.YAxis, Colour,
                                                 series.Line, series.Marker);
                }
                else if (X2 != null && Y2 != null)
                {
                    graphView.DrawArea(Title, X, Y, X2, Y2, series.XAxis, series.YAxis, Colour);
                }

                if (series.ShowRegressionLine)
                    AddRegressionLine(graphView);
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
                            if (Utility.Math.FloatsAreEqual(x, (double)rowX) &&
                                Utility.Math.FloatsAreEqual(y, (double)rowY))
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
                if (graphValues.TableName == null && graphValues.FieldName != null)
                {
                    // Use reflection to access a property.
                    object Obj = graph.Get(graphValues.FieldName);
                    if (Obj != null && Obj.GetType().IsArray)
                        return Obj as Array;
                }
                else if (graphValues.TableName != null && graphValues.FieldName != null)
                {
                    // Create the data if we haven't already
                    if (this.data == null)
                    {
                        this.data = this.dataStore.GetFilteredData(graphValues.TableName, filter);
                    }
                    
                    // If the field exists in our data table then return it.
                    if (this.data != null && graphValues.FieldName != null && this.data.Columns[graphValues.FieldName] != null)
                    {
                        if (this.data.Columns[graphValues.FieldName].DataType == typeof(DateTime))
                        {
                            return Utility.DataTable.GetColumnAsDates(this.data, graphValues.FieldName);
                        }
                        else if (this.data.Columns[graphValues.FieldName].DataType == typeof(string))
                        {
                            return Utility.DataTable.GetColumnAsStrings(this.data, graphValues.FieldName);
                        }
                        else
                        {
                            return Utility.DataTable.GetColumnAsDoubles(this.data, graphValues.FieldName);
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Add a regresion line, 1:1 line and regression stats to the graph.
            /// </summary>
            private void AddRegressionLine(IGraphView graphView)
            {
                IEnumerable x = X;
                IEnumerable y = Y;

                if (x != null && y != null)
                {
                    Utility.Math.RegrStats stats = Utility.Math.CalcRegressionStats(x, y);
                    if (stats != null)
                    {
                        // Show the regression line.
                        double minimumX = Utility.Math.Min(x);
                        double maximumX = Utility.Math.Max(x);
                        double[] regressionX = new double[] { minimumX, maximumX };
                        double[] regressionY = new double[] { stats.m * minimumX + stats.c, stats.m * maximumX + stats.c };
                        graphView.DrawLineAndMarkers("", regressionX, regressionY,
                                                     series.XAxis, series.YAxis, Colour,
                                                     Series.LineType.Solid, Series.MarkerType.None);

                        // Show the 1:1 line
                        double minimumY = Utility.Math.Min(y);
                        double maximumY = Utility.Math.Max(y);
                        double lowestAxisScale = Math.Min(minimumX, minimumY);
                        double largestAxisScale = Math.Max(maximumX, maximumY);
                        double[] oneToOne = new double[] { lowestAxisScale, largestAxisScale };
                        graphView.DrawLineAndMarkers("", oneToOne, oneToOne,
                                                     series.XAxis, series.YAxis, Colour,
                                                     Series.LineType.Dash, Series.MarkerType.None);

                        // Draw the equation.
                        double interval = (largestAxisScale - lowestAxisScale) / 20;
                        double yPosition = largestAxisScale - (seriesIndex + 1) * interval;

                        string equation = "y = " + stats.m.ToString("f2") + " x + " + stats.c.ToString("f2") + "\r\n"
                                         + "r2 = " + stats.R2.ToString("f2") + "\r\n"
                                         + "n = " + stats.n.ToString() + "\r\n"
                                         + "NSE = " + stats.NSE.ToString("f2") + "\r\n"
                                         + "ME = " + stats.ME.ToString("f2") + "\r\n"
                                         + "MAE = " + stats.MAE.ToString("f2") + "\r\n"
                                         + "RSR = " + stats.RSR.ToString("f2") + "\r\n"
                                         + "RMSD = " + stats.RMSD.ToString("f2");
                        graphView.DrawText(equation, lowestAxisScale, yPosition, series.XAxis, series.YAxis, Colour);
                    }
                }
            }
        }
    }
}
