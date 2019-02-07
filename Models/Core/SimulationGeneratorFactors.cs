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

        /// <summary>The column name/value pairs that our filter is based on.</summary>
        public List<KeyValuePair<string, string>> Columns { get; }

        /// <summary>Constructor</summary>
        public SimulationGeneratorFactors(string colName, string colValue)
        {
            Columns = new List<KeyValuePair<string, string>>();
            Factors = new List<KeyValuePair<string, string>>();
            Columns.Add(new KeyValuePair<string, string>(colName, colValue));
        }

        /// <summary>Constructor</summary>
        public SimulationGeneratorFactors(string[] colNames, string[] colValues, string factorName, string factorValue)
        {
            Columns = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < colNames.Length; i++)
                Columns.Add(new KeyValuePair<string, string>(colNames[i], colValues[i]));
            Factors = new List<KeyValuePair<string, string>>();
            Factors.Add(new KeyValuePair<string, string>(factorName, factorValue));
        }

        /// <summary>Constructor</summary>
        public SimulationGeneratorFactors(string colName, string colValue, string factorName, string factorValue)
        {
            Columns = new List<KeyValuePair<string, string>>();
            Columns.Add(new KeyValuePair<string, string>(colName, colValue));
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

        /// <summary>
        /// Get the value of a factor
        /// </summary>
        /// <param name="name">The name of the factor</param>
        public string GetColumnValue(string name)
        {
            var c = Columns.Find(column => column.Key == name);
            if (c.Key == name)
                return c.Value;
            return "?";
        }

        /// <summary>
        /// Remove the specified column
        /// </summary>
        /// <param name="name">The name of column to remove</param>
        public void RemoveColumn(string name)
        {
            Columns.RemoveAll(column => column.Key == name);
        }

        /// <summary>Returns true if this object is equal to rhs</summary>
        /// <param name="compareTo">The object to compare to</param>
        public bool Equals(ISimulationGeneratorFactors compareTo)
        {
            for (int i = 0; i < compareTo.Columns.Count; i++)
            {
                string keyToFind = compareTo.Columns[i].Key;
                if (Columns.Find(col => col.Key == keyToFind).Key != keyToFind)
                    return false;
            }

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
            foreach (var fromCol in from.Columns)
                Columns.Add(new KeyValuePair<string, string>(fromCol.Key, fromCol.Value));
        }

    }
}
