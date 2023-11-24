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
