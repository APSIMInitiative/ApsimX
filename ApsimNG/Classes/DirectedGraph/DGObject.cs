namespace ApsimNG.Classes.DirectedGraph
{
    using Cairo;
    using OxyPlot;
    using System;

    /// <summary>A base object for all directed graph view objects</summary>
    public abstract class DGObject
    {
        /// <summary>
        /// Is the object selected?
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// Cartesian location.
        /// </summary>
        public PointD Location { get; set; }

        /// <summary>
        /// Name of the object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Background colour.
        /// </summary>
        public OxyColor Colour { get; set; }

        /// <summary>
        /// Default outline colour if none is specified.
        /// </summary>
        public static OxyColor DefaultOutlineColour { get; set; }

        /// <summary>
        /// Default background colour if none is specified.
        /// </summary>
        public static OxyColor DefaultBackgroundColour { get; set; }

        /// <summary>Constructor</summary>
        public DGObject()
        {
            Colour = OxyColors.Black;
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="graphicsObject">The graphics context to draw on</param>
        public abstract void Paint(Context graphicsObject);

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public abstract bool HitTest(PointD clickPoint);

        /// <summary>Return a distance between the two points</summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <remarks>This doesn't belong here.</remarks>
        public static double GetDistance(PointD point1, PointD point2)
        {
            // Pythagoras theorem c^2 = a^2 + b^2
            // thus c = square root(a^2 + b^2)
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }

    }
}
