namespace APSIM.Shared.Graphing
{
    using System.Collections.Generic;
    using System.Drawing;
    using System;
    using System.Linq;

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
        public string Name { get; set; }

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
            SourceName = x.SourceName;
            DestinationName = x.DestinationName;
            Location = x.Location;
            Colour = x.Colour;
            Name = x.Name;
        }
    }

    /// <summary>Encapsulates a directed graph</summary>
    [Serializable]
    public class DirectedGraph
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
        public void AddNode(string name, Color colour, Color outlineColour, Point location = new Point())
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
            newNode.Location = location;
            newNode.OutlineColour = outlineColour;
            nodesToKeep.Add(newNode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(Node node)
        {
            AddNode(node.Name, node.Colour, node.OutlineColour, node.Location);
        }

        /// <summary>Remove a node from the graph</summary>
        public void DelNode(string name)
        {
            Node nodeToDelete = Nodes.Find(node => node.Name == name);
            if (nodeToDelete != null)
            {
                nodesToKeep.Remove(nodeToDelete);
                Nodes.Remove(nodeToDelete);
            }
        }

        /// <summary>Add a new arc to the graph</summary>
        public void AddArc(Arc arc)
        {
            AddArc(arc.Name, arc.SourceName, arc.DestinationName, arc.Colour, arc.Location);
        }

        /// <summary>Add a new arc to the graph</summary>
        public void AddArc(string text, string source, string destination, Color colour, Point location = new Point())
        {
            Arc newArc = Arcs.Find(arc => arc.SourceName == source && arc.DestinationName == destination && arc.Name == text);
            if (newArc == null)
            {
                newArc = new Arc();
                newArc.Name = text;
                newArc.SourceName = source;
                newArc.DestinationName = destination;
                Arcs.Add(newArc);
            }
            newArc.Colour = colour;
            newArc.Location = location;
            arcsToKeep.Add(newArc);
        }

        /// <summary>Remove a node from the graph</summary>
        public void DelArc(string name)
        {
            Arc arcToDelete = Arcs.Find(arc => arc.Name == name);
            if (arcToDelete != null)
            {
                arcsToKeep.Remove(arcToDelete);
                Arcs.Remove(arcToDelete);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string NextArcID()
        {
            int i = 1;
            while (Arcs.Any(a => a.Name == $"Arc {i}"))
                i++;
            return $"Arc {i}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string NextNodeID()
        {
            int i = 1;
            while (Nodes.Any(a => a.Name == $"Node {i}"))
                i++;
            return $"Node {i}";
        }
    }
}
