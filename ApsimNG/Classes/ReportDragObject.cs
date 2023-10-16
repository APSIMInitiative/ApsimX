using System;
using Gtk;

namespace ApsimNG.Classes
{
    public class ReportDragObject : EventArgs
    {
        /// <summary>
        /// Index of selected row in a ListView object.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Description of common report variable or common report frequency variable.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Code string of a common report or common report frequency variable.
        /// </summary>
        public string Code { get; set; }

        public DragBeginArgs OtherArgs { get; set; }
    }
}
