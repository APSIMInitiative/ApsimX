// -----------------------------------------------------------------------
// <copyright file="NeedContextItems.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.EventArguments
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    /// <summary>
    /// The editor view asks the presenter for context items. This structure
    /// is used to do that
    /// </summary>
    public class NeedContextItems : EventArgs
    {
        /// <summary>
        /// The name of the object that needs context items.
        /// </summary>
        public string ObjectName;

        /// <summary>
        /// The items returned from the presenter back to the view
        /// </summary>
        public List<string> Items;
    } 
}
