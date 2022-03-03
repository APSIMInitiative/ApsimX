using Markdig.Syntax.Inlines;
using APSIM.Interop.Markdown.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a reference inline markdown object to a PDF document.
    /// </summary>
    public class ReferenceInlineRenderer : PdfObjectRenderer<ReferenceInline>
    {
        /// <summary>
        /// Render the given reference inline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The reference inline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, ReferenceInline obj)
        {
            renderer.AppendReference(obj.ReferenceName, TextStyle.Normal);
        }
    }
}
