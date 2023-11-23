using System.Collections.Generic;
using System.Drawing;
using System;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public class GraphElement
    {
        /// <summary>ID for Node</summary>
        public int ID { get; set; }

        /// <summary>Name of node</summary>
        public string Name { get; set; }

        /// <summary>Location of node (centre point)</summary>
        public Point Location { get; set; }

        /// <summary>Fill colour of node</summary>
        public Color Colour { get; set; }
    }

    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public class Node : GraphElement
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

    /// <summary>Encapsulates an arc on a directed graph</summary>
    [Serializable]
    public class Arc: GraphElement
    {
        /// <summary>Source node (where arc starts)</summary>
        public int SourceID { get; set; }

        /// <summary>Destination node (where arc finishes)</summary>
        public int DestinationID { get; set; }

        /// <summary>Test conditions that need to be satisfied for this transition</summary>
        public List<string> Conditions { get; set; }

        /// <summary>Actions undertaken when making this transition</summary>
        public List<string> Actions { get; set; }

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
            DestinationID = x.DestinationID;
            Conditions = new List<string>(x.Conditions);
            Actions = new List<string>(x.Actions);
        }
    }

    /// <summary>Encapsulates a directed graph</summary>
    [Serializable]
    public class DirectedGraph
    {
        private Point nextNodePosition = new Point(50, 50);

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
            Nodes.Clear();
            Arcs.Clear();
        }

        /// <summary>End constrction of graph</summary>
        public void End()
        {
            // Remove unwanted nodes and arcs.
            //Nodes.RemoveAll(node => !nodesToKeep.Contains(node));
            //Arcs.RemoveAll(arc => !arcsToKeep.Contains(arc));
        }

        /// <summary>Add a new node to the graph</summary>
        public void AddTransparentNode(string name)
        {
            /*
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
            */
        }

        /// <summary>Add a new node to the graph</summary>
        public Node AddNode(int id, string name, Color colour, Color outlineColour, Point? location = null)
        {
            Node newNode = new Node();
            newNode.ID = id;
            if (newNode.ID == 0)
                newNode.ID = NextNodeID();
            newNode.Name = name;
            if (newNode.Name == null)
                newNode.Name = NextNodeName(newNode.ID);
            newNode.Colour = colour;
            newNode.OutlineColour = outlineColour;
            
            if (location == null)
            {
                newNode.Location = nextNodePosition;
                nextNodePosition.X += 150;
                if (nextNodePosition.X > 500)
                {
                    nextNodePosition.X = 50;
                    nextNodePosition.Y = nextNodePosition.Y + 150;
                }
            } 
            else
            {
                newNode.Location = (Point)location;
            }
            
            Nodes.Add(newNode);
            return newNode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public Node AddNode(Node node)
        {
            return AddNode(node.ID, node.Name, node.Colour, node.OutlineColour, node.Location);
        }

        /// <summary>Remove a node from the graph</summary>
        public void DelNode(string name)
        {
            Node nodeToDelete = Nodes.Find(node => node.Name == name);
            if (nodeToDelete != null)
            {
                Nodes.Remove(nodeToDelete);
            }
        }

        /// <summary>Add a new arc to the graph</summary>
        public Arc AddArc(Arc arc)
        {
            return AddArc(arc.ID, arc.Name, arc.SourceID, arc.DestinationID, arc.Colour, arc.Location);
        }

        /// <summary>Add a new arc to the graph</summary>
        public Arc AddArc(int id, string text, int source, int destination, Color colour, Point? location = null)
        {
            Arc newArc = new Arc();
            newArc.ID = id;
            if (newArc.ID == 0)
                newArc.ID = NextArcID();
            newArc.Name = text;
            if (newArc.Name == null)
                newArc.Name = NextArcName(newArc.ID);
            newArc.SourceID = source;
            newArc.DestinationID = destination;
            newArc.Colour = colour;
            if (location == null)
                newArc.Location = new Point();
            else
                newArc.Location = (Point)location;

            Arcs.Add(newArc);
            return newArc;
        }

        /// <summary>Remove a node from the graph</summary>
        public void DelArc(string name)
        {
            Arc arcToDelete = Arcs.Find(arc => arc.Name == name);
            if (arcToDelete != null)
            {
                Arcs.Remove(arcToDelete);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string NextArcName(int id)
        {
            return $"Arc {id}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string NextNodeName(int id)
        {
            return $"Node {id}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int NextArcID()
        {
            int id = 0;
            bool found = true;
            while(found)
            {
                id += 1;
                found = false;
                for (int i = 0; i < Nodes.Count && !found; i++)
                {
                    if (id == Nodes[i].ID)
                        found = true;
                }
                for (int i = 0; i < Arcs.Count && !found; i++)
                {
                    if (id == Arcs[i].ID)
                        found = true;
                }
            }
            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int NextNodeID()
        {
            int id = 0;
            bool found = true;
            while (found)
            {
                id += 1;
                found = false;
                for (int i = 0; i < Nodes.Count && !found; i++)
                {
                    if (id == Nodes[i].ID)
                        found = true;
                }
                for (int i = 0; i < Arcs.Count && !found; i++)
                {
                    if (id == Arcs[i].ID)
                        found = true;
                }
            }
            return id;
        }
    }
}
