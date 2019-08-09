using System;

namespace UserInterface.EventArguments
{
    /// <summary>
    /// Event arguments used when a cell is modified in a grid view.
    /// </summary>
    public class GridCellChangedArgs
    {
        /// <summary>
        /// Row index of the changed cell.
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Column index of the changed cell.
        /// </summary>
        public int ColIndex { get; set; }

        /// <summary>
        /// Contents of the cell before the edit.
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// Contents of the cell after the edit.
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="row">Row index of the changed cell.</param>
        /// <param name="col">Column index of the changed cell.</param>
        /// <param name="oldValue">Contents of the cell before the edit.</param>
        /// <param name="newValue">Contents of the cell after the edit.</param>
        public GridCellChangedArgs(int row, int col, string oldValue, string newValue)
        {
            this.RowIndex = row;
            this.ColIndex = col;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }
    }
}
