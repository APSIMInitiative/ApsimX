namespace Models.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using APSIM.Shared.Utilities;

    /// <summary>Represents a factor that can be varied on a graph</summary>
    public class SimulationGeneratorFactors : ISimulationGeneratorFactors
    {
        /// <summary>Name of factor</summary>
        public List<KeyValuePair<string, string>> Factors { get; private set; }

        /// <summary>The name of the column that our filter is based on e.g. 'SimulationName'</summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// Column values so that a DataView filter can be constructed for this factor
        /// e.g. 'Gatton87', 'Gatton88'
        /// </summary>
        public List<string> ColumnValues { get; private set; }

        /// <summary>Constructor</summary>
        public SimulationGeneratorFactors(string colName, string colValue)
        {
            Factors = new List<KeyValuePair<string, string>>();
            ColumnName = colName;
            ColumnValues = new List<string>() { colValue };
        }

        /// <summary>Constructor</summary>
        public SimulationGeneratorFactors(string colName, string colValue, string factorName, string factorValue)
        {
            ColumnName = colName;
            ColumnValues = new List<string>() { colValue };
            Factors = new List<KeyValuePair<string, string>>();
            Factors.Add(new KeyValuePair<string, string>(factorName, factorValue));
        }

        /// <summary>
        /// Add a factor name/value pair to this factor
        /// </summary>
        /// <param name="factorName"></param>
        /// <param name="factorValue"></param>
        public void AddFactor(string factorName, string factorValue)
        {
            Factors.Add(new KeyValuePair<string, string>(factorName, factorValue));
        }

        /// <summary>
        /// Add a factor name/value pair to this factor if it doesn't already exist
        /// </summary>
        /// <param name="factorName"></param>
        /// <param name="factorValue"></param>
        public void AddFactorIfNotExist(string factorName, string factorValue)
        {
            var f = Factors.Find(factor => factor.Key == factorName);
            if (f.Key != factorName)
                AddFactor(factorName, factorValue);
        }

        /// <summary>
        /// Remove the specified factor
        /// </summary>
        /// <param name="name">The name of factor to remove</param>
        public void RemoveFactor(string name)
        {
            Factors.RemoveAll(factor => factor.Key == name);
        }

        /// <summary>
        /// Get the value of a factor
        /// </summary>
        /// <param name="name">The name of the factor</param>
        public string GetFactorValue(string name)
        {
            var f = Factors.Find(factor => factor.Key == name);
            if (f.Key == name)
                return f.Value;
            return "?";
        }

        /// <summary>Returns true if this object is equal to rhs</summary>
        /// <param name="compareTo">The object to compare to</param>
        public bool Equals(ISimulationGeneratorFactors compareTo)
        {
            if (ColumnName != compareTo.ColumnName)
                return false;
            if (Factors.Count() != compareTo.Factors.Count())
                return false;
            for (int i = 0; i < Factors.Count; i++)
                if (Factors[i].Key != compareTo.Factors[i].Key ||
                    Factors[i].Value != compareTo.Factors[i].Value)
                    return false;

            return true;
        }

        /// <summary>
        /// Merge the specified object into this object
        /// </summary>
        /// <param name="from">The object to copy values from</param>
        public void Merge(ISimulationGeneratorFactors from)
        {
            ColumnValues.AddRange(from.ColumnValues);
        }

    }
}
