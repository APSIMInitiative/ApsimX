using System;

namespace Shared.Utilities
{
    /// <summary>
    /// Holds all the values for a gtk adjustment scroller so that it can be set to a specific position.
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
