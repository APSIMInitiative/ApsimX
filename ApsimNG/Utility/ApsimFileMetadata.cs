using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{

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
            FileName = Path.GetFullPath(file);
            ExpandedNodes = expandedNodes;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="file">Absolute path to file.</param>
        public ApsimFileMetadata(string file)
        {
            FileName = Path.GetFullPath(file);
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
