using APSIM.Interop.Graphing;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render a
    /// <see cref="Graph" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class GraphTagRenderer : TagRendererBase<Graph>
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
        protected override void Render(Graph graph, PdfBuilder renderer)
        {
            renderer.GetPageSize(out double width, out _);

            renderer.StartNewParagraph();
            renderer.AppendImage(exporter.Export(graph, width, width / aspectRatio));
            renderer.StartNewParagraph();
        }
    }
}
