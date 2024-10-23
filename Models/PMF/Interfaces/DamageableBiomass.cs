using System;
using Models.ForageDigestibility;
using Models.Functions;

namespace Models.PMF.Interfaces
{
    /// <summary>A class to hold a mass of biomass and its digestibility.</summary>
    public class DamageableBiomass
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of biomass.</param>
        /// <param name="total">Total Biomass (kg/ha).</param>
        /// <param name="isLive">Is biomass live.</param>
        /// <param name="digestibility">Optional digestibility (0-1).</param>
        public DamageableBiomass(string name, Biomass total, bool isLive, double? digestibility = null)
        {
            Name = name;
            Total = total;
            Consumable = total;
            IsLive = isLive;
            DigestibilityFromModel = digestibility;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of biomass.</param>
        /// <param name="total">Total Biomass (kg/ha).</param>
        /// <param name="fractionConsumable">Fraction of biomass that is consumable.</param>
        /// <param name="isLive">Is biomass live.</param>
        /// <param name="digestibility">Optional digestibility (0-1).</param>
        public DamageableBiomass(string name, Biomass total, double fractionConsumable, bool isLive, double? digestibility)
        {
            Name = name;
            Total = total;
            Consumable = total * fractionConsumable;
            IsLive = isLive;
            DigestibilityFromModel = digestibility;
        }

        /// <summary>Name of material.</summary>
        public string Name { get; }

        /// <summary>Total Biomass (kg/ha)</summary>
        public Biomass Total { get; }

        /// <summary>Consumable Biomass (kg/ha)</summary>
        public Biomass Consumable { get; }

        /// <summary>Is biomass live.</summary>
        public bool IsLive { get; }

        /// <summary>Digestibility (0-1) as calculated from a model. Can be null if model doesn't calculate digestibility.</summary>
        public double? DigestibilityFromModel { get; }
    }
}
