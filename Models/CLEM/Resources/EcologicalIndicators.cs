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

        /// <summary>
        /// Average monthly stocking rate (Adult Equivalents/square km)
        /// </summary>
        public double StockingRate { get; set; }

        /// <summary>
        /// Utilisation percentage
        /// </summary>
        public double Utilisation { get; set; }

        /// <summary>
        /// Erosion
        /// </summary>
        public double Erosion { get; set; }

        /// <summary>
        /// Runoff
        /// </summary>
        public double Runoff { get; set; }

        /// <summary>
        /// Rainfall
        /// </summary>
        public double Rainfall { get; set; }

        /// <summary>
        /// Cover
        /// </summary>
        public double Cover { get; set; }

        /// <summary>
        /// tree basal area
        /// </summary>
        public double TreeBasalArea { get; set; }

        /// <summary>
        /// Tree carbon
        /// </summary>
        public double TreeCarbon { get; set; }

        /// <summary>
        /// Perennials
        /// </summary>
        public double Perennials { get; set; }

        //methane - done elsewhere
        //soilC - need to look at this
        //Burnkg - will be in burn pasture activity
        //methaneFire - created by burn pasture activity
        //N2OOFire - created by burn pasture activity

        /// <summary>
        /// Reset all values
        /// </summary>
        public void Reset()
        {
            Erosion = 0;
            Runoff = 0;
            Rainfall = 0;
            Cover = 0;
            TreeBasalArea = 0;
            TreeCarbon = 0;
            Perennials = 0;
        }
    }
}
