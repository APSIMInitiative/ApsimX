﻿using Models.DCAPST.Canopy;

namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents an area of a canopy that can undergo assimilation
    /// </summary>
    public interface IAssimilationArea
    {
        /// <summary>
        /// The rates of various parameters at 25 Celsius
        /// </summary>
        ParameterRates At25C { get; }

        /// <summary>
        /// Leaf area index of this region of the canopy
        /// </summary>
        double LAI { get; set; }

        /// <summary>
        /// The energy the canopy absorbs through solar radiation
        /// </summary>
        double AbsorbedRadiation { get; set; }

        /// <summary>
        /// The number of photosynthetic active photons which reach the canopy
        /// </summary>
        double PhotonCount { get; set; }

        /// <summary>
        /// Retrieves the current data values of the area in a seperate object
        /// </summary>
        /// <remarks>
        /// This is intended to enable the extraction / tracking of data if necessary
        /// </remarks>
        AreaValues GetAreaValues();

        /// <summary>
        /// Runs the photosynthesis calculations for the canopy
        /// </summary>
        void DoPhotosynthesis(ITemperature temperature, Transpiration transpiration);
    }

    /// <summary>
    /// 
    /// </summary>
    public class ParameterRates
    {
        /// <summary>
        /// Maximum rubisco activity
        /// </summary>
        public double VcMax { get; set; }

        /// <summary>
        /// Maximum respiration
        /// </summary>
        public double Rd { get; set; }

        /// <summary>
        /// Maximum electron transport rate
        /// </summary>
        public double JMax { get; set; }

        /// <summary>
        /// Maximum PEPc activity
        /// </summary>
        public double VpMax { get; set; }

        /// <summary>
        /// Maximum mesophyll CO2 conductance
        /// </summary>
        public double Gm { get; set; }
    }
}
