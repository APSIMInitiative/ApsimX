using Markdig.Extensions.Tables;

namespace APSIM.Interop.Markdown.Renderers.Extras
{
    /// <summary>
    /// This class renders a <see cref="TableCell" /> object to a PDF document.
    /// </summary>
    public class TableCellRenderer : PdfObjectRenderer<TableCell>
    {
        /// <summary>
        /// Render the given table cell to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="cell">The table cell to be renderered.</param>
        protected override void Write(PdfBuilder renderer, TableCell cell)
        {
            renderer.StartTableCell();
            // todo: cells spanning multiple rows/columns.
            renderer.WriteChildren(cell);
            renderer.FinishTableCell();
        }
    }
}
