namespace UserInterface.EventArguments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class GridCellsChangedArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a list of the cells that have changed.
        /// </summary>
        public List<GridCellChangedArgs> ChangedCells { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="changedCells"></param>
        public GridCellsChangedArgs(params GridCellChangedArgs[] changedCells)
        {
            ChangedCells = changedCells.ToList();
        }
    }  
}
