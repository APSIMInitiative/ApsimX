using System;
using UserInterface.Views;
using Models.Graph;
using Models.Core;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Collections;
using Models.Factorial;

namespace UserInterface.Presenters
{
    class GraphPresenter : IPresenter
    {
        private IGraphView GraphView;
        private Graph Graph;
        private ExplorerPresenter ExplorerPresenter;
        private Models.DataStore DataStore = new Models.DataStore();
        private static Color[] colours = {Color.FromArgb(255,255,204),
                                          Color.FromArgb(217,240,163),
                                          Color.FromArgb(173,221,142),
                                          Color.FromArgb(120,198,121),
                                          Color.FromArgb(65,171,93),
                                          Color.FromArgb(35,132,67),
                                          Color.FromArgb(0,104,55),
                                          Color.FromArgb(0,90,50),
                                          Color.FromArgb(255,255,212),
                                          Color.FromArgb(254,227,145),
                                          Color.FromArgb(254,196,79),
                                          Color.FromArgb(254,153,41),
                                          Color.FromArgb(236,112,20),
                                          Color.FromArgb(204,76,2),
                                          Color.FromArgb(140,45,4)};

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
            ExplorerPresenter.CommandHistory.ModelChanged += OnGraphModelChanged;

            // Connect to a datastore.
            string dbName = Path.ChangeExtension(explorerPresenter.ApsimXFile.FileName, ".db");
            DataStore.Connect(dbName, true);

