// -----------------------------------------------------------------------
// <copyright file="NeedContextItemsArgs.cs" company="APSIM Initiative">
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
    public class NeedContextItemsArgs : EventArgs
    {
        /// <summary>
        /// The name of the object that needs context items.
        /// </summary>
        public string ObjectName;

        /// <summary>
        /// The items returned from the presenter back to the view
        /// </summary>
        public List<ContextItem> AllItems;

        /// <summary>
        /// Context item information
        /// </summary>
        public List<string> Items;

        /// <summary>
        /// Sorts the ContextItem list by name
        /// </summary>
        public void SortAllItems()
        {
            this.AllItems.Sort(delegate(ContextItem c1, ContextItem c2) { return c1.Name.CompareTo(c2.Name); });
        }

        /// <summary>
        /// Complete context item information
        /// </summary>
        public struct ContextItem
        {
            /// <summary>
            /// Name of the item
            /// </summary>
            public string Name;

            /// <summary>
            /// The return type as a string
            /// </summary>
            public string TypeName;

            /// <summary>
            /// Units string
            /// </summary>
            public string Units;

            /// <summary>
            /// The description string
            /// </summary>
            public string Descr;

            /// <summary>
            /// This is an event/method
            /// </summary>
            public bool IsEvent;

            /// <summary>
            /// String that represents the parameter list
            /// </summary>
            public string ParamString;

            /// <summary>
            /// This is a property
            /// </summary>
            public bool IsProperty;

            /// <summary>
            /// This property is writeable
            /// </summary>
            public bool IsWriteable;
        }
    } 
}
