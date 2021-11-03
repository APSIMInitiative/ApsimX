using Models.PMF;
using Models.PMF.Interfaces;

namespace Models.ForageDigestibility
{
    /// <summary>A class to hold a mass of digestible biomass. NOTE: mass is in kg/ha.</summary>
    public class DigestibleBiomass
    {
        private readonly DamageableBiomass material;
        private readonly ForageMaterialParameters parameters;
        private readonly double digestibility;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="material">Biomass.</param>
        /// <param name="parameters">Parameters.</param>
        public DigestibleBiomass(DamageableBiomass material, ForageMaterialParameters parameters)
        {
            this.material = material;
            this.parameters = parameters;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="material">Biomass.</param>
        /// <param name="digestibility">Digestibility.</param>
        public DigestibleBiomass(DamageableBiomass material, double digestibility)
        {
            this.material = material;
            this.digestibility = digestibility;
        }

        /// <summary>Name of material.</summary>
        public string Name => material.Name;

        /// <summary>Total biomass (kg/ha).</summary>
        public Biomass Total => material.Total;

        /// <summary>Consumable biomass (kg/ha).</summary>
        public Biomass Consumable => material.Consumable;

        /// <summary>Is biomass live.</summary>
        public bool IsLive => material.IsLive;

        /// <summary>Digestibility of material.</summary>
        public double Digestibility => parameters == null ? digestibility: parameters.Digestibility;
    }
}