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
        #region Class descriptors
        private double FloatingPointTolerance = 0.0000000001;
        /// <summary>The layer that this pore compartment is located in</summary>
        [XmlIgnore]
        public double Layer { get; set; }
        /// <summary>The size compartment that this pore represents</summary>
        [XmlIgnore]
        public double Compartment { get; set; }/// <summary>The thickness of the layer that the pore is within</summary>
        /// <summary>
        /// Allows Sorption processes to be switched off from the UI
        /// </summary>
        [Description("Include Sorption in Ks in.  Normally yes, this is for testing")]
        public bool IncludeSorption { get; set; }
        #endregion

        #region Pore Geometry
        /// <summary>
        /// The depth of the soil layer this pore compartment sits within
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double Thickness { get; set; }
        /// <summary>The diameter of the upper boundry of the pore</summary>
        [XmlIgnore]
        [Units("um")]
        public double DiameterUpper { get; set; }
        /// <summary>The diameter of the lower boundry of the pore</summary>
        [XmlIgnore]
        [Units("um")]
        public double DiameterLower { get; set; }
        /// <summary>The mean horizontal area of the pores in this pore compartment</summary>
        [XmlIgnore]
        [Units("um2")]
        public double Area { get { return Math.PI * Math.Pow(Radius,2); } }
        /// <summary>The mean horizontal radius of pores in this pore compartment</summary>
        [XmlIgnore]
        [Units("um")]
        public double Radius { get { return (DiameterLower + DiameterUpper) / 4; } }
        /// <summary>The number of pore 'cylinders' in this pore compartment</summary>
        [XmlIgnore]
        [Units("/m2")]
        public double Number { get { return Volume / (Area / 1000000000000); } }
        #endregion

        #region Porosity and Water
        /// <summary>The water potential when this pore is empty but all smaller pores are full</summary>
        [XmlIgnore]
        [Units("cm")]
        public double PsiLower { get { return -3000 / DiameterLower; } }
        /// <summary>The water potential when this pore is full but all larger pores are empty</summary>
        [XmlIgnore]
        [Units("cm")]
        public double PsiUpper { get { return -3000 / DiameterUpper; } }
        /// <summary>The water content of the soil when this pore is full and larger pores are empty</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double ThetaUpper { get; set; }
        /// <summary>The water content of the soil when this pore is empty and smaller pores are full</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double ThetaLower { get; set; }
        /// <summary>The volume of the the pore relative to the volume of soil</summary>
        [XmlIgnore]
        [Units("ml/ml")]
        public double Volume { get { return ThetaUpper - ThetaLower; } }
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
        #endregion

        #region Pore hydraulics
        /// <summary>
        /// Empirical parameter for estimating hydraulic conductivity of pore compartments
        /// divide values from Arya 1999 etal by 10000 to convert from cm to um
        /// </summary>
        [Description("Pore flow Rate coefficient")]
        public double CFlow { get { return 0.01; } }
        /// <summary>
        /// Empirical parameter for estimating hydraulic conductivity of pore compartments
        /// </summary>
        [Description("Pore flow Shape coefficient")]
        public double XFlow { get { return 1.6 + 2 * Math.Exp(Radius*-0.008); } }
        /// <summary>
        /// The volumetirc flow rate of a single pore
        /// Radius is divided by 10000 to convert from micron to cm
        /// </summary>
        [XmlIgnore]
        [Units("cm3/s")]
        public double PoreFlowRate { get { return CFlow * Math.Pow(Radius/10000,XFlow); } }
        /// <summary>The volume flow rate of water through this pore compartment</summary>
        [XmlIgnore]
        [Units("cm3/s/m2")]
        public double VolumetricFlowRate { get { return PoreFlowRate * Number ; } }
        /// <summary>The hydraulic conductivity of water through this pore compartment</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double Capillarity { get { return VolumetricFlowRate/1000*3600; } }
        /// <summary>The potential diffusion out of this pore</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double Diffusivity { get { return Capillarity * RelativeWaterContent * (1- TensionFactor); } }
        /// <summary>The potential diffusion into this pore</summary>
        [XmlIgnore]
        [Units("mm")]
        public double DiffusionCapacity { get { return AirDepth * (1 - TensionFactor); } }
        /// <summary>
        /// The rate of water movement into a pore space due to the chemical attraction from the matris
        /// </summary>
        [XmlIgnore]
        [Units("mm s^-1/2")]
        public double Sorptivity
        {
            get
            {
                return Math.Sqrt(((7.4/Radius*1000) * Capillarity * Math.Max(0,Volume - WaterFilledVolume))/0.5);
            }
        }
        /// <summary>
        /// Factor describing the effects of soil water content on hydrophobosity
        /// equals 1
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double RepelancyFactor
        {
            get
            {
                double RepFac = 1;
                if (RelativeWaterContent < 0.3)
                    RepFac = 0.3;
                return RepFac;
            }
        }
        /// <summary>
        /// The rate of water movement into a pore space due to the chemical attraction from the matris
        /// </summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double Sorption { get { return 0.5 * Sorptivity * Math.Pow(1, -0.5) * RepelancyFactor; } }
        /// <summary>The maximum possible conductivity through a pore of given size</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double HydraulicConductivityIn
        {
            get
            {
                if (Double.IsNaN(Sorption))
                    throw new Exception("Sorption is NaN");
                if (IncludeSorption)
                    return Capillarity + Sorption;
                else
                    return Capillarity;
            }
        }
        /// <summary>the gravitational potential for the layer this pore is in, calculated from height above zero potential base</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double GravitationalPotential { get; set; }
        /// <summary>
        /// Factor describing the effects of water surface tension holding water in pores.  Is zero where surface tension exceeds the forces of gravity and neglegable where suction is low in larger pores
        /// equals 1
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double TensionFactor
        {
            get
            {
                double factor = 0;
                if (GravitationalPotential < PsiUpper)
                    factor = 1;
                return factor;
            }
        }/// <summary>The conductivity of water moving out of a pore, The net result of gravity Opposed by capiliary draw back</summary>
        [XmlIgnore]
        [Units("mm/h")]
        public double HydraulicConductivityOut
        {
            get
            {
                return Capillarity * TensionFactor;
            }
        }
        #endregion
    }
}
