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

    /// <summary>
    /// Used by ManagerCursorLocation to hold all the values for a gtk adjustment scroller so that
    /// it can be reset to a specific position.
    /// </summary>
    [Serializable]
    public class ScrollerAdjustmentValues {

        /// <summary>
        /// Value
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Lower
        /// </summary>
        public double Lower { get; }

        /// <summary>
        /// Upper
        /// </summary>
        public double Upper { get; }

        /// <summary>
        /// StepIncrement
        /// </summary>
        public double StepIncrement { get; }

        /// <summary>
        /// PageIncrement
        /// </summary>
        public double PageIncrement { get; }

        /// <summary>
        /// PageSize
        /// </summary>
        public double PageSize { get; }

        /// <summary>
        /// Stores if this object has been set with values or not.
        /// </summary>
        public bool Valid { get; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ScrollerAdjustmentValues()
        {
            this.Value = 0;
            this.Lower = 0;
            this.Upper = 0;
            this.StepIncrement = 0;
            this.PageIncrement = 0;
            this.PageSize = 0;
            this.Valid = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ScrollerAdjustmentValues(double value, double lower, double upper, double stepIncrement, double pageIncrement, double pageSize)
        {
            this.Value = value;
            this.Lower = lower;
            this.Upper = upper;
            this.StepIncrement = stepIncrement;
            this.PageIncrement = pageIncrement;
            this.PageSize = pageSize;
            this.Valid = true;
        }
    }

}
