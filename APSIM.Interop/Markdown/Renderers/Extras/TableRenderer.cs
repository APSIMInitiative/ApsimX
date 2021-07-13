using Markdig.Extensions.Tables;

namespace APSIM.Interop.Markdown.Renderers.Extras
{
    /// <summary>
    /// This class renders a <see cref="Table" /> object to a PDF document.
    /// </summary>
    public class TableRenderer : PdfObjectRenderer<Table>
    {
        /// <summary>
        /// Render the given table block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="table">The table block to be renderered.</param>
        protected override void Write(PdfBuilder renderer, Table table)
        {
            renderer.StartTable(table.ColumnDefinitions.Count);
            renderer.WriteChildren(table);
            renderer.FinishTable();
        }
    }
}
