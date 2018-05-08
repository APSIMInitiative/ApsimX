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

        /// <summary>Gets a list of factors</summary>
        List<KeyValuePair<string, string>> GetFactors();

        /// <summary>Generates an .apsimx file for each simulation.</summary>
        /// <param name="path">Directory to save the file to.</param>
        void GenerateApsimXFile(string path);
    }
}
