using System.Drawing;
using APSIM.Interop.Drawing;

namespace Gtk.Sheet
{
    /// <summary>
    /// This cell painter will colour the column headings of a sheet and any selected cells.
    /// It will also tell the sheet not to paint a cell that is being edited.
    /// </summary>
    internal class CellPainterNoCellStates : ISheetCellPainter
    {
        /// <summary>The sheet to paint.</summary>
        Sheet sheet;

        /// <summary>The sheet widget to paint.</summary>
        SheetWidget sheetWidget;

        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet to paint.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        public CellPainterNoCellStates(Sheet sheet, SheetWidget sheetWidget)
        {
            this.sheet = sheet;
            this.sheetWidget = sheetWidget;
        }

        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public bool PaintCell(int columnIndex, int rowIndex)
        {
            bool cellBeingEdited = sheet.CellEditor != null && sheet.CellSelector != null && sheet.CellEditor.IsEditing && sheet.CellSelector.IsSelected(columnIndex, rowIndex);
            return !(cellBeingEdited);
        }

        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public States GetCellState(int columnIndex, int rowIndex)
        {
            if (sheet.CellSelector != null && sheet.CellSelector.IsSelected(columnIndex, rowIndex))
                return States.Selected; 
            if (rowIndex < sheet.NumberFrozenRows)
                return States.Insensitive;
            return States.Normal;
        }

        /// <summary>Gets whether to use a bold font for a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public bool TextBold(int columnIndex, int rowIndex)
        {
            return rowIndex < sheet.NumberFrozenRows;
        }

        /// <summary>Gets whether to use an italics font for a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public bool TextItalics(int columnIndex, int rowIndex)
        {
            return false;
        }

        /// <summary>Gets whether to left justify text in a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public bool TextLeftJustify(int columnIndex, int rowIndex)
        {
            return false;
        }
    }
}