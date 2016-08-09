using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Xml.Serialization;

namespace Models.Soils
{
    /// <summary>
    /// Data structure that holds parameters and variables specific to each pore component in the soil horizion
    /// </summary>
    [Serializable]
    public class Pore: Model
    {
        /// <summary>The thickness of the layer that the pore is within</summary>
        [XmlIgnore]
        [Units("mm")]
        public double Thickness { get; set; }
        /// <summary>The diameter of the upper boundry of the pore</summary>
        [XmlIgnore]
        [Units("nm")]
        public double MaxDiameter { get; set; }
        /// <summary>The diameter of the lower boundry of the pore</summary>
        [XmlIgnore]
        [Units("nm")]
        public double MinDiameter { get; set; }
        /// <summary>The volume of the the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double Volume { get; set; }
        /// <summary>The water filled volume of the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double WaterFilledVolume { get; set; }
        /// <summary>The depth of water in the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double Waterdepth { get { return WaterFilledVolume * Thickness; } }
    }
}
