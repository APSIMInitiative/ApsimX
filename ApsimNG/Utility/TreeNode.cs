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
}
