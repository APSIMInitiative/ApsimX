namespace Models.Core.Run
{
    using System.Collections.Generic;

    interface ISimulationDescriptionGenerator
    {
        /// <summary>Gets a list of simulation descriptions.</summary>
        List<SimulationDescription> GenerateSimulationDescriptions();
    }
}
