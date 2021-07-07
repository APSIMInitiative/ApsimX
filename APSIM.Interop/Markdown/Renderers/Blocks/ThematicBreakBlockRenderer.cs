using System;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="ThematicBreakBlock" /> object to a PDF document.
    /// </summary>
    public class ThematicBreakBlockRenderer : PdfObjectRenderer<ThematicBreakBlock>
    {
        /// <summary>
        /// Render the given thematic break to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="break">The thematic break to be renderered.</param>
        protected override void Write(PdfBuilder renderer, ThematicBreakBlock thematicBreak)
        {
            renderer.AppendHorizontalRule();
        }
    }
}
