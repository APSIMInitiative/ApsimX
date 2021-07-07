using Markdig.Extensions.Tables;

namespace APSIM.Interop.Markdown.Renderers.Extras
{
    /// <summary>
    /// This class renders a <see cref="TableRow" /> object to a PDF document.
    /// </summary>
    public class TableRowRenderer : PdfObjectRenderer<TableRow>
    {
        /// <summary>
        /// Render the given table row to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="row">The table row to be renderered.</param>
        protected override void Write(PdfRenderer renderer, TableRow row)
        {
            renderer.StartTableRow(row.IsHeader);
            renderer.WriteChildren(row);
        }
    }
}
