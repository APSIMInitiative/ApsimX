namespace UserInterface.EventArguments
{
    using Interfaces;
    using System;

    /// <summary>
    /// Structure to hold information about clicks that have occurred on 
    /// a grid column.
    /// </summary>
    public class GridColumnClickedArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the column that has been clicked.
        /// </summary>
        public IGridColumn Column { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the right mouse button was clicked.
        /// </summary>
        public bool RightClick { get; set; }

        /// <summary>
        /// If true, the click was on a column header; otherwise it was in the grid body
        /// </summary>
        public bool OnHeader { get; set; }
    }  
}
