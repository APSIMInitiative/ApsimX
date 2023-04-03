using Models.PMF;
using System.Collections.Generic;

namespace Models.Core
{
    /// <summary>
    /// The ICrop interface specifies the properties and methods that all
    /// crops must have. In effect this interface describes the interactions
    /// between a crop and the other models in APSIM.
    /// </summary>
    public interface IPlant : IModel
    {
        /// <summary>The plant type.</summary>
        /// <remarks>A substitute for the old Leguminosity.</remarks>
        string PlantType{ get; }

        /// <summary> Is the plant alive?</summary>
        bool IsAlive { get; }

        /// <summary>Gets a list of cultivar names</summary>
        string[] CultivarNames { get; }

        /// <summary>Get above ground biomass</summary>
        IBiomass AboveGround { get; }

        /// <summary>Daily soil water uptake from each soil layer (mm)</summary>
        IReadOnlyList<double> WaterUptake { get; }

        /// <summary>Daily nitrogen uptake from each soil layer (kg/ha).</summary>
        IReadOnlyList<double> NitrogenUptake { get; }

        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        /// <param name="rowConfig">The bud number.</param>
        /// <param name="seeds">The number of seeds sown (/m2).</param>
        /// <param name="tillering">tillering method (-1, 0, 1).</param>
        /// <param name="ftn">Fertile Tiller Number.</param>
        void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1, double rowConfig = 0, double seeds = 0, int tillering = 0, double ftn = 0.0);

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        bool IsReadyForHarvesting { get; }

        /// <summary>Harvest the crop</summary>
        void Harvest();

        /// <summary>End the crop</summary>
        void EndCrop();
    }
}