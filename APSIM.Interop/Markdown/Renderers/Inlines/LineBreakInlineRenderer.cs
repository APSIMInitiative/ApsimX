using System;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a <see cref="HtmlInline" /> object to a PDF document.
    /// </summary>
    public class LineBreakInlineRenderer : PdfObjectRenderer<LineBreakInline>
    {
        /// <summary>
        /// Render the given LineBreakInline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The LineBreakInline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, LineBreakInline obj)
        {
            // Html version of this class has an option to render soft line breaks
            // as a hard line break. We could implement something similar.
            string textToInsert = obj.IsHard ? Environment.NewLine : " ";
            renderer.AppendText(textToInsert, TextStyle.Normal);
        }
    }
}
