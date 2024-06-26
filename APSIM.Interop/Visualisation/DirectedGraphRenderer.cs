using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using APSIM.Interop.Drawing;
using APSIM.Shared.Graphing;

namespace APSIM.Interop.Visualisation
{
    /// <summary>
    /// This class can draw a directed graph on an <see cref="IDrawContext"/>.
    /// </summary>
    public class DirectedGraphRenderer
    {
        /// <summary>
        /// The directed graph to be rendered.
        /// </summary>
        public DirectedGraph Graph { get; private set; }

        /// <summary>
        /// Create a new <see cref="DirectedGraph"/> instance.
        /// </summary>
        /// <param name="graph">The directed graph to be rendered.</param>
        public DirectedGraphRenderer(DirectedGraph graph)
        {
            Graph = graph;
        }

        /// <summary>
        /// Draw the directed graph on the given drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        public void Draw(IDrawContext context)
        {
            Draw(context, Graph);
        }

        /// <summary>
        /// Draw the given <see cref="DirectedGraph"/> on a drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        /// <param name="graph">The directed graph instance to be drawn.</param>
        public static void Draw(IDrawContext context, DirectedGraph graph)
        {
            Draw(context, graph.Arcs, graph.Nodes);
        }

        /// <summary>
        /// Draw the given arcs and nodes from a directed graph on a drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        /// <param name="arcs">Arcs to be drawn.</param>
        /// <param name="nodes">Nodes to be drawn.</param>
        public static void Draw(IDrawContext context, IEnumerable<Arc> arcs, IEnumerable<Node> nodes)
        {
            Draw(context, arcs, nodes, null);
        }

        /// <summary>
        /// Draw the nodes and arcs on the given drawing context.
        /// </summary>
        /// <param name="context">A drawing context.</param>
        /// <param name="arcs">The arcs in the directed graph.</param>
        /// <param name="nodes">The nodes in the directed graph.</param>
        public static void Draw(IDrawContext context, IEnumerable<Arc> arcs, IEnumerable<Node> nodes, DGRectangle rectangle)
        {
            foreach (Arc arc in arcs)
                DrawArc(context, arc);

            foreach (APSIM.Shared.Graphing.Node node in nodes)
                DrawNode(context, node);

            if (rectangle != null)
                rectangle.Paint(context);
        }

