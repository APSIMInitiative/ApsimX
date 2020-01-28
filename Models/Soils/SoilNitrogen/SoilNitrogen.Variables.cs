using System;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using System.Text;
using Models.Core;
using Models;
using APSIM.Shared.Utilities;
using Models.Interfaces;
using Models.Surface;

namespace Models.Soils
{

    /// <remarks>
    /// This partial class contains most of the variables and input properties of SoilNitrogen
    /// </remarks>
    public partial class SoilNitrogen
    {

        #region Links to other modules

        /// <summary>Link to APSIM's Clock (time information).</summary>
        [Link]
        public Clock Clock = null;

        /// <summary>Link to APSIM's WeatherFile (weather data).</summary>
        [Link]
        public IWeather MetFile = null;

        /// <summary>Link to APSIM Summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary mySummary = null;

        /// <summary>Link to the surface organic matter.</summary>
        [Link]
        public SurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>Link to the soil.</summary>
        [Link]
        public Soil Soil = null;

        #endregion

        #region Parameters and inputs provided by the user or APSIM

        #region Parameters used on initialisation only

        #region General setting parameters

        /// <summary>
        /// Soil parameterisation set to use.
        /// </summary>
        /// <remarks>
        /// Used to determine which node of xml file will be used to overwrite some [Param]'s
        /// </remarks>
        [Units("")]
        [XmlIgnore]
        public string SoilNParameterSet = "standard";

        /// <summary>
        /// Flag for whether routines for nitrification and codenitrification are to be used (ignore old nitrification).
        /// </summary>
        private bool usingNewNitrification = false;

        /// <summary>
        /// Gets or sets flag for whether routines for codenitrification are to be used (yes/no).
        /// </summary>
        /// <remarks>
        /// When 'yes', nitrification is computed using nitritation + nitratation, and codenitrification is also computed
        /// </remarks>
        [Units("yes/no")]
        [XmlIgnore]
        public string UseCodenitrification
        {
            get { return (usingNewNitrification) ? "yes" : "no"; }
            set { usingNewNitrification = value.ToLower().Contains("yes"); }
        }

        /// <summary>
        /// Gets or sets flag for whether soil profile reduction is allowed (yes/no).
        /// </summary>
        [Units("yes/no")]
        [XmlIgnore]
        public string AllowProfileReduction
        {
            get { return (profileReductionAllowed) ? "yes" : "no"; }
            set { profileReductionAllowed = value.ToLower().Contains("yes"); }
        }

        /// <summary>
        /// Gests or sets flag for whether organic solutes are to be simulated (yes/no).
        /// </summary>
        /// <remarks>
        /// It should always be false, as organic solutes are not implemented yet
        /// </remarks>
        [Units("yes/no")]
        [XmlIgnore]
        public string allowOrganicSolutes
        {
            get { return (organicSolutesAllowed) ? "yes" : "no"; }
            set { organicSolutesAllowed = value.ToLower().Contains("yes"); }
        }

