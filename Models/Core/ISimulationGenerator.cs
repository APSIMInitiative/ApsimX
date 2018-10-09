namespace Models.Core
{
    using System.Collections.Generic;
    using System.Data;

    /// <summary>An interface for something that can generate simulations to run</summary>
    public interface ISimulationGenerator
    {
        /// <summary>Gets the next simulation to run</summary>
        Simulation NextSimulationToRun(bool fullFactorial = true);

        /// <summary>Gets a list of simulation names</summary>
        IEnumerable<string> GetSimulationNames(bool fullFactorial = true);

        /// <summary>Gets a list of factors</summary>
        List<ISimulationGeneratorFactors> GetFactors();

        /// <summary>Generates an .apsimx file for each simulation.</summary>
        /// <param name="path">Directory to save the file to.</param>
        void GenerateApsimXFile(string path);
    }

    /// <summary>Represents the factors coming from a ISimulationGenerator</summary>
    public interface ISimulationGeneratorFactors
    {
        /// <summary>Name of factor</summary>
        List<KeyValuePair<string, string>> Factors { get; }

        /// <summary>The column name/value pairs that our filter is based on.</summary>
        List<KeyValuePair<string, string>> Columns { get; }

        /// <summary>Returns true if this object is equal to rhs</summary>
        bool Equals(ISimulationGeneratorFactors rhs);

        /// <summary>
        /// Get the value of a factor
        /// </summary>
        /// <param name="name">The name of the factor</param>
        string GetFactorValue(string name);

        /// <summary>
        /// Add a factor name/value pair to this factor
        /// </summary>
        /// <param name="factorName"></param>
        /// <param name="factorValue"></param>
        void AddFactor(string factorName, string factorValue);

        /// <summary>
        /// Add a factor name/value pair to this factor if it doesn't already exist
        /// </summary>
        /// <param name="factorName"></param>
        /// <param name="factorValue"></param>
        void AddFactorIfNotExist(string factorName, string factorValue);

        /// <summary>
        /// Remove the specified factor
        /// </summary>
        /// <param name="name">The name of factor to remove</param>
        void RemoveFactor(string name);

        /// <summary>
        /// Get the value of a factor
        /// </summary>
        /// <param name="name">The name of the factor</param>
        string GetColumnValue(string name);

        /// <summary>
        /// Remove the specified column
        /// </summary>
        /// <param name="name">The name of column to remove</param>
        void RemoveColumn(string name);

        /// <summary>
        /// Merge the specified object into this object
        /// </summary>
        /// <param name="from">The object to copy values from</param>
        void Merge(ISimulationGeneratorFactors from);
    }
}
