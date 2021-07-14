using APSIM.Services.Documentation;
using APSIM.Services.Graphing;
using OxyPlot;

namespace APSIM.Interop.Graphing
{
    public static class GraphExtensions
    {
        /// <summary>
        /// Convert the given apsim graph to an oxyplot <see cref="PlotModel"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        public static PlotModel ToPlotModel(this Graph graph)
        {
            PlotModel plot = new PlotModel();
            foreach (Axis axis in graph.Axes)
                plot.Axes.Add(axis.ToOxyPlotAxis());
            return plot;
        }
    }
}