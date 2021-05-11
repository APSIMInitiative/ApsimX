namespace UserInterface.Views
{
    /// <summary>
    /// This cell painter will colour the column headings of a sheet and any selected cells.
    /// It will also tell the sheet not to paint a cell that is being edited.
    /// </summary>
    public class DefaultCellPainter : ISheetCellPainter
    {
        SheetView sheet;
        ISheetEditor editor;
        ISheetSelection selection;

        public DefaultCellPainter(SheetView sheetView, ISheetEditor sheetEditor = null, ISheetSelection sheetSelection = null)
        {
            sheet = sheetView;
            editor = sheetEditor;
            selection = sheetSelection;
        }

        public bool PaintCell(int columnIndex, int rowIndex)
        {
            bool cellBeingEdited = editor != null && selection != null && !editor.IsEditing && selection.IsSelected(columnIndex, rowIndex);
            return !(cellBeingEdited);
        }

        public Cairo.Color GetForegroundColour(int columnIndex, int rowIndex)
        {
            if (rowIndex < sheet.NumberFrozenRows)
                return new Cairo.Color(1, 1, 1); // white
            else
                return new Cairo.Color(0, 0, 0); // black
        }

        public Cairo.Color GetBackgroundColour(int columnIndex, int rowIndex)
        {
            if (selection != null && selection.IsSelected(columnIndex, rowIndex))
                return new Cairo.Color(198 / 255.0, 198 / 255.0, 198 / 255.0);  // light grey
            else if (rowIndex < sheet.NumberFrozenRows)
                return new Cairo.Color(102 / 255.0, 102 / 255.0, 102 / 255.0);  // dark grey
            else
                return new Cairo.Color(1, 1, 1); // white
        }


        public bool TextBold(int columnIndex, int rowIndex)
        {
            return false;
        }

        public bool TextItalics(int columnIndex, int rowIndex)
        {
            return false;
        }

        public bool TextLeftJustify(int columnIndex, int rowIndex)
        {
            return false;
        }
    }
}
