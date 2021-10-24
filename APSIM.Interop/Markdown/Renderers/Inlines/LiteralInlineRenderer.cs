using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a literal inline markdown object to a PDF document.
    /// </summary>
    public class LiteralInlineRenderer : PdfObjectRenderer<LiteralInline>
    {
        /// <summary>
        /// Render the given literal inline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The literal inline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, LiteralInline obj)
        {
            renderer.AppendText(obj.Content.ToString(), TextStyle.Normal);
        }
    }
}
