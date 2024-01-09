using Gtk;

namespace ApsimNG.Utility
{
    /// <summary>
    /// Helper class that defines the variables needed to restore the cursor and scrollbar positions of the manager script view.
    /// These values allow the view to stay the same when saving the file with a scripot open.
    /// </summary>
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
        public Adjustment ScrollH { get; set; }

        /// <summary>
        /// The values for the vertical scrollbar
        /// </summary>
        public Adjustment ScrollV { get; set; }

        public ManagerCursorLocation()
        {
            this.TabIndex = 0;
            this.Column = 0;
            this.Line = 0;
            this.ScrollH = new Adjustment(0, 0, 100, 0, 0, 0);
            this.ScrollV = new Adjustment(0, 0, 100, 0, 0, 0);
        }

        //Contrsuctor for Report presenter which uses this to select a row.
        public ManagerCursorLocation(int column, int line)
        {
            this.TabIndex = 0;
            this.Column = column;
            this.Line = line;
            this.ScrollH = new Adjustment(0, 0, 100, 0, 0, 0);
            this.ScrollV = new Adjustment(0, 0, 100, 0, 0, 0);
        }
    }
}
