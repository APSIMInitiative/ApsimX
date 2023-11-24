using System.Drawing;
using System;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates a node on a directed graph</summary>
    [Serializable]
    public class GraphObject
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
}
