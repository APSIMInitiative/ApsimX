namespace Models.Core.Run
{
    using Models.Storage;

    /// <summary>
    /// An interface for a post simulation tool
    /// </summary>
    public interface IPostSimulationTool
    {
        /// <summary>Runs the tool</summary>
        /// <param name="store">The data store where output should be stored</param>
        void Run(IDataStore store);
    }
}
