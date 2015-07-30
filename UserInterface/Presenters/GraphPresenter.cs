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
    /// <summary>
    /// A presenter for a graph.
    /// </summary>
    class GraphPresenter : IPresenter, IExportable
    {
        /// <summary>The graph view</summary>
        private IGraphView graphView;

        /// <summary>The graph</summary>
        private Graph graph;

        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The current presenter</summary>
        private IPresenter currentPresenter = null;

        /// <summary>The series definitions to show on graph.</summary>
        private List<SeriesDefinition> seriesDefinitions = new List<SeriesDefinition>();

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.graph = model as Graph;
            this.graphView = view as GraphView;
            this.explorerPresenter = explorerPresenter;

            graphView.OnAxisClick += OnAxisClick;
            graphView.OnLegendClick += OnLegendClick;
            graphView.OnCaptionClick += OnCaptionClick;
            graphView.OnHoverOverPoint += OnHoverOverPoint;
            explorerPresenter.CommandHistory.ModelChanged += OnGraphModelChanged;
            this.graphView.AddContextAction("Copy graph to clipboard", false, CopyGraphToClipboard);
            this.graphView.AddContextAction("Include in auto-documentation?", graph.IncludeInDocumentation, IncludeInDocumentationClicked);

            DrawGraph();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            if (currentPresenter != null)
                currentPresenter.Detach();
            graphView.OnAxisClick -= OnAxisClick;
            graphView.OnLegendClick -= OnLegendClick;
            graphView.OnCaptionClick -= OnCaptionClick;
            graphView.OnHoverOverPoint -= OnHoverOverPoint;
            explorerPresenter.CommandHistory.ModelChanged -= OnGraphModelChanged;
        }

        /// <summary>Draw the graph on the screen.</summary>
        public void DrawGraph()
        {
            graphView.Clear();
            if (graph != null && graph.Series != null)
            {
                // Get a list of series definitions.
                seriesDefinitions = graph.GetDefinitionsToGraph();
                foreach (SeriesDefinition definition in seriesDefinitions)
                    DrawOnView(definition);

                // Update axis maxima and minima
                graphView.UpdateView();

                // Get a list of series annotations.
                DrawOnView(graph.GetAnnotationsToGraph());

                // Format the axes.
                foreach (Models.Graph.Axis A in graph.Axes)
                    FormatAxis(A);

                // Format the legend.
                graphView.FormatLegend(graph.LegendPosition);

                // Format the title
                graphView.FormatTitle(graph.Name);

                //Format the footer
                if (graph.Caption == string.Empty)
                    graphView.FormatCaption("Double click to add a caption", true);
                else
                    graphView.FormatCaption(graph.Caption, false);

                // Remove series titles out of the graph disabled series list when
                // they are no longer valid i.e. not on the graph.
                IEnumerable<string> validSeriesTitles = this.seriesDefinitions.Select(s => s.title);
                List<string> seriesTitlesToKeep = new List<string>(validSeriesTitles.Intersect(this.graph.DisabledSeries));
                this.graph.DisabledSeries.Clear();
                this.graph.DisabledSeries.AddRange(seriesTitlesToKeep);

                graphView.Refresh();
            }
        }

        /// <summary>Draws the specified series definition on the view.</summary>
        /// <param name="definition">The definition.</param>
        private void DrawOnView(SeriesDefinition definition)
        {
            if (!graph.DisabledSeries.Contains(definition.title))
            {
                // Create the series and populate it with data.
                if (definition.type == SeriesType.Bar)
                    graphView.DrawBar(definition.title, definition.x, definition.y,
                                      definition.xAxis, definition.yAxis, definition.colour, definition.showInLegend);

                else if (definition.type == SeriesType.Scatter)
                    graphView.DrawLineAndMarkers(definition.title, definition.x, definition.y, 
                                                 definition.xAxis, definition.yAxis, definition.colour,
                                                 definition.line, definition.marker, definition.showInLegend);
                
                else if (definition.type == SeriesType.Area)
                    graphView.DrawArea(definition.title, definition.x, definition.y, definition.x2, definition.y2,
                                       definition.xAxis, definition.yAxis, definition.colour, definition.showInLegend);
            }
        }

        /// <summary>Draws the specified series definition on the view.</summary>
        /// <param name="definition">The definition.</param>
        private void DrawOnView(List<Annotation> annotations)
        {
            double minimumX = graphView.AxisMinimum(Axis.AxisType.Bottom);
            double maximumX = graphView.AxisMaximum(Axis.AxisType.Bottom);
            double minimumY = graphView.AxisMinimum(Axis.AxisType.Left);
            double maximumY = graphView.AxisMaximum(Axis.AxisType.Left);
            double majorStepY = graphView.AxisMajorStep(Axis.AxisType.Left);
            double lowestAxisScale = Math.Min(minimumX, minimumY);
            double largestAxisScale = Math.Max(maximumX, maximumY);
            
            for (int i = 0; i < annotations.Count; i++)
            {
                int numLines = StringUtilities.CountSubStrings(annotations[i].text, "\r\n") + 1;
                double interval = (largestAxisScale - lowestAxisScale) / 10; // fit 10 annotations on graph.

                double yPosition = largestAxisScale - i * interval;
                graphView.DrawText(annotations[i].text, minimumX, yPosition, Axis.AxisType.Bottom, Axis.AxisType.Left, annotations[i].colour);
            }
        }

        /// <summary>Format the specified axis.</summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxis(Models.Graph.Axis axis)
        {
            string title = axis.Title;
            if (axis.Title == null || axis.Title == string.Empty)
            {
                // Work out a default title by going through all series and getting the
                // X or Y field name depending on whether 'axis' is an x axis or a y axis.
                HashSet<string> names = new HashSet<string>();

                foreach (SeriesDefinition definition in seriesDefinitions)
                {
                    if (definition.x != null && definition.xAxis == axis.Type && definition.xFieldName != null)
                        names.Add(definition.xFieldName);
                    if (definition.y != null && definition.yAxis == axis.Type && definition.yFieldName != null)
                        names.Add(definition.yFieldName);
                }

                // Create a default title by appending all 'names' together.
                title = StringUtilities.BuildString(names.ToArray(), ", ");
            }
            graphView.FormatAxis(axis.Type, title, axis.Inverted, axis.Minimum, axis.Maximum, axis.Interval);
        }

        /// <summary>Export the contents of this graph to the specified file.</summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public string ConvertToHtml(string folder)
        {
            Rectangle r = new Rectangle(0, 0, 800, 500);
            Bitmap img = new Bitmap(r.Width, r.Height);

            graphView.Export(img, true);

            string fileName = Path.Combine(folder, graph.Name + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            string html = "<img class=\"graph\" src=\"" + fileName + "\"/>";
            if (this.graph.Caption != null)
                html += "<p>" + this.graph.Caption + "</p>";
            return html;
        }

        /// <summary>Export the contents of this graph to the specified file.</summary>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        public string ExportToPDF(string folder)
        {
            Rectangle r = new Rectangle(0, 0, 800, 500);
            Bitmap img = new Bitmap(r.Width, r.Height);

            graphView.Export(img, true);

            string fileName = Path.Combine(folder, graph.Name + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return fileName;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns>A list of series names.</returns>
        public string[] GetSeriesNames()
        {
            return seriesDefinitions.Select(s => s.title).ToArray();
        }

        /// <summary>The graph model has changed.</summary>
        /// <param name="Model">The model.</param>
        private void OnGraphModelChanged(object Model)
        {
            DrawGraph();
        }

        /// <summary>User has clicked an axis.</summary>
        /// <param name="axisType">Type of the axis.</param>
        private void OnAxisClick(Axis.AxisType axisType)
        {
            AxisPresenter AxisPresenter = new AxisPresenter();
            currentPresenter = AxisPresenter;
            AxisView A = new AxisView();
            graphView.ShowEditorPanel(A);
            AxisPresenter.Attach(GetAxis(axisType), A, explorerPresenter);
        }

        /// <summary>User has clicked a footer.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCaptionClick(object sender, EventArgs e)
        {
            TitlePresenter titlePresenter = new TitlePresenter();
            currentPresenter = titlePresenter;
            titlePresenter.ShowCaption = true;

            TitleView t = new TitleView();
            graphView.ShowEditorPanel(t);
            titlePresenter.Attach(graph, t, explorerPresenter);
        }

        /// <summary>Get an axis</summary>
        /// <param name="axisType">Type of the axis.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot find axis with type:  + axisType.ToString()</exception>
        private object GetAxis(Axis.AxisType axisType)
        {
            foreach (Axis A in graph.Axes)
                if (A.Type.ToString() == axisType.ToString())
                    return A;
            throw new Exception("Cannot find axis with type: " + axisType.ToString());
        }

        /// <summary>The axis has changed</summary>
        /// <param name="Axis">The axis.</param>
        private void OnAxisChanged(Axis Axis)
        {
            DrawGraph();
        }

        /// <summary>User has clicked the legend.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnLegendClick(object sender, LegendClickArgs e)
        {
            
            LegendPresenter presenter = new LegendPresenter(this);
            currentPresenter = presenter;

            LegendView view = new LegendView();
            graphView.ShowEditorPanel(view);
            presenter.Attach(graph, view, explorerPresenter);
    }

        /// <summary>User has hovered over a point on the graph.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnHoverOverPoint(object sender, EventArguments.HoverPointArgs e)
        {
            // Find the correct series.
            foreach (SeriesDefinition definition in seriesDefinitions)
            {
                if (definition.title == e.SeriesName)
                {
                    e.HoverText = GetSimulationNameForPoint(e.X, e.Y);
                    return;
                }
            }
        }

        /// <summary>User has clicked "copy graph" menu item.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void CopyGraphToClipboard(object sender, EventArgs e)
        {
            // Set the clipboard text.
            Bitmap bitmap = new Bitmap(800, 600);
            graphView.Export(bitmap, false);
            System.Windows.Forms.Clipboard.SetImage(bitmap);
        }

        /// <summary>User has clicked "Include In Documentation" menu item.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void IncludeInDocumentationClicked(object sender, EventArgs e)
        {
            graph.IncludeInDocumentation = !graph.IncludeInDocumentation; // toggle
            this.graphView.AddContextAction("Include in auto-documentation?", graph.IncludeInDocumentation, IncludeInDocumentationClicked);
        }

        /// <summary>
        /// Look for the row in data that has the specified x and y. 
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <returns>The simulation name of the row</returns>
        private string GetSimulationNameForPoint(double x, double y)
        {
            foreach (SeriesDefinition definition in seriesDefinitions)
            {
                if (definition.simulationNamesForEachPoint != null)
                {
                    IEnumerator xEnum = definition.x.GetEnumerator();
                    IEnumerator yEnum = definition.y.GetEnumerator();
                    IEnumerator simNameEnum = definition.simulationNamesForEachPoint.GetEnumerator();

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
                                return simulationName.ToString();
                        }
                    }
                }
            }
            return null;
        }
    }
}
