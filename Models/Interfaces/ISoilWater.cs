using Models.Core;
using Models.Soils;

namespace Models.Interfaces
{

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface ISoilWater
    {
        ///<summary>Remove water from the profile</summary>
        void RemoveWater(double[] amountToRemove);

        ///<summary>Soil thickness for each layer(</summary>
        [Units("mm")]
        double[] Thickness { get; }

        ///<summary>Volumetric soil water content(</summary>
        [Units("mm/mm")]
        double[] SW { get; set; }

        ///<summary>Water content</summary>
        [Units("mm")]
        double[] SWmm { get; }

        ///<summary>Soil water potential</summary>
        [Units("cm")]
        double[] PSI { get; }

        ///<summary>Soil hydraulic conductivity</summary>
        [Units("cm/h")]
        double[] K { get; }

        ///<summary>Pore interaction index</summary>
        double[] PoreInteractionIndex { get; set; }


        ///<summary>Extractable soil water relative to LL15</summary>
        [Units("mm")]
        double[] ESW { get; }

        ///<summary>Potential evaporation from soil surface</summary>
        [Units("mm")]
        double Eos { get; }

        /// <summary>Actual (realised) soil water evaporation</summary>
        [Units("mm")]
        double Es { get; }

        /// <summary>Potential evapotranspiration of the whole soil-plant system</summary>
        [Units("mm")]
        double Eo { get; set; }

        /// <summary>Total vegetation and residue cover of the zone</summary>
        [Units("0-1")]
        double CoverTotal { get; set; }

        /// <summary>Amount of water runoff</summary>
        [Units("mm")]
        double Runoff { get; }

        /// <summary>Amount of water drainage from bottom of profile</summary>
        [Units("mm")]
        double Drainage { get; }

        /// <summary>Subsurface drain</summary>
        [Units("mm")]
        double SubsurfaceDrain { get; }

        /// <summary>Pond depth</summary>
        [Units("mm")]
        double Pond { get; }

        /// <summary>Fraction of incoming radiation reflected from bare soil</summary>
        [Units("0-1")]
        double Salb { get; }

        /// <summary>Amount of water moving laterally out of the profile</summary>
        [Units("mm")]
        double[] LateralOutflow { get; }

        /// <summary>Amount of N leaching as NO3-N from the deepest soil layer</summary>
        [Units("kg/ha")]
        double LeachNO3 { get; }

        /// <summary>Amount of N leaching as NH4-N from the deepest soil layer</summary>
        [Units("kg/ha")]
        double LeachNH4 { get; }

        /// <summary>Amount of N leaching as urea-N  from the deepest soil layer</summary>
        [Units("kg/ha")]
        double LeachUrea { get; }

        /// <summary>Amount of Cl leaching from the deepest soil layer</summary>
        [Units("kg/ha")]
        double LeachCl { get; }

        /// <summary>Amount of water moving upward from each soil layer during unsaturated flow (negative value means downward movement)</summary>
        [Units("mm")]
        double[] Flow { get; }

        /// <summary>Amount of water moving downward out of each soil layer due to gravity drainage (above DUL)</summary>
        [Units("mm")]
        double[] Flux { get; }

        /// <summary>Plant available water SW-LL15</summary>
        [Units("mm/mm")]
        double[] PAW { get; }

        /// <summary>Plant available water SW-LL15</summary>
        [Units("mm")]
        double[] PAWmm { get; }

        /// <summary>Potential infiltration (rainfall less that intercepted by the canopy and residue components)</summary>
        [Units("mm")]
        double PotentialInfiltration { get; set; }

        /// <summary>Rainfall intercepted by crop and residue canopies</summary>
        [Units("mm")]
        double PrecipitationInterception { get; set; }

        /// <summary>Water table depth</summary>
        [Units("mm")]
        double WaterTable { get; set; }

        /// <summary>Sets the water table.</summary>
        /// <param name="InitialDepth">The initial depth.</param>
        void SetWaterTable(double InitialDepth);

        ///<summary>Perform a reset</summary>
        void Reset();

        ///<summary>Perform tillage</summary>
        void Tillage(TillageType Data);

        ///<summary>Perform tillage</summary>
        void Tillage(string tillageType);
    }
}
