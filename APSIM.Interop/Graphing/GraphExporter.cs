using Image = System.Drawing.Image;
using APSIM.Services.Documentation;
using OxyPlot;
using System.IO;
using APSIM.Interop.Utility;
using System;
using APSIM.Services.Graphing;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// This class will export a graph to an image.
    /// </summary>
    /// <remarks>
    /// This class is responsible for graph -> oxyplot and oxyplot -> image conversions.
    /// todo: should the other methods be added to the interface? I'm going to leave
    /// them as-is for now, but there's no real reason they couldn't be part of the interface.
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
        public Image Export(Graph graph, double width, double height)
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
                // SvgExporter wants dimensions in points.
                double widthPt = width / pointsToPixels;
                double heightPt = height / pointsToPixels;

                // Write the image to a memory stream in SVG format.
                SvgExporter exporter = new SvgExporter();
                exporter.Width = widthPt;
                exporter.Height = heightPt;
#if NETCOREAPP
                // This is a workaround for a bug in oxyplot's svg exporter;
                // Without this, the vertical alignment of axis tick labels
                // is incorrect (they are rendered too high).
                exporter.UseVerticalTextAlignmentWorkaround = true;
#endif
                exporter.Export(plot, stream);
                stream.Seek(0, SeekOrigin.Begin);

                // Setting height to 0 will cause the aspect ratio to be preserved.
                return ImageUtilities.ReadSvg(stream, (int)width, (int)height);
            }
        }

        /// <summary>
        /// Convert the given apsim graph to an oxyplot <see cref="PlotModel"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        public IPlotModel ToPlotModel(IGraph graph)
        {
            if (graph.Axes == null)
                throw new NullReferenceException("Graph has no axes");
            if (graph.Legend == null)
                throw new NullReferenceException("Graph has no legend configuration");
            if (graph.Series == null)
                throw new NullReferenceException("Graph has no series");

            PlotModel plot = new PlotModel();

            // Axes
            foreach (Axis axis in graph.Axes)
                plot.Axes.Add(axis.ToOxyPlotAxis());

            // Series
            foreach (Series series in graph.Series)
                plot.Series.Add(series.ToOxyPlotSeries());

            // Legend
            plot.LegendOrientation = graph.Legend.Orientation.ToOxyPlotLegendOrientation();
            plot.LegendPosition = graph.Legend.Position.ToOxyPlotLegendPosition();
            plot.LegendPlacement = graph.Legend.InsideGraphArea ? LegendPlacement.Inside : LegendPlacement.Outside;

            // Apply font
            plot.TitleFont = font;
            plot.LegendFont = font;
            plot.PlotAreaBorderThickness = new OxyThickness(0);
            plot.Title = graph.Title;

            return plot;
        }
    }
}
