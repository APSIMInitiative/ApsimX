using System;
using System.Collections.Generic;

namespace APSIM.Interop.Markdown.Tags
{
    /// <summary>
    /// This class encapsulates a piece of text in a markdown document.
    /// It's not a tag in and of itself, but it's a building block used
    /// by other tags (such as paragraphs, headings, etc).
    /// </summary>
    public class TableRow
    {
        public IEnumerable<TableCell> Cells { get; private set; }
        public TableRow(IEnumerable<TableCell> cells) => Cells = cells;
    }
}
