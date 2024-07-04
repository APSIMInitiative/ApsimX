namespace Gtk.Sheet
{
    /// <summary>Describes the public interface of a class that supports sheet cell selection.</summary>
    public interface ISheetSelection
    {
        /// <summary>Gets whether a cell is selected.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        /// <returns>True if selected, false otherwise.</returns>
        bool IsSelected(int columnIndex, int rowIndex);

        /// <summary>Gets the currently selected cell..</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        void GetSelection(out int columnIndex, out int rowIndex);

        /// <summary>Moves the selected cell to the left one column.</summary>
        void MoveLeft(bool shift = false);

        /// <summary>Moves the selected cell to the right one column.</summary>
        void MoveRight(bool shift = false);

        /// <summary>Moves the selected cell up one row.</summary>
        void MoveUp(bool shift = false);

        /// <summary>Moves the selected cell down one row.</summary>
        void MoveDown(bool shift = false);

        /// <summary>Cut cells to clipboard, deleting them from the cell</summary>
        void Cut();

        /// <summary>Copy cells to clipboard.</summary>
        void Copy();

        /// <summary>Past cells from clipboard.</summary>
        void Paste();

        /// <summary>Delete contents of cell</summary>
        void Delete();

        /// <summary></summary>
        void SelectAll();
    }
}