namespace Models.Core.Run
{
    /// <summary>An interface for a pre simulation tool</summary>
    public interface IPreSimulationTool : IModel
    {
        /// <summary>Main run method for performing our calculations and storing data.</summary>
        void Run();
    }
}
