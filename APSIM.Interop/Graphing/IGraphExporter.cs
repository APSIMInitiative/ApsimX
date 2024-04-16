using Image = System.Drawing.Image;
using APSIM.Shared.Documentation;
using OxyPlot;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// Interface for a class which can export a graph to a System.Drawing.Image.
    /// </summary>
    public interface IGraphExporter
    {
        /// <summary>
        /// Export a graph to an image.
        /// </summary>
        /// <param name="graph">Graph to be converted.</param>
        /// <param name="width">Desired width of the image (in px).</param>
        /// <param name="height">Desired height of the image (in px).</param>
        SkiaSharp.SKImage Export(IGraph graph, double width, double height);

        /// <summary>
        /// Export a plot model to an image.
        /// </summary>
        /// <param name="plot">Plot model to be exported.</param>
        /// <param name="width">Desired width of the image (in px).</param>
        /// <param name="height">Desired height of the image (in px).</param>
        SkiaSharp.SKImage Export(IPlotModel plot, double width, double height);

        /// <summary>
        /// Convert the given apsim graph to an oxyplot <see cref="PlotModel"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        IPlotModel ToPlotModel(IGraph graph);
    }
}
