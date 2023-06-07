namespace Models.Soils.Nutrients
{

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface INutrient
    {
        /// <summary>The inert pool.</summary>
        INutrientPool Inert { get; }

        /// <summary>The microbial pool.</summary>
        INutrientPool Microbial { get; }

        /// <summary>The humic pool.</summary>
        INutrientPool Humic { get; }

        /// <summary>The fresh organic matter cellulose pool.</summary>
        INutrientPool FOMCellulose { get; }

        /// <summary>The fresh organic matter carbohydrate pool.</summary>
        INutrientPool FOMCarbohydrate { get; }

        /// <summary>The fresh organic matter lignin pool.</summary>
        INutrientPool FOMLignin { get; }

        /// <summary>The fresh organic matter pool.</summary>
        INutrientPool FOM { get; }

        /// <summary>The fresh organic matter surface residue pool.</summary>
        INutrientPool SurfaceResidue { get; }

        /// <summary>Soil organic nitrogen (FOM + Microbial + Humic)</summary>
        INutrientPool Organic { get; }

        /// <summary>The NO3 pool.</summary>
        ISolute NO3 { get; }

        /// <summary>The NH4 pool.</summary>
        ISolute NH4 { get; }

        /// <summary>The Urea pool.</summary>
        ISolute Urea { get; }

        /// <summary>Total C in each soil layer</summary>
        double[] TotalC { get; }

        /// <summary>Total C lost to the atmosphere</summary>
        double[] Catm { get; }

        /// <summary>Total N lost to the atmosphere</summary>
        double[] Natm { get; }

        /// <summary>Total N2O lost to the atmosphere</summary>
        double[] N2Oatm { get; }

        /// <summary>Total Net N Mineralisation in each soil layer</summary>
        double[] MineralisedN { get; }

        /// <summary>Net N Mineralisation from surface residue</summary>
        double[] MineralisedNSurfaceResidue { get; }

        /// <summary>Denitrified Nitrogen (N flow from NO3).</summary>
        double[] DenitrifiedN { get; }

        /// <summary>Nitrified Nitrogen (from NH4 to either NO3 or N2O).</summary>
        double[] NitrifiedN { get; }

        /// <summary>Urea converted to NH4 via hydrolysis.</summary>
        double[] HydrolysedN { get; }

        /// <summary>Total Mineral N in each soil layer</summary>
        double[] MineralN { get; }

        /// <summary>Total N in each soil layer</summary>
        double[] TotalN { get; }

        /// <summary>Carbon to Nitrogen Ratio for Fresh Organic Matter for a given layer</summary>
        double[] FOMCNRFactor { get; }

        /// <summary>
        /// Calculate actual decomposition
        /// </summary>
        SurfaceOrganicMatterDecompType CalculateActualSOMDecomp();
        /// <summary>
        /// Incorporate FOM
        /// </summary>
        void DoIncorpFOM(FOMLayerType FOMdata);
        /// <summary>
        /// Reset all Pools
        /// </summary>
        void Reset();

    }
}
