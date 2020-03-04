namespace Models
{
    using System;

    /// <summary>
    /// A class representing an axis on a graph.
    /// </summary>
    [Serializable]
    public class Axis
    {
        /// <summary>
        /// Constructor for axis class.
        /// </summary>
        public Axis()
        {
            this.Minimum = double.NaN;
            this.Maximum = double.NaN;
            this.Interval = double.NaN;
        }

        /// <summary>
        /// An enumeration for different axis orientations
        /// </summary>
        public enum AxisType
        {
            /// <summary>
            /// Left orientation
            /// </summary>
            Left,

            /// <summary>
            /// Top orientation
            /// </summary>
            Top,

            /// <summary>
            /// Right orientation
            /// </summary>
            Right,

            /// <summary>
            /// Bottom orientation
            /// </summary>
            Bottom
        }

        /// <summary>
        /// Gets or sets the 'type' of axis - left, top, right or bottom.
        /// </summary>
        public AxisType Type { get; set; }

        /// <summary>
        /// Gets or sets the title of the axis.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the axis is inverted?
        /// </summary>
        public bool Inverted { get; set; }

        /// <summary>
        /// Gets or sets the minimum axis scale
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum axis scale
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// Gets or sets the interval axis scale
        /// </summary>
        public double Interval { get; set; }

        /// <summary>
        /// Keeps track of whether date/time information is displayed on this
        /// axis.
        /// </summary>
        public bool DateTimeAxis { get; set; }

        /// <summary>
        /// Does this axis cross the other axis at the zero point?
        /// </summary>
        public bool CrossesAtZero { get; set; } = false;
    }
}
