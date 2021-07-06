using System;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="QuoteBlock" /> object to a PDF document.
    /// </summary>
    public class QuoteBlockRenderer : PdfObjectRenderer<QuoteBlock>
    {
        /// <summary>
        /// Render the given quote block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="quote">The quote block to be renderered.</param>
        protected override void Write(PdfRenderer renderer, QuoteBlock quote)
        {
            renderer.AppendText("", TextStyle.Normal, true);
            renderer.PushStyle(TextStyle.Quote);
            renderer.WriteChildren(quote);
            renderer.PopStyle();
        }
    }
}
