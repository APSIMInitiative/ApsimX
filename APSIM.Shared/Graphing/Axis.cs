using System;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Graph axis configuration.
    /// </summary>
    [Serializable]
    public class Axis
    {
        /// <summary>
        /// Title of the axis.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Axis location (left, top, right, or bottom).
        /// </summary>
        public AxisPosition Position { get; set; }

        /// <summary>
        /// Is the axis scale inverted?
        /// </summary>
        public bool Inverted { get; set; }

        /// <summary>
        /// Does this axis cross the other axis at the zero point?
        /// </summary>
        public bool CrossesAtZero { get; set; }

        /// <summary>
        /// Make the axis label display all on one line.
        /// </summary>
        public bool LabelOnOneLine { get; set; }

        /// <summary>
        /// Axis minimum (optional). Will be automatically calculated if null.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// Axis maximum (optional). Will be automatically calculated if null.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// Axis tick interval (optional). Will be automatically calculated if null.
        /// </summary>
        public double? Interval { get; set; }

        /// <summary>
        /// Default constructor provided for json deserialization.
        /// Please don't use this.
        /// </summary>
        [Obsolete]
        public Axis() { }

        /// <summary>
        /// Create an axis instance.
        /// </summary>
        /// <param name="title">Axis title.</param>
        /// <param name="position">Axis location/position.</param>
        /// <param name="inverted">Is the axis inverted?</param>
        /// <param name="crossesZero">Should the axis cross through the origin?</param>
        /// <param name="min">Axis minimum (optional). Will be automatically calculated if null.</param>
        /// <param name="max">Axis maximum (optional). Will be automatically calculated if null.</param>
        /// <param name="interval">Axis tick interval (optional). Will be automatically calculated if null.</param>
        /// <param name="labelOnOneLine">Make the axis label display all on one line.</param>
        public Axis(string title, AxisPosition position, bool inverted, bool crossesZero, double? min = null, double? max = null, double? interval = null, bool labelOnOneLine = false)
        {
            Inverted = inverted;
            CrossesAtZero = crossesZero;
            Minimum = min;
            Maximum = max;
            Interval = interval;
            Title = title;
            Position = position;
            LabelOnOneLine = labelOnOneLine;
        }

        /// <summary>
        /// Simpler constructor with defaults which will be sensible in most cases.
        /// </summary>
        /// <param name="title">Axis title.</param>
        /// <param name="position">Axis location/position.</param>
        public Axis(string title, AxisPosition position) : this(title, position, false, false, null, null, null)
        {
        }
    }
}
