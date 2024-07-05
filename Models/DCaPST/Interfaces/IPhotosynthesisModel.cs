namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents a model that simulates daily photosynthesis
    /// </summary>
    public interface IPhotosynthesisModel
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lai"></param>
        /// <param name="sln"></param>
        void DailyRun(double lai, double sln);

        /// <summary>
        /// Calculates the biomass using the supplied soil water and the ratio that 
        /// should be attributed to the roots.
        /// </summary>
        /// <param name="soilWaterAvailable"></param>
        /// <param name="rootShootRatio"></param>
        void CalculateBiomass(double soilWaterAvailable, double rootShootRatio);
    }
}
