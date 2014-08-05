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

namespace UserInterface.Presenters
{
    class GraphPresenter : IPresenter, IExportable
    {
        private IGraphView GraphView;
        private Graph Graph;
        private ExplorerPresenter ExplorerPresenter;
        private Models.DataStore DataStore;
        private IPresenter CurrentPresenter = null;
        private static Color[] colours = {
                                        Color.FromArgb(228,26,28),
                                        Color.FromArgb(55,126,184),
                                        Color.FromArgb(77,175,74),
                                        Color.FromArgb(152,78,163),
                                        Color.FromArgb(255,127,0),
                                        Color.FromArgb(255,255,51),
                                        Color.FromArgb(166,86,40),
                                        Color.FromArgb(247,129,191),
                                        Color.FromArgb(153,153,153),
                                        Color.FromArgb(251,180,174),
                                        Color.FromArgb(179,205,227),
                                        Color.FromArgb(204,235,197),
                                        Color.FromArgb(222,203,228),
                                        Color.FromArgb(254,217,166),
                                        Color.FromArgb(255,255,204),
                                        Color.FromArgb(229,216,189),
                                        Color.FromArgb(253,218,236),
                                        Color.FromArgb(242,242,242)                                         
                                         };

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
                foreach (Models.Graph.Series S in Graph.Series)
                {
                    int seriesNumber = 1;
                
                    if (S.X != null && S.Y != null)
                    {
                        // We need to handle the case where the series needs to be duplicated
                        // for each simulation in scope.
                        bool duplicateForEachSimulation = S.SeparateSeriesForAllSimulationsInScope;
                        List<string> simulationNames = new List<string>();
                        if (duplicateForEachSimulation)
                        {
                            // get all simulation names in scope.
                            string[] simulationNamesInScope = FindSimulationNamesInScope();

                            foreach (string simulationName in simulationNamesInScope)
                                simulationNames.Add(simulationName);
                        }
                        else
                            simulationNames.Add("*");

                        string seriesTitle = S.Title;
                        Color seriesColour = S.Colour;
                        for (int i = 0; i < simulationNames.Count; i++)
                        {
                            string simulationName = simulationNames[i];

                            // If this is a multi simulation series then choose colour.
                            if (simulationNames.Count> 1)
                                seriesColour = ChooseColour(seriesNumber - 1);

                            // If this is a wildcard series then add the simulation name to the
                            // title of the series.
                            if (duplicateForEachSimulation && simulationNames.Count > 1)
                                seriesTitle = S.Title + " [" + simulationNames[i] + "]";

                            // If ShowInLegend is false then blank the series title.
                            if (!S.ShowInLegend)
                            {
                                seriesTitle = string.Empty;
                            }

                            // Get data.
                            IEnumerable x = GetData(simulationName, S.X.TableName, S.X.FieldName);
                            IEnumerable y = GetData(simulationName, S.Y.TableName, S.Y.FieldName);

                            // Create the series and populate it with data.
                            if (S.Type == Models.Graph.Series.SeriesType.Bar)
                                GraphView.DrawBar(seriesTitle, x, y, S.XAxis, S.YAxis, seriesColour);

                            else if (S.Type == Series.SeriesType.Line || S.Type == Series.SeriesType.Scatter)
                            {
                                GraphView.DrawLineAndMarkers(seriesTitle, x, y, S.XAxis, S.YAxis, seriesColour,
                                                             S.Line, S.Marker);
                            }
                            else if (S.X2 != null && S.Y2 != null)
                            {
                                // Get extra data for area series.
                                IEnumerable x2 = GetData(simulationName, S.X2.TableName, S.X2.FieldName);
                                IEnumerable y2 = GetData(simulationName, S.Y2.TableName, S.Y2.FieldName);

                                GraphView.DrawArea(seriesTitle, x, y, x2, y2, S.XAxis, S.YAxis, seriesColour);
                            }
                            if (S.ShowRegressionLine)
                                AddRegressionLine(seriesNumber, seriesTitle, x, y, S.XAxis, S.YAxis, seriesColour);
                            
                            
                            seriesNumber++;
                        }


                    }
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
        private string[] FindSimulationNamesInScope()
        {
            Model parent = FindParent();
            if (parent is Experiment)
                return (parent as Experiment).Names();
            else if (parent is Simulation)
                return new string[1] { parent.Name };
            else if (parent is Folder)
            {
                List<string> names = new List<string>();
                foreach (Model model in parent.Children.AllRecursively)
                {
                    if (model is Simulation)
                    {
                        names.Add(model.Name);
                    }
                    else if (model is Experiment)
                    {
                        names.AddRange((model as Experiment).Names());
                    }
                }
                return names.ToArray();
            }
            else
                return Graph.DataStore.SimulationNames;
        }

        private Model FindParent()
        {
            Model parent = Graph;
            while (parent != null && parent.GetType() != typeof(Simulation) &&
                                     parent.GetType() != typeof(Experiment) &&
                                     parent.GetType() != typeof(Folder) &&
                                     parent.GetType() != typeof(Simulations))
                parent = parent.Parent;
            return parent;
        }

        /// <summary>
        /// Add a regresion line, 1:1 line and regression stats to the graph.
        /// </summary>
        private void AddRegressionLine(int seriesNumber, string seriesTitle, IEnumerable x, IEnumerable y, Axis.AxisType xAxisType, Axis.AxisType yAxisType, Color colour)
        {
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
                    double yPosition = largestAxisScale - (seriesNumber+1) * interval;

                    string equation = "y = " + stats.m.ToString("f2") + " x + " + stats.c.ToString("f2") + "\r\n"
                                     + "r2 = " + stats.R2.ToString("f2") + "\r\n"
                                     + "n = " + stats.n.ToString() + "\r\n"
                                     + "NSE = " + stats.NSE.ToString("f2") + "\r\n"
                                     + "ME = " + stats.ME.ToString("f2") + "\r\n"
                                     + "MAE = " + stats.MAE.ToString("f2") + "\r\n"
                                     + "RSR = " + stats.RSR.ToString("f2") + "\r\n"
                                     + "RMSD = " + stats.RMSD.ToString("f2");
                    GraphView.DrawText(equation, lowestAxisScale, yPosition, xAxisType, yAxisType, colour);
                }
            }
        }



        /// <summary>
        /// Return values to caller.
        /// </summary>
        public IEnumerable GetData(string simulationName, string tableName, string fieldName)
        {
            if (tableName == null && fieldName != null)
            {
                // Use reflection to access a property.
                object Obj = Graph.Variables.Get(fieldName);
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
        /// Export the contents of this graph to the specified file.
        /// </summary>
        public string ConvertToHtml(string folder)
        {
            Rectangle r = new Rectangle(0, 0, 600, 600);
            Bitmap img = new Bitmap(r.Width, r.Height);

            GraphView.Export(img);

            string fileName = Path.Combine(folder, Graph.Name + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return "<img src=\"" + Graph.Name + ".png" + "\"/>";
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
            if (colourNumber >= colours.Length)
                Math.DivRem(colourNumber, colours.Length, out colourNumber);
            return colours[colourNumber];


        }


    }
}
