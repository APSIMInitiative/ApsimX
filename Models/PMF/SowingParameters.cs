using System;

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
        public double Population { get; set; }

        /// <summary>The depth</summary>
        public double Depth { get; set; }

        /// <summary>The row spacing</summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SowingParameters"/> class.
        /// </summary>
        public SowingParameters()
        {
            Cultivar = "";
            Population = 100;
            Depth = 100;
            RowSpacing = 150;
            MaxCover = 1;
            BudNumber = 1;
            SkipType = 0;
            SkipRow = 0;
            SkipPlant = 1;
            SkipDensityScale = 1;
        }
    }
}
