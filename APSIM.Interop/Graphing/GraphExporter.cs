using Image = System.Drawing.Image;
using APSIM.Services.Documentation;
using OxyPlot;
using System.IO;
using APSIM.Interop.Utility;
using System;
using APSIM.Services.Graphing;
using APSIM.Interop.Graphing.Extensions;
using System.Collections.Generic;
using System.Linq;
#if NETCOREAPP
using Legend = OxyPlot.Legends.Legend;
using OxyLegendOrientation = OxyPlot.Legends.LegendOrientation;
using OxyLegendPosition = OxyPlot.Legends.LegendPosition;
using OxyLegendPlacement = OxyPlot.Legends.LegendPlacement;
using OxyPlot.SkiaSharp;
#endif

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// This class will export a graph to an image.
    /// </summary>
    /// <remarks>
    /// This class is responsible for graph -> oxyplot and oxyplot -> image conversions.
    /// </remarks>
    public class GraphExporter : IGraphExporter
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
        public Image Export(IGraph graph, double width, double height)
        {
            return Export(ToPlotModel(graph), width, height);
        }

        /// <summary>
        /// Export a plot model to an image.
        /// </summary>
        /// <param name="plot">Plot model to be exported.</param>
        /// <param name="width">Desired width of the image (in px).</param>
        /// <param name="height">Desired height of the image (in px).</param>
        public Image Export(IPlotModel plot, double width, double height)
        {
            using (Stream stream = new MemoryStream())
            {
#if NETCOREAPP
                PngExporter.Export(plot, stream, (int)width, (int)height);
                stream.Seek(0, SeekOrigin.Begin);
                return Image.FromStream(stream);
#else
                // Using the built-in svg exporter for netfx builds.
                // This doesn't look great.

                // SvgExporter wants dimensions in points.
                double widthPt = width / pointsToPixels;
                double heightPt = height / pointsToPixels;

                SvgExporter exporter = new SvgExporter();
                exporter.Width = widthPt;
                exporter.Height = heightPt;
                exporter.Export(plot, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return ImageUtilities.ReadSvg(stream, (int)width, (int)height);;
#endif
            }
        }

        /// <summary>
        /// Convert the given apsim graph to an oxyplot <see cref="PlotModel"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        public IPlotModel ToPlotModel(IGraph graph)
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
            }

            // Axes (don't add them if there are no series to display on the graph).
            if (previous != null)
            {
                plot.Axes.Add(graph.XAxis.ToOxyPlotAxis(previous.XAxisRequirements, labels.XLabels));
                plot.Axes.Add(graph.YAxis.ToOxyPlotAxis(previous.YAxisRequirements, labels.YLabels));
            }

            // Legend
#if NETFRAMEWORK
            plot.LegendOrientation = graph.Legend.Orientation.ToOxyPlotLegendOrientation();
            plot.LegendPosition = graph.Legend.Position.ToOxyPlotLegendPosition();
            plot.LegendPlacement = graph.Legend.InsideGraphArea ? LegendPlacement.Inside : LegendPlacement.Outside;
#else
            plot.Legends.Add(new Legend()
            {
                LegendOrientation = graph.Legend.Orientation.ToOxyPlotLegendOrientation(),
                LegendPosition = graph.Legend.Position.ToOxyPlotLegendPosition(),
                LegendPlacement = graph.Legend.InsideGraphArea ? OxyLegendPlacement.Inside : OxyLegendPlacement.Outside,
                Font = font,
            });
#endif

            // Apply font
            plot.TitleFont = font;
            plot.SetLegendFont(font);
            plot.PlotAreaBorderThickness = new OxyThickness(0);
            plot.Title = graph.Title;

            return plot;
        }
    }
}