        /// <summary>
        /// Factor to convert organic carbon to organic matter (g/g).
        /// </summary>
        [Units("g/g")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [XmlIgnore]
        public double DefaultCarbonInSoilOM = 0.588;

        /// <summary>
        /// Default carbon weight fraction in FOM (g/g).
        /// </summary>
        /// <remarks>
        /// Used to convert FOM amount into fom_c
        /// </remarks>
        [Units("g/g")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [XmlIgnore]
        public double DefaultCarbonInFOM = 0.4;

        /// <summary>
        /// Default initial pH, used case no pH is initialised in model.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double DefaultInitialpH = 6.0;

        /// <summary>
        /// Threshold for raising a warning due to small negative values.
        /// </summary>
        /// <remarks>
        /// Any value between this and the FatalNegativeThreshold will be zeroed, but a warning message
        /// is raised, values smaller than this threshold will be zeroed without any message
        /// </remarks>
        [Units("")]
        [XmlIgnore]
        public double WarningNegativeThreshold = -0.0000000001;

        /// <summary>
        /// Threshold for a fatal error due to negative values (loss of mass balance).
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double FatalNegativeThreshold = -0.000001;

        #endregion general settings

        #region Parameters for setting up soil organic matter

        /// <summary>
        /// The C:N ratio of the soil humus (active + inert).
        /// </summary>
        /// <remarks>
        /// Remains fixed throughout the simulation
        /// </remarks>
        [Bounds(Lower = 5, Upper = 30)]
        [Units("g/g")]
        [XmlIgnore]
        public double[] HumusCNr = { 10.0, 11.0 };

        /// <summary>
        /// The C:N ratio of soil microbial biomass.
        /// </summary>
        /// <remarks>
        /// Remains fixed throughout the simulation
        /// </remarks>
        [Bounds(Lower = 5, Upper = 15)]
        [Units("")]
        [XmlIgnore]
        public double MBiomassCNr = 8.0;

        /// <summary>
        /// Proportion of biomass-C in the initial mineralisable humic-C (g/g).
        /// </summary>
        [Bounds(Lower = 0, Upper = 1)]
        [Units("g/g")]
        [XmlIgnore]
        public double[] FBiom = { 0.05, 0.01 };

        /// <summary>
        /// Proportion of the initial total soil C that is inert, cannot be mineralised (g/g).
        /// </summary>
        [Bounds(Lower = 0, Upper = 1)]
        [Units("g/g")]
        [XmlIgnore]
        public double[] FInert = { 0.5, 0.95 };

        #endregion params for OM setup

        #region Parameters for setting up fresh organic matter (FOM)

        /// <summary>
        /// Initial amount of FOM in the soil (kgDM/ha).
        /// </summary>
        [Bounds(Lower = 0, Upper = 100000)]
        [Units("kg/ha")]
        [XmlIgnore]
        public double InitialFOMWt = 2000;

        /// <summary>
        /// Initial depth over which FOM is distributed within the soil profile (mm).
        /// </summary>
        /// <remarks>
        /// If not given (-ve), FOM will be distributed over the whole soil profile
        /// Distribution follows an exponential function
        /// </remarks>
        [Units("mm")]
        [XmlIgnore]
        public double InitialFOMDepth = -99.0;

        /// <summary>
        /// Exponent of function used to compute initial distribution of FOM in the soil.
        /// </summary>
        [Bounds(Lower = 0.01, Upper = 10.0)]
        [Units("")]
        [XmlIgnore]
        public double InitialFOMDistCoefficient = 3.0;

        /// <summary>Initial C:N ratio of soil FOM (g/g).</summary>
        [Units("g/g")]
        [XmlIgnore]
        public double InitialFOMCNr = 40.0;

        /// <summary>
        /// FOM type used on initialisation and reset.
        /// </summary>
        /// <remarks>
        /// The default value (0) is always assumed
        /// </remarks>
        private int FOMtypeID_reset = 0;

        /// <summary>
        /// FOM type to be used on initialisation.
        /// </summary>
        /// <remarks>
        /// This sets the partition of FOM C between the different pools (carbohydrate, cellulose, lignin)
        /// </remarks>
        [XmlIgnore]
        public string InitialFOMType
        {
            get { return fom_types[FOMtypeID_reset]; }
            set
            {
                FOMtypeID_reset = 0;
                for (int i = 0; i < fom_types.Length; i++)
                {
                    if (fom_types[i] == value)
                    {
                        FOMtypeID_reset = i;
                        break;
                    }
                }
                if (InitialFOMType != fom_types[FOMtypeID_reset])
                {   // no valid FOM type was given, use default
                    FOMtypeID_reset = 0;
                    mySummary.WriteWarning(this,"   The initial FOM type was not found, the default type will be used");
                }
            }
        }

        /// <summary>
        /// List of available FOM types names.
        /// </summary>
        [XmlArray("fom_type")]
        public string[] fom_types = { "default", "manure", "mucuna", "lablab", "shemp", "stable" };

        /// <summary>
        /// Pool 1 fraction in FOM [carbohydrate], for each FOM type (g/g).
        /// </summary>
        [Units("g/g")]
        public double[] fract_carb = { 0.2, 0.3, 0.54, 0.57, 0.45, 0.0 };

        /// <summary>
        /// Pool 2 fraction in FOM [cellulose], for each FOM type (g/g).
        /// </summary>
        [Units("g/g")]
        public double[] fract_cell = { 0.7, 0.3, 0.37, 0.37, 0.47, 0.1 };

        /// <summary>
        /// Pool 3 fraction in FOM [lignin], for each FOM type (g/g).
        /// </summary>
        [Units("g/g")]
        public double[] fract_lign = { 0.1, 0.4, 0.09, 0.06, 0.08, 0.9 };

        #endregion  params for FOM setup

        #region Parameters for the decomposition process of FOM and SurfaceOM

        #region Surface OM

        /// <summary>
        /// Fraction of residue C mineralised lost to atmopshere due to respiration (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double ResiduesRespirationFactor = 0.6;

        /// <summary>
        /// Fraction of retained residue C transferred to microbial biomass (g/g).
        /// </summary>
        /// <remarks>
        /// The remaining will go into humus
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double ResiduesFractionIntoBiomass = 0.9;

        /// <summary>
        /// Depth from which mineral N can be immobilised when decomposing surface residues (mm).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm")]
        [XmlIgnore]
        public double ResiduesDecompDepth = 100;

        #endregion

        #region Fresh OM

        /// <summary>
        /// Optimum rate for decomposition of FOM pool 1 [carbohydrate], aerobic and anaerobic conditions (/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/day")]
        [XmlIgnore]
        public double[] Pool1FOMTurnOverRate = { 0.2, 0.1 };

        /// <summary>
        /// Optimum rate for decomposition of FOM pool 2 [cellulose], aerobic and anaerobic conditions (/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/day")]
        [XmlIgnore]
        public double[] Pool2FOMTurnOverRate = { 0.05, 0.25 };

        /// <summary>
        /// Optimum rate for decomposition of FOM pool 3 [lignin], aerobic and anaerobic conditions (/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/day")]
        [XmlIgnore]
        public double[] Pool3FOMTurnOverRate = { 0.0095, 0.003 };

        /// <summary>
        /// Fraction of the FOM C decomposed lost to atmopshere due to respiration (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double FOMRespirationFactor = 0.6;

        /// <summary>
        /// Fraction of the retained FOM C transferred to biomass (g/g).
        /// </summary>
        /// <remarks>
        /// The remaining will go into humus
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double FOMFractionIntoBiomass = 0.9;

        #region Limiting factors

        /// <summary>
        /// Coefficient for the exponential phase of C:N effects on decomposition of FOM.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("")]
        [XmlIgnore]
        public double FOMDecomp_CNCoefficient = 0.693;

        /// <summary>
        /// Value of C:N ratio above which decomposition rate of FOM declines.
        /// </summary>
        [Bounds(Lower = 5.0, Upper = 100.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double FOMDecomp_CNThreshold = 25.0;

        /// <summary>
        /// Data for calculating the temperature effect on FOM decomposition.
        /// </summary>
        private BentStickData FOMDecomp_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for FOM decomposition, aerobic and anaerobic conditions (oC).
        /// </summary>
        [Units("oC")]
        [XmlIgnore]
        public double[] FOMDecomp_TOptimum
        {
            get { return FOMDecomp_TemperatureFactorData.xValueForOptimum; }
            set { FOMDecomp_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for FOM decomposition at zero degrees, aerobic and anaerobic conditions.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] FOMDecomp_TFactorAtZero
        {
            get { return FOMDecomp_TemperatureFactorData.yValueAtZero; }
            set { FOMDecomp_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient for temperature factor of FOM decomposition, aerobic and anaerobic conditions.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] FOMDecomp_TCurveCoeff
        {
            get { return FOMDecomp_TemperatureFactorData.CurveExponent; }
            set { FOMDecomp_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters for calculating the soil moisture factor for FOM decomposition.
        /// </summary>
        private BrokenStickData FOMDecomp_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of the modified normalised soil water content at which the moisture factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=sat
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] FOMDecomp_NormWaterContents
        { set { FOMDecomp_MoistureFactorData.xVals = value; } }

        /// <summary>
        /// Moisture factor values for the given values of normalised soil water content.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] FOMDecomp_MoistureFactors
        { set { FOMDecomp_MoistureFactorData.yVals = value; } }

        #endregion

        #endregion FOM

        #endregion params for SurfOM + FOM decomposition

        #region Parameters for SOM mineralisation/immobilisation process

        /// <summary>
        /// Potential rate of soil biomass mineralisation, aerobic and anaerobic conditions (/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/day")]
        [XmlIgnore]
        public double[] MBiomassTurnOverRate = { 0.0081, 0.004 };

        /// <summary>
        /// Fraction of microbial biomass C mineralised that is lost to the atmosphere due to respiration (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double MBiomassRespirationFactor = 0.6;

        /// <summary>
        /// Fraction of retained microbial biomass C that goes back to microbial biomass (g/g).
        /// </summary>
        /// <remarks>
        /// The remaining will go in to humus
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("g/g")]
        [XmlIgnore]
        public double MBiomassFractionIntoBiomass = 0.6;

        /// <summary>
        /// Potential rate of active humus mineralisation, aerobic and anaerobic conditions (/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/day")]
        [XmlIgnore]
        public double[] AHumusTurnOverRate = { 0.00015, 0.00007 };

        /// <summary>
        /// Fraction of active humic C mineralised that is lost to the atmosphere due to respiration (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/g/")]
        [XmlIgnore]
        public double AHumusRespirationFactor = 0.6;

        #region Limiting factors

        /// <summary>
        /// Data to calculate the temperature effect on soil OM mineralisation.
        /// </summary>
        private BentStickData SOMMiner_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for soil OM mineralisation (oC).
        /// </summary>
        [Units("oC")]
        [XmlIgnore]
        public double[] SOMMiner_TOptimum
        {
            get { return SOMMiner_TemperatureFactorData.xValueForOptimum; }
            set { SOMMiner_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for soil OM mineralisation at zero degree.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] SOMMiner_TFactorAtZero
        {
            get { return SOMMiner_TemperatureFactorData.yValueAtZero; }
            set { SOMMiner_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient to calculate temperature factor for soil OM mineralisation.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] SOMMiner_TCurveCoeff
        {
            get { return SOMMiner_TemperatureFactorData.CurveExponent; }
            set { SOMMiner_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters to calculate soil moisture factor for soil OM mineralisation.
        /// </summary>
        private BrokenStickData SOMMiner_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of the modified normalised soil water content at which moisture factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=SAT
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] SOMMiner_NormWaterContents
        {
            get { return SOMMiner_MoistureFactorData.xVals; }
            set { SOMMiner_MoistureFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the moisture factor at given values of the normalised water content.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] SOMMiner_MoistureFactors
        {
            get { return SOMMiner_MoistureFactorData.yVals; }
            set { SOMMiner_MoistureFactorData.yVals = value; }
        }

        #endregion

        #endregion params for OM decomposition

        #region Parameters for urea hydrolysis process

        /// <summary>
        /// Minimum potential hydrolysis rate for urea (/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/day")]
        [XmlIgnore]
        public double UreaHydrol_MinRate = 0.25;

        /// <summary>
        /// Parameter A for the potential urea hydrolysis function.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double UreaHydrol_parmA = -1.12;

        /// <summary>
        /// Parameter B for the potential urea hydrolysis function.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double UreaHydrol_parmB = 1.31;

        /// <summary>
        /// Parameter C for the potential urea hydrolysis function.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double UreaHydrol_parmC = 0.203;

        /// <summary>
        /// Parameter D for the potential urea hydrolysis function.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double UreaHydrol_parmD = -0.155;

        #region limiting factors

        /// <summary>
        /// Parameters to calculate the temperature effect on urea hydrolysis.
        /// </summary>
        private BentStickData UreaHydrolysis_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for urea hydrolysis (oC).
        /// </summary>
        [Bounds(Lower = 5.0, Upper = 100.0)]
        [Units("oC")]
        [XmlIgnore]
        public double[] UreaHydrol_TOptimum
        {
            get { return UreaHydrolysis_TemperatureFactorData.xValueForOptimum; }
            set { UreaHydrolysis_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for urea hydrolysis at zero degrees.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] UreaHydrol_TFactorAtZero
        {
            get { return UreaHydrolysis_TemperatureFactorData.yValueAtZero; }
            set { UreaHydrolysis_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient to calculate the temperature factor for urea hydrolysis.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] UreaHydrol_TCurveCoeff
        {
            get { return UreaHydrolysis_TemperatureFactorData.CurveExponent; }
            set { UreaHydrolysis_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters to calculate the moisture effect on urea hydrolysis
        /// </summary>
        private BrokenStickData UreaHydrolysis_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of the modified normalised soil water content at which factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=SAT
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] UreaHydrol_NormWaterContents
        {
            get { return UreaHydrolysis_MoistureFactorData.xVals; }
            set { UreaHydrolysis_MoistureFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the moisture factor at given values of the normalised water content.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] UreaHydrol_MoistureFactors
        {
            get { return UreaHydrolysis_MoistureFactorData.yVals; }
            set { UreaHydrolysis_MoistureFactorData.yVals = value; }
        }

        #endregion

        #endregion params for hydrolysis

        #region Parameters for nitrification process

        /// <summary>
        /// Maximum soil nitrification potential, Michaelis-Menten dynamics (ug NH4/g soil/day).
        /// </summary>
        /// <remarks>
        /// This is the parameter M on Michaelis-Menten equation, r = MC/(k+C)
        /// </remarks>
        [Units("ppm/day")]
        [XmlIgnore]
        public double NitrificationMaxPotential = 40.0;

        /// <summary>
        /// NH4 concentration at half potential nitrification, Michaelis-Menten dynamics (ppm).
        /// </summary>
        /// <remarks>
        /// This is the parameter k on Michaelis-Menten equation, r = MC/(k+C)
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 200.0)]
        [Units("ppm")]
        [XmlIgnore]
        public double NitrificationNH4ForHalfRate = 90.0;

        /// <summary>
        /// Fraction of nitrification lost as denitrification
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double Nitrification_DenitLossFactor = 0.0;

        /// <summary>
        /// Parameters to calculate the temperature effect on nitrification.
        /// </summary>
        private BentStickData Nitrification_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for nitrification (oC).
        /// </summary>
        [Bounds(Lower = 5.0, Upper = 100.0)]
        [Units("oC")]
        [XmlIgnore]
        public double[] Nitrification_TOptimum
        {
            get { return Nitrification_TemperatureFactorData.xValueForOptimum; }
            set { Nitrification_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for nitrification at zero degrees.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification_FactorAtZero
        {
            get { return Nitrification_TemperatureFactorData.yValueAtZero; }
            set { Nitrification_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient for calculating the temperature factor for nitrification.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification_CurveCoeff
        {
            get { return Nitrification_TemperatureFactorData.CurveExponent; }
            set { Nitrification_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil moisture factor for nitrification.
        /// </summary>
        private BrokenStickData Nitrification_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of the modified normalised soil water content at which the moisture factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=SAT
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification_NormWaterContents
        {
            get { return Nitrification_MoistureFactorData.xVals; }
            set { Nitrification_MoistureFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the moisture factor at given values of the normalised water content.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification_MoistureFactors
        {
            get { return Nitrification_MoistureFactorData.yVals; }
            set { Nitrification_MoistureFactorData.yVals = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil pH factor for nitrification.
        /// </summary>
        private BrokenStickData Nitrification_pHFactorData = new BrokenStickData();

        /// <summary>
        /// Values of pH at which the pH factor is given.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 14.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification_pHValues
        {
            get { return Nitrification_pHFactorData.xVals; }
            set { Nitrification_pHFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of pH factor at given pH values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification_pHFactors
        {
            get { return Nitrification_pHFactorData.yVals; }
            set { Nitrification_pHFactorData.yVals = value; }
        }

        #region Parameters for Nitritation + Nitration processes

        /// <summary>
        /// Maximum soil potential nitritation rate (ug NH4/g soil/day).
        /// </summary>
        /// <remarks>
        /// This is the parameter M on Michaelis-Menten equation, r = MC/(k+C)
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 200.0)]
        [Units("ppm/day")]
        [XmlIgnore]
        public double NitritationMaxPotential = 40.0;

        /// <summary>
        /// NH4 concentration when nitritation is half of potential (ppm).
        /// </summary>
        /// <remarks>
        /// This is the parameter k on Michaelis-Menten equation, r = MC/(k+C)
        /// </remarks>
        [Units("ppm")]
        [Bounds(Lower = 0.0, Upper = 200.0)]
        [XmlIgnore]
        public double NitritationNH4ForHalfRate = 90.0;

        /// <summary>
        /// Maximum soil potential nitratation rate (ug NO2/g soil/day).
        /// </summary>
        /// <remarks>
        /// This is the parameter M on Michaelis-Menten equation, r = MC/(k+C)
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("ppm/day")]
        [XmlIgnore]
        public double NitratationMaxPotential = 400.0;

        /// <summary>
        /// NO2 concentration when nitratation is half of potential (ppm).
        /// </summary>
        /// <remarks>
        /// This is the parameter k on Michaelis-Menten equation, r = MC/(k+C)
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 500.0)]
        [Units("ppm")]
        [XmlIgnore]
        public double NitratationNH4ForHalfRate = 90.0;

        /// <summary>
        /// Parameter to determine the base fraction of ammonia oxidate lost as N2O.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double AmmoxLossParam1 = 0.0025;

        /// <summary>
        /// Parameter to determine the changes in fraction of ammonia oxidate lost as N2O.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double AmmoxLossParam2 = 0.45;

        /// <summary>
        /// Parameters to calculate the temperature effect on nitrification (Nitrition + Nitration).
        /// </summary>
        private BentStickData Nitrification2_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for nitrification (Nitrition + Nitration).
        /// </summary>
        [Bounds(Lower = 5.0, Upper = 100.0)]
        [Units("oC")]
        [XmlIgnore]
        public double[] Nitrification2_TOptimum
        {
            get { return Nitrification2_TemperatureFactorData.xValueForOptimum; }
            set { Nitrification2_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for nitrification (Nitrition + Nitration) at zero degrees.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification2_TFactorAtZero
        {
            get { return Nitrification2_TemperatureFactorData.yValueAtZero; }
            set { Nitrification2_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient for calculating the temperature factor for nitrification (Nitrition + Nitration).
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification2_TCurveCoeff
        {
            get { return Nitrification2_TemperatureFactorData.CurveExponent; }
            set { Nitrification2_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil moisture factor for nitrification (Nitrition + Nitration).
        /// </summary>
        private BrokenStickData Nitrification2_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of the modified soil water content at which the moisture factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=SAT
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification2_NormWaterContents
        {
            get { return Nitrification2_MoistureFactorData.xVals; }
            set { Nitrification2_MoistureFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the moisture factor at given normalised water content values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitrification2_MoistureFactors
        {
            get { return Nitrification2_MoistureFactorData.yVals; }
            set { Nitrification2_MoistureFactorData.yVals = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil pH factor for nitritation.
        /// </summary>
        private BrokenStickData Nitritation_pHFactorData = new BrokenStickData();

        /// <summary>
        /// Values of pH at which factors is given.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 14.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitritation_pHValues
        {
            get { return Nitritation_pHFactorData.xVals; }
            set { Nitritation_pHFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of pH factor at given pH values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitritation_pHFactors
        {
            get { return Nitritation_pHFactorData.yVals; }
            set { Nitritation_pHFactorData.yVals = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil pH factor for nitratation.
        /// </summary>
        private BrokenStickData Nitratation_pHFactorData = new BrokenStickData();

        /// <summary>
        /// Values of pH at which factors is given.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 14.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitratation_pHValues
        {
            get { return Nitratation_pHFactorData.xVals; }
            set { Nitratation_pHFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of pH factor at given pH values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Nitratation_pHFactors
        {
            get { return Nitratation_pHFactorData.yVals; }
            set { Nitratation_pHFactorData.yVals = value; }
        }

        #endregion params for nitritation+nitratation

        #endregion params for nitrification

        #region Parameters for codenitrification and N2O emission processes

        /// <summary>
        /// Denitrification rate coefficient (kg soil/mg C/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("kg/mg/day")]
        [XmlIgnore]
        public double CodenitrificationRateCoefficient = 0.0006;

        /// <summary>
        /// Parameters to calculate the temperature effect on codenitrification.
        /// </summary>
        private BentStickData Codenitrification_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for codenitrification.
        /// </summary>
        [Bounds(Lower = 5.0, Upper = 100.0)]
        [Units("oC")]
        [XmlIgnore]
        public double[] Codenitrification_TOptmimun
        {
            get { return Codenitrification_TemperatureFactorData.xValueForOptimum; }
            set { Codenitrification_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for codenitrification at zero degrees.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_TFactorAtZero
        {
            get { return Codenitrification_TemperatureFactorData.yValueAtZero; }
            set { Codenitrification_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient for calculating the temperature factor for codenitrification.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_TCurveCoeff
        {
            get { return Codenitrification_TemperatureFactorData.CurveExponent; }
            set { Codenitrification_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil moisture factor for codenitrification.
        /// </summary>
        private BrokenStickData Codenitrification_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of modified soil water content at which the moisture factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=SAT
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_NormWaterContents
        {
            get { return Codenitrification_MoistureFactorData.xVals; }
            set { Codenitrification_MoistureFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the moisture factor at given water content values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_MoistureFactors
        {
            get { return Codenitrification_MoistureFactorData.yVals; }
            set { Codenitrification_MoistureFactorData.yVals = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil pH factor for codenitrification.
        /// </summary>
        private BrokenStickData Codenitrification_pHFactorData = new BrokenStickData();

        /// <summary>
        /// Values of soil pH at which the pH factor is given.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_pHValues
        {
            get { return Codenitrification_pHFactorData.xVals; }
            set { Codenitrification_pHFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the pH factor at given pH values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_pHFactors
        {
            get { return Codenitrification_pHFactorData.yVals; }
            set { Codenitrification_pHFactorData.yVals = value; }
        }

        /// <summary>
        /// Parameters to calculate the N2:N2O ratio during denitrification.
        /// </summary>
        private BrokenStickData Codenitrification_NH3NO2FactorData = new BrokenStickData();

        /// <summary>
        /// Values of soil NH3+NO2 at which the N2 fraction is given.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("ppm")]
        [XmlIgnore]
        public double[] Codenitrification_NHNOValues
        {
            get { return Codenitrification_NH3NO2FactorData.xVals; }
            set { Codenitrification_NH3NO2FactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the N2 fraction at given NH3+NO2 values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Codenitrification_NHNOFactors
        {
            get { return Codenitrification_NH3NO2FactorData.yVals; }
            set { Codenitrification_NH3NO2FactorData.yVals = value; }
        }

        #endregion params for codenitrification

        #region Parameters for denitrification and N2O emission processes

        /// <summary>
        /// Denitrification rate coefficient (kg/mg).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("kg/mg")]
        [XmlIgnore]
        public double DenitrificationRateCoefficient = 0.0006;

        /// <summary>
        /// Parameter A of linear function to compute soluble carbon.
        /// </summary>
        [Units("g/g")]
        [XmlIgnore]
        public double actC_parmA = 24.5;

        /// <summary>
        /// Parameter B of linear function to compute soluble carbon.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double actC_parmB = 0.0031;

        /// <summary>
        /// Parameter A of exponential function to compute soluble carbon.
        /// </summary>
        [Units("g/g")]
        [XmlIgnore]
        public double actCExp_parmA = 0.011;

        /// <summary>
        /// Parameter B of exponential function to compute soluble carbon.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double actCExp_parmB = 0.895;

        /// <summary>
        /// Parameters to calculate the temperature effect on denitrification.
        /// </summary>
        private BentStickData Denitrification_TemperatureFactorData = new BentStickData();

        /// <summary>
        /// Optimum temperature for denitrification (oC).
        /// </summary>
        [Bounds(Lower = 5.0, Upper = 100.0)]
        [Units("oC")]
        [XmlIgnore]
        public double[] Denitrification_TOptimum
        {
            get { return Denitrification_TemperatureFactorData.xValueForOptimum; }
            set { Denitrification_TemperatureFactorData.xValueForOptimum = value; }
        }

        /// <summary>
        /// Temperature factor for denitrification at zero degrees.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Denitrification_TFactorAtZero
        {
            get { return Denitrification_TemperatureFactorData.yValueAtZero; }
            set { Denitrification_TemperatureFactorData.yValueAtZero = value; }
        }

        /// <summary>
        /// Curve coefficient for calculating the temperature factor for denitrification.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double[] Denitrification_TCurveCoeff
        {
            get { return Denitrification_TemperatureFactorData.CurveExponent; }
            set { Denitrification_TemperatureFactorData.CurveExponent = value; }
        }

        /// <summary>
        /// Parameters to calculate the soil moisture factor for denitrification
        /// </summary>
        private BrokenStickData Denitrification_MoistureFactorData = new BrokenStickData();

        /// <summary>
        /// Values of modified normalised soil water content at which the moisture factor is given.
        /// </summary>
        /// <remarks>
        /// X values for the moisture factor function, with: 0=dry, 1=LL, 2=DUL, 3=SAT
        /// </remarks>
        [Bounds(Lower = 0.0, Upper = 3.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Denitrification_NormWaterContents
        {
            get { return Denitrification_MoistureFactorData.xVals; }
            set { Denitrification_MoistureFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the moisture factor at given normalised water content values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Denitrification_MoistureFactors
        {
            get { return Denitrification_MoistureFactorData.yVals; }
            set { Denitrification_MoistureFactorData.yVals = value; }
        }

        #region Parameters for N2:N2O partition

        /// <summary>
        /// Parameter k1 from Thorburn et al (2010) for N2O model.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("")]
        [XmlIgnore]
        public double Denit_k1 = 25.1;

        /// <summary>
        /// Parameter A in the function computing the N2:N2O ratio.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double N2N2O_parmA = 0.16;

        /// <summary>
        /// Parameter B in the function computing the N2:N2O ratio.
        /// </summary>
        [Units("")]
        [XmlIgnore]
        public double N2N2O_parmB = -0.80;

        /// <summary>
        /// Parameters to calculate the effect of water filled pore space on N2:N2O ratio during denitrification.
        /// </summary>
        private BrokenStickData Denitrification_WFPSFactorData = new BrokenStickData();

        /// <summary>
        /// Values of soil water filled pore sapce at which the WFPS factor is given.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("%")]
        [XmlIgnore]
        public double[] Denit_WPFSValues
        {
            get { return Denitrification_WFPSFactorData.xVals; }
            set { Denitrification_WFPSFactorData.xVals = value; }
        }

        /// <summary>
        /// Values of the WFPS factor at given water fille pore space values.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double[] Denit_WFPSFactors
        {
            get { return Denitrification_WFPSFactorData.yVals; }
            set { Denitrification_WFPSFactorData.yVals = value; }
        }

        #endregion

        #endregion params for denitrification

        #endregion params for initialisation

        #region Parameters that do or may change during simulation

        #region Parameter for handling patches

        /// <summary>
        /// The approach used for partitioning the N between patches.
        /// </summary>
        [XmlIgnore]
        public string NPartitionApproach
        {
            get { return patchNPartitionApproach; }
            set { patchNPartitionApproach = value.Trim(); }
        }

        /// <summary>
        /// Layer thickness to consider when N partition between patches is BasedOnSoilConcentration (mm).
        /// </summary>
        [Units("mm")]
        [XmlIgnore]
        public double LayerForNPartition = -99;

        /// <summary>
        /// Minimum relative area (fraction of paddock) for any patch.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [XmlIgnore]
        public double MininumRelativeAreaCNPatch
        {
            get { return minimumPatchArea; }
            set { minimumPatchArea = value; }
        }

        /// <summary>
        /// Maximum NH4 uptake rate for plants (ppm/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("ppm/day")]
        [XmlIgnore]
        public double MaximumUptakeRateNH4
        {
            get { return reset_MaximumNH4Uptake; }
            set
            {
                reset_MaximumNH4Uptake = value;
                if (initDone)
                { // after initialisation done, the setting has to be here
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        // set maximum uptake rates for N forms (only really used for AgPasture when patches exist)
                        maximumNH4UptakeRate[layer] = reset_MaximumNH4Uptake / convFactor[layer];
                    }
                }
            }
        }

        /// <summary>
        /// Maximum NO3 uptake rate for plants (ppm/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("ppm/day")]
        [XmlIgnore]
        public double MaximumUptakeRateNO3
        {
            get { return reset_MaximumNO3Uptake; }
            set
            {
                reset_MaximumNO3Uptake = value;
                if (initDone)
                { // after initialisation done, the setting has to be here
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        // set maximum uptake rates for N forms (only really used for AgPasture when patches exist)
                        maximumNO3UptakeRate[layer] = reset_MaximumNO3Uptake / convFactor[layer];
                    }
                }
            }
        }

        /// <summary>
        /// The maximum amount of N that is made available to plants in one day (kg/ha/day).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("kg/ha/day")]
        [XmlIgnore]
        public double MaximumNitrogenAvailableToPlants
        {
            get { return maxTotalNAvailableToPlants; }
            set
            {
                maxTotalNAvailableToPlants = value;
            }
        }

        #region Parameter for amalgamating patches

        /// <summary>
        /// Flag for whether auto amalgamation of CN patches is allowed (yes/no).
        /// </summary>
        [Units("yes/no")]
        [XmlIgnore]
        public string AllowPatchAutoAmalgamation
        {
            get { return (patchAutoAmalgamationAllowed) ? "yes" : "no"; }
            set { patchAutoAmalgamationAllowed = value.ToLower().Contains("yes"); }
        }

        /// <summary>
        /// Approach to use when comparing patches for AutoAmalagamation.
        /// </summary>
        /// <remarks>
        /// Options:
        ///  - CompareAll: All patches are compared before they are merged
        ///  - CompareBase: All patches are compare to base first, then merged, then compared again
        ///  - CompareMerge: Patches are compare and merged at once if deemed equal, then compare to next
        /// </remarks>
        [XmlIgnore]
        public string AutoAmalgamationApproach
        {
            get { return patchAmalgamationApproach; }
            set { patchAmalgamationApproach = value.Trim(); }
        }

        /// <summary>
        /// Approach to use when defining the base patch.
        /// </summary>
        /// <remarks>
        /// This is used to define the patch considered the 'base'. It is only used when comparing patches during
        /// potential auto-amalgamation (comparison against base are more lax)
        /// Options:
        ///  - IDBased: the patch with lowest ID (=0) is used as the base
        ///  - AreaBased: The [first] patch with the biggest area is used as base
        /// </remarks>
        [XmlIgnore]
        public string basePatchApproach
        {
            get { return patchbasePatchApproach; }
            set { patchbasePatchApproach = value.Trim(); }
        }

        /// <summary>
        /// Flag for whether an age check is used to force amalgamation of patches (yes/no).
        /// </summary>
        [Units("yes/no")]
        [XmlIgnore]
        public string AllowPatchAmalgamationByAge
        {
            get { return (patchAmalgamationByAgeAllowed) ? "yes" : "no"; }
            set { patchAmalgamationByAgeAllowed = value.ToLower().Contains("yes"); }
        }

        /// <summary>
        /// Age of patch at which merging is enforced (years).
        /// </summary>
        [Units("years")]
        [XmlIgnore]
        public double PatchAgeForForcedMerge
        {
            get { return forcedMergePatchAge; }
            set { forcedMergePatchAge = value; }
        }

        /// <summary>
        /// Relative difference in total organic carbon (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_TotalOrgC = 0.02;

        /// <summary>
        /// Relative difference in total organic nitrogen (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_TotalOrgN = 0.02;

        /// <summary>
        /// Relative difference in total organic nitrogen (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_TotalBiomC = 0.02;

        /// <summary>
        /// Relative difference in total urea N amount (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_TotalUrea = 0.02;

        /// <summary>
        /// Relative difference in total NH4 N amount (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_TotalNH4 = 0.02;

        /// <summary>
        /// Relative difference in total NO3 N amount (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_TotalNO3 = 0.02;

        /// <summary>
        /// Relative difference in urea N amount at any layer (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_LayerBiomC = 0.02;

        /// <summary>
        /// Relative difference in urea N amount at any layer (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_LayerUrea = 0.02;

        /// <summary>
        /// Relative difference in NH4 N amount at any layer (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_LayerNH4 = 0.02;

        /// <summary>
        /// Relative difference in NO3 N amount at any layer (g/g).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double relativeDiff_LayerNO3 = 0.02;

        /// <summary>
        /// Absolute difference in total organic carbon (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_TotalOrgC = 500;

        /// <summary>
        /// Absolute difference in total organic nitrogen (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_TotalOrgN = 50;

        /// <summary>
        /// Absolute difference in total organic nitrogen (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_TotalBiomC = 50;

        /// <summary>
        /// Absolute difference in total urea N amount (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_TotalUrea = 2;

        /// <summary>
        /// Absolute difference in total NH4 N amount (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_TotalNH4 = 5;

        /// <summary>
        /// Absolute difference in total NO3 N amount (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_TotalNO3 = 5;

        /// <summary>
        /// Absolute difference in urea N amount at any layer (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_LayerBiomC = 1;

        /// <summary>
        /// Absolute difference in urea N amount at any layer (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_LayerUrea = 1;

        /// <summary>
        /// Absolute difference in NH4 N amount at any layer (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_LayerNH4 = 1;

        /// <summary>
        /// Absolute difference in NO3 N amount at any layer (kg/ha).
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double absoluteDiff_LayerNO3 = 1;

        /// <summary>
        /// Depth to consider when testing diffs by layer, if -ve soil depth is used (mm).
        /// </summary>
        [Units("mm")]
        [XmlIgnore]
        public double DepthToTestByLayer = 99;

        /// <summary>
        /// Factor to adjust the tests between patches other than base (0-1).
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [XmlIgnore]
        public double DiffAdjustFactor = 0.5;

        #endregion amalgamating patches

        #endregion

        #region Soil pH data

        /// <summary>
        /// pH of soil (assumed equivalent to a 1:1 soil-water slurry).
        /// </summary>
        [Bounds(Lower = 3.0, Upper = 11.0)]
        [XmlIgnore]
        public double[] ph = { 6, 6 };

        #endregion ph data

        #region Values for soil organic matter (som)

        /// <summary>
        /// Total soil organic carbon content (%).
        /// </summary>
        [Units("%")]
        [XmlIgnore]
        public double[] oc
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; ++layer)
                        result[layer] = (HumicC[layer] + MicrobialC[layer]) * convFactor[layer] / 10000;  // (100/1000000) = convert to ppm and then to %
                }
                else
                {
                    // no value has been asigned yet, return null
                    result = reset_oc;
                }

                return result;
            }
            set
            {
                if (initDone)
                {
                    mySummary.WriteMessage(this, " Attempt to assign values for OC during simulation, "
                                     + "this operation is not valid and will be ignored");
                }
                else
                {
                    // Store initial values, check and initialisation of C pools is done on InitCalc().
                    reset_oc = value;
                    // these values are also used OnReset
                }
            }
        }

        #endregion soil organic matter data

        #region Values for soil mineral N
        /// <summary>
        /// 
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("mg/kg")]
        [XmlIgnore]
        public double[] ureappm
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; ++layer)
                        result[layer] = CalculateUrea()[layer] * convFactor[layer];
                }
                else
                    result = reset_ureappm;
                return result;
            }
            set
            {
                if (!initDone)
                    reset_ureappm = value;    // check is done on InitCalc
                else
                    mySummary.WriteMessage(this, "An external module attempted to change the value of urea during simulation, the command will be ignored");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("mg/kg")]
        [XmlIgnore]
        public double[] NH4ppm
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; ++layer)
                        result[layer] = CalculateNH4()[layer] * convFactor[layer];
                }
                else
                    result = reset_nh4ppm;
                return result;
            }
            set
            {
                if (!initDone)
                    reset_nh4ppm = value;
                else
                    mySummary.WriteMessage(this, "An external module attempted to change the value of NH4 during simulation, the command will be ignored");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 10000.0)]
        [Units("mg/kg")]
        [XmlIgnore]
        public double[] NO3ppm
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; ++layer)
                        result[layer] = CalculateNO3()[layer] * convFactor[layer];
                }
                else
                    result = reset_no3ppm;
                return result;
            }
            set
            {
                if (!initDone)
                    reset_no3ppm = value;
                else
                    mySummary.WriteMessage(this, "An external module attempted to change the value of NO3 during simulation, the command will be ignored");
            }
        }
        #endregion  mineral N data
        #region Soil loss data

        //// NOTE: it is assumed any changes in soil profile are due to erosion
        //// this should be done via an event

        /// <summary>
        /// Define whether soil profile reduction is on.
        /// </summary>
        [Units("on/off")]
        public string n_reduction
        { set { profileReductionAllowed = value.StartsWith("on"); } }

        /// <summary>
        /// Soil loss due to erosion (t/ha).
        /// </summary>
        [Units("t/ha")]
        [XmlIgnore]
        public double soil_loss;

        #endregion

        #region Pond data

        /// <summary>
        /// Flag for whether pond is active or not (yes/no).
        /// </summary>
        /// <remarks>
        /// If there is a pond, the decomposition of surface OM will be done by that model
        /// </remarks>
        private bool isPondActive = false;
        [Units("yes/no")]
        private string pond_active
        { set { isPondActive = (value == "yes"); } }

        /// <summary>
        /// Amount of C decomposed in pond that is added to soil m. biomass.
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double pond_biom_C;

        /// <summary>
        /// Amount of C decomposed in pond that is added to soil humus.
        /// </summary>
        [Units("kg/ha")]
        [XmlIgnore]
        public double pond_hum_C;

        #endregion

        #region Inhibitors data

        /// <summary>
        /// Factor reducing nitrification due to the presence of a inhibitor.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        private double[] nitrification_inhibition
        {
            set
            {
                if (initDone)
                {
                    for (int layer = 0; layer < nLayers; layer++)
                    {
                        if (layer < value.Length)
                        {
                            inhibitionFactor_Nitrification[layer] = value[layer];
                            if (inhibitionFactor_Nitrification[layer] < -epsilon)
                            {
                                inhibitionFactor_Nitrification[layer] = 0.0;
                                mySummary.WriteWarning(this, "Value for nitrification inhibition is below lower limit, value will be adjusted to 0.0");
                            }
                            else if (inhibitionFactor_Nitrification[layer] > 1.0)
                            {
                                inhibitionFactor_Nitrification[layer] = 1.0;
                                mySummary.WriteWarning(this, "Value for nitrification inhibition is above upper limit, value will be adjusted to 1.0");
                            }
                        }
                        else
                            inhibitionFactor_Nitrification[layer] = 0.0;
                    }
                }
            }
        }

        #endregion

        #region Plant data

        /// <summary>
        /// Current depth of root zone (mm).
        /// </summary>
        private double rootDepth = 0.0;

        /// <summary>
        /// Depth of root zone (mm).
        /// </summary>
        /// <remarks>
        /// This is used to compute plant available N when using patches
        /// </remarks>
        [Units("mm")]
        [XmlIgnore]
        public double RootingDepth
        {
            get { return rootDepth; }
            set { rootDepth = value; }
        }

        #endregion

        #endregion params that may change

        #endregion params and inputs

        #region Outputs we make available to other components

        #region Outputs for Nitrogen

        #region Changes for today - deltas

        /// <summary>
        /// N carried out in sediment via runoff/erosion
        /// </summary>
        [Units("kg")]
        public double dlt_n_loss_in_sed
        {
            get
            {
                double result = 0.0;

                for (int k = 0; k < Patch.Count; k++)
                    result += Patch[k].dlt_n_loss_in_sed * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net nh4 change today
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_nh4_net
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].nh4[layer] - Patch[k].TodaysInitialNH4[layer]) * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>
        /// Net NH4 transformation today
        /// </summary>
        [Units("kg/ha")]
        public double[] nh4_transform_net
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_res_nh4_min[layer] +
                                         Patch[k].dlt_n_fom_to_min[layer] +
                                         Patch[k].dlt_n_biom_to_min[layer] +
                                         Patch[k].dlt_n_hum_to_min[layer] -
                                         Patch[k].dlt_nitrification[layer] +
                                         Patch[k].dlt_urea_hydrolysis[layer] +
                                         Patch[k].nh4_deficit_immob[layer]) *
                                         Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net no3 change today
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_no3_net
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].no3[layer] - Patch[k].TodaysInitialNO3[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net NO3 transformation today
        /// </summary>
        [Units("kg/ha")]
        public double[] no3_transform_net
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_res_no3_min[layer] -
                                         Patch[k].dlt_no3_dnit[layer] +
                                         Patch[k].dlt_nitrification[layer] -
                                         Patch[k].dlt_n2o_nitrif[layer] -
                                         Patch[k].nh4_deficit_immob[layer]) *
                                         Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net mineralisation today
        /// </summary>
        [Units("kg/ha")]
        public double[] MineralisedN
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_n_hum_to_min[layer] +
                                          Patch[k].dlt_n_biom_to_min[layer] +
                                          Patch[k].dlt_n_fom_to_min[layer]) *
                                          Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net N mineralisation from residue decomposition
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_n_min_res
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_res_no3_min[layer] + Patch[k].dlt_res_nh4_min[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net NH4 mineralisation from residue decomposition
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_res_nh4_min
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_res_nh4_min[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net NO3 mineralisation from residue decomposition
        /// </summary>
        /// <remarks>
        /// Net convertion of NO3 for residue mineralisation/immobilisation
        /// </remarks>
        [Units("kg/ha")]
        public double[] dlt_res_no3_min
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_res_no3_min[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net FOM N mineralised (negative for immobilisation)
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_n_min
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_fom_to_min[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net N mineralised for humic pool
        /// </summary>
        /// <remarks>
        /// Net humic N mineralised, negative for immobilisation
        /// </remarks>
        [Units("kg/ha")]
        public double[] dlt_hum_n_min
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_hum_to_min[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Net N mineralised from m. biomass pool
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_biom_n_min
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_biom_to_min[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Total net N mineralised (residues plus soil OM)
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_n_min_tot
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_n_hum_to_min[layer] +
                                          Patch[k].dlt_n_biom_to_min[layer] +
                                          Patch[k].dlt_n_fom_to_min[layer] +
                                          Patch[k].dlt_res_no3_min[layer] +
                                          Patch[k].dlt_res_nh4_min[layer]) *
                                          Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Nitrogen coverted by hydrolysis (from urea to NH4)
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_urea_hydrol
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_urea_hydrolysis[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Nitrogen coverted by nitrification (from NH4 to either NO3 or N2O)
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_rntrf
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nitrification[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Nitrogen coverted by nitrification (from NH4 to either NO3 or N2O)
        /// </summary>
        [Units("kg/ha")]
        public double[] Nitrification
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nitrification[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Effective, or net, nitrogen coverted by nitrification (from NH4 to NO3)
        /// </summary>
        [Units("kg/ha")]
        public double[] effective_nitrification
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_nitrification[layer] - Patch[k].dlt_n2o_nitrif[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// N2O N produced during nitrification
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_n2o_nitrif
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n2o_nitrif[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// N2O N produced during nitrification
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_nh4_dnit
        { get { return dlt_n2o_nitrif; } }

        /// <summary>
        /// N2O N produced during nitrification
        /// </summary>
        [Units("kg/ha")]
        public double[] n2o_atm_nitrification
        { get { return dlt_n2o_nitrif; } }

        /// <summary>
        /// NO3 N denitrified
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_no3_dnit
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_no3_dnit[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// N2O N produced during denitrification
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_n2o_dnit
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n2o_dnit[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// N2O N produced during denitrification
        /// </summary>
        [Units("kg/ha")]
        public double[] n2o_atm_denitrification
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n2o_dnit[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Total N2O amount produced today
        /// </summary>
        [Units("kg/ha")]
        public double[] n2o_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_n2o_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of N2 produced
        /// </summary>
        [Units("kg/ha")]
        public double[] n2_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_no3_dnit[layer] - Patch[k].dlt_n2o_dnit[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// N converted by denitrification
        /// </summary>
        [Units("kg/ha")]
        public double[] Denitrification
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_no3_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Excess N required above NH4 supply (for immobilisation)
        /// </summary>
        [Units("kg/ha")]
        public double[] nh4_deficit_immob
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].nh4_deficit_immob[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        #endregion deltas

        #region Amounts in solute forms

        /// <summary>
        /// Soil urea nitrogen amount (kgN/ha)
        /// </summary>
        public double[] CalculateUrea()
        {
            if (dlayer != null)
            {
                double[] result = new double[nLayers];
                for (int k = 0; k < Patch.Count; k++)
                    for (int layer = 0; layer < nLayers; ++layer)
                        result[layer] += Patch[k].urea[layer] * Patch[k].RelativeArea;

                return result;
            }

            return null;
        }
        /// <summary>Setter for urea</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        public void SetUrea(SoluteSetterType callingModelType, double[] value)
        {
            // get the delta N
            var currentUrea = CalculateUrea();
            double[] deltaN = new double[value.Length];
            for (int layer = 0; layer < Math.Min(nLayers, value.Length); layer++)
                deltaN[layer] = value[layer] - currentUrea[layer];

            SetUreaDelta(callingModelType, deltaN);
        }

        /// <summary>Setter for urea delta</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="deltaN">New values</param>
        public void SetUreaDelta(SoluteSetterType callingModelType, double[] deltaN)
        {
            // get the sender module (this is for report/testing only)
            if (callingModelType == SoluteSetterType.Soil)
                senderModule = "WaterModule";
            else if (callingModelType == SoluteSetterType.Plant)
                senderModule = "Plant";
            else if (callingModelType == SoluteSetterType.Fertiliser)
                senderModule = "Fertiliser";
            else
                senderModule = "Other";

            // get the delta N
            bool hasChanges = false;
            for (int layer = 0; layer < Math.Min(nLayers, deltaN.Length); layer++)
            {
                if (Math.Abs(deltaN[layer]) > epsilon)
                    hasChanges = true;
            }

            // check partitioning and pass the appropriate values to patches
            if (hasChanges)
            {
                if ((Patch.Count > 1) &&
                ((callingModelType == SoluteSetterType.Soil) ||
                (callingModelType == SoluteSetterType.Plant)))
                {
                    // the values come from a module that requires partition
                    double[][] newDelta = partitionDelta(deltaN, "Urea", patchNPartitionApproach.ToLower());

                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_urea = newDelta[k];
                }
                else
                {
                    // the values come from a module that do not require partition or there is only one patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_urea = deltaN;
                }
            }
        }
        /// <summary>
        /// Soil ammonium nitrogen amount (kgN/ha)
        /// </summary>
        public double[] CalculateNH4()
        {
            double[] result = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
                for (int k = 0; k < Patch.Count; k++)
                    result[layer] += (Patch[k].nh4[layer] + Patch[k].nh3[layer]) * Patch[k].RelativeArea;

            return result;
        }

        /// <summary>Setter for NH4</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        public void SetNH4(SoluteSetterType callingModelType, double[] value)
        {
            // get the delta N
            var nh4 = CalculateNH4();
            double[] deltaN = new double[value.Length];
            for (int layer = 0; layer < Math.Min(nLayers, value.Length); layer++)
                deltaN[layer] = value[layer] - nh4[layer];
            
            SetNH4Delta(callingModelType, deltaN);
        }

        /// <summary>Setter for NH4 delta.</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="deltaN">New values</param>
        public void SetNH4Delta(SoluteSetterType callingModelType, double[] deltaN)
        {
            // get the sender module (this is for report/testing only)
            if (callingModelType == SoluteSetterType.Soil)
                senderModule = "WaterModule";
            else if (callingModelType == SoluteSetterType.Plant)
                senderModule = "Plant";
            else if (callingModelType == SoluteSetterType.Fertiliser)
                senderModule = "Fertiliser";
            else
                senderModule = "Other";

            // get the delta N
            bool hasChanges = false;
            for (int layer = 0; layer < Math.Min(nLayers, deltaN.Length); layer++)
            {
                if (Math.Abs(deltaN[layer]) > epsilon)
                    hasChanges = true;
            }

            // check partitioning and pass the appropriate values to patches
            if (hasChanges)
            {
                if ((Patch.Count > 1) &&
                ((callingModelType == SoluteSetterType.Soil) ||
                (callingModelType == SoluteSetterType.Plant)))
                {
                    // the values come from a module that requires partition
                    double[][] newDelta = partitionDelta(deltaN, "NH4", patchNPartitionApproach.ToLower());

                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_nh4 = newDelta[k];
                }
                else
                {
                    // the values come from a module that do not require partition or there is only one patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_nh4 = deltaN;
                }
            }
        }

        /// <summary>
        /// Soil nitrate nitrogen amount (kgN/ha)
        /// </summary>
        public double[] CalculateNO3()
        {
            if (dlayer != null)
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].no3[layer] * Patch[k].RelativeArea;

                return result;
            }

            return null;
        }

        /// <summary>Setter for NO3</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        public void SetNO3(SoluteSetterType callingModelType, double[] value)
        {
            // get the delta N
            double[] no3 = CalculateNO3();
            double[] deltaN = new double[value.Length];
            for (int layer = 0; layer < Math.Min(nLayers, value.Length); layer++)
                deltaN[layer] = value[layer] - no3[layer];
            SetNO3Delta(callingModelType, deltaN);
         }

        /// <summary>Setter for delta NO3</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// The values passed, or in fact the deltas, need to be partitioned appropriately when there is more than one CNPatch
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="deltaN">New values</param>
        public void SetNO3Delta(SoluteSetterType callingModelType, double[] deltaN)
        {
            // get the sender module (this is for report/testing only)
            if (callingModelType == SoluteSetterType.Soil)
                senderModule = "WaterModule";
            else if (callingModelType == SoluteSetterType.Plant)
                senderModule = "Plant";
            else if (callingModelType == SoluteSetterType.Fertiliser)
                senderModule = "Fertiliser";
            else
                senderModule = "Other";

            // get the delta N
            bool hasChanges = false;
            for (int layer = 0; layer < Math.Min(nLayers, deltaN.Length); layer++)
            {
                if (Math.Abs(deltaN[layer]) > epsilon)
                {
                    hasChanges = true;
                    break;
                }
            }

            // check partitioning and pass the appropriate values to patches
            if (hasChanges)
            {
                if ((Patch.Count > 1) &&
                ((callingModelType == SoluteSetterType.Soil) ||
                (callingModelType == SoluteSetterType.Plant)))
                {
                    // the values come from a module that requires partition
                    double[][] newDelta = partitionDelta(deltaN, "NO3", patchNPartitionApproach.ToLower());

                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_no3 = newDelta[k];
                }
                else
                {
                    // the values come from a module that do not require partition or there is only one patch
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].dlt_no3 = deltaN;
                }
            }
        }

        /// <summary>
        /// Soil ammonium nitrogen amount available to plants, limited per patch (kgN/ha)
        /// </summary>
        public double[] CalculatePlantAvailableNH4()
        {
            double[] result = new double[nLayers];
            if (initDone)
            {
                for (int k = 0; k < Patch.Count; k++)
                {
                    Patch[k].CalcTotalMineralNInRootZone();
                    var nh4 = Patch[k].nh4AvailableToPlants;
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] += nh4[layer] * Patch[k].RelativeArea;
                }
            }

            return result;
        }

        /// <summary>Setter for Plant Available NH4</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        public void SetPlantAvailableNH4(SoluteSetterType callingModelType, double[] value)
        {
            // this variable should not actually be set but needed for SoluteManager to find it
            throw new ApsimXException(this, "should not be trying to set plant available nh4");
        }

        /// <summary>
        /// Soil nitrate nitrogen amount available to plants, limited per patch (kgN/ha)
        /// </summary>
        public double[] CalculatePlantAvailableNO3()
        {
            double[] result = new double[nLayers];
            if (initDone)
            {
                for (int k = 0; k < Patch.Count; k++)
                {
                    Patch[k].CalcTotalMineralNInRootZone();
                    var no3 = Patch[k].no3AvailableToPlants;
                    for (int layer = 0; layer < nLayers; layer++)
                        result[layer] += no3[layer] * Patch[k].RelativeArea;
                }
            }

            return result;
        }

        /// <summary>Setter for Plant Available NO3</summary>
        /// <remarks>
        /// This is necessary to allow the use of the SoilCNPatch capability
        /// </remarks>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="value">New values</param>
        public void SetPlantAvailableNO3(SoluteSetterType callingModelType, double[] value)
        {
            // this variable should not actually be set but needed for SoluteManager to find it
            throw new ApsimXException(this, "should not be trying to set plant available no3");
        }


        #endregion

        #region Amounts in various pools

        /// <summary>
        /// Total nitrogen in FOM
        /// </summary>
        [Units("kg/ha")]
        public double[] FOMN
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        for (int pool = 0; pool < Patch[k].fom_n.Length; pool++)
                            result[layer] += Patch[k].fom_n[pool][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Nitrogen in FOM pool 1
        /// </summary>
        [Units("kg/ha")]
        public double[] fom_n_pool1
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n[0][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Nitrogen in FOM pool 2
        /// </summary>
        [Units("kg/ha")]
        public double[] fom_n_pool2
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n[1][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Nitrogen in FOM pool 3
        /// </summary>
        [Units("kg/ha")]
        public double[] fom_n_pool3
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n[2][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Soil humic N
        /// </summary>
        [Units("kg/ha")]
        public double[] HumicN
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].hum_n[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Inactive soil humic N
        /// </summary>
        [Units("kg/ha")]
        public double[] IntertN
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].inert_n[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Soil biomass nitrogen
        /// </summary>
        [Units("kg/ha")]
        public double[] MicrobialN
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].biom_n[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Soil mineral nitrogen
        /// </summary>
        [Units("kg/ha")]
        public double[] mineral_n
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].urea[layer] + Patch[k].nh4[layer] + Patch[k].no3[layer] + Patch[k].nh3[layer] + Patch[k].no2[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Soil organic nitrogen, old style
        /// </summary>
        [Units("kg/ha")]
        public double[] organic_n
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].fom_n[0][layer]
                                       + Patch[k].fom_n[1][layer]
                                       + Patch[k].fom_n[2][layer]
                                       + Patch[k].hum_n[layer]
                                       + Patch[k].biom_n[layer]) * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Total N in soil
        /// </summary>
        [Units("kg/ha")]
        public double[] TotalN
        {
            get
            {
                double[] result = null;

                if (dlayer != null)
                {
                    result = new double[nLayers];
                    for (int layer = 0; layer < nLayers; ++layer)
                        for (int k = 0; k < Patch.Count; k++)
                            result[layer] = Patch[k].nit_tot[layer] * Patch[k].RelativeArea;
                }

                return result;
            }
        }

        #endregion

        #region Nitrogen balance

        /// <summary>
        /// SoilN balance for nitrogen: deltaN - losses
        /// </summary>
        [Units("kg/ha")]
        public double nitrogenbalance
        {
            get
            {
                double Nlosses = 0.0;

                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        Nlosses += (Patch[k].dlt_n2o_nitrif[layer] + Patch[k].dlt_no3_dnit[layer]) * Patch[k].RelativeArea;  // exchange with 'outside' world computed by soilN

                double deltaN = SumDoubleArray(TotalN) - TodaysInitialN;  // Variation in N today  -  Not sure how/when inputs and leaching are taken in account
                return -(Nlosses + deltaN);
            }
        }

        #endregion

        #endregion

        #region Outputs for Carbon

        #region General values

        /// <summary>
        /// Carbohydrate fraction of FOM (0-1)
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double fr_carb
        { get { return fract_carb[fom_type]; } }

        /// <summary>
        /// Cellulose fraction of FOM (0-1)
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double fr_cell
        { get { return fract_cell[fom_type]; } }

        /// <summary>
        /// Lignin fraction of FOM (0-1)
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double fr_lign
        { get { return fract_lign[fom_type]; } }

        #endregion

        #region Changes for today - deltas

        /// <summary>
        /// Carbon loss in sediment, via runoff/erosion
        /// </summary>
        [Units("kg")]
        public double dlt_c_loss_in_sed
        {
            get
            {
                double result = 0.0;

                for (int k = 0; k < Patch.Count; k++)
                    result += Patch[k].dlt_c_loss_in_sed * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C converted from FOM to humic (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_c_hum
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        for (int pool = 0; pool < 3; pool++)
                            result[layer] += Patch[k].dlt_c_fom_to_hum[pool][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C converted from FOM to m. biomass (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_c_biom
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        for (int pool = 0; pool < 3; pool++)
                            result[layer] += Patch[k].dlt_c_fom_to_biom[pool][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C lost to atmosphere from FOM
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_c_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        for (int pool = 0; pool < 3; pool++)
                            result[layer] += Patch[k].dlt_c_fom_to_atm[pool][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Humic C converted to biomass
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_hum_c_biom
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_hum_to_biom[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Humic C lost to atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_hum_c_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_hum_to_atm[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Biomass C converted to humic
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_biom_c_hum
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_biom_to_hum[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Biomass C lost to atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_biom_c_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_biom_to_atm[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Carbon from residues converted to biomass C
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_res_c_biom
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_res_to_biom[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Carbon from residues converted to humic C
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_res_c_hum
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_res_to_hum[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Carbon from residues lost to atmosphere during decomposition
        /// </summary>
        [Units("kg/ha")]
        public double dlt_res_c_atm
        {
            get
            {
                double result = 0.0;

                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result += Patch[k].dlt_c_res_to_atm[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Delta C in pool 1 of FOM - needed by SoilP
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_c_pool1
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_c_fom_to_hum[0][layer] +
                                          Patch[k].dlt_c_fom_to_biom[0][layer] +
                                          Patch[k].dlt_c_fom_to_atm[0][layer]) *
                                          Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Delta C in pool 2 of FOM - needed by SoilP
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_c_pool2
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_c_fom_to_hum[1][layer] +
                                          Patch[k].dlt_c_fom_to_biom[1][layer] +
                                          Patch[k].dlt_c_fom_to_atm[1][layer]) *
                                          Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Delta C in pool 3 of FOM - needed by SoilP
        /// </summary>
        [Units("kg/ha")]
        public double[] dlt_fom_c_pool3
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].dlt_c_fom_to_hum[2][layer] +
                                          Patch[k].dlt_c_fom_to_biom[2][layer] +
                                          Patch[k].dlt_c_fom_to_atm[2][layer]) *
                                          Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Carbon from all residues to m. biomass
        /// </summary>
        [Units("kg/ha")]
        public double[] soilp_dlt_res_c_biom
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_res_to_biom[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Carbon from all residues to humic pool
        /// </summary>
        [Units("kg/ha")]
        public double[] soilp_dlt_res_c_hum
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_res_to_hum[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Carbon lost from all residues to atmosphere
        /// </summary>
        [Units("kg/ha")]
        public double[] soilp_dlt_res_c_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_res_to_atm[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Total CO2 amount produced today in the soil
        /// </summary>
        [Units("kg/ha")]
        public double[] co2_atm
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].co2_atm[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        #endregion

        #region Amounts in various pools

        /// <summary>
        /// Fresh organic C - FOM
        /// </summary>
        [Units("kg/ha")]
        public double[] FOMC
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        for (int pool = 0; pool < 3; pool++)
                            result[layer] += Patch[k].fom_c[pool][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C in pool 1 of FOM
        /// </summary>
        [Units("kg/ha")]
        public double[] fom_c_pool1
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c[0][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C in pool 2 of FOM
        /// </summary>
        [Units("kg/ha")]
        public double[] fom_c_pool2
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c[1][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C in pool 3 of FOM
        /// </summary>
        [Units("kg/ha")]
        public double[] fom_c_pool3
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c[2][layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C in humic pool
        /// </summary>
        [Units("kg/ha")]
        public double[] HumicC
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].hum_c[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C in inert humic pool
        /// </summary>
        [Units("kg/ha")]
        public double[] InertC
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].inert_c[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of C in m. biomass pool
        /// </summary>
        [Units("kg/ha")]
        public double[] MicrobialC
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].biom_c[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Amount of water soluble C
        /// </summary>
        [Units("kg/ha")]
        public double[] waterSoluble_c
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].waterSoluble_c[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Total carbon amount in the soil
        /// </summary>
        [Units("kg/ha")]
        public double[] TotalC
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].carbon_tot[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        #endregion

        #region Carbon Balance

        /// <summary>
        /// Balance of C in soil: deltaC - losses
        /// </summary>
        [Units("kg/ha")]
        public double carbonbalance
        {
            get
            {
                double Closses = 0.0;

                for (int layer = 0; layer < nLayers; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        Closses += (Patch[k].dlt_c_res_to_atm[layer] +
                                    Patch[k].dlt_c_fom_to_atm[0][layer] +
                                    Patch[k].dlt_c_fom_to_atm[1][layer] +
                                    Patch[k].dlt_c_fom_to_atm[2][layer] +
                                    Patch[k].dlt_c_hum_to_atm[layer] +
                                    Patch[k].dlt_c_biom_to_atm[layer]) *
                                    Patch[k].RelativeArea;

                double deltaC = SumDoubleArray(TotalC) - TodaysInitialC;
                return -(Closses + deltaC);
            }
        }

        #endregion

        #endregion

        #region Factors and other outputs

        /// <summary>
        /// amount of P coverted by residue mineralisation (needed by SoilP)
        /// </summary>
        [Units("kg/ha")]
        public double[] soilp_dlt_org_p
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].soilp_dlt_org_p[layer] * Patch[k].RelativeArea;

                return result;
            }
        }

        #endregion

        #region Outputs related to internal patches    * * * * * * * * * * * * * *

        #region General variables

        /// <summary>
        /// Number of internal patches
        /// </summary>
        public int PatchCount
        { get { return Patch.Count; } }

        /// <summary>
        /// Relative area of each internal patch
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double[] PatchArea
        {
            get
            {
                double[] result = new double[Patch.Count];

                for (int k = 0; k < Patch.Count; k++)
                    result[k] = Patch[k].RelativeArea;

                return result;
            }
        }

        /// <summary>
        /// Name of each internal patch
        /// </summary>
        public string[] PatchName
        {
            get
            {
                string[] result = new string[Patch.Count];

                for (int k = 0; k < Patch.Count; k++)
                    result[k] = Patch[k].PatchName;

                return result;
            }
        }

        /// <summary>
        /// Age of each existing internal patch
        /// </summary>
        [Units("days")]
        public double[] PatchAge
        {
            get
            {
                double[] result = new double[Patch.Count];

                for (int k = 0; k < Patch.Count; k++)
                    result[k] = (Clock.Today - Patch[k].CreationDate).TotalDays + 1;

                return result;
            }
        }

        #endregion

        #region Outputs for Nitrogen

        #region Changes for today - deltas

        #region Totals (whole profile)

        /// <summary>
        /// N carried out for each patch in sediment via runoff/erosion
        /// </summary>
        [Units("kg")]
        public double[] PatchNLostInSediment
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    result[k] = Patch[k].dlt_n_loss_in_sed;

                return result;
            }
        }

        // - These are summed up over the whole profile  ----------------------------------------------

        /// <summary>
        /// Total net N mineralisation from residue decomposition, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNMineralisedFromResidues
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_res_no3_min[layer] + Patch[k].dlt_res_nh4_min[layer];

                return result;
            }
        }

        /// <summary>
        /// Total net FOM N mineralised, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNMineralisedFromFOM
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n_fom_to_min[layer];

                return result;
            }
        }

        /// <summary>
        /// Total net humic N mineralised, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNMineralisedFromHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n_hum_to_min[layer];

                return result;
            }
        }

        /// <summary>
        /// Total net biomass N mineralised, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNMineralisedFromMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n_biom_to_min[layer];

                return result;
            }
        }

        /// <summary>
        /// Total net N mineralised, for each patch (residues plus soil OM)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNMineralised
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n_biom_to_min[layer];

                return result;
            }
        }

        /// <summary>
        /// Total nitrogen coverted by hydrolysis, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalUreaHydrolysis
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_urea_hydrolysis[layer];

                return result;
            }
        }

        /// <summary>
        /// Total nitrogen coverted by nitrification, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_nitrification[layer];

                return result;
            }
        }

        /// <summary>
        /// Total effective amount of NH4-N coverted into NO3 by nitrification, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalEffectiveNitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_nitrification[layer] - Patch[k].dlt_n2o_nitrif[layer];

                return result;
            }
        }

        /// <summary>
        /// Total N2O N produced during nitrification, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalN2O_Nitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n2o_nitrif[layer];

                return result;
            }
        }

        /// <summary>
        /// Total NO3 N denitrified, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3_Denitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_no3_dnit[layer];

                return result;
            }
        }

        /// <summary>
        /// Total N2O N produced during denitrification, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalN2O_Denitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n2o_dnit[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of N2O N produced, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalN2OLostToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_n2o_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of N2 produced, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalN2LostToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_no3_dnit[layer] - Patch[k].dlt_n2o_dnit[layer];

                return result;
            }
        }

        /// <summary>
        /// Total N converted by denitrification (all forms), for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDenitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_no3_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of urea changed by the soil water module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalUreaLeached
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    result[k] += Patch[k].urea_flow[nLayers - 1];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NH4 changed by the soil water module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNH4Leached
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    result[k] += Patch[k].nh4_flow[nLayers - 1];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NO3 changed by the soil water module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3Leached
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    result[k] += Patch[k].no3_flow[nLayers - 1];

                return result;
            }
        }

        /// <summary>
        /// Total amount of urea taken up by any plant module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalUreaUptake
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].urea_uptake[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NH4 taken up by any plant module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNH4Uptake
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].nh4_uptake[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NO3 taken up by any plant module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3Uptake
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].no3_uptake[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of urea added by the fertiliser module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalUreaFertiliser
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].urea_fertiliser[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NH4 added by the fertiliser module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNH4Fertiliser
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].nh4_fertiliser[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NO3 added by the fertiliser module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3Fertiliser
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].no3_fertiliser[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of urea changed by the any other module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalUreaChangedOther
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].urea_ChangedOther[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NH4 changed by the any other module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNH4ChangedOther
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].nh4_ChangedOther[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of NO3 changed by the any other module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3ChangedOther
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].no3_ChangedOther[layer];

                return result;
            }
        }

        // --------------------------------------------------------------------------------------------
        #endregion

        #region Values for each soil layer

        /// <summary>
        /// Net N mineralisation from residue decomposition for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNMineralisedFromResidues
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_res_no3_min[layer] + Patch[k].dlt_res_nh4_min[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Net FOM N mineralised for each patch (negative for immobilisation)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNMineralisedFromFOM
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n_fom_to_min[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Net N mineralised for humic pool for each patch (negative for immobilisation)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNMineralisedFromHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n_hum_to_min[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Net N mineralised from m. biomass pool for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNMineralisedFromMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n_biom_to_min[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Net total N mineralised for each patch (residues plus soil OM)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNMineralisedTotal
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n_hum_to_min[layer] +
                                                       Patch[k].dlt_n_biom_to_min[layer] +
                                                       Patch[k].dlt_n_fom_to_min[layer] +
                                                       Patch[k].dlt_res_no3_min[layer] +
                                                       Patch[k].dlt_res_nh4_min[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Nitrogen coverted by hydrolysis (from urea to NH4) for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltUreaHydrolysis
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_urea_hydrolysis[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Nitrogen coverted by nitrification (from NH4 to either NO3 or N2O) for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltNitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_nitrification[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Effective, or net, nitrogen coverted by nitrification for each patch (from NH4 to NO3)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchEffectiveDltNitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_nitrification[layer] - Patch[k].dlt_n2o_nitrif[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// N2O N produced during nitrification for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltN2O_Nitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n2o_nitrif[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// NO3 N denitrified for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltNO3_Denitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_no3_dnit[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// N2O N produced during denitrification for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltN2O_Denitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n2o_dnit[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total N2O amount produced for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchN2OLostToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_n2o_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of N2 produced for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchN2LostToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_no3_dnit[layer] - Patch[k].dlt_n2o_dnit[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// N converted by all forms of denitrification for each patch (to be deleted?)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_dnit
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_no3_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// N converted by all forms of denitrification for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltDenitrification
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_no3_dnit[layer] + Patch[k].dlt_n2o_nitrif[layer];
                }

                return result;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------
        // Variables that are owned by other modules but which are related to SoilNitrogen.
        // here the estimated values partitioned amongst internal patches are published

        /// <summary>
        /// Amount of urea changed by the soil water module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_UreaFlow
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].urea_flow[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NH4 changed by the soil water module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NH4Flow
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].nh4_flow[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NO3 changed by the soil water module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NO3Flow
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].no3_flow[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of urea taken up by any plant module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_UreaUptake
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].urea_uptake[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NH4 taken up by any plant module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NH4Uptake
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].nh4_uptake[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NO3 taken up by any plant module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NO3Uptake
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].no3_uptake[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of urea added by the fertiliser module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_UreaFertiliser
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].urea_fertiliser[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NH4 added by the fertiliser module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NH4Fertiliser
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].nh4_fertiliser[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NO3 added by the fertiliser module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NO3Fertiliser
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].no3_fertiliser[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of urea changed by the any other module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_UreaChangedOther
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].urea_ChangedOther[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NH4 changed by the any other module, for each patch
        /// </summary>
        [Units("kg/ha")]
        private CNPatchVariableType Patch_NH4ChangedOther
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].nh4_ChangedOther[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NO3 changed by the any other module, for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType Patch_NO3ChangedOther
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].no3_ChangedOther[layer];
                }

                return result;
            }
        }

        // ----------------------------------------------------------------------------------------------------------------

        #endregion

        #endregion deltas

        #region Amounts in solute forms

        /// <summary>
        /// Amount of urea N in each internal patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchUrea
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].urea[layer];
                }

                if (Patch.Count > 1)
                    nLayers += 0;

                return result;
            }
        }

        /// <summary>
        /// Amount of NH4 N in each internal patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNH4
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].nh4[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of NO3 N in each internal patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchNO3
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].no3[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of N as NH4 available to plants, in each internal patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchPlantAvailableNH4
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    if (initDone)
                    {
                        Patch[k].CalcTotalMineralNInRootZone();
                        for (int layer = 0; layer < nLayers; layer++)
                            result.Patch[k].Value[layer] = Patch[k].nh4AvailableToPlants[layer];
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of N as NO3 available to plants, in each internal patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchPlantAvailableNO3
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    if (initDone)
                    {
                        Patch[k].CalcTotalMineralNInRootZone();
                        for (int layer = 0; layer < nLayers; layer++)
                            result.Patch[k].Value[layer] = Patch[k].no3AvailableToPlants[layer];
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Total urea N in each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalUrea
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].urea[layer];

                return result;
            }
        }

        /// <summary>
        /// Total NH4 N in each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNH4
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].nh4[layer];

                return result;
            }
        }

        /// <summary>
        /// Total NO3 N in each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].no3[layer];

                return result;
            }
        }

        /// <summary>
        /// Total NH4 N available to plants in each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNH4_PlantAvailable
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                if (initDone)
                {
                    for (int k = 0; k < nPatches; k++)
                    {
                        Patch[k].CalcTotalMineralNInRootZone();
                        for (int layer = 0; layer < nLayers; layer++)
                            result[k] += Patch[k].nh4AvailableToPlants[layer];
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Total NO3 N available to plants in each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalNO3_PlantAvailable
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                if (initDone)
                {
                    for (int k = 0; k < nPatches; k++)
                    {
                        Patch[k].CalcTotalMineralNInRootZone();
                        for (int layer = 0; layer < nLayers; layer++)
                            result[k] += Patch[k].no3AvailableToPlants[layer];
                    }
                }

                return result;
            }
        }

        #endregion

        #region Amounts in various pools

        /// <summary>
        /// Total nitrogen in FOM for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchPoolVariableType PatchFOM_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                int nPools = 3;
                CNPatchPoolVariableType result = new CNPatchPoolVariableType();
                result.Patch = new CNPatchPoolVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchPoolVariablePatchType();
                    result.Patch[k].Pool = new CNPatchPoolVariablePatchPoolType[nPools];
                    for (int pool = 0; pool < nPools; pool++)
                    {
                        result.Patch[k].Pool[pool] = new CNPatchPoolVariablePatchPoolType();
                        result.Patch[k].Pool[pool].Value = new double[nLayers];
                        for (int layer = 0; layer < nLayers; layer++)
                            result.Patch[k].Pool[pool].Value[layer] = Patch[k].fom_n[pool][layer];
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Soil humic N for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchHum_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].hum_n[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Inactive soil humic N for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchInert_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].inert_n[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Soil biomass nitrogen for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchBiom_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].biom_n[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total mineral N in soil for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchSoilMineralN
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].urea[layer] + Patch[k].nh4[layer] + Patch[k].no3[layer] + Patch[k].nh3[layer] + Patch[k].no2[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total organic N in soil for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchSoilOrganicN
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].fom_n[0][layer]
                                                     + Patch[k].fom_n[1][layer]
                                                     + Patch[k].fom_n[2][layer]
                                                     + Patch[k].hum_n[layer]
                                                     + Patch[k].biom_n[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total N in soil for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchTotalN
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].nit_tot[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total FOM N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalFOM_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].fom_n[0][layer] + Patch[k].fom_n[1][layer] + Patch[k].fom_n[2][layer];

                return result;
            }
        }

        /// <summary>
        /// Total Humic N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalHum_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].hum_n[layer];

                return result;
            }
        }

        /// <summary>
        /// Total inert N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalInert_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].inert_n[layer];

                return result;
            }
        }

        /// <summary>
        /// Total biomass N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalBiom_N
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].biom_n[layer];

                return result;
            }
        }

        /// <summary>
        /// Total mineral N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalSoilMineralN
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].urea[layer] + Patch[k].nh4[layer] + Patch[k].no3[layer] + Patch[k].nh3[layer] + Patch[k].no2[layer];

                return result;
            }
        }

        /// <summary>
        /// Total organic N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalSoilOrganicN
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].fom_n[0][layer] + Patch[k].fom_n[1][layer] + Patch[k].fom_n[2][layer] +
                                     Patch[k].hum_n[layer] + Patch[k].biom_n[layer];

                return result;
            }
        }

        /// <summary>
        /// Total N in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalSoilN
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].nit_tot[layer];

                return result;
            }
        }

        #endregion

        #region Nitrogen balance

        /// <summary>
        /// SoilN balance for nitrogen, for each patch: deltaN - losses
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchNitrogenBalance
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nLayers];
                for (int k = 0; k < Patch.Count; k++)
                {
                    double Nlosses = 0.0;

                    for (int layer = 0; layer < nLayers; layer++)
                        Nlosses += (Patch[k].dlt_n2o_nitrif[layer] + Patch[k].dlt_no3_dnit[layer]);

                    double deltaN = SumDoubleArray(TotalN) - TodaysInitialN;
                    result[k] = -(Nlosses + deltaN);
                }

                return result;
            }
        }

        #endregion

        #endregion

        #region Outputs for Carbon

        #region Changes for today - deltas

        /// <summary>
        /// Carbon loss in sediment for each patch, via runoff/erosion
        /// </summary>
        [Units("kg")]
        public double[] PatchCLostInSediment
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    result[k] = Patch[k].dlt_c_loss_in_sed;

                return result;
            }
        }

        /// <summary>
        /// Amount of C converted from FOM to humic for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromFOMToHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        for (int pool = 0; pool < 3; pool++)
                            result.Patch[k].Value[layer] = Patch[k].dlt_c_fom_to_hum[pool][layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of C converted from FOM to m. biomass for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromFOMToMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        for (int pool = 0; pool < 3; pool++)
                            result.Patch[k].Value[layer] = Patch[k].dlt_c_fom_to_biom[pool][layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of C lost to atmosphere from FOM for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromFOMToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        for (int pool = 0; pool < 3; pool++)
                            result.Patch[k].Value[layer] = Patch[k].dlt_c_fom_to_atm[pool][layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Humic C converted to biomass for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromHumusToMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_hum_to_biom[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Humic C lost to atmosphere for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromHumusToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_hum_to_atm[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Biomass C converted to humic for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromMBiomassToHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_biom_to_hum[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Biomass C lost to atmosphere for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromMBiomassToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_biom_to_atm[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Carbon from residues converted to biomass C for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromResiduesToMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_res_to_biom[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Carbon from residues converted to humic C for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromResiduesToHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_res_to_hum[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Carbon from residues lost to atmosphere for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchDltCFromResiduesToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].dlt_c_res_to_atm[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total CO2 amount produced in the soil for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchCO2Produced
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].co2_atm[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total amount of C converted from FOM to humic for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromFOMToHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        for (int pool = 0; pool < 3; pool++)
                            result[k] += Patch[k].dlt_c_fom_to_hum[pool][layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of C converted from FOM to m. biomass for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromFOMToMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        for (int pool = 0; pool < 3; pool++)
                            result[k] += Patch[k].dlt_c_fom_to_biom[pool][layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of FOM C lost to atmosphere for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromFOMToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        for (int pool = 0; pool < 3; pool++)
                            result[k] += Patch[k].dlt_c_fom_to_atm[pool][layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of humic C converted to m. biomass for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromHumusToMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_hum_to_biom[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of humic C lost to atmosphere for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromHumusToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_hum_to_atm[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of biomass C converted to humus for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromMBiomassToHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_biom_to_hum[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of biomass C lost to atmosphere for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromMBiomassToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_biom_to_atm[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of C from residues converted to m. biomass for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromResiduesToMBiomass
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_res_to_biom[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of C from residues converted to humus for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromResiduesToHumus
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_res_to_hum[layer];

                return result;
            }
        }

        /// <summary>
        /// Total amount of C from residues lost to atmosphere for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalDltCFromResiduesToAtmosphere
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].dlt_c_res_to_atm[layer];

                return result;
            }
        }

        /// <summary>
        /// Total CO2 amount produced in the soil for each patch (kg/ha)
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalCO2Produced
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].co2_atm[layer];

                return result;
            }
        }

        #endregion

        #region Amounts in various pools

        /// <summary>
        /// Fresh organic C - FOM for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchPoolVariableType PatchFOM_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                int nPools = 3;
                CNPatchPoolVariableType result = new CNPatchPoolVariableType();
                result.Patch = new CNPatchPoolVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchPoolVariablePatchType();
                    result.Patch[k].Pool = new CNPatchPoolVariablePatchPoolType[nPools];
                    for (int pool = 0; pool < nPools; pool++)
                    {
                        result.Patch[k].Pool[pool] = new CNPatchPoolVariablePatchPoolType();
                        result.Patch[k].Pool[pool].Value = new double[nLayers];
                        for (int layer = 0; layer < nLayers; layer++)
                            result.Patch[k].Pool[pool].Value[layer] = Patch[k].fom_c[pool][layer];
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of C in humic pool for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchHum_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].hum_c[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of C in inert humic pool for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchInert_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].inert_c[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of C in m. biomass pool for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchBiom_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].biom_c[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Amount of water soluble C for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchWaterSolubleC
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].waterSoluble_c[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total carbon amount in the soil for each patch
        /// </summary>
        [Units("kg/ha")]
        public CNPatchVariableType PatchTotalC
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                CNPatchVariableType result = new CNPatchVariableType();
                result.Patch = new CNPatchVariablePatchType[nPatches];
                for (int k = 0; k < nPatches; k++)
                {
                    result.Patch[k] = new CNPatchVariablePatchType();
                    result.Patch[k].Value = new double[nLayers];
                    for (int layer = 0; layer < nLayers; layer++)
                        result.Patch[k].Value[layer] = Patch[k].carbon_tot[layer];
                }

                return result;
            }
        }

        /// <summary>
        /// Total FOM C in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalFOM_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].fom_c[0][layer] + Patch[k].fom_c[1][layer] + Patch[k].fom_c[2][layer];

                return result;
            }
        }

        /// <summary>
        /// Total Humic C in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalHum_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].hum_c[layer];

                return result;
            }
        }

        /// <summary>
        /// Total inert C in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalInert_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].inert_c[layer];

                return result;
            }
        }

        /// <summary>
        /// Total biomass C in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalBiom_C
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].biom_c[layer];

                return result;
            }
        }

        /// <summary>
        /// Total water soluble C in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalWaterSolubleC
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].waterSoluble_c[layer];

