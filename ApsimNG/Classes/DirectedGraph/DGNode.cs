namespace ApsimNG.Classes.DirectedGraph
{
    using Cairo;
    using Models;
    using OxyPlot;
    using OxyPlot.GtkSharp;
    using System;

    /// <summary>
    /// Encapsulates a node on a directed graph. The 'Location' property in the
    /// base class represents the center of this node (circle).
    /// </summary>
    public class DGNode : DGObject
    {
        public OxyColor ForegroundColour = OxyColors.Black;
        public string Description { get; set; }
        public int Width { get { return 120; } }
        private bool transparent;
        /// <summary>Constructor</summary>
        public DGNode(Node directedGraphNode)
        {
            Name = directedGraphNode.Name;
            Location = new PointD(directedGraphNode.Location.X, directedGraphNode.Location.Y);
            Colour = OxyPlot.OxyColor.FromRgb(directedGraphNode.Colour.R, directedGraphNode.Colour.G, directedGraphNode.Colour.B);
            ForegroundColour = OxyPlot.OxyColor.FromRgb(directedGraphNode.OutlineColour.R, directedGraphNode.OutlineColour.G, directedGraphNode.OutlineColour.B);
            transparent = directedGraphNode.Transparent;
        }

        /// <summary>Get a DirectedGraph node from this instance.</summary>
        public Node ToNode()
        {
            Node n = new Node();
            n.Name = Name;
            n.Location = new System.Drawing.Point((int)Location.X, (int)Location.Y);
            n.Colour = System.Drawing.Color.FromArgb(Colour.R, Colour.G, Colour.B);
            n.OutlineColour = System.Drawing.Color.FromArgb(ForegroundColour.R, ForegroundColour.G, ForegroundColour.B);
            n.Transparent = transparent;
            return n;
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        public override void Paint(Cairo.Context context)
        {
            OxyColor outlineColour;
            if (Selected)
                outlineColour = OxyColors.Blue;
            else if (transparent)
                outlineColour = DefaultBackgroundColour;
            else
                outlineColour = DefaultOutlineColour;

            OxyColor backgroundColour = transparent ? DefaultBackgroundColour : Colour;
            OxyColor textColour = DefaultOutlineColour;

            // Draw circle
            context.SetSourceColor(outlineColour);
            context.LineWidth = 3;
            context.NewPath();
            context.Arc(Location.X, Location.Y, Width/2, 0, 2 * Math.PI);
            context.StrokePreserve();
            context.SetSourceColor(backgroundColour);
            context.Fill();

            // Write text
            context.LineWidth = 1;
            context.SetSourceColor(textColour);
            context.SetFontSize(13);


            DrawCentredText(context, Name, Location);
        }

        /// <summary>
        /// Draw centred text
        /// </summary>
        /// <param name="context">The graphics context to draw on</param>
        /// <param name="text">The text to draw</param>
        /// <param name="point">The point to centre the text around</param>
        public static void DrawCentredText(Context context, string text, PointD point)
        {
            TextExtents extents = context.TextExtents(text);
            double x = point.X - (extents.Width / 2 + extents.XBearing);
            double y = point.Y - (extents.Height / 2 + extents.YBearing);
            context.MoveTo(x, y);
            context.ShowText(text);
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public override bool HitTest(PointD clickPoint)
        {
            double dist = GetDistance(Location, clickPoint);
            return dist < (Width / 2);
        }
    }
}
