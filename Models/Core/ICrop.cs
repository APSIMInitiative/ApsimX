using System.Collections.Generic;

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
        /// Provides canopy data to SoilWater.
        /// </summary>
        NewCanopyType CanopyData { get; }

        /// <summary>
        /// MicroClimate will get 'CropType' and use it to look up
        /// canopy properties for this crop.
        /// </summary>
        string CropType { get; }

        /// <summary>
        /// Crop specific relative growth stress factor (0-1). MicroClimate
        /// uses this to calculate the crop canopy conductance
        /// </summary>
        double FRGR { get; }

        /// <summary>
        /// Potential evapotranspiration. MicroClimate calculates this and sets
        /// this property in the crop.
        /// </summary>
        double PotentialEP { get; set; }

        /// <summary>
        /// MicroClimate calculates a layered canopy energy balance and sets
        /// this property in the crop.
        /// </summary>
        CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
        string[] CultivarNames { get; }


        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        List<Soils.UptakeInfo> GetSWUptake(List<Soils.UptakeInfo> info);

        /// <summary>
        /// Set the potential sw uptake for today
        /// </summary>
        void SetSWUptake(List<Soils.UptakeInfo> info);


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