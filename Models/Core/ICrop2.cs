using Models.PMF;
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


        // need to add in the uptake/supply in layers - crop needs this for root growth
        }
}