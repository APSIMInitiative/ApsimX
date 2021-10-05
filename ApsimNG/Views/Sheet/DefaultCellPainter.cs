using System.Drawing;
using Utility;

namespace UserInterface.Views
{
    /// <summary>
    /// This cell painter will colour the column headings of a sheet and any selected cells.
    /// It will also tell the sheet not to paint a cell that is being edited.
    /// </summary>
    public class DefaultCellPainter : ISheetCellPainter
    {
        /// <summary>The sheet to paint.</summary>
        Sheet sheet;

        /// <summary>The sheet widget to paint.</summary>
        SheetWidget sheetWidget;

        /// <summary>The optional cell editor instance.</summary>
        ISheetEditor editor;

        /// <summary>The optional cell selection instance.</summary>
        ISheetSelection selection;


        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet to paint.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        /// <param name="sheetEditor">The optional cell editor instance.</param>
        /// <param name="sheetSelection">The optional cell selection instance.</param>
        public DefaultCellPainter(Sheet sheet, SheetWidget sheetWidget, ISheetEditor sheetEditor = null, ISheetSelection sheetSelection = null)
        {
            this.sheet = sheet;
            this.sheetWidget = sheetWidget;
            editor = sheetEditor;
            selection = sheetSelection;
        }

        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public bool PaintCell(int columnIndex, int rowIndex)
        {
            bool cellBeingEdited = editor != null && selection != null && editor.IsEditing && selection.IsSelected(columnIndex, rowIndex);
            return !(cellBeingEdited);
        }

        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public States GetCellState(int columnIndex, int rowIndex)
        {
            if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                return States.Selected;
            else if (rowIndex < sheet.NumberFrozenRows)
                return States.Insensitive;
            else
                return States.Normal;
        }
#if NETCOREAPP

#else
        /// <summary>Gets the foreground colour of a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public Color GetForegroundColour(int columnIndex, int rowIndex)
        {

            if (Utility.Configuration.Settings.DarkTheme)
            {


                if (rowIndex < sheet.NumberFrozenRows)
                    return Color.FromArgb(255, 255, 255); // white
                else
                    return Color.FromArgb(255, 255, 255); // white
            }
            else
            {
                if (rowIndex < sheet.NumberFrozenRows)
                    return Color.FromArgb(255, 255, 255); // white
                else
                    return Color.FromArgb(0, 0, 0); // black
            }
        }

        /// <summary>Gets the background colour of a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public Color GetBackgroundColour(int columnIndex, int rowIndex)
        {
            if (Utility.Configuration.Settings.DarkTheme)
            {
                if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                    return Color.FromArgb(150, 150, 150);  // light grey
                else if (rowIndex < sheet.NumberFrozenRows)
                    return Color.FromArgb(102, 102, 102);  // dark grey
                else
                    return sheetWidget.Style.Background(Gtk.StateType.Normal).FromGtk();
            }
            else
            {
                if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                    return Color.FromArgb(198, 198, 198);  // light grey
                else if (rowIndex < sheet.NumberFrozenRows)
                    return Color.FromArgb(102, 102, 102);  // dark grey
                else
                    return Color.FromArgb(255, 255, 255); // white
            }
        }
#endif

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