namespace Models.Core.Run
{
    /// <summary>An interface for a post simulation tool</summary>
    public interface IPostSimulationTool
    {
        /// <summary>Main run method for performing our calculations and storing data.</summary>
        void Run();
    }
}
