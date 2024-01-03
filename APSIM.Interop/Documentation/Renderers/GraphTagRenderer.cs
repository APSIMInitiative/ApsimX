using System.Linq;
using APSIM.Interop.Graphing;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render a
    /// <see cref="Graph" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    /// <remarks>
    /// This should maybe extend the <see cref="ImageTagRenderer"/>. However,
    /// doing so would prevent us from implementing <see cref="TagRendererBase{IGraph}"/>.
    /// </remarks>
    internal class GraphTagRenderer : TagRendererBase<IGraph>
    {
        /// <summary>
        /// The graph will always be given full page width. The graph's
        /// height will be calculated using this aspect ratio. This
        /// could be made into a user-settable property.
        /// </summary>
        private const double aspectRatio = 16d / 9d;

        /// <summary>
        /// The graph exporter to be used.
        /// </summary>
        private IGraphExporter exporter;

        /// <summary>
        /// Create a GraphTagRenderer with the default image exporter.
        /// </summary>
        public GraphTagRenderer() : this(new GraphExporter()) { }

        /// <summary>
        /// Create a GraphTagRenderer with a custom image exporter.
        /// </summary>
        /// <param name="exporter">Graph exporter to be used.</param>
        public GraphTagRenderer(IGraphExporter exporter)
        {
            this.exporter = exporter;
        }

        /// <summary>
        /// Render the given graph tag to the PDF document.
        /// </summary>
        /// <param name="graph">Graph tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(IGraph graph, PdfBuilder renderer)
        {
            renderer.GetPageSize(out double width, out _);

            renderer.StartNewParagraph();
            var plot = exporter.ToPlotModel(graph);

            // Temp hack - set marker size to 5. We need to review
            // appropriate sizing for graphs in autodocs.
            if (plot is OxyPlot.PlotModel model)
                foreach (var series in model.Series.OfType<OxyPlot.Series.LineSeries>())
                    series.MarkerSize = 5;

            renderer.AppendImage(exporter.Export(plot, width, width / aspectRatio));
            renderer.StartNewParagraph();
        }
    }
}
