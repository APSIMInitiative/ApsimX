namespace Models.Core
{
    /// <summary>
    /// The ICrop interface specifies the properties and methods that all
    /// crops must have. In effect this interface describes the interactions
    /// between a crop and the other models in APSIM.
    /// </summary>
    public interface IPlant
    {
        /// <summary>Gets a value indicating how leguminous a plant is</summary>
        double Legumosity { get; }

        /// <summary>Gets a value indicating whether the biomass is from a c4 plant or not</summary>
        bool IsC4 { get; }

        /// <summary> Is the plant alive?</summary>
        bool IsAlive { get; }

        /// <summary>Gets a list of cultivar names</summary>
        string[] CultivarNames { get; }

        /// <summary>Get above ground biomass</summary>
        PMF.Biomass AboveGround { get; }

        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">The cultivar.</param>
        /// <param name="population">The population.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowSpacing">The row spacing.</param>
        /// <param name="maxCover">The maximum cover.</param>
        /// <param name="budNumber">The bud number.</param>
        /// <param name="rowConfig">The bud number.</param>
        void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1, double rowConfig = 1);

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        bool IsReadyForHarvesting { get; }

        /// <summary>Harvest the crop</summary>
        void Harvest();

        /// <summary>End the crop</summary>
        void EndCrop();
    }
}