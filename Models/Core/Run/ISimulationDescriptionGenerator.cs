namespace Models.Core.Run
{
    using System.Collections.Generic;

    /// <summary>
    /// An interface for a model that generates simulation descriptions.
    /// </summary>
    public interface ISimulationDescriptionGenerator
    {
        /// <summary>Gets a list of simulation descriptions.</summary>
        List<SimulationDescription> GenerateSimulationDescriptions();
    }
}
