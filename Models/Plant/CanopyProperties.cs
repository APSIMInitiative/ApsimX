using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.PMF
{
    [Serializable]
    public class CanopyProperties
    {

        /// <summary>
        /// Crop type was used to assign generic types of properties (e.g. maximum stomatal conductance) to crops
        /// Probably not needed now as the crops will have to supply these themselves
        /// ???? delete ????
        /// </summary>
        public string CropType;

        /// <summary>
        /// The name as it appears in the GUI e.g. "Wheat3" 
        /// </summary>
        public string Name;

        /// <summary>
        /// Green leaf area index (m2/m2) 
        /// Used in the light and energy arbitration
        /// </summary>
        public double LAI;

        /// <summary>
        /// Total (includes dead) leaf area index (m2/m2) 
        /// Used in the light and energy arbitration
        /// </summary>
        public double LAItot;

        /// <summary>
        /// Green cover (m2/m2) - fractional cover resulting from the assigned LAI, 
        /// Calculate this using an assumed light interception coefficient
        /// Used in the light and energy arbitration
        /// </summary>
        public double CoverGreen;

        /// <summary>
        /// Total (green and dead) cover (m2/m2) - fractional cover resulting from the assigned LAItot, 
        /// Calculate this using an assumed light interception coefficient
        /// Used in the light and energy arbitration
        /// </summary>
        public double CoverTot;

        /// <summary>
        /// Height to the top of the canopy (mm) 
        /// Used in the light and energy arbitration
        /// </summary>
        public double CanopyHeight;

        /// <summary>
        /// Depth of the canopy (mm).  If the canopy is continuous from the ground to the top of the canopy then 
        /// the depth = height, otherwise depth must be less than the height
        /// Used in the light and energy arbitration
        /// </summary>
        public double CanopyDepth;

        /// <summary>
        /// Stomatal conductance in (m/s) that will be seen under non-limiting light, humidity and nutrients
        /// For default values see:
        ///     Kelliher, FM, Leuning, R, Raupach, MR, Schulze, E-D (1995) Maximum conductances for evaporation from 
        ///     global vegetation types. Agricultural and Forest Meteorology 73, 1–16.
        /// </summary>
        public double MaximumStomatalConductance;

        /// <summary>
        /// Fractional relative growth rate (-) with 1.0 at full growth rate and 0.0 at no growth
        /// Used in the calculation of actual stomatal conductance by scaling back the MaximumStomatalConductance 
        /// for stresses other than humidity, water deficit, temperature.  Usually has a value of 1.0.
        /// </summary>
        public double Frgr;

    }
}
