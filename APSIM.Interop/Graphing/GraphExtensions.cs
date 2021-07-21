using System.IO;
using APSIM.Interop.Utility;
using APSIM.Services.Documentation;
using APSIM.Services.Graphing;
using OxyPlot;
using Image = System.Drawing.Image;

namespace APSIM.Interop.Graphing
{
    public static class GraphExtensions
    {
        /// <summary>
        /// Font used for labels/headings/legends/etc.
        /// </summary>
        private const string font = "Calibri Light";

        /// <summary>
        /// Convert the given apsim graph to an oxyplot <see cref="PlotModel"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        public static IPlotModel ToPlotModel(this Graph graph)
        {
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
            // plot.Title = graph.Title;

            return plot;
        }

        /// <summary>
        /// Export a graph to an image.
        /// </summary>
        /// <param name="graph">Graph to be converted.</param>
        /// <param name="width">Desired width of the image (in px).</param>
        /// <param name="height">Desired height of the image (in px).</param>
        public static Image ToImage(this Graph graph, double width, double height)
        {
            using (Stream stream = new MemoryStream())
            {
                // SvgExporter wants dimensions in points.
                const double pointsToPixels = 96.0 / 72;
                double widthPt = width / pointsToPixels;
                double heightPt = height / pointsToPixels;

                // Write the image to a memory stream in SVG format.
                SvgExporter.Export(graph.ToPlotModel(), stream, widthPt, heightPt, false);
                stream.Seek(0, SeekOrigin.Begin);

                // Setting height to 0 will cause the aspect ratio to be preserved.
                return ImageUtilities.ReadSvg(stream, (int)width, (int)height);
            }
        }
    }
}
