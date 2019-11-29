namespace Models.Interfaces
{
    using Models.Soils;

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface INutrient
    {

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
