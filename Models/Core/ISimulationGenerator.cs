namespace Models.Core
{
    using System.Collections.Generic;

    /// <summary>An interface for something that can generate simulations to run</summary>
    public interface ISimulationGenerator
    {
        /// <summary>Gets the next simulation to run</summary>
        Simulation NextSimulationToRun(bool fullFactorial = true);

        /// <summary>Gets a list of simulation names</summary>
        IEnumerable<string> GetSimulationNames(bool fullFactorial = true);
    }
}
