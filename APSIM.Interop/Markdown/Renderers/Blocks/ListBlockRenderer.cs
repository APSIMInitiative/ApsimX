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
        protected override void Write(PdfBuilder renderer, ListBlock list)
        {
            renderer.StartNewParagraph();

            int i = 1;
            // Implicit cast from Block to ListItemBlock here. Markdig's builtin html
            // renderer also makes this assumption.
            foreach (ListItemBlock child in list)
            {
                // Write the bullet.
                // todo: should use proper indentation (\t) here.
                string bullet = list.IsOrdered ? $"{i}." : list.BulletType.ToString();

                // Calling StartListItem() will prevent the list item's contents
                // from creating a new paragraph.
                renderer.StartListItem($" {bullet} ");

                // Write the contents of the list item.
                renderer.WriteChildren(child);

                // Write a newline character. We don't want multiple list items on the same line.
                // renderer.AppendText(Environment.NewLine, TextStyle.Normal, false);
                renderer.FinishListItem();

                i++;
            }
            renderer.StartNewParagraph();
        }
    }
}
