using Models.PMF;
using Models.Interfaces;
namespace Models.Core
{
    /// <summary>
    /// The ICrop interface specifies the properties and methods that all
    /// crops must have. In effect this interface describes the interactions
    /// between a crop and the other models in APSIM.
    /// </summary>
    public interface ICrop2
    {
        /// <summary>
        /// Provides canopy data to Arbitrator.
        /// </summary>
        CanopyProperties CanopyProperties { get;  }

        /// <summary>
        /// Provides root data to Arbitrator.
        /// </summary>
        RootProperties RootProperties { get;  }

        /// <summary>
        /// Potential evapotranspiration. Arbitrator calculates this and sets this property in the crop.
        /// </summary>
        double demandWater { get; set; }

        /// <summary>
        /// Actual transpiration by the crop. Calculated by Arbitrator based on PotentialEP across all crops, soil and root properties
        /// </summary>
        double[] uptakeWater { get; set; }

        /// <summary>
        /// MicroClimate calculates a layered canopy energy balance and sets
        /// this property in the crop.
        /// </summary>
        CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }


        /// <summary>
        /// Crop calculates demandNitrogen after getting its water allocation
        /// </summary>
        double demandNitrogen { get; set; }

        /// <summary>
        /// Arbitrator supplies actualNitrogenSupply based on soil supply and other crop demand
        /// </summary>
        double[] uptakeNitrogen { set; }

        /// <summary>
        /// The proportion of supplyNitrogen that is supplied as NO3, the remainder is NH4
        /// </summary>
        double[] uptakeNitrogenPropNO3 { set; }

        /// <summary>
        /// MicroClimate will get 'CropType' and use it to look up
        /// canopy properties for this crop.
        /// </summary>
        string CropType { get; }

        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
        string[] CultivarNames { get; }
        // need to add in the uptake/supply in layers - crop needs this for root growth

        /// <summary>
        /// test is plant is sown
        /// </summary>
        bool PlantInGround { get; }

        /// <summary>
        /// test is plant has emerged
        /// </summary>
        bool PlantEmerged { get; }
        // need to add in the uptake/supply in layers - crop needs this for root growth

        }
}