using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.Soils
{
    /// <summary>
    /// Data structure that holds parameters and variables specific to each pore component in the soil horizion
    /// </summary>
    [Serializable]
    public class HourlyData : Model
    {
        /// <summary>
        /// Irrigation applied
        /// </summary>
        [XmlIgnore]
        public double[] Irrigation { get; set; }
        /// <summary>
        /// Rainfall occured
        /// </summary>
        [XmlIgnore]
        public double[] Rainfall { get; set; }
        /// <summary>
        /// Drainage occured
        /// </summary>
        [XmlIgnore]
        public double[] Drainage { get; set; }
        /// <summary>
        /// Infiltration occured
        /// </summary>
        [XmlIgnore]
        public double[] Infiltration { get; set; }
        /// <summary>
        /// Initialise arays on construction
        /// </summary>
        public HourlyData()
        {
            Irrigation = new double[24];
            Rainfall = new double[24];
            Drainage = new double[24];
            Infiltration = new double[24];
        }
    }
}
