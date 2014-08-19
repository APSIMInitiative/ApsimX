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
        /// This is a generic type of crop (e.g. C3 or wheat) and is used to do something that might not be needed later
        /// </summary>
        public string CropType;

        /// <summary>
        /// This is the name as it appears in the GUI e.g. "Wheat3"
        /// </summary>
        public string Name;

        /// <summary>
        /// Green leaf area index (m2/m2) 
        /// </summary>
        public double lai;

        /// <summary>
        /// Total (green and dead) leaf area index (m2/m2) 
        /// </summary>
        public double lai_tot;

        /// <summary>
        /// Fractional cover that the green lai provides (m2/m2) 
        /// </summary>
        public double cover;

        /// <summary>
        /// Fractional cover that the total (green and dead) lai provides (m2/m2) 
        /// </summary>
        public double cover_tot;

        /// <summary>
        /// Canopy height (mm) 
        /// </summary>
        public double height;

        /// <summary>
        /// Canopy depth (mm).  If the canopy is continuous to the ground then depth = height
        /// </summary>
        public double depth;

        /// <summary>
        /// This is that stomatal conductance in (m/s) that will be seen under non-limiting light and nutrients
        /// </summary>
        public double MaximumStomatalConductance;

    }
}
