using System;
using UserInterface.Interfaces;
namespace UserInterface.EventArguments
{
    /// <summary>
    /// Event arguments used to perform an action on a range of grid cells.
    /// </summary>
    public class GridCellActionArgs : EventArgs
    {
        /// <summary>
        /// First (top-left) cell in the range of cells to be copied.
        /// </summary>
        public IGridCell StartCell;

        /// <summary>
        /// Last (bottom-right) cell in the range of cells to be copied.
        /// </summary>
        public IGridCell EndCell;

        /// <summary>
        /// Grid which data is being copied to/from.
        /// </summary>
        public IGridView Grid;
    }
}
