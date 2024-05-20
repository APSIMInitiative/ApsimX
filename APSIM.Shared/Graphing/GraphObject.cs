using System.Drawing;
using System;
using Newtonsoft.Json;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public abstract class GraphObject
    {
        /// <summary>ID for Node</summary>
        public int ID { get; set; }

        /// <summary>Name of node</summary>
        public string Name { get; set; }

        /// <summary>Location of node (centre point)</summary>
        public Point Location { get; set; }

        /// <summary>Fill colour of node</summary>
        public Color Colour { get; set; }

        /// <summary>
        /// Is the object selected?
        /// </summary>
        [JsonIgnore]
        public bool Selected { get; set; }

        /// <summary>
        /// Is the object hovered over?
        /// </summary>
        [JsonIgnore]
        public bool Hover { get; set; }

        /// <summary>
        /// Default outline colour if none is specified.
        /// </summary>
        [JsonIgnore]
        public static Color DefaultOutlineColour { get; set; } = Color.Black;

        /// <summary>
        /// Default background colour if none is specified.
        /// </summary>
        [JsonIgnore]
        public static Color DefaultBackgroundColour { get; set; } = Color.White;

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public abstract bool HitTest(Point clickPoint);

        /// <summary>Return true if the clickPoint is on this object</summary>
        public abstract bool HitTest(Rectangle selection);

        /// <summary>Return a distance between the two points</summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double GetDistance(Point point1, Point point2)
        {
            // Pythagoras theorem c^2 = a^2 + b^2
            // thus c = square root(a^2 + b^2)
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }
    }
}
