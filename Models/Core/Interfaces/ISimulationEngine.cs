namespace Models.Core.Interfaces
{
    /// <summary>
    /// An interface for the APSIM simulation engine
    /// </summary>
    public interface ISimulationEngine
    {
        /// <summary>Return link service</summary>
        Links Links { get; }

        /// <summary>Returns an instance of an events service</summary>
        /// <param name="model">The model the service is for</param>
        IEvent GetEventService(IModel model);

        /// <summary>Return filename</summary>
        string FileName { get; }
    }
}