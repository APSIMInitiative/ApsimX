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
        List<ISimulationGeneratorFactor> GetFactors();

        /// <summary>Generates an .apsimx file for each simulation.</summary>
        /// <param name="path">Directory to save the file to.</param>
        void GenerateApsimXFile(string path);
    }

    /// <summary>Represents the factors coming from a ISimulationGenerator</summary>
    public interface ISimulationGeneratorFactor
    {
        /// <summary>Name of factor</summary>
        string Name { get; }

        /// <summary>Value of factor</summary>
        string Value { get; }

        /// <summary>The name of the column that our filter is based on e.g. 'SimulationName'</summary>
        string ColumnName { get; }

        /// <summary>
        /// Column values so that a DataView filter can be constructed for this factor
        /// e.g. 'Gatton87', 'Gatton88'
        /// </summary>
        List<string> ColumnValues { get; }

        /// <summary>Returns true if this object is equal to rhs</summary>
        bool Equals(ISimulationGeneratorFactor rhs);

        /// <summary>
        /// Merge the specified object into this object
        /// </summary>
        /// <param name="from">The object to copy values from</param>
        void Merge(ISimulationGeneratorFactor from);
    }
}
