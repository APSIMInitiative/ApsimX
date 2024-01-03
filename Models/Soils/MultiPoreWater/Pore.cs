using System;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Soils
{
    /// <summary>
    /// Data structure that holds parameters and variables specific to each pore component in the soil horizion
    /// </summary>
    [Serializable]
    public class Pore : Model
    {
        #region Class descriptors
        private double FloatingPointTolerance = 0.0000000001;
        /// <summary>The layer that this pore compartment is located in</summary>
        [JsonIgnore]
        public double Layer { get; set; }
        /// <summary>The size compartment that this pore represents</summary>
        [JsonIgnore]
        public double Compartment { get; set; }/// <summary>The thickness of the layer that the pore is within</summary>
                                               /// <summary>
                                               /// Allows Sorption processes to be switched off from the UI
                                               /// </summary>
        [Description("Include Sorption in Ks in.  Normally yes, this is for testing")]
        public bool IncludeSorption { get; set; }
        #endregion

        #region Pore Geometry
        /// <summary>The diameter of the upper boundry of the pore</summary>
        [JsonIgnore]
        [Units("um")]
        public double DiameterUpper { get; set; }
        /// <summary>The diameter of the lower boundry of the pore</summary>
        [JsonIgnore]
        [Units("um")]
        public double DiameterLower { get; set; }
        /// <summary>The mean horizontal area of the pores in this pore compartment</summary>
        [JsonIgnore]
        [Units("um2")]
        public double Area { get { return Math.PI * Math.Pow(Radius, 2); } }
        /// <summary>The mean horizontal radius of pores in this pore compartment</summary>
        [JsonIgnore]
        [Units("um")]
        public double Radius { get { return (DiameterLower + DiameterUpper) / 4; } }
        /// <summary>The number of pore 'cylinders' in this pore compartment</summary>
        [JsonIgnore]
        [Units("/m2")]
        public double Number { get { return Volume / (Area / 1e12); } }
        #endregion

        #region Porosity and Water
        /// <summary>
        /// The depth of the soil layer this pore compartment sits within
        /// </summary>
        [JsonIgnore]
        [Units("mm")]
        public double Thickness { get; set; }
        /// <summary>The water potential when this pore is empty but all smaller pores are full</summary>
        [JsonIgnore]
        [Units("mm")]
        public double PsiLower { get { return -30000 / DiameterLower; } }
        /// <summary>The water potential when this pore is full but all larger pores are empty</summary>
        [JsonIgnore]
        [Units("mm")]
        public double PsiUpper { get { return -30000 / DiameterUpper; } }
        /// <summary>The water content of the soil when this pore is full and larger pores are empty</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double ThetaUpper { get; set; }
        /// <summary>The water content of the soil when this pore is empty and smaller pores are full</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double ThetaLower { get; set; }
        /// <summary>The volume of the the pore relative to the volume of soil</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double Volume { get { return ThetaUpper - ThetaLower; } }
        /// <summary>The volume of the the pore in mm</summary>
        [JsonIgnore]
        [Units("mm")]
        public double VolumeDepth { get { return Volume * Thickness; } }
        /// <summary>The water filled volume of the pore</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double WaterFilledVolume { get { return WaterDepth / Thickness; } }
        /// <summary>The water filled volume of the pore relative to the air space</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double RelativeWaterContent
        {
            get
            {
                if (Volume > 0)
                    return WaterFilledVolume / Volume;
                else
                    return 0;
            }
        }
        /// <summary>The air filled volume of the pore</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double AirFilledVolume { get { return Volume - WaterFilledVolume; } }

        private double _WaterDepth = 0;
        /// <summary>The depth of water in the pore</summary>
        [JsonIgnore]
        [Units("mm")]
        public double WaterDepth
        {
            get
            { return _WaterDepth; }
            set
            {
                if (value < -0.000000000001) throw new Exception("Trying to set a negative pore water depth");
                _WaterDepth = Math.Max(value, 0);//discard floating point errors
                if (_WaterDepth - VolumeDepth > FloatingPointTolerance)
                    throw new Exception("Trying to put more water into pore " + Compartment + "in layer " + Layer + " than will fit");
                if (Double.IsNaN(_WaterDepth))
                    throw new Exception("Something has just set Water depth to Nan for Pore " + Compartment + " in layer " + Layer + ".  Don't Worry, things like this happen sometimes.");
            }
        }
        /// <summary>The depth of Air in the pore</summary>
        [JsonIgnore]
        [Units("ml/ml")]
        public double AirDepth { get { return AirFilledVolume * Thickness; } }
        #endregion

        #region Pore hydraulics
        /// <summary>
        /// Empirical parameter for estimating hydraulic conductivity of pore compartments
        /// divide values from Arya 1999 etal by 10000 to convert from cm to um
        /// </summary>
        [Description("Pore flow Rate coefficient")]
        public double CFlow { get; set; }
        /// <summary>
        /// Empirical parameter for estimating hydraulic conductivity of pore compartments
        /// </summary>
        [Description("Pore flow Shape coefficient")]
        public double XFlow { get; set; }
        /// <summary>
        /// The volumetirc flow rate of a single pore
        /// </summary>
        [JsonIgnore]
        [Units("mm3/s")]
        public double PoreFlowRate { get { return CFlow * Math.Pow(Radius, XFlow); } }
        /// <summary>The hydraulic conductivity of water through this pore compartment</summary>
        [JsonIgnore]
        [Units("mm/h")]
        public double PoiseuilleFlow { get { return (PoreFlowRate * Number) / 1e6 * 3600; } }
        /// <summary>The potential diffusion out of this pore</summary>
        [JsonIgnore]
        [Units("mm/h")]
        public double Diffusivity { get { return PoiseuilleFlow * RelativeWaterContent * (1 - TensionFactor); } }
        /// <summary>The potential diffusion into this pore</summary>
        [JsonIgnore]
        [Units("mm")]
        public double DiffusionCapacity { get { return AirDepth * (1 - TensionFactor); } }
        /// <summary>
        /// The rate of water movement into a pore space due to the chemical attraction from the matris
        /// </summary>
        [JsonIgnore]
        [Units("mm s^-1/2")]
        public double Sorptivity
        {
            get
            {
                return Math.Sqrt(((7.4 / Radius * 1000) * PoiseuilleFlow * Math.Max(0, Volume - WaterFilledVolume)) / 0.5);
            }
        }
        /// <summary>
        /// Factor describing the effects of soil water content on hydrophobosity
        /// equals 1 if soil is hydrophyllic and decreases is soil becomes more hydrophobic
        /// </summary>
        [JsonIgnore]
        [Units("0-1")]
        public double RepelancyFactor { get; set; }
        /// <summary>
        /// The rate of water movement into a pore space due to the chemical attraction from the matrix
        /// </summary>
        [JsonIgnore]
        [Units("mm/h")]
        public double Sorption { get { return 0.5 * Sorptivity * Math.Pow(1, -0.5); } }
        /// <summary>The maximum possible conductivity through a pore of given size</summary>
        [JsonIgnore]
        [Units("mm/h")]
        public double HydraulicConductivityIn
        {
            get
            {
                if (Double.IsNaN(Sorption))
                    throw new Exception("Sorption is NaN");
                if (IncludeSorption)
                    return PoiseuilleFlow + (Sorption * RepelancyFactor);
                else
                    return PoiseuilleFlow;
            }
        }
        /// <summary>the gravitational potential for the layer this pore is in, calculated from height above zero potential base</summary>
        [JsonIgnore]
        [Units("mm")]
        public double GravitationalPotential { get; set; }
        /// <summary>
        /// Factor describing the effects of water surface tension holding water in pores.  Is zero where surface tension exceeds the forces of gravity and neglegable where suction is low in larger pores
        /// equals 1
        /// </summary>
        [JsonIgnore]
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
        }
        /// <summary>The conductivity of water moving out of a pore, The net result of gravity Opposed by capiliary draw back</summary>
        [JsonIgnore]
        [Units("mm/h")]
        public double HydraulicConductivityOut
        {
            get
            {
                return Math.Max(0, PoiseuilleFlow) * TensionFactor;
            }
        }
        #endregion

        #region Plant water extraction
        /// <summary>Factor to scale potential water extraction in each pore </summary>
        public double ExtractionMultiplier { get; set; }

        /// <summary>
        /// The proportion of pores in this cohort that have absorbing roots present
        /// </summary>
        [JsonIgnore]
        [Units("mm/mm3")]
        public double RootLengthDensity { get; set; }

        /// <summary>
        /// The amount of water that may be extracted from this pore class by plant roots each hour
        /// </summary>
        [JsonIgnore]
        [Units("mm/h")]
        public double PotentialWaterExtraction
        {
            get
            {
                double MeanDiffusionDistance = Math.Sqrt(1 / RootLengthDensity) * 0.5; //assumes root length density represents the number of roots transecting a layer
                double UptakeProp = (PoiseuilleFlow / MeanDiffusionDistance) * ExtractionMultiplier;
                double PotentialRootUptake = WaterDepth * UptakeProp;
                return Math.Min(PotentialRootUptake, WaterDepth);
            }
        }
        #endregion
    }
}
