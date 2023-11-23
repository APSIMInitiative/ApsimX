namespace APSIM.Interop.Visualisation
{
    using System.Drawing;
    using System;
    using APSIM.Interop.Drawing;

    /// <summary>A base object for all directed graph view objects</summary>
    public abstract class DGObject
    {
        /// <summary>
        /// ID of the object.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Name of the object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Cartesian location.
        /// </summary>
        public Point Location { get; set; }

        /// <summary>
        /// Background colour.
        /// </summary>
        public Color Colour { get; set; } = Color.Black;

        /// <summary>
        /// Is the object selected?
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// Is the object hovered over?
        /// </summary>
        public bool Hover { get; set; }

        /// <summary>
        /// Default outline colour if none is specified.
        /// </summary>
        public static Color DefaultOutlineColour { get; set; } = Color.Black;

        /// <summary>
        /// Default background colour if none is specified.
        /// </summary>
        public static Color DefaultBackgroundColour { get; set; } = Color.White;

        /// <summary>Constructor</summary>
        public DGObject(int id, string name, Color colour, Point location)
        {
            ID = id;
            Name = name;
            Colour = colour;
            Location = new Point(location.X, location.Y);
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="graphicsObject">The graphics context to draw on</param>
        public abstract void Paint(IDrawContext graphicsObject);

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public abstract bool HitTest(Point clickPoint);

        public abstract bool HitTest(Rectangle selection);

        /// <summary>Return a distance between the two points</summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <remarks>This doesn't belong here.</remarks>
        public static double GetDistance(Point point1, Point point2)
        {
            // Pythagoras theorem c^2 = a^2 + b^2
            // thus c = square root(a^2 + b^2)
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }

        /// <summary>
        /// Draw centred text
        /// </summary>
        /// <param name="context">The graphics context to draw on</param>
        /// <param name="text">The text to draw</param>
        /// <param name="point">The point to centre the text around</param>
        protected void DrawCentredText(IDrawContext context, string text, Point point)
        {
            (int left, int top, int width, int height) = context.GetPixelExtents(text, false, false);
            double x = point.X - (width / 2 + left);
            double y = point.Y - (height / 2 + top);
            context.MoveTo(x, y);
            context.DrawText(text, false, false);
        }
    }
}
