using System;

namespace Shared.Utilities
{
    /// <summary>
    /// Helper class that defines the variables needed to restore the cursor and scrollbar positions of the manager script view.
    /// These values allow the view to stay the same when saving the file with a scripot open.
    /// </summary>
    [Serializable]
    public class ManagerCursorLocation
    {
        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        public int TabIndex { get; set; }

        /// <summary>
        /// Column that the caret is on
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Line that the caret is on
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The values for the horizontal scrollbar
        /// </summary>
        public ScrollerAdjustmentValues ScrollH { get; set; }

        /// <summary>
        /// The values for the vertical scrollbar
        /// </summary>
        public ScrollerAdjustmentValues ScrollV { get; set; }

        /// <summary>
        /// Default Contructor
        /// </summary>
        public ManagerCursorLocation()
        {
            this.TabIndex = 0;
            this.Column = 0;
            this.Line = 0;
            this.ScrollH = new ScrollerAdjustmentValues();
            this.ScrollV = new ScrollerAdjustmentValues();
        }

        /// <summary>
        /// Contrsuctor for Report presenter which uses this to select a row.
        /// </summary>
        public ManagerCursorLocation(int column, int line)
        {
            this.TabIndex = 0;
            this.Column = column;
            this.Line = line;
            this.ScrollH = new ScrollerAdjustmentValues();
            this.ScrollV = new ScrollerAdjustmentValues();
        }
    }
}