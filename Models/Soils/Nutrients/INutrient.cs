using Models.Surface;
using System.Collections.Generic;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface INutrient
    {
        /// <summary>The inert pool.</summary>
        IOrganicPool Inert { get; }

        /// <summary>The microbial pool.</summary>
        IOrganicPool Microbial { get; }

        /// <summary>The humic pool.</summary>
        IOrganicPool Humic { get; }

        /// <summary>The fresh organic matter cellulose pool.</summary>
        IOrganicPool FOMCellulose { get; }

        /// <summary>The fresh organic matter carbohydrate pool.</summary>
        IOrganicPool FOMCarbohydrate { get; }

        /// <summary>The fresh organic matter lignin pool.</summary>
        IOrganicPool FOMLignin { get; }

        /// <summary>The fresh organic matter pool.</summary>
        IOrganicPool FOM { get; }

        /// <summary>Soil organic nitrogen (FOM + Microbial + Humic)</summary>
        IOrganicPool Organic { get; }

        /// <summary>The NO3 pool.</summary>
        ISolute NO3 { get; }

        /// <summary>The NH4 pool.</summary>
        ISolute NH4 { get; }

        /// <summary>The Urea pool.</summary>
        ISolute Urea { get; }

        /// <summary>Total C in each soil layer</summary>
        IReadOnlyList<double> TotalC { get; }

        /// <summary>Total C lost to the atmosphere</summary>
        IReadOnlyList<double> Catm { get; }

        /// <summary>Total N lost to the atmosphere</summary>
        IReadOnlyList<double> Natm { get; }

        /// <summary>Total N2O lost to the atmosphere</summary>
        IReadOnlyList<double> N2Oatm { get; }

        /// <summary>Total Net N Mineralisation in each soil layer</summary>
        IReadOnlyList<double> MineralisedN { get; }

        /// <summary>Denitrified Nitrogen (N flow from NO3).</summary>
        IReadOnlyList<double> DenitrifiedN { get; }

        /// <summary>Nitrified Nitrogen (from NH4 to either NO3 or N2O).</summary>
        IReadOnlyList<double> NitrifiedN { get; }

        /// <summary>Urea converted to NH4 via hydrolysis.</summary>
        IReadOnlyList<double> HydrolysedN { get; }

        /// <summary>Total Mineral N in each soil layer</summary>
        IReadOnlyList<double> MineralN { get; }

        /// <summary>Total N in each soil layer</summary>
        IReadOnlyList<double> TotalN { get; }

        /// <summary>Carbon to Nitrogen Ratio for Fresh Organic Matter for a given layer</summary>
        IReadOnlyList<double> FOMCNRFactor { get; }

        /// <summary>
        /// Incorporate FOM
        /// </summary>
        void DoIncorpFOM(FOMLayerType FOMdata);

        /// <summary>Partition the given FOM C and N into fractions in each layer (FOM pools)</summary>
        /// <param name="FOMPoolData">The in fom pool data.</param>
        void IncorpFOMPool(FOMPoolType FOMPoolData);

        /// <summary>
        /// Reset all Pools
        /// </summary>
        void Reset();
    }
}
