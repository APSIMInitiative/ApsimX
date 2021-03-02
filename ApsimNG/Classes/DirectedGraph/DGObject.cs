namespace ApsimNG.Classes.DirectedGraph
{
    using Cairo;
    using OxyPlot;
    using System;

    /// <summary>A base object for all directed graph view objects</summary>
    public abstract class DGObject
    {
        public bool Selected { get; set; }
        public PointD Location { get; set; }
        public string Name { get; set; }
        public OxyColor Colour { get; set; }
        public static OxyColor DefaultOutlineColour { get; set; }
        public static OxyColor DefaultBackgroundColour { get; set; }

        /// <summary>Constructor</summary>
        public DGObject()
        {
            Colour = OxyColors.Black;
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        public abstract void Paint(Context graphicsObject);

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public abstract bool HitTest(PointD clickPoint);

        /// <summary>Return a distance between the two points</summary>
        /// <param name="clickPoint"></param>
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
