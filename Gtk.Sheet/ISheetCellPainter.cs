using System.Drawing;
using APSIM.Interop.Drawing;

namespace Gtk.Sheet
{
    /// <summary>
    /// An interface for a class that paints a cell in a sheet widget.
    /// </summary>
    public interface ISheetCellPainter
    {
        /// <summary>Gets whether to left justify text in a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        bool TextLeftJustify(int columnIndex, int rowIndex);

        /// <summary>Gets whether to use a bold font for a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        bool TextBold(int columnIndex, int rowIndex);

        /// <summary>Gets whether to use an italics font for a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        bool TextItalics(int columnIndex, int rowIndex);

        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        bool PaintCell(int columnIndex, int rowIndex);

        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        States GetCellState(int columnIndex, int rowIndex);




    }
}