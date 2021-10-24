using APSIM.Interop.Markdown.Extensions;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="CodeBlock" /> object to a PDF document.
    /// </summary>
    public class CodeBlockRenderer : PdfObjectRenderer<CodeBlock>
    {
        /// <summary>
        /// Render the given code block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="codeBlock">The code block to be renderered.</param>
        protected override void Write(PdfBuilder renderer, CodeBlock codeBlock)
        {
            if (codeBlock.Lines.Lines != null)
            {
                renderer.StartNewParagraph();

                // We could set the language if CodeBlock is of type FencedCodeBlock.
                // string lang = (codeBlock as FencedCode)?.Info;
                renderer.PushStyle(TextStyle.Code);
                renderer.WriteLeafInlines(codeBlock);
                renderer.PopStyle();

                renderer.StartNewParagraph();
            }
        }
    }
}
