using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.PMF
{
    /// <summary>
    /// Holds the collection of canopy properties that each crop must supply to the rest of the world
    /// </summary>
    [Serializable]
    public class CanopyProperties
    {
        /// <summary>
        /// The name as it appears in the GUI e.g. "Wheat3" ??? is this really needed?  Note looking up other properties any more.
        /// </summary>
        public string Name; // { get; set; }

        /// <summary>
        /// Green leaf area index (m2/m2) 
        /// Used in the light and energy arbitration
        /// </summary>
        public double LAIGreen { get; set; }

        /// <summary>
        /// Total (includes dead) leaf area index (m2/m2) 
        /// Used in the light and energy arbitration
        /// </summary>
        public double LAItot { get; set; }

        /// <summary>
        /// Green cover (m2/m2) - fractional cover resulting from the assigned LAI, 
        /// Calculate this using an assumed light interception coefficient
        /// Used in the light and energy arbitration
        /// </summary>
        public double CoverGreen { get; set; }

        /// <summary>
        /// Total (green and dead) cover (m2/m2) - fractional cover resulting from the assigned LAItot, 
        /// Calculate this using an assumed light interception coefficient
        /// Used in the light and energy arbitration
        /// </summary>
        public double CoverTot { get; set; }

        /// <summary>
        /// Height to the top of the canopy (mm) 
        /// Used in the light and energy arbitration
        /// </summary>
        public double CanopyHeight { get; set; }

        /// <summary>
        /// Depth of the canopy (mm).  If the canopy is continuous from the ground to the top of the canopy then 
        /// the depth = height, otherwise depth must be less than the height
        /// Used in the light and energy arbitration
        /// </summary>
        public double CanopyDepth { get; set; }

        /// <summary>
        /// Stomatal conductance in (m/s) that will be seen under non-limiting light, humidity and nutrients
        /// For default values see:
        ///     Kelliher, FM, Leuning, R, Raupach, MR, Schulze, E-D (1995) Maximum conductances for evaporation from 
        ///     global vegetation types. Agricultural and Forest Meteorology 73, 1–16.
        ///     DOI: 10.1016/0168-1923(94)02178-M
        /// </summary>
        public double MaximumStomatalConductance { get; set; }

        /// <summary>
        /// HalfSatStomatalConductance (W/m2) is the indicent solar radiation at which stomatal conductance is half of MaximumStomatalConductance
        /// It is generally about 200 W/m2 for agricultural crops but about 150 W/m2 in C4 plants.  This is used in the calculation of canopy conductance 
        /// and so feeds into the calculation of water demand.
        /// </summary>
        public double HalfSatStomatalConductance { get; set; }

        /// <summary>
        /// Canopy emissivity (-) is used in the calculation of long-wave radiation.  A value of 0.96 is generally acceptable.
        /// </summary>
        public double CanopyEmissivity { get; set; }

        /// <summary>
        /// Albedo (-) is the proprotion of radiation that is reflected and is used in the calculation of the radiation balance.
        /// </summary>
        public double Albedo { get; set; }

        /// <summary>
        /// Fractional relative growth rate (-) with 1.0 at full growth rate and 0.0 at no growth
        /// Used in the calculation of actual stomatal conductance by scaling back the MaximumStomatalConductance 
        /// for stresses other than humidity, water deficit, temperature.  Usually has a value of 1.0.
        /// </summary>
        public double Frgr { get; set; }

    }
}
