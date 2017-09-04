// -----------------------------------------------------------------------
// <copyright file="GraphPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

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
    using Models.Graph;
    using Views;

    /// <summary>
    /// A presenter for a graph.
    /// </summary>
    public class GraphPresenter : IPresenter, IExportable
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

            this.graphView.OnAxisClick += this.OnAxisClick;
            this.graphView.OnLegendClick += this.OnLegendClick;
            this.graphView.OnCaptionClick += this.OnCaptionClick;
            this.graphView.OnHoverOverPoint += this.OnHoverOverPoint;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnGraphModelChanged;
            this.graphView.AddContextAction("Copy graph to clipboard", this.CopyGraphToClipboard);
            this.graphView.AddContextOption("Include in auto-documentation?", this.IncludeInDocumentationClicked, this.graph.IncludeInDocumentation);

            this.DrawGraph();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnGraphModelChanged;
            if (this.currentPresenter != null)
            {
                this.currentPresenter.Detach();
            }

            this.graphView.OnAxisClick -= this.OnAxisClick;
            this.graphView.OnLegendClick -= this.OnLegendClick;
            this.graphView.OnCaptionClick -= this.OnCaptionClick;
            this.graphView.OnHoverOverPoint -= this.OnHoverOverPoint;
        }

        /// <summary>Draw the graph on the screen.</summary>
        public void DrawGraph()
        {
            this.graphView.Clear();
            if (this.graph != null && this.graph.Series != null)
            {
                // Get a list of series definitions.
                this.seriesDefinitions = this.graph.GetDefinitionsToGraph();
                foreach (SeriesDefinition definition in this.seriesDefinitions)
                {
                    this.DrawOnView(definition);
                }

                // Update axis maxima and minima
                this.graphView.UpdateView();

                // Get a list of series annotations.
                this.DrawOnView(this.graph.GetAnnotationsToGraph());

                // Format the axes.
                foreach (Models.Graph.Axis a in this.graph.Axes)
                {
                    this.FormatAxis(a);
                }

                // Format the legend.
                this.graphView.FormatLegend(this.graph.LegendPosition);

                // Format the title
                this.graphView.FormatTitle(this.graph.Name);

                // Format the footer
                if (string.IsNullOrEmpty(this.graph.Caption))
                {
                    this.graphView.FormatCaption("Double click to add a caption", true);
                }
                else
                {
                    this.graphView.FormatCaption(this.graph.Caption, false);
                }

                // Remove series titles out of the graph disabled series list when
                // they are no longer valid i.e. not on the graph.
                IEnumerable<string> validSeriesTitles = this.seriesDefinitions.Select(s => s.title);
                List<string> seriesTitlesToKeep = new List<string>(validSeriesTitles.Intersect(this.graph.DisabledSeries));
                this.graph.DisabledSeries.Clear();
                this.graph.DisabledSeries.AddRange(seriesTitlesToKeep);

                this.graphView.Refresh();
            }
        }

        /// <summary>Export the contents of this graph to the specified file.</summary>
        /// <param name="folder">The name of the folder</param>
        /// <returns>The HTML string</returns>
        public string ConvertToHtml(string folder)
        {
            Rectangle r = new Rectangle(0, 0, 800, 500);
            Bitmap img = new Bitmap(r.Width, r.Height);

            this.graphView.Export(ref img, r, true);

            string fileName = Path.Combine(folder, this.graph.Name + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            string html = "<img class=\"graph\" src=\"" + fileName + "\"/>";
            if (this.graph.Caption != null)
            {
                html += "<p>" + this.graph.Caption + "</p>";
            }

            return html;
        }

        /// <summary>Export the contents of this graph to the specified file.</summary>
        /// <param name="folder">The folder.</param>
        /// <returns>The filename of the pdf</returns>
        public string ExportToPDF(string folder)
        {
            // The rectange numbers below are optimised for generation of PDF document
            // on a computer that has its display settings at 100%.
            Rectangle r = new Rectangle(0, 0, 600, 450);
            Bitmap img = new Bitmap(r.Width, r.Height);

            this.graphView.Export(ref img, r, true);

            string path = Apsim.FullPath(this.graph).Replace(".Simulations.", string.Empty);
            string fileName = Path.Combine(folder, path + ".png");
            img.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return fileName;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns>A list of series names.</returns>
        public string[] GetSeriesNames()
        {
            return this.seriesDefinitions.Select(s => s.title).ToArray();
        }

        /// <summary>Draws the specified series definition on the view.</summary>
        /// <param name="definition">The definition.</param>
        private void DrawOnView(SeriesDefinition definition)
        {
            if (!this.graph.DisabledSeries.Contains(definition.title))
            {
                // Create the series and populate it with data.
                if (definition.type == SeriesType.Bar)
                {
                    this.graphView.DrawBar(definition.title, definition.x, definition.y, definition.xAxis, definition.yAxis, definition.colour, definition.showInLegend);
                }
                else if (definition.type == SeriesType.Scatter)
                {
                    this.graphView.DrawLineAndMarkers(
                                                definition.title,
                                                definition.x,
                                                definition.y,
                                                definition.xAxis,
                                                definition.yAxis,
                                                definition.colour,
                                                definition.line,
                                                definition.marker,
                                                definition.lineThickness,
                                                definition.markerSize,
                                                definition.showInLegend);
                }
                else if (definition.type == SeriesType.Area)
                {
                    this.graphView.DrawArea(
                                        definition.title,
                                        definition.x,
                                        definition.y,
                                        definition.x2,
                                        definition.y2,
                                        definition.xAxis,
                                        definition.yAxis,
                                        definition.colour,
                                        definition.showInLegend);
                }
            }
        }

        /// <summary>
        /// Draws the specified series definition on the view.
        /// </summary>
        /// <param name="annotations">The list of annotations</param>
        private void DrawOnView(List<Annotation> annotations)
        {
            double minimumX = this.graphView.AxisMinimum(Axis.AxisType.Bottom);
            double maximumX = this.graphView.AxisMaximum(Axis.AxisType.Bottom);
            double minimumY = this.graphView.AxisMinimum(Axis.AxisType.Left);
            double maximumY = this.graphView.AxisMaximum(Axis.AxisType.Left);
            double lowestAxisScale = Math.Min(minimumX, minimumY);
            double largestAxisScale = Math.Max(maximumX, maximumY);

            for (int i = 0; i < annotations.Count; i++)
            {
                if (annotations[i] is TextAnnotation)
                {
                    TextAnnotation textAnnotation = annotations[i] as TextAnnotation;
                    if (textAnnotation.x is double && ((double)textAnnotation.x) == double.MinValue)
                    {
                        double interval = (largestAxisScale - lowestAxisScale) / 10; // fit 10 annotations on graph.

                        double yPosition = largestAxisScale - (i * interval);
                        this.graphView.DrawText(
                                           textAnnotation.text, 
                                           minimumX, 
                                           yPosition,
                                           textAnnotation.leftAlign, 
                                           textAnnotation.textRotation,
                                           Axis.AxisType.Bottom, 
                                           Axis.AxisType.Left, 
                                           textAnnotation.colour);
                    }
                    else
                    {
                        this.graphView.DrawText(
                                           textAnnotation.text, 
                                           textAnnotation.x, 
                                           textAnnotation.y,
                                           textAnnotation.leftAlign, 
                                           textAnnotation.textRotation,
                                           Axis.AxisType.Bottom, 
                                           Axis.AxisType.Left, 
                                           textAnnotation.colour);
                    }
                }
                else
                {
                    LineAnnotation lineAnnotation = annotations[i] as LineAnnotation;

                    this.graphView.DrawLine(
                                       lineAnnotation.x1, 
                                       lineAnnotation.y1,
                                       lineAnnotation.x2, 
                                       lineAnnotation.y2,
                                       lineAnnotation.type, 
                                       lineAnnotation.thickness, 
                                       lineAnnotation.colour);
                }
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

                foreach (SeriesDefinition definition in this.seriesDefinitions)
                {
                    if (definition.x != null && definition.xAxis == axis.Type && definition.xFieldName != null)
                    {
                        string xName = definition.xFieldName;
                        if (definition.xFieldUnits != null)
                        {
                            xName = xName + " " + definition.xFieldUnits;
                        }

                        names.Add(xName);
                    }

                    if (definition.y != null && definition.yAxis == axis.Type && definition.yFieldName != null)
                    {
                        string yName = definition.yFieldName;
                        if (definition.yFieldUnits != null)
                        {
                            yName = yName + " " + definition.yFieldUnits;
                        }

                        names.Add(yName);
                    }
                }

                // Create a default title by appending all 'names' together.
                title = StringUtilities.BuildString(names.ToArray(), ", ");
            }

            this.graphView.FormatAxis(axis.Type, title, axis.Inverted, axis.Minimum, axis.Maximum, axis.Interval);
        }

        /// <summary>The graph model has changed.</summary>
        /// <param name="model">The model.</param>
        private void OnGraphModelChanged(object model)
        {
            this.DrawGraph();
        }

        /// <summary>User has clicked an axis.</summary>
        /// <param name="axisType">Type of the axis.</param>
        private void OnAxisClick(Axis.AxisType axisType)
        {
            if (this.currentPresenter != null)
            {
                this.currentPresenter.Detach();
            }

            AxisPresenter axisPresenter = new AxisPresenter();
            this.currentPresenter = axisPresenter;
            AxisView a = new AxisView(this.graphView as GraphView);
            string dimension = (axisType == Axis.AxisType.Left || axisType == Axis.AxisType.Right) ? "Y" : "X";
            this.graphView.ShowEditorPanel(a.MainWidget, dimension + "-Axis options");
            axisPresenter.Attach(this.GetAxis(axisType), a, this.explorerPresenter);
        }

        /// <summary>User has clicked a footer.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCaptionClick(object sender, EventArgs e)
        {
            if (this.currentPresenter != null)
            {
                this.currentPresenter.Detach();
            }

            TitlePresenter titlePresenter = new TitlePresenter();
            this.currentPresenter = titlePresenter;
            titlePresenter.ShowCaption = true;

            TitleView t = new TitleView(this.graphView as GraphView);
            this.graphView.ShowEditorPanel(t.MainWidget, "Title options");
            titlePresenter.Attach(this.graph, t, this.explorerPresenter);
        }

        /// <summary>Get an axis</summary>
        /// <param name="axisType">Type of the axis.</param>
        /// <returns>The Axis object</returns>
        /// <exception cref="System.Exception">Cannot find axis with type:  + axisType.ToString()</exception>
        private object GetAxis(Axis.AxisType axisType)
        {
            foreach (Axis a in this.graph.Axes)
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
            this.DrawGraph();
        }

        /// <summary>User has clicked the legend.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnLegendClick(object sender, LegendClickArgs e)
        {
            if (this.currentPresenter != null)
            {
                this.currentPresenter.Detach();
            }

            LegendPresenter presenter = new LegendPresenter(this);
            this.currentPresenter = presenter;

            LegendView view = new LegendView(this.graphView as GraphView);
            this.graphView.ShowEditorPanel(view.MainWidget, "Legend options");
            presenter.Attach(this.graph, view, this.explorerPresenter);
        }

        /// <summary>User has hovered over a point on the graph.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnHoverOverPoint(object sender, EventArguments.HoverPointArgs e)
        {
            // Find the correct series.
            foreach (SeriesDefinition definition in this.seriesDefinitions)
            {
                if (definition.title == e.SeriesName)
                {
                    e.HoverText = this.GetSimulationNameForPoint(e.X, e.Y);
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
            this.graphView.ExportToClipboard();
        }

        /// <summary>User has clicked "Include In Documentation" menu item.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void IncludeInDocumentationClicked(object sender, EventArgs e)
        {
            this.graph.IncludeInDocumentation = !this.graph.IncludeInDocumentation; // toggle
            this.graphView.AddContextOption("Include in auto-documentation?", this.IncludeInDocumentationClicked, this.graph.IncludeInDocumentation);
        }

        /// <summary>
        /// Look for the row in data that has the specified x and y. 
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <returns>The simulation name of the row</returns>
        private string GetSimulationNameForPoint(double x, double y)
        {
            foreach (SeriesDefinition definition in this.seriesDefinitions)
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
