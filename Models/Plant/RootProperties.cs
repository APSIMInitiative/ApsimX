using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.PMF
{
    /// <summary>
    /// Holds the collection of root properties that each crop must supply to the rest of the world
    /// </summary>
    [Serializable]
    public class RootProperties
    {
        /// <summary>
        /// Rooting depth (mm) - used in the aribtration of water and nutrient uptake.
        /// </summary>
        public double RootDepth { get; set; }

        /// <summary>
        /// The bastardised Passioura/Monteith K*L (/day)
        /// At some point this will be replaced by one soil property and the root length density.
        /// </summary>
        public double[] KL { get; set; }

        /// <summary>
        /// The lower limit of water storage that the crops can extract water to from the soil if the soil is fully ramified by roots (mm).
        /// At some point in the future this will be replaced by the minimum leaf water potential.
        /// </summary>
        public double[] LowerLimitDep { get; set; }

        /// <summary>
        /// The length of root contained within a unit volume of soil (mm/mm3).  Note that 1 cm/cm3 = 0.01 mm/mm3.  
        /// Crops need to calculate this from their root mass.  A typical specific root length for wheat is 105000 mm/g.
        /// If only relative root distribution is known then assume 0.05 mm/mm3 as a fully ramified root lenght desnity for the surface soil 
        /// and then calculate the density for the lower soil layers according to best judement.  This is the responsibility of the crop model.
        /// </summary>
        public double[] RootLengthDensityByVolume { get; set; }

        /// <summary>
        /// Indicates if the roots have fully explored the soil layer.  Has value of 1.0 if the roots are fully through
        /// the layer, 0.0 if not yet reached the layer, or the fractional depth of exploration.
        /// </summary>
        public double[] RootExplorationByLayer { get; set; }

        /// <summary>
        /// Nitrate uptake coefficient
        /// </summary>
        public double KNO3 { get; set; }

        /// <summary>
        /// Nitrate uptake coefficient
        /// </summary>
        public double KNH4 { get; set; }

        /// <summary>
        /// Hamish's additional coefficient for N uptake
        /// </summary>
        public double MaximumDailyNUptake { get; set; }

        // if want to make this more generic and useful for other routines then add root biomass, C and nutrients
    
    }
}
