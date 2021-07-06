using System;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="ListBlock" /> object to a PDF document.
    /// </summary>
    public class ListBlockRenderer : PdfObjectRenderer<ListBlock>
    {
        /// <summary>
        /// Render the given list block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="html">The list block to be renderered.</param>
        protected override void Write(PdfRenderer renderer, ListBlock list)
        {
            renderer.AppendText("", TextStyle.Normal, true);

            int i = 1;
            // Implicit cast from Block to ListItemBlock here. Markdig's builtin html
            // renderer also makes this assumption.
            foreach (ListItemBlock child in list)
            {
                // Write the bullet.
                string bullet = list.IsOrdered ? i.ToString() : list.BulletType.ToString();
                renderer.AppendText($" {bullet} ", TextStyle.Normal, false);

                // Write the list item. Hopefully nothing in the block writes to a new paragraph.
                renderer.WriteChildren(child);

                // Write a newline character. We don't want multiple list items on the same line.
                renderer.AppendText(Environment.NewLine, TextStyle.Normal, false);
                i++;
            }
        }
    }
}
