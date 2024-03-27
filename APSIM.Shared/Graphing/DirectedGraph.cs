using System.Collections.Generic;
using System.Drawing;
using System;

namespace APSIM.Shared.Graphing
{ 
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        public Node AddNode(Node node)
        {
            Node newNode = new Node();
            newNode.CopyFrom(node);
            Nodes.Add(newNode);
            return newNode;
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
            Arc newArc = new Arc();
            newArc.CopyFrom(arc);
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

        /// <summary>Get a reference to the node with the given ID. Null if not found</summary>
        public Node GetNodeByID(int id)
        {
            foreach (Node node in Nodes)
                if (node.ID == id)
                    return node;
            return null;
        }

        /// <summary>Get a reference to the arc with the given ID. Null if not found</summary>
        public Arc GetArcByID(int id)
        {
            foreach (Arc arc in Arcs)
                if (arc.ID == id)
                    return arc;
            return null;
        }

        /// <summary>
        /// Gets the next available ID for a Node or Arc.
        /// A restricted list can be provided for cases where multiple ID must be requested in a row.
        /// </summary>
        /// <returns></returns>
        public int NextID(List<int> restricted = null)
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
                if (restricted != null)
                {
                    for (int i = 0; i < restricted.Count && !found; i++)
                    {
                        if (id == restricted[i])
                            found = true;
                    }
                }
            }
            return id;
        }
    }
}
