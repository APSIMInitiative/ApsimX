namespace ApsimNG.Classes.DirectedGraph
{
    using Cairo;
    using Models;
    using OxyPlot;
    using OxyPlot.GtkSharp;
    using System;
    using System.Collections.Generic;

    public class DGArc : DGObject
    {
        private int clickTolerence = 3;
        private DGNode sourceNode = null;
        private DGNode targetNode = null;
        private BezierCurve bezCurve = new BezierCurve();
        private List<PointD> bezPoints = new List<PointD>();
        private double[] bezParameters = new double[8];

        /// <summary>Constrcutor</summary>
        /// <param name="directedGraphArc">The arc information to use</param>
        /// <param name="allNodes">A list of all nodes</param>
        public DGArc(Arc directedGraphArc, List<DGNode> allNodes)
        {
            Location = new PointD(directedGraphArc.Location.X, directedGraphArc.Location.Y);
            sourceNode = allNodes.Find(node => node.Name == directedGraphArc.SourceName);
            targetNode = allNodes.Find(node => node.Name == directedGraphArc.DestinationName);
            Colour = OxyColor.FromRgb(directedGraphArc.Colour.R, directedGraphArc.Colour.G, directedGraphArc.Colour.B);
            Name = directedGraphArc.Text;
        }

        /// <summary>Get a DirectedGraph arc from this instance.</summary>
        public Arc ToArc()
        {
            Arc a = new Arc();
            a.Location = new System.Drawing.Point((int)Location.X, (int)Location.Y);
            if (sourceNode != null)
                a.SourceName = sourceNode.Name;
            if (targetNode != null)
                a.DestinationName = targetNode.Name;
            a.Text = Name;
            a.Colour = System.Drawing.Color.FromArgb(Colour.A, Colour.R, Colour.B);
            return a;
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        public override void Paint(Cairo.Context context)
        {
            CalcBezPoints();

            if (bezPoints.Count != 0)
            {
                // Create point for upper-left corner of drawing.
                context.NewPath();
                if (Selected)
                    context.SetSourceColor(OxyColors.Blue);
                else
                    context.SetSourceColor(DefaultOutlineColour);

                // Draw text if necessary
                if (Name != null)
                    DGNode.DrawCentredText(context, Name, Location);

                context.MoveTo(bezPoints[0]);
                PointD controlPoint = new PointD(Location.X, Location.Y);
                context.CurveTo(controlPoint, controlPoint, bezPoints[bezPoints.Count - 1]);
                context.Stroke();

                // find closest point in the bezPoints to the intersection point that is outside the target
                // work backwards through BezPoints array and use the first one that is outside the target
                for (int i = bezPoints.Count - 1; i >= 0; i--)
                {
                    PointD arrowHead;
                    if (!targetNode.HitTest(bezPoints[i]))
                    {
                        arrowHead = bezPoints[i];
                        i--;
                        //keep moving along the line until distance = target radius
                        double targetRadius = targetNode.Width / 2;
                        while (i >= 0)
                        {
                            double dist = GetDistance(bezPoints[i], arrowHead);
                            if (dist >= 20)
                            {
                                DrawArrow(context, bezPoints[i], arrowHead);
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
        private static void DrawArrow(Context context, PointD start, PointD end)
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
            context.LineTo(end);
            context.LineTo(x1, y1);
            context.StrokePreserve();
            context.Fill();
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public override bool HitTest(PointD clickPoint)
        {
            foreach (PointD p in bezPoints)
            {
                if (p.X > clickPoint.X - clickTolerence && p.X < clickPoint.X + clickTolerence)
                {
                    if (p.Y > clickPoint.Y - clickTolerence && p.Y < clickPoint.Y + clickTolerence)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Calculate the bezier points</summary>
        private void CalcBezPoints()
        {
            bezPoints.Clear();
            if (sourceNode == null || targetNode == null) return;
            PointD ep1 = new PointD();
            PointD ep2 = new PointD();
            if (sourceNode != targetNode)
            {
                ep1 = sourceNode.Location;
                ep2 = targetNode.Location;
            }
            else
            {
                double d = sourceNode.Width / 4;
                double m;
                if ((sourceNode.Location.X - Location.X) != 0)
                    m = Math.Atan((sourceNode.Location.Y - Location.Y) / (double)(sourceNode.Location.X - Location.X));
                else
                    if (sourceNode.Location.Y > Location.Y)
                    m = Math.PI * 0.5;
                else
                    m = Math.PI * 1.5;
                double m1 = m - Math.PI / 2;
                double m2 = m + Math.PI / 2;
                ep1.X = sourceNode.Location.X + (int)(d * Math.Cos(m1));
                ep1.Y = sourceNode.Location.Y + (int)(d * Math.Sin(m1));
                ep2.X = sourceNode.Location.X + (int)(d * Math.Cos(m2));
                ep2.Y = sourceNode.Location.Y + (int)(d * Math.Sin(m2));
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
            bezParameters[0] = ep1.X;
            bezParameters[1] = ep1.Y;
            bezParameters[2] = Location.X;
            bezParameters[3] = Location.Y;
            bezParameters[4] = Location.X;
            bezParameters[5] = Location.Y;
            bezParameters[6] = ep2.X;
            bezParameters[7] = ep2.Y;

            bezCurve.Bezier2D(bezParameters, (points) / 2, output);
            for (int i = 0; i < points - 2; i += 2)
            {
                bezPoints.Add(new PointD((int)output[i], (int)output[i + 1]));
            }
        }

    }

}
