using System;
using Gtk;

namespace ApsimNG.Classes
{
    public class ListViewArgs : EventArgs
    {
        /// <summary> Holds the original args for when a user hovers over a Gtk.TreeView object.</summary>
        public QueryTooltipArgs QueryTooltipArgs { get; set; }

        /// <summary> Index of the row in the Gtk.TreeView.</summary>
        public int ListViewRowIndex { get; set; }

        /// <summary> New X coordinate for location of hover. </summary>
        public int NewX { get; set; }

        /// <summary> New  Y coordinate for location of hover. </summary>
        public int NewY { get; set; }

    }
}
