using Models.DCAPST.Canopy;

namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents attributes of a plant canopy
    /// </summary>
    public interface ICanopyAttributes
    {
        /// <summary>
        /// The section of canopy currently in sunlight
        /// </summary>
        IAssimilationArea Sunlit { get; }

        /// <summary>
        /// The section of canopy currently in shade
        /// </summary>
        IAssimilationArea Shaded { get; }        

        /// <summary>
        /// Performs initial calculations for the canopy provided daily conditions 
        /// </summary>
        void InitialiseDay(double lai, double sln);

        /// <summary>
        /// Updates the total canopy on a new timestep
        /// </summary>
        void DoTimestepAdjustment(ISolarRadiation radiation);

        /// <summary>
        /// Adjusts the properties of the canopy to account for the suns movement across the sky
        /// </summary>
        void DoSolarAdjustment(double sunAngleRadians);

        /// <summary>
        /// Gets the amount of radiation intercepted by the canopy
        /// </summary>
        /// <returns></returns>
        double GetInterceptedRadiation();

        /// <summary>
        /// Calculates the total boundary heat conductance of the canopy
        /// </summary>
        double CalcBoundaryHeatConductance();

        /// <summary>
        /// Calculates the boundary heat conductance of the sunlit area of the canopy
        /// </summary>
        double CalcSunlitBoundaryHeatConductance();
    }
}
