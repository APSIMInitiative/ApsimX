using APSIM.Core;

namespace Models.Core
{
    /// <summary>
    /// An interface for a model that creates dynamic temporary nodes
    /// All generated nodes must be marked as read only and not saved to the 
    /// apsimx file.
    /// </summary>
    public interface IGenerateNodes
    {
        /// <summary>
        /// Generates a list of commands and then runs them over the open file 
        /// to create and add nodes to the tree.
        /// </summary>
        public bool CreateNodes();

        /// <summary>
        /// Cleans up all generated nodes from the tree.
        /// </summary>
        public bool DeleteNodes();

        /// <summary>
        /// Does this model require updating due to a change in its properties.
        /// This allows us to skip remaking nodes if nothing has changed on 
        /// this since it loaded.
        /// </summary>
        public bool RequiresUpdating();

        /// <summary>
        /// Filepath object to all Nodes system to pass in working directory.
        /// </summary>
        public APSIMFilePath FilePath { get; set; }
    }
}
