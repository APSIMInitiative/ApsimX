using System;

namespace Models
{
    /// <summary>Stores information about a fertiliser application.</summary>
    public class FertiliserApplicationType : EventArgs
    {
        /// <summary>Amount of fertiliser applied (kg/ha).</summary>
        public double Amount { get; set; }

        /// <summary>Depth to which fertiliser was applied (mm).</summary>
        public double Depth { get; set; }

        /// <summary>Type of fertiliser applied.</summary>
        public Fertiliser.Types FertiliserType { get; set; }
    }
}