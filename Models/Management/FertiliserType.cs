using System;

namespace Models
{
    /// <summary>A class for holding a fertiliser type.</summary>
    [Serializable]
    public class FertiliserType
    {
        /// <summary>The name of the fertiliser type.</summary>
        public string Name { get; set; }

        /// <summary>A description of the fertiliser type.</summary>
        public string Description { get; set; }

        /// <summary>The fraction of no3.</summary>
        public double FractionNO3 { get; set; }

        /// <summary>The fraction of nh4.</summary>
        public double FractionNH4 { get; set; }

        /// <summary>The fraction of urea.</summary>
        public double FractionUrea { get; set; }

        /// <summary>The fraction of rock p.</summary>
        public double FractionRockP { get; set;}

        /// <summary>The fraction of banded p.</summary>
        public double FractionBandedP{get;set;}

        /// <summary>The fraction of labile p.</summary>
        public double FractionLabileP{get;set;}

        /// <summary>The fraction of ca.</summary>
        public double FractionCa { get; set; }
    }
}