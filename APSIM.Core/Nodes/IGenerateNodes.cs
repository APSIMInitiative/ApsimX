namespace Models.Core
{
    /// <summary>An interface for a model that creates dynamic temporary nodes</summary>
    public interface IGenerateNodes
    {
        /// <summary>
        /// Generate the nodes
        /// </summary>
        public bool GenerateNodes();
    }
}
