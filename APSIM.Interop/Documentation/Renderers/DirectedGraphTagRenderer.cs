using APSIM.Interop.Drawing.Skia;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Visualisation;
using APSIM.Shared.Documentation.Tags;
using APSIM.Shared.Graphing;
using System.Drawing;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render an
    /// <see cref="ITag" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class DirectedGraphTagRenderer : TagRendererBase<DirectedGraphTag>
    {
        /// <summary>
        /// Render the given DirectedGraphTag tag to the PDF document.
        /// </summary>
        /// <param name="tag">Directed graph tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(DirectedGraphTag tag, PdfBuilder renderer)
        {
            renderer.GetPageSize(out double width, out double height);
            renderer.StartNewParagraph();
            renderer.AppendImage(WriteToImage(tag.Graph, (int)width, (int)width));
            renderer.StartNewParagraph();
        }

        /// <summary>
        /// Write a directed graph to an image using SkiaSharp.
        /// </summary>
        /// <param name="graph">The directed graph.</param>
        /// <param name="width">Desired width of the image.</param>
        /// <param name="height">Desired height of the image.</param>
        /// <returns></returns>
        private SkiaSharp.SKImage WriteToImage(DirectedGraph graph, int width, int height)
        {
            using (SkiaContext context = new SkiaContext((int)width, (int)height))
            {
                DirectedGraphRenderer.Draw(context, graph);
                return context.Save();
            }
        }
    }
}
