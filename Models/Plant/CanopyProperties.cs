using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.PMF
{
    [Serializable]
    public class CanopyProperties
    {
        public double height; 
        public double depth;
        public double lai;
        public double lai_tot;
        public double cover;
        public double cover_tot;
        // VS added the stuff below 18 Aug 2014

        /// <summary>
        /// This is the name as it appears in the GUI e.g. "Wheat3"
        /// </summary>
        public string Name;

        /// <summary>
        /// This is a generic type of crop (e.g. C3 or wheat) and is used to do something that might not be needed later
        /// </summary>
        public string CropType;

        /// <summary>
        /// This is that stomatal conductance in (units) that will be seen under non-limiting light and nutrients
        /// </summary>
        public double MaximumStomatalConductance;

    }
}
