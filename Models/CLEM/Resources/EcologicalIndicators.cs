using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A store of all ecological indicators for a given pasture
    /// </summary>
    [Serializable]
    public class EcologicalIndicators
    {
        /// <summary>
        /// Name of the resource holding these details
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Land condition index
        /// </summary>
        public double LandConditionIndex { get; set; }

        /// <summary>
        /// Grass basla area
        /// </summary>
        public double GrassBasalArea { get; set; }

        //erosion
        //tree basal area
        //perennials
        //%runoff
        //methane
        //soilC
        //TreeC
        //Burnkg
        //methaneFire
        //N2OOFire

        /// <summary>
        /// Average monthly stocking rate (Adult Equivalents/square km)
        /// </summary>
        public double StockingRate { get; set; }

        /// <summary>
        /// Utilisation percentage
        /// </summary>
        public double Utilisation { get; set; }

    }
}
