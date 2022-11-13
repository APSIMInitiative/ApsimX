using System;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    /// <summary>
    /// Encapsulates a node in the simulations tree.
    /// </summary>
    public class TreeNode
    {
        public int[] Indices { get; set; }

        public TreeNode(int[] indices)
        {
            Indices = indices;
        }

        /// <summary>
        /// Default constructor, provided for deserialization. Should not be used.
        /// </summary>
        public TreeNode()
        {

        }
    }

    public class ApsimFileMetadata
    {
        /// <summary>
        /// Filename.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Expanded nodes in the simulations tree.
        /// </summary>
        public TreeNode[] ExpandedNodes { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">Absolute path to file.</param>
        /// <param name="expandedNodes">List of expanded nodes in the simulations tree.</param>
        public ApsimFileMetadata(string file, TreeNode[] expandedNodes)
        {
            FileName = file;
            ExpandedNodes = expandedNodes;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">Absolute path to file.</param>
        public ApsimFileMetadata(string file)
        {
            FileName = file;
            ExpandedNodes = new TreeNode[0];
        }

        /// <summary>
        /// Default constructor, provided for deserialization. Should not be used.
        /// </summary>
        public ApsimFileMetadata()
        {

        }
    }
}
