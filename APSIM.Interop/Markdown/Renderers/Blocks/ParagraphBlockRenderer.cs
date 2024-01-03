using System;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="ParagraphBlock" /> object to a PDF document.
    /// </summary>
    public class ParagraphBlockRenderer : PdfObjectRenderer<ParagraphBlock>
    {
        /// <summary>
        /// Render the given paragraph block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="paragraph">The paragraph block to be renderered.</param>
        protected override void Write(PdfBuilder renderer, ParagraphBlock paragraph)
        {
            renderer.StartNewParagraph();
            renderer.WriteChildren(paragraph.Inline);
            renderer.StartNewParagraph();
        }
    }
}
