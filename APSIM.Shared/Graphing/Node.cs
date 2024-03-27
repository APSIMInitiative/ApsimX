using System.Drawing;
using System;
using Newtonsoft.Json;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public class Node : GraphObject
    {
        /// <summary>Description of the node.</summary>
        public string Description { get; set; }

        /// <summary>
        /// If true, the node's background and outline will be the same colour
        /// as the canvas' background.
        /// </summary>
        public bool Transparent { get; set; }

        /// <summary>
        /// Diameter of the node (in px?).
        /// </summary>
        [JsonIgnore]
        public int Width { get { return 120; } }

        /// <summary>Constructor</summary>
        public Node()
        {
            Name = "";
            Description = "";
            Colour = Color.Beige;
            Transparent = false;
        }

        /// <summary>
        /// Copy all properties of a node into this node.
        /// </summary>
        /// <param name="x">The node to be copied.</param>
        public void CopyFrom(Node x)
        {
            Colour = x.Colour;
            Location = x.Location;
            Name = x.Name;
            Description = x.Description;
            ID = x.ID;
            Transparent = x.Transparent;
        }

        /// <summary>
        /// Constructor - creates a copy of x.
        /// </summary>
        /// <param name="x">The node to be copied.</param>
        public Node(Node x)
        {
            if (x != null)
                CopyFrom(x);
            else
            {
                Colour = Color.Beige;
            }
        }

        /// <summary>
        /// Constructor - creates a copy of x with given description
        /// </summary>
        /// <param name="x">The node to be copied.</param>
        /// <param name="description">Description for the node</param>
        public Node(Node x, string description)
        {
            if (x != null)
                CopyFrom(x);
            else
            {
                Colour = Color.Beige;
            }
            Description = description;
        }

        

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="clickPoint"></param>
        public override bool HitTest(Point clickPoint)
        {
            double dist = GetDistance(Location, clickPoint);
            return dist < (Width / 2);
        }

        /// <summary>Return true if the clickPoint is on this object</summary>
        /// <param name="selection"></param>
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
