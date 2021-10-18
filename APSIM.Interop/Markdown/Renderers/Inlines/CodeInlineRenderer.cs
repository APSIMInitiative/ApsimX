using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a code inline markdown object to a PDF document.
    /// </summary>
    public class CodeInlineRenderer : PdfObjectRenderer<CodeInline>
    {
        /// <summary>
        /// Render the given code inline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The code inline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, CodeInline obj)
        {
            // todo: what is a code inline, and how should it be rendered?
            // For now, treat it like any other inline, but with code style.
            renderer.AppendText(obj.Content, TextStyle.Code);
        }
    }
}