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
        private double FloatingPointTolerance = 0.0000000001;
        /// <summary>The layer that this pore compartment is located in</summary>
        [XmlIgnore]
        public double Layer { get; set; }
        /// <summary>The size compartment that this pore represents</summary>
        [XmlIgnore]
        public double Compartment { get; set; }/// <summary>The thickness of the layer that the pore is within</summary>
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
        /// <summary>The mean pore diameter</summary>
        [XmlIgnore]
        [Units("nm")]
        public double Diameter { get { return (MaxDiameter + MinDiameter) / 2; } }
        /// <summary>The water potential at mean diameter</summary>
        [XmlIgnore]
        [Units("cm")]
        public double Psi { get { return -3000 / Diameter; } }
        /// <summary>The volume of the the pore relative to the volume of soil</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double Volume { get; set; }
        /// <summary>The volume of the the pore in mm</summary>
        [XmlIgnore]
        [Units("mm")]
        public double VolumeDepth { get { return Volume * Thickness; } }
        /// <summary>The water filled volume of the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double WaterFilledVolume { get { return WaterDepth / Thickness; } }
        /// <summary>The water filled volume of the pore relative to the air space</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double RelativeWaterContent { get { return WaterFilledVolume / Volume; } }
        /// <summary>The air filled volume of the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double AirFilledVolume { get { return Volume - WaterFilledVolume; }  }

        private double _WaterDepth = 0;
        /// <summary>The depth of water in the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double WaterDepth
        {
            get
            { return _WaterDepth; }
            set
            {
                if (value < -0.000000000001) throw new Exception("Trying to set a negative pore water depth");
                _WaterDepth = Math.Max(value,0);//discard floating point errors
                if (_WaterDepth - VolumeDepth>FloatingPointTolerance)
                    throw new Exception("Trying to put more water into pore " + Compartment + "in layer " + Layer + " than will fit");

            }
        }
        /// <summary>The depth of Air in the pore</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double AirDepth { get { return AirFilledVolume * Thickness; } }
        /// <summary>The conductivity of water moving into a pore, The net result of gravity driving it in, capilary forces drawing it in and repellency stopping it</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double HydraulicConductivity { get; set; }
        /// <summary>The maximum possible conductivity through a pore of given size</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double HydraulicConductivityIn
        {
            get
            {
                return HydraulicConductivity * AdsorptionFactor;
            }
        }
        /// <summary>The conductivity of water moving out of a pore, The net result of gravity Opposed by capiliary draw back</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double HydraulicConductivityOut
        {
            get
            {
                return HydraulicConductivity * CapillaryFactor;
            }
        }
        /// <summary>
        /// Factor describing the effects of Adsorption of water onto solid pore surfaces.  Has a value of 1 when soil is non-repellent and decreases as pore becomes more hydrophobic
        /// </summary>
        public double AdsorptionFactor
        {
            get
            {
                double AbsFac = 1;
                if (RelativeWaterContent < 0.3)
                    AbsFac = 0.3;
                return AbsFac;
            }
        }
        /// <summary>
        /// Factor describing the effects of water surface tension holding water in pores.  Is zero where surface tension exceeds the forces of gravity and neglegable where suction is low in larger pores
        /// equals 1
        /// </summary>
        public double CapillaryFactor
        {
            get
            {
                double GravityPotential = -100; //Fix me.  Make Gravity Potential a function of distance from water table
                return Math.Max(0,Psi-GravityPotential)/-GravityPotential;
            }
        }
    }
}
