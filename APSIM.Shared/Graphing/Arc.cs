using System.Collections.Generic;
using System;
using System.Drawing;
using System.Text.Json.Serialization;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates an arc on a directed graph</summary>
    [Serializable]
    public class Arc : GraphObject
    {
        private int clickTolerence = 20;

        /// <summary>Source node (where arc starts)</summary>
        public int SourceID { get; set; }

        /// <summary>Destination node (where arc finishes)</summary>
        public int DestinationID { get; set; }

        /// <summary>Test conditions that need to be satisfied for this transition</summary>
        public List<string> Conditions { get; set; }

        /// <summary>Actions undertaken when making this transition</summary>
        public List<string> Actions { get; set; }

        /// <summary>Bezier Points for calculated for the arc. Used by GUI</summary>
        [JsonIgnore]
        public List<Point> BezierPoints { get; set; }

        /// <summary>Reference to the starting node of the arc. Linked at runtime and not stored.</summary>
        [JsonIgnore]
        public Node Source { get; set; }

        /// <summary>Bezier Points for calculated for the arc. Linked at runtime and not stored.</summary>
        [JsonIgnore]
        public Node Destination { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Arc()
        {
        }

        /// <summary>
        /// Create a copy of the given arc.
        /// </summary>
        /// <param name="x">An arc to be copied.</param>
        public Arc(Arc x)
        {
            if (x != null)
                CopyFrom(x);
        }

        /// <summary>
        /// Copy all properties from a given arc.
        /// </summary>
        /// <param name="x">An <see cref="Arc" />.</param>
        public void CopyFrom(Arc x)
        {
            ID = x.ID;
            Name = x.Name;
            Location = x.Location;
            Colour = x.Colour;
            SourceID = x.SourceID;
            Source = x.Source;
            DestinationID = x.DestinationID;
            Destination = x.Destination;
            Conditions = new List<string>(x.Conditions);
            Actions = new List<string>(x.Actions);
            BezierPoints = new List<Point>(x.BezierPoints);
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public override bool HitTest(Point clickPoint)
        {
            foreach (Point p in BezierPoints)
            {
                if (p.X > clickPoint.X - clickTolerence && p.X < clickPoint.X + clickTolerence)
                {
                    if (p.Y > clickPoint.Y - clickTolerence && p.Y < clickPoint.Y + clickTolerence)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="selection"></param>
        public override bool HitTest(Rectangle selection)
        {
            foreach (Point p in BezierPoints)
            {
                double minX = selection.X - clickTolerence;
                double maxX = selection.X + clickTolerence + selection.Width;
                double minY = selection.Y - clickTolerence;
                double maxY = selection.Y + clickTolerence + selection.Height;

                if (minX < p.X && p.X < maxX && minY < p.Y && p.Y < maxY)
                    return true;
            }
            return false;
        }

        
    }
}