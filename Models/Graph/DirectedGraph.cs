namespace Models
{
    using Models.Core;
    using Models.Interfaces;
    using System.Collections.Generic;
    using System.Drawing;
    using System;

    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public class Node
    {
        /// <summary>Name of node</summary>
        public string Name { get; set; }

        /// <summary>Location of node (centre point)</summary>
        public Point Location { get; set; }

        /// <summary>Fill colour of node</summary>
        public Color Colour { get; set; }

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
            Colour = Color.Beige;
            OutlineColour = Color.Black;
        }
    }

    /// <summary>Encapsulates an arc on a directed graph</summary>
    [Serializable]
    public class Arc
    {
        /// <summary>Source node (where arc starts)</summary>
        public string SourceName { get; set; }

        /// <summary>Destination node (where arc finishes)</summary>
        public string DestinationName { get; set; }

        /// <summary>Location of arc (centre/control point)</summary>
        public Point Location { get; set; }

        /// <summary>Colour of arc</summary>
        public Color Colour { get; set; }

        /// <summary>Text to show on arc</summary>
        public string Text { get; set; }
    }

    /// <summary>Encapsulates a directed graph</summary>
    [Serializable]
    public class DirectedGraph : AutoDocumentation.ITag
    {
        private Point nextNodePosition = new Point(50, 50);
        private List<Node> nodesToKeep = new List<Node>();
        private List<Arc> arcsToKeep = new List<Arc>();


        /// <summary>A collection of nodes</summary>
        public List<Node> Nodes { get; set; }

        /// <summary>A collection of arcs</summary>
        public List<Arc> Arcs { get; set; }

        /// <summary>Constructor</summary>
        public DirectedGraph()
        {
            Nodes = new List<Node>();
            Arcs = new List<Arc>();
        }

        /// <summary>Begin constrction of graph</summary>
        public void Begin()
        {
            nodesToKeep.Clear();
            arcsToKeep.Clear();
        }

        /// <summary>End constrction of graph</summary>
        public void End()
        {
            // Remove unwanted nodes and arcs.
            Nodes.RemoveAll(node => !nodesToKeep.Contains(node));
            Arcs.RemoveAll(arc => !arcsToKeep.Contains(arc));
        }

        /// <summary>Add a new node to the graph</summary>
        public void AddTransparentNode(string name)
        {
            Node newNode = Nodes.Find(node => node.Name == name);
            if (newNode == null)
            {
                newNode = new Node();
                newNode.Location = nextNodePosition;
                nextNodePosition.X += 150;
                if (nextNodePosition.X > 500)
                {
                    nextNodePosition.X = 50;
                    nextNodePosition.Y = nextNodePosition.Y + 150;
                }
                Nodes.Add(newNode);
            }
            newNode.Name = name;
            newNode.Transparent = true;
            nodesToKeep.Add(newNode);
        }

        /// <summary>Add a new node to the graph</summary>
        public void AddNode(string name, Color colour, Color outlineColour)
        {
            Node newNode = Nodes.Find(node => node.Name == name);
            if (newNode == null)
            {
                newNode = new Node();
                newNode.Location = nextNodePosition;
                nextNodePosition.X += 150;
                if (nextNodePosition.X > 500)
                {
                    nextNodePosition.X = 50;
                    nextNodePosition.Y = nextNodePosition.Y + 150;
                }
                Nodes.Add(newNode);
            }
            newNode.Name = name;
            newNode.Colour = colour;
            newNode.OutlineColour = outlineColour;
            nodesToKeep.Add(newNode);
        }

        /// <summary>Add a new arc to the graph</summary>
        public void AddArc(string text, string source, string destination, Color colour)
        {
            Arc newArc = Arcs.Find(arc => arc.SourceName == source && arc.DestinationName == destination && arc.Text == text);
            if (newArc == null)
            {
                newArc = new Arc();
                newArc.Text = text;
                newArc.SourceName = source;
                newArc.DestinationName = destination;
                Arcs.Add(newArc);
            }
            newArc.Colour = colour;
            arcsToKeep.Add(newArc);
        }
    }


}