                return result;
            }
        }

        /// <summary>
        /// Total C in the whole profile for each patch
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchTotalSoilC
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nPatches];

                for (int k = 0; k < nPatches; k++)
                    for (int layer = 0; layer < nLayers; layer++)
                        result[k] += Patch[k].carbon_tot[layer];

                return result;
            }
        }

        #endregion

        #region Carbon Balance

        /// <summary>
        /// Balance of C in soil,  for each patch: deltaC - losses
        /// </summary>
        [Units("kg/ha")]
        public double[] PatchCarbonBalance
        {
            get
            {
                int nPatches = (Patch != null) ? Patch.Count : 1;
                double[] result = new double[nLayers];
                for (int k = 0; k < nPatches; k++)
                {
                    double CLosses = 0.0;

                    for (int layer = 0; layer < nLayers; layer++)
                        CLosses += (Patch[k].dlt_c_res_to_atm[layer] +
                                            Patch[k].dlt_c_fom_to_atm[0][layer] +
                                            Patch[k].dlt_c_fom_to_atm[1][layer] +
                                            Patch[k].dlt_c_fom_to_atm[2][layer] +
                                            Patch[k].dlt_c_hum_to_atm[layer] +
                                            Patch[k].dlt_c_biom_to_atm[layer]);

                    double deltaC = SumDoubleArray(Patch[k].carbon_tot) - Patch[k].TodaysInitialC;
                    result[k] = -(CLosses + deltaC);
                }

                return result;
            }
        }

        #endregion

        #endregion

        #endregion outputs from patches      * * * * * * * * * * * * * * * * * *

        #endregion outputs

        #region Useful constants

        /// <summary>
        /// Value to evaluate precision against floating point variables.
        /// </summary>
        private readonly double epsilon = 0.000000000001;
        ////private double epsilon = Math.Pow(2, -24);

        #endregion constants

        #region Internal variables

        #region Components

        /// <summary>
        /// List of all existing patches (internal instances of C and N processes).
        /// </summary>
        [XmlIgnore]
        public List<soilCNPatch> Patch;

        #endregion components

        #region Soil physics data

        /// <summary>
        /// Soil layers' thichness (mm).
        /// </summary>
        [Units("mm")]
        private double[] dlayer;

        /// <summary>
        /// Number layers in the soil.
        /// </summary>
        private int nLayers;

        #endregion

        #region Initial C and N amounts

        /// <summary>
        /// The initial OC content for each layer of the soil (%). Also used onReset.
        /// </summary>
        private double[] reset_oc = { 3, 1 };

        /// <summary>
        /// Initial content of urea in each soil layer (ppm). Also used onReset.
        /// </summary>
        private double[] reset_ureappm;

        /// <summary>
        /// Initial content of NH4 in each soil layer (ppm). Also used onReset.
        /// </summary>
        private double[] reset_nh4ppm = { 1, 0.5 };

        /// <summary>
        /// Initial content of NO3 in each soil layer (ppm). Also used onReset.
        /// </summary>
        private double[] reset_no3ppm = { 5, 2 };

        #endregion

        #region Mineral N amounts

        #endregion

        #region Deltas in mineral nitrogen

        /// <summary>
        /// Variations in urea as given by another component.
        /// </summary>
        /// <remarks>
        /// This property checks changes in the amount of urea at each soil layer
        ///  - If values are not supplied for all layers, these will be assumed zero
        ///  - If values are supplied in excess, these will ignored
        ///  - Each value is tested whether it is within bounds, then it is added to the actual amount, this amount is then tested for its bounds
        /// </remarks>
        private double[] dlt_urea
        {
            set
            {
                // pass the value to patches
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_urea = value;
            }
        }

        /// <summary>
        /// Variations in nh4 as given by another component.
        /// </summary>
        private double[] dlt_nh4
        {
            set
            {
                // pass the value to patches
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_nh4 = value;
            }
        }

        /// <summary>
        /// Variations in no3 as given by another component.
        /// </summary>
        private double[] dlt_no3
        {
            set
            {
                // pass the value to patches
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_no3 = value;
            }
        }

        #endregion

        #region Decision auxiliary variables

        /// <summary>
        /// Marker for whether initialisation has been finished or not.
        /// </summary>
        private bool initDone = false;

        /// <summary>
        /// Indicates whether soil profile reduction is allowed (from erosion).
        /// </summary>
        private bool profileReductionAllowed = false;

        /// <summary>
        /// Indicates whether organic solutes are to be simulated.
        /// </summary>
        /// <remarks>
        /// It should always be false, as organic solutes are not implemented yet
        /// </remarks>
        private bool organicSolutesAllowed = false;

        #endregion

        #region Residue decomposition information

        /// <summary>
        /// Name of residues decomposing.
        /// </summary>
        private string[] residueName;

        /// <summary>
        /// Type of decomposing residue.
        /// </summary>
        private string[] residueType;

        /// <summary>
        /// Potential residue C decomposition (kg/ha).
        /// </summary>
        private double[] pot_c_decomp;

        /// <summary>
        /// Potential residue N decomposition (kg/ha).
        /// </summary>
        private double[] pot_n_decomp;

        /// <summary>
        /// Potential residue P decomposition (kg/ha).
        /// </summary>
        private double[] pot_p_decomp;

        #endregion

        #region Miscelaneous

        /// <summary>
        /// Total C content at the beginning of the day.
        /// </summary>
        [XmlIgnore]
        public double TodaysInitialC;

        /// <summary>
        /// Total N content at the beginning of the day.
        /// </summary>
        [XmlIgnore]
        public double TodaysInitialN;

        /// <summary>
        /// The initial FOM distribution, a 0-1 fraction for each layer.
        /// </summary>
        private double[] FOMiniFraction;

        /// <summary>
        /// Type of FOM being used
        /// </summary>
        private int fom_type;

        /// <summary>
        /// Number of surface residues whose decomposition is being calculated.
        /// </summary>
        private int nResidues = 0;

        /// <summary>
        /// The C:N ratio of each fom pool.
        /// </summary>
        private double[] fomPoolsCNratio;

        /// <summary>
        /// Factor for converting kg/ha into ppm.
        /// </summary>
        private double[] convFactor;

        /// <summary>
        /// Factor reducing nitrification due to the presence of a inhibitor.
        /// </summary>
        private double[] inhibitionFactor_Nitrification = null;

        #endregion

        #region CNpatch related variables

        /// <summary>
        /// Minimum allowable relative area for a CNpatch (0-1).
        /// </summary>
        private double minimumPatchArea = 0.000001;

        /// <summary>
        /// Approach to use when partitioning dltN amongst patches.
        /// </summary>
        private string patchNPartitionApproach = "BasedOnConcentrationAndDelta";

        /// <summary>
        /// Whether auto amalgamation of patches is allowed.
        /// </summary>
        private bool patchAutoAmalgamationAllowed = false;

        /// <summary>
        /// Approach to use for AutoAmalagamation (CompareAll, CompareBase, CompareMerge).
        /// </summary>
        private string patchAmalgamationApproach = "CompareAll";

        /// <summary>
        /// indicates whether an age check is used to force amalgamation of patches.
        /// </summary>
        private bool patchAmalgamationByAgeAllowed = false;

        /// <summary>
        /// Age after which patches will be merged if AllowPatchAmalgamationByAge.
        /// </summary>
        private double forcedMergePatchAge = 3;

        /// <summary>
        /// Approach to use for defining the base patch (IDBased, AreaBased).
        /// </summary>
        private string patchbasePatchApproach = "AreaBased";

        /// <summary>
        /// Layer down to which test for diffs are made (upon auto amalgamation).
        /// </summary>
        private int layerDepthToTestDiffs;

        /// <summary>
        /// A description of the module sending a change in soil nitrogen (used for partitioning).
        /// </summary>
        private string senderModule;

        /// <summary>
        /// Maximum NH4 uptake rate for plants (ppm/day) (only used when dealing with patches).
        /// </summary>
        private double reset_MaximumNH4Uptake = 9999.9;

        /// <summary>
        /// Maximum NO3 uptake rate for plants (ppm/day) (only used when dealing with patches).
        /// </summary>
        private double reset_MaximumNO3Uptake = 9999.9;

        /// <summary>
        /// Maximum NH4 uptake rate for plants (only used when dealing with patches).
        /// </summary>
        private double[] maximumNH4UptakeRate;

        /// <summary>
        /// Maximum NO3 uptake rate for plants (only used when dealing with patches).
        /// </summary>
        private double[] maximumNO3UptakeRate;

        /// <summary>
        /// The maximum amount of N that is made available to plants in one day.
        /// </summary>
        private double maxTotalNAvailableToPlants = 9999.9;

        #endregion

        #endregion internal variables

        #region Types and structures

        /// <summary>
        /// The parameters to compute a exponential type function (used for example for temperature factor)
        /// </summary>
        [Serializable]
        private struct BentStickData
        {
            // this is a bending stick type data
            /// <summary>
            /// Optimum temperature, when factor is equal to one
            /// </summary>
            public double[] xValueForOptimum;
            /// <summary>
            /// Value of factor when temperature is equal to zero celsius
            /// </summary>
            public double[] yValueAtZero;
            /// <summary>
            /// Exponent defining the curvature of the factor
            /// </summary>
            public double[] CurveExponent;
        }

        /// <summary>
        /// Lists of x and y values used to describe certain a 'broken stick' function (e.g. moisture factor)
        /// </summary>
        [Serializable]
        private struct BrokenStickData
        {
            /// <summary>
            /// The values in the x-axis
            /// </summary>
            public double[] xVals;
            /// <summary>
            /// The values in the y-axis
            /// </summary>
            public double[] yVals;
        }

        #endregion
    }

    #region classes for organising data

    #region CNPatches

    /// <summary>
    /// CNPatchPoolVariablePatchPool
    /// </summary>
    public class CNPatchPoolVariablePatchPoolType
    {
        /// <summary>The Value</summary>
        public Double[] Value;
    }

    /// <summary>
    /// CNPatchPoolVariablePatch
    /// </summary>
    public class CNPatchPoolVariablePatchType
    {
        /// <summary>The Pool</summary>
        public CNPatchPoolVariablePatchPoolType[] Pool;
    }

    /// <summary>
    /// CNPatchPoolVariableType
    /// </summary>
    public class CNPatchPoolVariableType
    {
        /// <summary>The Patch</summary>
        public CNPatchPoolVariablePatchType[] Patch;
    }

    /// <summary>
    /// CNPatchVariablePatchType
    /// </summary>
    public class CNPatchVariablePatchType
    {
        /// <summary>The Value</summary>
        public Double[] Value;
    }

    /// <summary>
    /// CNPatchVariablePatchDelegate
    /// </summary>
    public delegate void CNPatchVariablePatchDelegate(CNPatchVariablePatchType Data);

    /// <summary>
    /// CNPatchVariableType
    /// </summary>
    public class CNPatchVariableType
    {
        /// <summary>The Patch</summary>
        public CNPatchVariablePatchType[] Patch;
    }

    /// <summary>
    /// MergeSoilCNPatchType
    /// </summary>
    public class MergeSoilCNPatchType
    {
        /// <summary>The sender</summary>
        public string Sender = "";
        /// <summary>The affected patches_nm</summary>
        public string[] AffectedPatches_nm;
        /// <summary>The affected patches_id</summary>
        public int[] AffectedPatches_id;
        /// <summary>The merge all</summary>
        public bool MergeAll;
        /// <summary>Supress messages</summary>
        public string SuppressMessages;
    }

    /// <summary>
    /// AddSoilCNPatchwithFOMFOM
    /// </summary>
    public class AddSoilCNPatchwithFOMFOMType
    {
        /// <summary>The Name</summary>
        public String Name = "";
        /// <summary>The Type</summary>
        public String Type = "";
        /// <summary>The SoilOrganicMaterialType</summary>
        public SoilOrganicMaterialType[] Pool;
    }

    /// <summary>
    /// AddSoilCNPatchwithFOMFOMDelegate
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void AddSoilCNPatchwithFOMFOMDelegate(AddSoilCNPatchwithFOMFOMType Data);

    /// <summary>
    /// AddSoilCNPatchwithFOM
    /// </summary>
    public class AddSoilCNPatchwithFOMType
    {
        /// <summary>The Sender</summary>
        public String Sender = "";
        /// <summary>The SuppressMessages</summary>
        public String SuppressMessages = "";
        /// <summary>The DepositionType</summary>
        public String DepositionType = "";
        /// <summary>The AffectedPatches_nm</summary>
        public String[] AffectedPatches_nm;
        /// <summary>The AffectedPatches_id</summary>
        public Int32[] AffectedPatches_id;
        /// <summary>The AreaNewPatch</summary>
        public Double AreaNewPatch;
        /// <summary>The PatchName</summary>
        public String PatchName = "";
        /// <summary>The Water</summary>
        public Double[] Water;
        /// <summary>The Urea</summary>
        public Double[] Urea;
        /// <summary>The NH4</summary>
        public Double[] NH4;
        /// <summary>The NO3</summary>
        public Double[] NO3;
        /// <summary>The POX</summary>
        public Double[] POX;
        /// <summary>The SO4</summary>
        public Double[] SO4;
        /// <summary>The AshAlk</summary>
        public Double[] AshAlk;
        /// <summary>The AddSoilCNPatchwithFOMFOMType</summary>
        public AddSoilCNPatchwithFOMFOMType FOM;
    }

    /// <summary>
    /// AddSoilCNPatchwithFOMDelegate
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void AddSoilCNPatchwithFOMDelegate(AddSoilCNPatchwithFOMType Data);

    /// <summary>
    /// AddSoilCNPatch
    /// </summary>
    public class AddSoilCNPatchType
    {
        /// <summary>The Sender</summary>
        public String Sender = "";
        /// <summary>The SuppressMessages</summary>
        public String SuppressMessages = "";
        /// <summary>The DepositionType</summary>
        public String DepositionType = "";
        /// <summary>The AffectedPatches_nm</summary>
        public String[] AffectedPatches_nm;
        /// <summary>The AffectedPatches_id</summary>
        public Int32[] AffectedPatches_id;
        /// <summary>The AreaFraction</summary>
        public Double AreaFraction;
        /// <summary>The PatchName</summary>
        public String PatchName = "";
        /// <summary>The Water</summary>
        public Double[] Water;
        /// <summary>The Urea</summary>
        public Double[] Urea;
        /// <summary>The NH4</summary>
        public Double[] NH4;
        /// <summary>The NO3</summary>
        public Double[] NO3;
        /// <summary>The POX</summary>
        public Double[] POX;
        /// <summary>The SO4</summary>
        public Double[] SO4;
        /// <summary>The AshAlk</summary>
        public Double[] AshAlk;
        /// <summary>The FOM_C</summary>
        public Double[] FOM_C;
        /// <summary>The FOM_C_pool1</summary>
        public Double[] FOM_C_pool1;
        /// <summary>The FOM_C_pool2</summary>
        public Double[] FOM_C_pool2;
        /// <summary>The FOM_C_pool3</summary>
        public Double[] FOM_C_pool3;
        /// <summary>The FOM_N</summary>
        public Double[] FOM_N;
    }

    /// <summary>
    /// AddSoilCNPatchDelegate
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void AddSoilCNPatchDelegate(AddSoilCNPatchType Data);

    #endregion

    #region Organic matter types

    /// <summary>
    /// FOMType
    /// </summary>
    [Serializable]
    public class FOMType
    {
        /// <summary>The amount</summary>
        public double amount;
        /// <summary>The c</summary>
        public double C;
        /// <summary>The n</summary>
        public double N;
        /// <summary>The p</summary>
        public double P;
        /// <summary>The ash alk</summary>
        public double AshAlk;
    }

    /// <summary>
    /// FOMPoolType
    /// </summary>
    [Serializable]
    public class FOMPoolType
    {
        /// <summary>The layer</summary>
        public FOMPoolLayerType[] Layer;
    }

    /// <summary>
    /// FOMLayerType
    /// </summary>
    [Serializable]
    public class FOMLayerType
    {
        /// <summary>The type</summary>
        public string Type = "";
        /// <summary>The layer</summary>
        public FOMLayerLayerType[] Layer;
    }

    /// <summary>
    /// FOMPoolLayerType
    /// </summary>
    [Serializable]
    public class FOMPoolLayerType
    {
        /// <summary>The thickness</summary>
        public double thickness;
        /// <summary>The no3</summary>
        public double no3;
        /// <summary>The NH4</summary>
        public double nh4;
        /// <summary>The po4</summary>
        public double po4;
        /// <summary>The pool</summary>
        public FOMType[] Pool;
    }

    /// <summary>
    /// FOMLayerLayerType
    /// </summary>
    [Serializable]
    public class FOMLayerLayerType
    {
        /// <summary>The fom</summary>
        public FOMType FOM;
        /// <summary>The CNR</summary>
        public double CNR;
        /// <summary>The labile p</summary>
        public double LabileP;
    }

    /// <summary>
    /// SurfaceOrganicMatterDecompPoolType
    /// </summary>
    [Serializable]
    public class SurfaceOrganicMatterDecompPoolType
    {
        /// <summary>The name</summary>
        public string Name = "";
        /// <summary>The organic matter type</summary>
        public string OrganicMatterType = "";
        /// <summary>The fom</summary>
        public FOMType FOM;
    }

    /// <summary>
    /// SurfaceOrganicMatterDecompType
    /// </summary>
    [Serializable]
    public class SurfaceOrganicMatterDecompType
    {
        /// <summary>The pool</summary>
        public SurfaceOrganicMatterDecompPoolType[] Pool;
    }

    /// <summary>
    /// FOMdecompData
    /// </summary>
    public struct FOMdecompData
    {
        // lists with values from FOM decompostion
        /// <summary>The dlt_c_hum</summary>
        public double[] dlt_c_hum;
        /// <summary>The dlt_c_biom</summary>
        public double[] dlt_c_biom;
        /// <summary>The dlt_c_atm</summary>
        public double[] dlt_c_atm;
        /// <summary>The dlt_fom_n</summary>
        public double[] dlt_fom_n;
        /// <summary>The dlt_n_min</summary>
        public double dlt_n_min;
    }

    /// <summary>
    /// SoilOrganicMaterial
    /// </summary>
    /// <remarks>
    /// This was a trial and was meant to replace FOM type (RCichota)
    /// </remarks>
    public class SoilOrganicMaterialType
    {
        /// <summary>The Name</summary>
        public String Name = "";
        /// <summary>The Type</summary>
        public String Type = "";
        /// <summary>The C</summary>
        public Double[] C;
        /// <summary>The N</summary>
        public Double[] N;
        /// <summary>The P</summary>
        public Double[] P;
        /// <summary>The S</summary>
        public Double[] S;
        /// <summary>The AshAlk</summary>
        public Double[] AshAlk;
    }

    /// <summary>
    /// SoilOrganicMaterialDelegate
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void SoilOrganicMaterialDelegate(SoilOrganicMaterialType Data);

    #endregion

    #region Apsim stuff

    /// <summary>
    /// NitrogenChangedType
    /// </summary>
    public class NitrogenChangedType
    {
        /// <summary>The sender</summary>
        public string Sender = "";
        /// <summary>The sender type</summary>
        public string SenderType = "";
        /// <summary>The delta n o3</summary>
        public double[] DeltaNO3;
        /// <summary>The delta n h4</summary>
        public double[] DeltaNH4;
        /// <summary>The delta urea</summary>
        public double[] DeltaUrea;
    }

    /// <summary>
    /// NitrogenChangedDelegate
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void NitrogenChangedDelegate(NitrogenChangedType Data);

    /// <summary>
    /// AddUrineType
    /// </summary>
    /// <remarks>
    /// This was used in an early attempt to add excreta to SoilNitrogen in classic Apsim.
    /// It was never really implemented and was superseeded (partially anyways) by CNPatches
    /// It probably can be eliminated, or fully developed
    /// </remarks>
    public class AddUrineType
    {
        /// <summary>The number of urinations in the time step</summary>
        public double Urinations;
        /// <summary>The volume per urination in m^3</summary>
        public double VolumePerUrination;
        /// <summary>The area per urination in m^2</summary>
        public double AreaPerUrination;
        /// <summary>The eccentricity</summary>
        public double Eccentricity;
        /// <summary>The urea in kg N/ha</summary>
        public double Urea;
        /// <summary>The pox in kg P/ha</summary>
        public double POX;
        /// <summary>The s o4 in kg S/ha</summary>
        public double SO4;
        /// <summary>The ash alkin mol/ha</summary>
        public double AshAlk;
    }

    /// <summary>
    /// ExternalMassFlowType
    /// </summary>
    /// <remarks>
    /// This was used in classic Apsim to send mass balance info to SysBal.
    /// It is not used currently, and perhaps should be deleted
    /// </remarks>
    public class ExternalMassFlowType
    {
        /// <summary>The pool class</summary>
        public string PoolClass = "";
        /// <summary>The flow type</summary>
        public string FlowType = "";
        /// <summary>The c</summary>
        public double C;
        /// <summary>The n</summary>
        public double N;
        /// <summary>The p</summary>
        public double P;
        /// <summary>The dm</summary>
        public double DM;
        /// <summary>The sw</summary>
        public double SW;
    }

    #endregion

    #endregion
}