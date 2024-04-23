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
            Digestibility = digestibility;
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
            Digestibility = digestibility;
        }

        /// <summary>Name of material.</summary>
        public string Name { get; }

        /// <summary>Total Biomass (kg/ha)</summary>
        public Biomass Total { get; }

        /// <summary>Consumable Biomass (kg/ha)</summary>
        public Biomass Consumable { get; }

        /// <summary>Is biomass live.</summary>
        public bool IsLive { get; }

        /// <summary>Optional digestibility (0-1). Can be null missing digestibility.</summary>
        public double? Digestibility { get; }
    }
}
