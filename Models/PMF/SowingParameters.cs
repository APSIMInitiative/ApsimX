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
        public Plant Plant = null;

        /// <summary>The cultivar to be sown.</summary>
        public string Cultivar { get; set; }

        /// <summary>The population.</summary>
        [Units("/m2")]
        public double Population { get; set; } = 100;

        /// <summary>The number of seeds sown.</summary>
        [Units("")]
        public double Seeds { get; set; } = 0;

        /// <summary>The depth</summary>
        [Units("mm")]
        public double Depth { get; set; } = 100;

        /// <summary>The row spacing</summary>
        [Units("mm")]
        public double RowSpacing { get; set; } = 150;

        /// <summary>The maximum cover</summary>
        public double MaxCover { get; set; } = 1;

        /// <summary>The bud number</summary>
        public double BudNumber { get; set; } = 1;

        /// <summary>The skip type</summary>
        public double SkipType { get; set; }

        /// <summary>The skip row</summary>
        public double SkipRow { get; set; }

        /// <summary>The skip plant</summary>
        public double SkipPlant { get; set; } = 1;

        /// <summary>The skip plant seed density adjustment</summary>
        public double SkipDensityScale { get; set; } = 1;
    }
}
