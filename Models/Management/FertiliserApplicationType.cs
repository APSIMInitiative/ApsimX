using System;

namespace Models
{
    /// <summary>Stores information about a fertiliser application.</summary>
    public class FertiliserApplicationType : EventArgs
    {
        /// <summary>Amount of fertiliser applied (kg/ha).</summary>
        public double Amount { get; set; }

        /// <summary>The top depth to which fertiliser was applied (mm).</summary>
        public double DepthTop { get; set; }

        /// <summary>The bottom depth to which fertiliser was applied (mm).</summary>
        public double DepthBottom { get; set; }

        /// <summary>Type of fertiliser applied.</summary>
        public string FertiliserType { get; set; }
    }
}