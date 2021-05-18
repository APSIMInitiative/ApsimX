namespace UserInterface.Views
{
    /// <summary>
    /// This cell painter will colour the column headings of a sheet and any selected cells.
    /// It will also tell the sheet not to paint a cell that is being edited.
    /// </summary>
    public class DefaultCellPainter : ISheetCellPainter
    {
        /// <summary>The sheet to paint.</summary>
        SheetView sheet;

        /// <summary>The optional cell editor instance.</summary>
        ISheetEditor editor;

        /// <summary>The optional cell selection instance.</summary>
        ISheetSelection selection;


        /// <summary>Constructor.</summary>
        /// <param name="sheetView">The sheet to paint.</param>
        /// <param name="sheetEditor">The optional cell editor instance.</param>
        /// <param name="sheetSelection">The optional cell selection instance.</param>
        public DefaultCellPainter(SheetView sheetView, ISheetEditor sheetEditor = null, ISheetSelection sheetSelection = null)
        {
            sheet = sheetView;
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

#if NETCOREAPP
        /// <summary>Paint a cell in the sheet.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public Gtk.StateFlags GetCellState(int columnIndex, int rowIndex)
        {
            if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                return Gtk.StateFlags.Selected;
            else if (rowIndex < sheet.NumberFrozenRows)
                return Gtk.StateFlags.Insensitive;
            else
                return Gtk.StateFlags.Normal;
        }
#else
        /// <summary>Gets the foreground colour of a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public Cairo.Color GetForegroundColour(int columnIndex, int rowIndex)
        {

            if (Utility.Configuration.Settings.DarkTheme)
            {


                if (rowIndex < sheet.NumberFrozenRows)
                    return new Cairo.Color(1, 1, 1); // white
                else
                    return new Cairo.Color(1, 1, 1); // white
            }
            else
            {
                if (rowIndex < sheet.NumberFrozenRows)
                    return new Cairo.Color(1, 1, 1); // white
                else
                    return new Cairo.Color(0, 0, 0); // black
            }
        }

        /// <summary>Gets the background colour of a cell.</summary>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="rowIndex">The row index of the cell.</param>
        public Cairo.Color GetBackgroundColour(int columnIndex, int rowIndex)
        {
            if (Utility.Configuration.Settings.DarkTheme)
            {
                if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                    return new Cairo.Color(150 / 255.0, 150 / 255.0, 150 / 255.0);  // light grey
                else if (rowIndex < sheet.NumberFrozenRows)
                    return new Cairo.Color(102 / 255.0, 102 / 255.0, 102 / 255.0);  // dark grey
                else
                    return new Cairo.Color(0, 0, 0); // black
            }
            else
            {
                if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                    return new Cairo.Color(198 / 255.0, 198 / 255.0, 198 / 255.0);  // light grey
                else if (rowIndex < sheet.NumberFrozenRows)
                    return new Cairo.Color(102 / 255.0, 102 / 255.0, 102 / 255.0);  // dark grey
                else
                    return new Cairo.Color(1, 1, 1); // white
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