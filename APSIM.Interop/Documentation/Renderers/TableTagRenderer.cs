using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render an
    /// <see cref="ITag" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class TableTagRenderer : TagRendererBase<Table>
    {
        /// <summary>
        /// Render the given Table tag to the PDF document.
        /// </summary>
        /// <param name="table">Table tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(Table table, PdfBuilder renderer)
        {
            // Add the Table to a new paragraph.
            renderer.AppendTable(table.Data);
        }
    }
}
