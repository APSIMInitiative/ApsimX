// -----------------------------------------------------------------------
// <copyright file="GridHeaderClickedArgs.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.EventArguments
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class GridHeaderClickedArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the column that had its header clicked.
        /// </summary>
        public IGridColumn Column { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the right mouse button was clicked.
        /// </summary>
        public bool RightClick { get; set; }
    }  
}
