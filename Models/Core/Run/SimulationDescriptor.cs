using System;

namespace Models.Core.Run
{
    /// <summary>Encapsulates a descriptor for a simulation.</summary>
    [Serializable]
    public class SimulationDescriptor
    {
        /// <summary>The name of the descriptor.</summary>
        public string Name { get; set; }

        /// <summary>The value of the descriptor.</summary>
        public string Value { get; set; }

        /// <summary>Constructor</summary>
        /// <param name="name">Name of the descriptor.</param>
        /// <param name="value">Value of the descriptor.</param>
        public SimulationDescriptor(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}