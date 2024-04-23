using System;
using Models.Core;

namespace Models.PMF
{
    /// <summary>
    /// Parameters which control how a plant is sown.
    /// </summary>
    [Serializable]
    public class SowingParameters : EventArgs
    {
        /// <summary>The plant being sown.</summary>
        public Plant Plant;

        /// <summary>The cultivar to be sown.</summary>
        public string Cultivar { get; set; }

        /// <summary>The population.</summary>
        [Units("/m2")]
        public double Population { get; set; }

        /// <summary>The number of seeds sown.</summary>
        [Units("")]
        public double Seeds { get; set; }

        /// <summary>The depth</summary>
        [Units("mm")]
        public double Depth { get; set; }

        /// <summary>The row spacing</summary>
        [Units("mm")]
        public double RowSpacing { get; set; }

        /// <summary>The maximum cover</summary>
        public double MaxCover { get; set; }

        /// <summary>The bud number</summary>
        public double BudNumber { get; set; }

        /// <summary>The skip type</summary>
        public double SkipType { get; set; }

        /// <summary>The skip row</summary>
        public double SkipRow { get; set; }

        /// <summary>The skip plant</summary>
        public double SkipPlant { get; set; }

        /// <summary>The skip plant seed density adjustment</summary>
        public double SkipDensityScale { get; set; }

        /// <summary>Tillering Method to set Fixed or dynamic tillering</summary>
        /// <summary>Tillering Method: -1 = Rule of Thumb, 0 = FixedTillering - uses FTN, 1 = DynamicTillering</summary>
        public int TilleringMethod { get; set; }

        /// <summary>Fertile Tiller Number</summary>
        public double FTN { get; set; }
    }
}
