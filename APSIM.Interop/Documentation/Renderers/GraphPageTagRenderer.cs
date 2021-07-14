using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render a
    /// <see cref="GraphPage" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class GraphPageTagRenderer : TagRendererBase<GraphPage>
    {
        /// <summary>
        /// Render the given graph page to the PDF document.
        /// </summary>
        /// <param name="GraphPage">Graph page to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(GraphPage page, PdfBuilder renderer)
        {
            // throw new System.NotImplementedException();
            // foreach (Graph graph in page.Graphs)
            // {

            // }
        }
    }
}
