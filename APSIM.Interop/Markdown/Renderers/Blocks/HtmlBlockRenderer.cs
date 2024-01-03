using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="HtmlBlock" /> object to a PDF document.
    /// </summary>
    public class HtmlBlockRenderer : PdfObjectRenderer<HtmlBlock>
    {
        /// <summary>
        /// Render the given html block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="html">The html block to be renderered.</param>
        protected override void Write(PdfBuilder renderer, HtmlBlock html)
        {
            renderer.WriteChildren(html.Inline);
        }
    }
}
