namespace APSIM.Interop.Visualisation
{
    using System;
    using APSIM.Interop.Drawing;
    using System.Drawing;
    using APSIM.Shared.Graphing;

    /// <summary>
    /// Encapsulates a node on a directed graph. The 'Location' property in the
    /// base class represents the center of this node (circle).
    /// </summary>
    public class DGNode : DGObject
    {
        /// <summary>
        /// Foreground colour.
        /// </summary>
        public Color ForegroundColour = Color.Black;

        /// <summary>
        /// Description. Unsure if this is actually used.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Diameter of the node (in px?).
        /// </summary>
        public int Width { get { return 120; } }

        /// <summary>
        /// Should the node be transparent?
        /// </summary>
        private bool transparent;

        /// <summary>Constructor</summary>
        public DGNode(Node directedGraphNode) : base(directedGraphNode.ID, directedGraphNode.Name, directedGraphNode.Colour, directedGraphNode.Location)
        {
            ForegroundColour = directedGraphNode.OutlineColour;
            transparent = directedGraphNode.Transparent;
        }

        /// <summary>Get a DirectedGraph node from this instance.</summary>
        public Node ToNode()
        {
            Node n = new Node();
            n.ID = ID;
            n.Name = Name;
            n.Location = new System.Drawing.Point((int)Location.X, (int)Location.Y);
            n.Colour = System.Drawing.Color.FromArgb(Colour.R, Colour.G, Colour.B);
            n.OutlineColour = System.Drawing.Color.FromArgb(ForegroundColour.R, ForegroundColour.G, ForegroundColour.B);
            n.Transparent = transparent;
            return n;
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        public override void Paint(IDrawContext context)
        {
            Color outlineColour;
            if (Selected)
                outlineColour = Color.Blue;
            else if (Hover)
                outlineColour = Color.OrangeRed;
            else if (transparent)
                outlineColour = DefaultBackgroundColour;
            else
                outlineColour = DefaultOutlineColour;

            Color backgroundColour = transparent ? DefaultBackgroundColour : Colour;
            Color textColour = DefaultOutlineColour;

            // Draw circle
            context.SetColour(outlineColour);
            context.SetLineWidth(3);
            context.NewPath();
            context.Arc(Location.X, Location.Y, Width/2, 0, 2 * Math.PI);
            context.StrokePreserve();
            context.SetColour(backgroundColour);
            context.Fill();

            // Write text
            context.SetLineWidth(1);
            context.SetColour(textColour);
            context.SetFontSize(13);


            DrawCentredText(context, Name, Location);
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public override bool HitTest(Point clickPoint)
        {
            double dist = GetDistance(Location, clickPoint);
            return dist < (Width / 2);
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public override bool HitTest(Rectangle selection)
        {
            double minX = selection.X - (Width / 2);
            double maxX = selection.X + selection.Width + (Width / 2);
            double minY = selection.Y - (Width / 2);
            double maxY = selection.Y + selection.Height + (Width / 2);

            if (minX < Location.X && Location.X < maxX && minY < Location.Y && Location.Y < maxY)
                return true;
            else
                return false;
        }
    }
}