        /// <summary>
        /// Draw centred text
        /// </summary>
        /// <param name="context">The graphics context to draw on</param>
        /// <param name="text">The text to draw</param>
        /// <param name="point">The point to centre the text around</param>
        private static void DrawCentredText(IDrawContext context, string text, Point point)
        {
            (int left, int top, int width, int height) = context.GetPixelExtents(text, false, false);
            double x = point.X - (width / 2 + left);
            double y = point.Y - (height / 2 + top);
            context.MoveTo(x, y);
            context.DrawText(text, false, false);
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        /// <param name="node">Node to draw</param>
        private static void DrawNode(IDrawContext context, APSIM.Shared.Graphing.Node node)
        {
            Color outlineColour;
            if (node.Selected)
                outlineColour = Color.Blue;
            else if (node.Hover)
                outlineColour = Color.OrangeRed;
            else if (node.Transparent)
                outlineColour = GraphObject.DefaultBackgroundColour;
            else
                outlineColour = GraphObject.DefaultOutlineColour;

            Color backgroundColour = node.Transparent ? GraphObject.DefaultBackgroundColour : node.Colour;
            Color textColour = GraphObject.DefaultOutlineColour;

            // Draw circle
            context.SetColour(outlineColour);
            context.SetLineWidth(5);
            context.NewPath();
            context.Arc(node.Location.X, node.Location.Y, node.Width / 2, 0, 2 * Math.PI);
            context.StrokePreserve();
            context.SetColour(backgroundColour);
            context.Fill();

            // Write text
            context.SetLineWidth(1);
            context.SetColour(textColour);
            context.SetFontSize(13);
            DrawCentredText(context, node.Name, node.Location);
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        /// <param name="arc">The arc to draw</param>
        private static void DrawArc(IDrawContext context, Arc arc)
        {
            CalcBezPoints(arc);

            if (arc.BezierPoints.Count != 0)
            {
                // Create point for upper-left corner of drawing.
                context.NewPath();
                if (arc.Selected)
                    context.SetColour(Color.Blue);
                else if (arc.Hover)
                    context.SetColour(Color.OrangeRed);
                else
                    context.SetColour(GraphObject.DefaultOutlineColour);

                // Draw text if necessary
                if (arc.Name != null)
                    DrawCentredText(context, arc.Name, arc.Location);

                context.MoveTo(arc.BezierPoints[0].X, arc.BezierPoints[0].Y);
                Point controlPoint = new Point(arc.Location.X, arc.Location.Y);
                Point bezPoint = arc.BezierPoints[arc.BezierPoints.Count - 1];
                context.CurveTo(controlPoint.X, controlPoint.Y, controlPoint.X, controlPoint.Y, bezPoint.X, bezPoint.Y);
                context.Stroke();

                APSIM.Shared.Graphing.Node target = arc.Destination;

                // find closest point in the BezierPoints to the intersection point that is outside the target
                // work backwards through BezierPoints array and use the first one that is outside the target
                for (int i = arc.BezierPoints.Count - 1; i >= 0; i--)
                {
                    Point arrowHead;
                    if (!target.HitTest(arc.BezierPoints[i]))
                    {
                        arrowHead = arc.BezierPoints[i];
                        i--;
                        //keep moving along the line until distance = target radius
                        double targetRadius = target.Width / 2;
                        while (i >= 0)
                        {
                            double dist = GraphObject.GetDistance(arc.BezierPoints[i], arrowHead);
                            if (dist >= 20)
                            {
                                DrawArrow(context, arc.BezierPoints[i], arrowHead);
                                break;
                            }

                            i--;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>Draw an arrow on the specified graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        /// <param name="start">The start point of the arrow</param>
        /// <param name="end">The end point of the arrow</param>
        private static void DrawArrow(IDrawContext context, Point start, Point end)
        {
            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X) + Math.PI;

            double arrowLenght = 10;
            double arrowDegrees = 10;
            double x1 = start.X + arrowLenght * Math.Cos(angle - arrowDegrees);
            double y1 = start.Y + arrowLenght * Math.Sin(angle - arrowDegrees);
            double x2 = start.X + arrowLenght * Math.Cos(angle + arrowDegrees);
            double y2 = start.Y + arrowLenght * Math.Sin(angle + arrowDegrees);
            context.NewPath();
            context.MoveTo(x1, y1);
            context.LineTo(x2, y2);
            context.LineTo(end.X, end.Y);
            context.LineTo(x1, y1);
            context.StrokePreserve();
            context.Fill();
        }

        /// <summary>Calculate the bezier points</summary>
        private static void CalcBezPoints(Arc arc)
        {
            if (arc.BezierPoints == null)
                arc.BezierPoints = new List<Point>();
            arc.BezierPoints.Clear();

            APSIM.Shared.Graphing.Node source = arc.Source;
            APSIM.Shared.Graphing.Node target = arc.Destination;

            if (source == null || target == null) return;

            Point ep1 = new Point();
            Point ep2 = new Point();
            if (source != target)
            {
                ep1 = source.Location;
                ep2 = target.Location;
            }
            else
            {
                double d = source.Width / 4;
                double m;
                if ((source.Location.X - arc.Location.X) != 0)
                    m = Math.Atan((source.Location.Y - arc.Location.Y) / (double)(source.Location.X - arc.Location.X));
                else
                    if (source.Location.Y > arc.Location.Y)
                    m = Math.PI * 0.5;
                else
                    m = Math.PI * 1.5;
                double m1 = m - Math.PI / 2;
                double m2 = m + Math.PI / 2;
                ep1.X = source.Location.X + (int)(d * Math.Cos(m1));
                ep1.Y = source.Location.Y + (int)(d * Math.Sin(m1));
                ep2.X = source.Location.X + (int)(d * Math.Cos(m2));
                ep2.Y = source.Location.Y + (int)(d * Math.Sin(m2));
            }

            int start = (int)Math.Min(ep1.X, ep2.X);
            int end = (int)Math.Max(ep1.X, ep2.X);
            int xPoints = end - start;
            start = (int)Math.Min(ep1.Y, ep2.Y);
            end = (int)Math.Max(ep1.Y, ep2.Y);
            int yPoints = end - start;

            //will calc a min of 100 points
            int points = Math.Max(Math.Max(xPoints, yPoints), 50) * 2;
            double[] output = new double[points];
            double[] bezParameters = new double[8];
            bezParameters[0] = ep1.X;
            bezParameters[1] = ep1.Y;
            bezParameters[2] = arc.Location.X;
            bezParameters[3] = arc.Location.Y;
            bezParameters[4] = arc.Location.X;
            bezParameters[5] = arc.Location.Y;
            bezParameters[6] = ep2.X;
            bezParameters[7] = ep2.Y;

            BezierCurve bezCurve = new BezierCurve();
            bezCurve.Bezier2D(bezParameters, (points) / 2, output);
            for (int i = 0; i < points - 2; i += 2)
                arc.BezierPoints.Add(new Point((int)output[i], (int)output[i + 1]));
        }
    }
}