using System.Collections.Generic;
using System.Drawing;
using System;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public class Node : GraphObject
    {
        /// <summary>Description of the node.</summary>
        public string Description { get; set; }

        /// <summary>Outline colour of node</summary>
        public Color OutlineColour { get; set; }

        /// <summary>
        /// If true, the node's background and outline will be the same colour
        /// as the canvas' background.
        /// </summary>
        public bool Transparent { get; set; }

        /// <summary>Constructor</summary>
        public Node()
        {
            Name = "";
            Description = "";
            Colour = Color.Beige;
            OutlineColour = Color.Black;
        }

        /// <summary>
        /// Copy all properties of a node into this node.
        /// </summary>
        /// <param name="x">The node to be copied.</param>
        public void CopyFrom(Node x)
        {
            Colour = x.Colour;
            OutlineColour = x.OutlineColour;
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
                OutlineColour = Color.Black;
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
                OutlineColour = Color.Black;
            }
            Description = description;
        }
    }
}
