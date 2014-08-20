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
        /// Provides canopy data to SoilWater.
        /// </summary>
        CanopyProperties CanopyProperties { get;  }

        /// <summary>
        /// Provides canopy data to SoilWater.
        /// </summary>
        RootProperties RootProperties { get;  }

        
        /// <summary>
        /// MicroClimate will get 'CropType' and use it to look up
        /// canopy properties for this crop.
        /// </summary>
        //string CropType { get;  }

        /// <summary>
        /// Crop specific relative growth stress factor (0-1). MicroClimate
        /// uses this to calculate the crop canopy conductance
        /// </summary>
        double FRGR { get;  }

        /// <summary>
        /// Potential evapotranspiration. MicroClimate calculates this and sets
        /// this property in the crop.
        /// </summary>
        double PotentialEP { get; set; }

        /// <summary>
        /// Actual transpiration by the crop. Calculated by Arbitrator based on PotentialEP across all crops, soil and root properties
        /// </summary>
        double ActualEP { set; }

        /// <summary>
        /// MicroClimate calculates a layered canopy energy balance and sets
        /// this property in the crop.
        /// </summary>
        CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get;  }

        /// <summary>
        /// Describes the state of the crop, if true then is actually in the ground - sown
        /// </summary>
        //bool IsActive { get;}
        // IsActive states that the crop


        }
}