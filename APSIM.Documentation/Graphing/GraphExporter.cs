using Image = System.Drawing.Image;
using APSIM.Shared.Documentation;
using OxyPlot;
using System.IO;
using System;
using APSIM.Shared.Graphing;

using Legend = OxyPlot.Legends.Legend;
using OxyLegendOrientation = OxyPlot.Legends.LegendOrientation;
using OxyLegendPosition = OxyPlot.Legends.LegendPosition;
using OxyLegendPlacement = OxyPlot.Legends.LegendPlacement;
using OxyPlot.SkiaSharp;


namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// This class will export a graph to an image.
    /// </summary>
    /// <remarks>
    /// This class is responsible for graph -> oxyplot and oxyplot -> image conversions.
    /// </remarks>
    public class GraphExporter
    {
        /// <summary>
        /// Points to pixels conversion factor.
        /// </summary>
        private const double pointsToPixels = 96.0 / 72;

        /// <summary>
        /// Font used for labels/headings/legends/etc.
        /// </summary>
        /// <remarks>
        /// todo: make this user configurable.
        /// </remarks>
        private const string font = "Calibri Light";

        /// <summary>
        /// Export a graph to an image.
        /// </summary>
        /// <param name="graph">Graph to be exported.</param>
        /// <param name="width">Desired width of the image (in px).</param>
        /// <param name="height">Desired height of the image (in px).</param>
        public SkiaSharp.SKImage Export(IGraph graph, double width, double height)
        {
            return Export(ToPlotModel(graph), width, height);
        }

        /// <summary>
        /// Export a plot model to an image.
        /// </summary>
        /// <param name="plot">Plot model to be exported.</param>
        /// <param name="width">Desired width of the image (in px).</param>
        /// <param name="height">Desired height of the image (in px).</param>
        public SkiaSharp.SKImage Export(IPlotModel plot, double width, double height)
        {
            using (Stream stream = new MemoryStream())
            {
                PngExporter exporter = new PngExporter();
                exporter.Height = (int)height;
                exporter.Width = (int)width;
                exporter.UseTextShaping = false;
                exporter.Export(plot, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return SkiaSharp.SKImage.FromEncodedData(stream);  
            }
        }

        /// <summary>
        /// Convert the given apsim graph to an oxyplot <see cref="PlotModel"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        public IPlotModel ToPlotModel(IGraph graph)
        {
            try
            {
                if (graph.XAxis == null)
                    throw new NullReferenceException("Graph has no x-axis");
                if (graph.YAxis == null)
                    throw new NullReferenceException("Graph has no y-axis");
                if (graph.Legend == null)
                    throw new NullReferenceException("Graph has no legend configuration");
                if (graph.Series == null)
                    throw new NullReferenceException("Graph has no series");

                PlotModel plot = new PlotModel();

                // Add series to graph.
                AxisLabelCollection labels = AxisLabelCollection.Empty();
                ExportedSeries previous = null;
                AxisRequirements xAxisRequirements = null;
                AxisRequirements yAxisRequirements = null;
                foreach (Series graphSeries in graph.Series)
                {
                    ExportedSeries series = graphSeries.ToOxyPlotSeries(labels);
                    labels = series.AxisLabels;
                    plot.Series.Add(series.Result);
                    if (previous == null)
                        previous = series;
                    else
                    {
                        previous.ThrowIfIncompatibleWith(series);
                        previous = series;
                    }
                    if (series.XAxisRequirements.AxisKind != null)
                        xAxisRequirements = series.XAxisRequirements;
                    if (series.YAxisRequirements.AxisKind != null)
                        yAxisRequirements = series.YAxisRequirements;
                }

                // Axes (don't add them if there are no series to display on the graph).
                if (xAxisRequirements?.AxisKind != null)
                    plot.Axes.Add(graph.XAxis.ToOxyPlotAxis(xAxisRequirements, labels.XLabels));
                if (yAxisRequirements?.AxisKind != null)
                    plot.Axes.Add(graph.YAxis.ToOxyPlotAxis(yAxisRequirements, labels.YLabels));

                // Legend

                plot.Legends.Add(new Legend()
                {
                    LegendOrientation = graph.Legend.Orientation.ToOxyPlotLegendOrientation(),
                    LegendPosition = graph.Legend.Position.ToOxyPlotLegendPosition(),
                    LegendPlacement = graph.Legend.InsideGraphArea ? OxyLegendPlacement.Inside : OxyLegendPlacement.Outside,
                    Font = font,
                });


                // Apply font
                plot.TitleFont = font;
                plot.SetLegendFont(font);
                plot.PlotAreaBorderThickness = new OxyThickness(0);
                plot.Title = graph.Title;

                return plot;
            }
            catch (Exception err)
            {
                var graphName = graph.Title;
                graphName = $"path: {graph.Path} title: {graphName}";
                throw new Exception($"Error found while exporting graph {graphName}", err);
            }

        }
    }
}
