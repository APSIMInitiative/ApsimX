namespace Models.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using APSIM.Shared.Utilities;

    /// <summary>Represents a factor that can be varied on a graph</summary>
    public class SimulationGeneratorFactor : ISimulationGeneratorFactor
    {
        /// <summary>The factor name e.g. 'Experiment'</summary>
        public string Name { get; private set; }

        /// <summary>The factor value e.g. 'APS14'</summary>
        public string Value { get; private set; }

        /// <summary>The name of the column that our filter is based on e.g. 'SimulationName'</summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// Column values so that a DataView filter can be constructed for this factor
        /// e.g. 'Gatton87', 'Gatton88'
        /// </summary>
        public List<string> ColumnValues { get; private set; }

        /// <summary>Constructor</summary>
        public SimulationGeneratorFactor(string factorName, string factorValue, string colName)
        {
            Name = factorName;
            Value = factorValue;
            ColumnName = colName;
            ColumnValues = new List<string>();
        }

        /// <summary>Returns true if this object is equal to rhs</summary>
        /// <param name="compareTo">The object to compare to</param>
        public bool Equals(ISimulationGeneratorFactor compareTo)
        {
            return Name == compareTo.Name && Value == compareTo.Value && ColumnName == compareTo.ColumnName;
        }

        /// <summary>
        /// Merge the specified object into this object
        /// </summary>
        /// <param name="from">The object to copy values from</param>
        public void Merge(ISimulationGeneratorFactor from)
        {
            ColumnValues.AddRange(from.ColumnValues);
        }

    }
}
