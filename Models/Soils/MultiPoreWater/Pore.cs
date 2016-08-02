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
        /// <summary>The thickness of the pore layer</summary>
        [XmlIgnore]
        [Units("mm")]
        public double Thickness { get; set; }
        /// <summary>The thickness of the pore layer</summary>
        [XmlIgnore]
        [Units("mm")]
        public double MaxDiameter { get; set; }
        /// <summary>The thickness of the pore layer</summary>
        [XmlIgnore]
        [Units("mm")]
        public double MinDiameter { get; set; }
        /// <summary>The thickness of the pore layer</summary>
        [XmlIgnore]
        [Units("mm")]
        public double Volume { get; set; }
        /// <summary>The thickness of the pore layer</summary>
        [XmlIgnore]
        [Units("mm")]
        public double WaterVolume { get; set; }
    }
}
