using System;
using Models.Core;
namespace Models.ForageDigestibility
{
    /// <summary>Encapsulates parameters for a forage material (e.g. leaf.live, leaf.dead, stem.live  etc).</summary>
    [Serializable]
    public class ForageMaterialParameters
    {
        /// <summary>Name of material e.g. AGPRyegrass.Leaf</summary>
        [Display]
        public string Name { get; set; }

        /// <summary>Live digestibility.</summary>
        [Display(DisplayName = "Live digestibility")]
        public string LiveDigestibility { get; set; }

        /// <summary>Dead digestibility.</summary>
        [Display(DisplayName = "Dead digestibility")]
        public string DeadDigestibility { get; set; }

        /// <summary>Fraction of live material that is consumable.</summary>
        [Display(DisplayName = "Live fraction consumable")]
        public double LiveFractionConsumable { get; set; }

        /// <summary>Fraction of dead material that is consumable.</summary>
        [Display(DisplayName = "Dead fraction consumable")]
        public double DeadFractionConsumable { get; set; }

        /// <summary>Minimum amount (mass) of live material that is consumable (kg/ha).</summary>
        [Display(DisplayName = "Live minimum biomass")]
        [Units("kg/ha")]
        public double LiveMinimumBiomass { get; set; }

        /// <summary>Minimum amount (mass) of dead material that is consumable (kg/ha).</summary>
        [Display(DisplayName = "Dead minimum biomass")]
        [Units("kg/ha")]
        public double DeadMinimumBiomass { get; set; }

    }
}