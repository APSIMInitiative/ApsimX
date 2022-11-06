using System;
using APSIM.Interop.Markdown.Renderers;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Extensions
{
    internal static class PdfBuilderExtensions
    {
        /// <summary>
        /// Write the lines of a leaf block to a PDF document.
        /// </summary>
        /// <param name="leaf">The leaf block to be written.</param>
        internal static void WriteLeafInlines(this PdfBuilder builder, LeafBlock leaf)
        {
            if (leaf == null)
                // Throwing here may be a bit harsh(?).
                throw new NullReferenceException("Unable to write null leaf block");
            if (leaf.Lines.Lines == null)
                return;

            for (int i = 0; i < leaf.Lines.Count; i++)
            {
                builder.AppendText(leaf.Lines.Lines[i].Slice.ToString(), TextStyle.Normal);
                if (i < leaf.Lines.Count - 1)
                    builder.AppendText(Environment.NewLine, TextStyle.Normal);
            }
        }
    }
}
