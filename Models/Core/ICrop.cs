using System.Collections.Generic;
using Models.Soils.Arbitrator;

namespace Models.Core
{
    /// <summary>
    /// The ICrop interface specifies the properties and methods that all
    /// crops must have. In effect this interface describes the interactions
    /// between a crop and the other models in APSIM.
    /// </summary>
    public interface ICrop
    {
        /// <summary>
        /// Is the plant alive?
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
        string[] CultivarNames { get; }


        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        List<Soils.Arbitrator.ZoneWaterAndN> GetSWUptakes(SoilState soilstate);

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        List<Soils.Arbitrator.ZoneWaterAndN> GetNUptakes(SoilState soilstate);


        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        void SetSWUptake(List<Soils.Arbitrator.ZoneWaterAndN> info);
        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        void SetNUptake(List<Soils.Arbitrator.ZoneWaterAndN> info);


        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1);
    }
}