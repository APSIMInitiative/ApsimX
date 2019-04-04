namespace Models.Core
{
    using Models.Core.Run;
    using System.Collections.Generic;

    /// <summary>An interface for something that can generate simulations to run</summary>
    public interface ISimulationDescriptionGenerator
    {
        /// <summary>Gets a list of simulation descriptions.</summary>
        List<SimulationDescription> GenerateSimulationDescriptions();
    }
}