            DrawGraph();
        }


        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            GraphView.OnAxisClick -= OnAxisClick;
            GraphView.OnPlotClick -= OnPlotClick;
            GraphView.OnLegendClick -= OnLegendClick;
            GraphView.OnTitleClick -= OnTitleClick;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnGraphModelChanged;
            DataStore.Disconnect();
        }

        /// <summary>
        /// Draw the graph on the screen.
        /// </summary>
        private void DrawGraph()
        {
            GraphView.Clear();
            if (Graph != null && Graph.Series != null)
            {
                int seriesNumber = 1;
                foreach (Models.Graph.Series S in Graph.Series)
                {
                    if (S.X != null && S.Y != null)
                    {
                        // We need to handle the case where series X and Y SimulationName member
                        // may be a wildcard '*'. If found then it duplicates the creating of
                        // graph series for each simulation found in the datastore.
                        bool simulationWildCard = S.X.SimulationName == "*";
                        List<string> simulationNames = new List<string>();
                        if (simulationWildCard)
                        {
                            // get all simulation names in scope.
                            string[] simulationNamesInScope = FindSimulationNamesInScope();

                            foreach (string simulationName in simulationNamesInScope)
                                simulationNames.Add(simulationName);
                        }
                        else
                            simulationNames.Add(S.X.SimulationName);

                        string seriesTitle = S.Title;
                        Color seriesColour = S.Colour;
                        for (int i = 0; i < simulationNames.Count; i++)
                        {
                            string simulationName = simulationNames[i];

                            // If this is a multi simulation series then choose colour.
                            seriesColour = ChooseColour(seriesNumber - 1);

                            // If this is a wildcard series then add the simulation name to the
                            // title of the series.
                            if (simulationWildCard)
                                seriesTitle = S.Title + " [" + simulationNames[i] + "]";

                            // Get data.
                            IEnumerable x = GetData(simulationName, S.X.TableName, S.X.FieldName);
                            IEnumerable y = GetData(simulationName, S.Y.TableName, S.Y.FieldName);

                            // Create the series and populate it with data.
                            if (S.Type == Models.Graph.Series.SeriesType.Bar)
                                GraphView.DrawBar(seriesTitle, x, y, S.XAxis, S.YAxis, seriesColour);

                            else
                                GraphView.DrawLineAndMarkers(seriesTitle, x, y, S.XAxis, S.YAxis, seriesColour,
                                                             S.Line, S.Marker);

                            if (S.ShowRegressionLine)
                                AddRegressionLine(seriesNumber, seriesTitle, x, y, S.XAxis, S.YAxis, seriesColour);
                            
                            
                            seriesNumber++;
                        }


                    }
                }

                // Format the axes.
                foreach (Models.Graph.Axis A in Graph.Axes)
                    GraphView.FormatAxis(A.Type, A.Title, A.Inverted);

                // Format the legend.
                GraphView.FormatLegend(Graph.LegendPosition);

                // Format the title
                GraphView.FormatTitle(Graph.Title);

                GraphView.Refresh();
            }

        }

        /// <summary>
        /// Return a list of simulation names in scope.
        /// </summary>
        /// <returns></returns>
        private string[] FindSimulationNamesInScope()
        {
            Model parent = FindParent();
            if (parent is Experiment)
                return (parent as Experiment).Names();
            else if (parent is Simulation)
                return new string[1] { parent.Name };
            else
                return Graph.DataStore.SimulationNames;
        }

        private Model FindParent()
        {
            Model parent = Graph;
            while (parent != null && parent.GetType() != typeof(Simulation) &&
                                     parent.GetType() != typeof(Experiment) &&
                                     parent.GetType() != typeof(Simulations))
                parent = parent.Parent;
            return parent;
        }

        /// <summary>
        /// Add a regresion line, 1:1 line and regression stats to the graph.
        /// </summary>
        private void AddRegressionLine(int seriesNumber, string seriesTitle, IEnumerable x, IEnumerable y, Axis.AxisType xAxisType, Axis.AxisType yAxisType, Color colour)
        {
            Utility.Math.RegrStats stats = Utility.Math.CalcRegressionStats(x, y);
            if (stats != null)
            {
                // Show the regression line.
                double minimumX = Utility.Math.Min(x);
                double maximumX = Utility.Math.Max(x);
                double[] regressionX = new double[] { minimumX, maximumX };
                double[] regressionY = new double[] { stats.m * minimumX + stats.c, stats.m * maximumX + stats.c };
                GraphView.DrawLineAndMarkers("", regressionX, regressionY,
                                             xAxisType, yAxisType, colour,  
                                             Series.LineType.Solid, Series.MarkerType.None);

                // Show the 1:1 line
                double minimumY = Utility.Math.Min(y);
                double maximumY = Utility.Math.Max(y);
                double lowestAxisScale = Math.Min(minimumX, minimumY);
                double largestAxisScale = Math.Max(maximumX, maximumY);
                double[] oneToOne = new double[] { lowestAxisScale, largestAxisScale };
                GraphView.DrawLineAndMarkers("", oneToOne, oneToOne,
                                             xAxisType, yAxisType, colour,
                                             Series.LineType.Dash, Series.MarkerType.None);

                // Draw the equation.
                double interval = (largestAxisScale - lowestAxisScale) / 20;
                double yPosition = largestAxisScale - seriesNumber * interval;

                string equation = "y = " + stats.m.ToString("f2") + " x + " + stats.c.ToString("f2") + "\r\n"
                                 + "r2 = " + stats.R2.ToString("f2") + "\r\n"
                                 + "n = " + stats.n.ToString() + "\r\n"
                                 + "RMSD = " + stats.RMSD.ToString("f2");
                GraphView.DrawText(equation, minimumX, yPosition, xAxisType, yAxisType, colour);
            }
        }



        /// <summary>
        /// Return values to caller.
        /// </summary>
        public IEnumerable GetData(string simulationName, string tableName, string fieldName)
        {
            if (simulationName == null && tableName == null && fieldName != null)
            {
                // Use reflection to access a property.
                object Obj = Graph.Get(fieldName);
                if (Obj != null && Obj.GetType().IsArray)
                    return Obj as Array;
            }
            else if (tableName != null && fieldName != null)
            {
                System.Data.DataTable DataSource = DataStore.GetData(simulationName, tableName);
                if (DataSource != null && fieldName != null && DataSource.Columns[fieldName] != null)
                {
                    if (DataSource.Columns[fieldName].DataType == typeof(DateTime))
                        return Utility.DataTable.GetColumnAsDates(DataSource, fieldName);
                    else if (DataSource.Columns[fieldName].DataType == typeof(string))
                        return Utility.DataTable.GetColumnAsStrings(DataSource, fieldName);
                    else
                        return Utility.DataTable.GetColumnAsDoubles(DataSource, fieldName);
                }
            }
            return null;
        }






        /// <summary>
        /// The graph model has changed.
        /// </summary>
        private void OnGraphModelChanged(object Model)
        {
            if (Graph.Axes.Count >= 2 &&
                (Model == Graph || Model == Graph.Axes[0] || Model == Graph.Axes[1]))
                DrawGraph();
        }

        /// <summary>
        /// User has clicked an axis.
        /// </summary>
        private void OnAxisClick(OxyPlot.Axes.AxisPosition AxisPosition)
        {
            AxisPresenter AxisPresenter = new AxisPresenter();
            AxisView A = new AxisView();
            GraphView.ShowEditorPanel(A);
            AxisPresenter.Attach(GetAxis(AxisPosition), A, ExplorerPresenter);
        }

        /// <summary>
        /// User has clicked the plot area.
        /// </summary>
        private void OnPlotClick()
        {
            SeriesPresenter SeriesPresenter = new SeriesPresenter();
            SeriesView SeriesView = new SeriesView();
            GraphView.ShowEditorPanel(SeriesView);
            SeriesPresenter.Attach(Graph, SeriesView, ExplorerPresenter);
        }

        /// <summary>
        /// User has clicked a title.
        /// </summary>
        private void OnTitleClick()
        {
            TitlePresenter titlePresenter = new TitlePresenter();
            TitleView t = new TitleView();
            GraphView.ShowEditorPanel(t);
            titlePresenter.Attach(Graph, t, ExplorerPresenter);
        }

        /// <summary>
        /// Get an axis 
        /// </summary>
        private object GetAxis(OxyPlot.Axes.AxisPosition AxisType)
        {
            foreach (Axis A in Graph.Axes)
                if (A.Type.ToString() == AxisType.ToString())
                    return A;
            throw new Exception("Cannot find axis with type: " + AxisType.ToString());
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
        void OnLegendClick()
        {
            LegendPresenter presenter = new LegendPresenter();
            LegendView view = new LegendView();
            GraphView.ShowEditorPanel(view);
            presenter.Attach(Graph, view, ExplorerPresenter);
        }



        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        private static Color ChangeColorBrightness(Color color, double correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= (float)correctionFactor;
                green *= (float)correctionFactor;
                blue *= (float)correctionFactor;
            }
            else
            {
                red = (float)((255 - red) * correctionFactor + red);
                green = (float)((255 - green) * correctionFactor + green);
                blue = (float)((255 - blue) * correctionFactor + blue);
            }

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        private static Color ChooseColour(int colourNumber)
        {
            return colours[colourNumber];


        }


    }
}
