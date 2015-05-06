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

namespace Models.Soils
{

    /// <summary>
    /// This partial class contains most of the variables and input properties of SoilNitrogen
    /// </summary>
    public partial class SoilNitrogen
    {

        #region Links to other modules

        /// <summary>Link to APSIM's clock</summary>
        [Link]
        private Clock Clock = null;

        /// <summary>Link to APSIM's metFile (weather data)</summary>
        [Link]
        private IWeather MetFile = null;

        /// <summary>The soil</summary>
        [Link]
        private Soil Soil = null;

        /// <summary>The soil organic matter</summary>
        [Link]
        private SoilOrganicMatter SoilOrganicMatter = null;

        #endregion

        #region Parameters and inputs provided by the user or APSIM

        #region Parameters added by RCichota

        // whether to use new functions to compute temp and moist factors
        /// <summary>The use new STF function</summary>
        private bool useNewSTFFunction = false;  // for stf
        /// <summary>The use new SWF function</summary>
        private bool useNewSWFFunction = false;  // for swf
        /// <summary>The use new processes</summary>
        private bool useNewProcesses = false;    // for processes

        /// <summary>Sets the use all new functions.</summary>
        /// <value>The use all new functions.</value>
        public string useAllNewFunctions
        {
            set
            {
                useNewSTFFunction = value.ToLower().Contains("yes");
                useNewSWFFunction = useNewSTFFunction;
                useNewProcesses = useNewSTFFunction;
            }
        }

        /// <summary>Sets the use new function4 tf.</summary>
        /// <value>The use new function4 tf.</value>
        public string useNewFunction4TF
        { set { useNewSTFFunction = value.ToLower().Contains("yes"); } } // for stf

        /// <summary>Sets the use new function4 wf.</summary>
        /// <value>The use new function4 wf.</value>
        public string useNewFunction4WF
        { set { useNewSWFFunction = value.ToLower().Contains("yes"); } } // for swf

        // whether to use single temp and moist factors for SOM and FOM mineralisation ir separated
        /// <summary>The use single miner factors</summary>
        private bool useSingleMinerFactors = true;

        /// <summary>Sets the use single factors4 miner.</summary>
        /// <value>The use single factors4 miner.</value>
        public string useSingleFactors4Miner
        { set { useSingleMinerFactors = value.ToLower().Contains("yes"); } }

        // whether calculate one set of mineralisation factors (stf and swf) or one for each pool
        /// <summary>The use factors by so mpool</summary>
        private bool useFactorsBySOMpool = false;

        /// <summary>Sets the use multi factors4 miner som.</summary>
        /// <value>The use multi factors4 miner som.</value>
        public string useMultiFactors4MinerSOM
        { set { useFactorsBySOMpool = value.ToLower().Contains("yes"); } }
        /// <summary>The use factors by fo mpool</summary>
        private bool useFactorsByFOMpool = false;

        /// <summary>Sets the use multi factors4 miner fom.</summary>
        /// <value>The use multi factors4 miner fom.</value>
        public string useMultiFactors4MinerFOM
        { set { useFactorsByFOMpool = value.ToLower().Contains("yes"); } }

        /// <summary>The n partition approach</summary>
        [XmlIgnore]
        public string NPartitionApproach = "BasedOnLayerConcentration";

        #endregion

        //*Following parameters might be better merged into other regions but it is clear to have it separtately, FLi 
        #region ALTERNATIVE Params for alternarive nitrification/denitrification processes

        // soil texture by layer: COARSE = 1.0;/MEDIUM = 2.0; FINE = 3.0; VERYFINE = 4.0;
        /// <summary>The soil texture identifier</summary>
        double[] SoilTextureID = null;
        /// <summary>Sets the texture.</summary>
        /// <value>The texture.</value>
        public double[] texture
        {
            set
            {
                double IDvalue = 2.0;  // default texture is medium
                for (int layer = 0; layer < dlayer.Length; layer++)
                {
                    if (value != null)
                        IDvalue = value[layer];
                    SoilTextureID[layer] = IDvalue;
                }
            }
        }

        //Alternative N2O emission
        /// <summary>The n2o_approach</summary>
        [XmlIgnore]
        public int n2o_approach = 0;           // Approches used for nitri/denitri process for n2o emission 

        //WNMM
        /// <summary>The wnmm_n_alpha</summary>
        [XmlIgnore]
        public double wnmm_n_alpha = 0.002;             // maximum fraction of nitrification rate as N2O

        /// <summary>The wnmm_dn_alpha</summary>
        [XmlIgnore]
        public double wnmm_dn_alpha = 0.5;            // maximum fraction of denitrification rate at wfps = 0.8

        //NWMIS
        /// <summary>The nemis_dn_km</summary>
        [XmlIgnore]
        public double nemis_dn_km = 22;              // half-saturation consntant for NO3 reduction (unit ppm = mgN/kg)

        /// <summary>The nemis_dn_pot</summary>
        [XmlIgnore]
        public double nemis_dn_pot = 7.194; 	        // default = 7.194; potential denitrification rate at 20C, on undisturbed soil 
        // saturated with water in the lab and placed at a nitrate content near to 200 mgN/kg
        //CENTURY
        /// <summary>The cent_n_soilt_ave</summary>
        [XmlIgnore]
        public double cent_n_soilt_ave = 15;             // average soil surface temperature

        /// <summary>The cent_n_maxt_ave</summary>
        [XmlIgnore]
        public double cent_n_maxt_ave = 25; 	            // long term average maximum monthly temperature of the hottest month	

        /// <summary>The cent_n_wfps_ave</summary>
        [XmlIgnore]
        public double cent_n_wfps_ave = 0.7;              // default = 0.7; average wfps in top nitrifyDepth of soil

        /// <summary>The cent_n_max_rate</summary>
        [XmlIgnore]
        public double cent_n_max_rate = 0.1;              // default = 0.1, maximum fraction of ammonium to NO3 during nitrification (gN/m2)
        #endregion

        #region Parameters used on initialisation only

        #region General setting parameters

        /// <summary>Soil parameterisation set to use</summary>
        /// <remarks>Used to determine which node of xml file will be used to read 's</remarks>
        private string SoilCNParameterSet = "standard";

        /// <summary>Gets or sets the soiltype.</summary>
        /// <value>The soiltype.</value>
        [XmlIgnore]
        public string soiltype
        {
            get { return SoilCNParameterSet; }
            set { SoilCNParameterSet = value.Trim(); }
        }

        /// <summary>Indicates whether simpleSoilTemp is allowed</summary>
        /// <remarks>
        /// When 'yes', soil temperature may be computed internally, if an external value is not supplied.
        /// If 'no', a value for soil temperature must be supplied or an fatal error will occur.
        /// </remarks>
        private bool AllowsimpleSoilTemp = false;

        /// <summary>Gets or sets the allow_simple soil temporary.</summary>
        /// <value>The allow_simple soil temporary.</value>
        [XmlIgnore]
        public string allow_simpleSoilTemp
        {
            get { return (AllowsimpleSoilTemp) ? "yes" : "no"; }
            set { AllowsimpleSoilTemp = value.ToLower().Contains("yes"); }
        }

        /// <summary>Indicates whether soil profile reduction is allowed (from erosion)</summary>
        private bool AllowProfileReduction = false;

        /// <summary>Gets or sets the profile_reduction.</summary>
        /// <value>The profile_reduction.</value>
        [XmlIgnore]
        public string profile_reduction
        {
            get { return (AllowProfileReduction) ? "yes" : "no"; }
            set { AllowProfileReduction = value.ToLower().StartsWith("on"); }
        }

        /// <summary>Indicates whether organic solutes are to be simulated</summary>
        /// <remarks>Always false as this is not implemented yet</remarks>
        private bool useOrganicSolutes = false;

        /// <summary>Gets or sets the use_organic_solutes.</summary>
        /// <value>The use_organic_solutes.</value>
        [XmlIgnore]
        public string use_organic_solutes
        {
            get { return (useOrganicSolutes) ? "yes" : "no"; }
            set { useOrganicSolutes = value.ToLower().StartsWith("on"); }
        }

        /// <summary>Minimum allowable Urea content (ppm)</summary>
        [XmlIgnore]
        public double ureappm_min = 0.0;

        /// <summary>Minimum allowable NH4 content (ppm)</summary>
        [XmlIgnore]
        public double nh4ppm_min = 0.0;

        /// <summary>Minimum allowable NO3 content (ppm)</summary>
        [XmlIgnore]
        public double no3ppm_min = 0.0;

        /// <summary>Minimum allowable FOM content (kg/ha)</summary>
        [XmlIgnore]
        public double fom_min = 0.0;

        /// <summary>FOM type for initalisation</summary>
        [XmlIgnore]
        public string ini_FOMtype = "default";

        /// <summary>Factor to convert from OC to OM</summary>
        [XmlIgnore]
        public double oc2om_factor = 0.0;

        /// <summary>Default weight fraction of C in carbohydrates</summary>
        /// <remarks>Used to convert FOM amount into fom_c</remarks>
        private double defaultFOMCarbonContent = 0.4;
        /// <summary>Gets or sets the c_in_fom.</summary>
        /// <value>The c_in_fom.</value>
        [XmlIgnore]
        public double c_in_fom
        {
            get { return defaultFOMCarbonContent; }
            set { defaultFOMCarbonContent = value; }
        }

        /// <summary>Defaul value for initialising soil pH</summary>
        
        [XmlIgnore]
        public double ini_pH = 0.0;

        /// <summary>Minimum relative area (fraction of paddock) for any patch</summary>
        private double MinimumPatchArea = 1.0;

        /// <summary>Gets or sets the minimum patch area.</summary>
        /// <value>The minimum patch area.</value>
        [XmlIgnore]
        public double minPatchArea
        {
            get { return MinimumPatchArea; }
            set { MinimumPatchArea = value; }
        }

        /// <summary>
        /// Absolute threshold value to trigger a warning message when negative values are detected
        /// </summary>
        [XmlIgnore]
        public double WarningThreshold = -0.000000001;

        /// <summary>
        /// Absolute threshold value to trigger a fatal error when negative values are detected
        /// </summary>

        [XmlIgnore]
        public double FatalThreshold = -0.00001;

        #endregion

        #region Parameters for handling soil loss process

        /// <summary>Coefficient a of the enrichment equation</summary>

        private double enr_a_coeff;

        /// <summary>Coefficient b of the enrichment equation</summary>

        private double enr_b_coeff;

        #endregion

        #region Parameters for setting up soil organic matter

        /// <summary>The C:N ratio of the soil humus (active + inert)</summary>
        /// <remarks>Remains fixed throughout the simulation</remarks>
        private double hum_cn = 0.0;
        /// <summary>The C:N ratio of the soil OM, from xml/GUI (actually of humus)</summary>
        /// <value>The soil_cn.</value>
        private double soil_cn
        {
            get { return hum_cn; }
            set { hum_cn = value; }
        }

        /// <summary>The C:N ratio of microbial biomass</summary>
        /// <remarks>Remains fixed throughout the simulation</remarks>
        private double biom_cn = 8.0;
        /// <summary>Gets or sets the MCN.</summary>
        /// <value>The MCN.</value>
        [XmlIgnore]
        private double mcn
        {
            get { return biom_cn; }
            set { biom_cn = value; }
        }

        /// <summary>Proportion of biomass-C in the initial mineralizable humic-C (0-1)</summary>
        private double[] fbiom;

        /// <summary>
        /// Proportion of the initial total soil C that is inert, not subject to mineralisation (0-1)
        /// </summary>
        private double[] finert;

        #endregion

        #region Parameters for setting fresh organic matter (FOM)

        /// <summary>Initial weight of fom in the soil (kgDM/ha)</summary>
        private double iniFomWt = 0.0;
        /// <summary>Gets or sets the root_wt.</summary>
        /// <value>The root_wt.</value>
        private double root_wt
        {
            get { return iniFomWt; }
            set { iniFomWt = value; }
        }

        /// <summary>Initial depth over which fom is distributed within the soil profile (mm)</summary>
        /// <remarks>
        /// If not given fom will be distributed over the whole soil profile
        /// Distribution is homogenous over this depth
        /// </remarks>
        private double iniFomDepth;

        /// <summary>Gets or sets the root_depth.</summary>
        /// <value>The root_depth.</value>
        [XmlIgnore]
        private double root_depth
        {
            get { return iniFomDepth; }
            set { iniFomDepth = value; }
        }

        /// <summary>Initial C:N ratio of roots (actually FOM)</summary>
        private double iniFomCNratio = 0.0;
        /// <summary>Gets or sets the root_cn.</summary>
        /// <value>The root_cn.</value>
        private double root_cn
        {
            get { return iniFomCNratio; }
            set { iniFomCNratio = value; }
        }

        /// <summary>
        /// Initial C:N ratio of each of the three fom composition pools (carbohydrate, cellulose, and lignin)
        /// </summary>
        /// <remarks>Case not given, iniFomCNratio is used</remarks>
        private double[] fomPoolsCNratio = null;

        /// <summary>The root_cn_pool</summary>
        [XmlIgnore]
        public double[] root_cn_pool = {40, 40, 40};

        /// <summary>List of available FOM types names</summary>
        [XmlArray("fom_type")]
        public String[] fom_types = { "default", "manure", "mucuna", "lablab", "shemp", "stable" };


        /// <summary>Fraction of carbohydrate in FOM (0-1), for each FOM type</summary>
        public double[] fract_carb = { 0.2, 0.3, 0.54, 0.57, 0.45, 0.0 };

        /// <summary>Fraction of cellulose in FOM (0-1), for each FOM type</summary>
        public double[] fract_cell = { 0.7, 0.3, 0.37, 0.37, 0.47, 0.1 };
        /// <summary>Fraction of lignin in FOM (0-1), for each FOM type</summary>
        public double[] fract_lign = { 0.1, 0.4, 0.09, 0.06, 0.08, 0.9 };

        #endregion

        #region Parameters for FOM and SurfaceOM mineralisation process

        #region Surface OM

        /// <summary>Fraction of residue C mineralised retained in system (0-1)</summary>
        [XmlIgnore]
        public double ef_res = 0.4;

        /// <summary>Fraction of retained residue C transferred to biomass (0-1)</summary>
        /// <remarks>Remaining will got into humus</remarks>
        [XmlIgnore]
        public double fr_res_biom = 0.9;

        /// <summary>
        /// Depth from which mineral N can be immobilised when decomposing surface residues (mm)
        /// </summary>
        [XmlIgnore]
        public double min_depth = 100.0;

        #endregion

        #region Fresh OM

        /// <summary>
        /// Optimum rate constant for decomposition of FOM pools [carbohydrate component] (0-1)
        /// </summary>
        [XmlIgnore]
        public double[] rd_carb = { 0.2, 0.1 };

        /// <summary>Optimum rate constant for decomposition of FOM pools [cellulose component] (0-1)</summary>
        [XmlIgnore]
        public double[] rd_cell = { 0.05, 0.25 };

        /// <summary>Optimum rate constant for decomposition of FOM pools [lignin component] (0-1)</summary>
        [XmlIgnore]
        public double[] rd_lign = { 0.0095, 0.003 };

        /// <summary>Fraction of FOM C mineralised retained in system (0-1)</summary>
        [XmlIgnore]
        public double ef_fom = 0.4;

        /// <summary>Fraction of retained FOM C transferred to biomass (0-1)</summary>
        [XmlIgnore]
        public double fr_fom_biom = 0.9;

        #region Old parameters

        /// <summary>Coeff. to determine the magnitude of C:N effects on decomposition of FOM</summary>
        [XmlIgnore]
        public double cnrf_coeff = 0.693;

        /// <summary>C:N above which decomposition rate of FOM declines</summary>
        [XmlIgnore]
        public double cnrf_optcn = 25.0;

        #endregion

        #region New parameters

        /// <summary>Data for calculating the temperature effect on FOM mineralisation</summary>
        private BentStickData TempFactorData_MinerFOM = new BentStickData();


        /// <summary>Optimum temperature for mineralisation of FOM</summary>
        /// <value>The STF miner fo m_ topt.</value>

        public double[] stfMinerFOM_Topt
        { set { TempFactorData_MinerFOM.xValueForOptimum = value; } }

        /// <summary>Temperature factor for mineralisation of FOM at zero degrees</summary>
        /// <value>The STF miner fo m_ FCTR zero.</value>

        public double[] stfMinerFOM_FctrZero
        { set { TempFactorData_MinerFOM.yValueAtZero = value; } }

        /// <summary>Curve exponent for temperature factor for mineralisation of FOM</summary>
        /// <value>The STF miner fo m_ cv exp.</value>

        public double[] stfMinerFOM_CvExp
        { set { TempFactorData_MinerFOM.CurveExponent = value; } }

        /// <summary>Parameters for calculating the soil moisture factor for FOM mineralisation</summary>
        private BrokenStickData MoistFactorData_MinerFOM = new BrokenStickData();

        /// <summary>Values of modified soil water content at which the moisture factor is given</summary>
        /// <value>The SWF miner fo M_X.</value>

        public double[] swfMinerFOM_x
        { set { MoistFactorData_MinerFOM.xVals = value; } }

        /// <summary>Moiture factor values for the given modified soil water content</summary>
        /// <value>The SWF miner fo m_y.</value>

        public double[] swfMinerFOM_y
        { set { MoistFactorData_MinerFOM.yVals = value; } }

        /// <summary>Optimum C:N ratio, below which mineralisation of FOM is unlimited</summary>
        private double CNFactorMinerFOM_OptCN;

        /// <summary>Sets the CNF miner fo m_ opt cn.</summary>
        /// <value>The CNF miner fo m_ opt cn.</value>
        public double cnfMinerFOM_OptCN
        { set { CNFactorMinerFOM_OptCN = value; } }

        /// <summary>Decrease for the CN factor when C:N is greater then optimum</summary>
        private double CNFactorMinerFOM_RateCN;

        /// <summary>Sets the CNF miner fo m_ rate cn.</summary>
        /// <value>The CNF miner fo m_ rate cn.</value>
        public double cnfMinerFOM_RateCN
        { set { CNFactorMinerFOM_RateCN = value; } }

        #endregion

        #endregion

        #endregion

        #region Parameters for SOM mineralisation/immobilisation process

        /// <summary>Potential rate of soil biomass mineralisation (fraction per day)</summary>
        [XmlIgnore]
        public double[] rd_biom = {0.0081, 0.004};

        /// <summary>Fraction of biomass C mineralised retained in system (0-1)</summary>
        [XmlIgnore]
        public double ef_biom = 0.4;

        /// <summary>Fraction of retained biomass C returned to biomass (0-1)</summary>
        [XmlIgnore]
        public double fr_biom_biom = 0.6;

        /// <summary>Potential rate of humus mineralisation (per day)</summary>
        [XmlIgnore]
        public double[] rd_hum = {0.00015, 0.00007};

        /// <summary>Fraction of humic C mineralised retained in system (0-1)</summary>
        [XmlIgnore]
        public double ef_hum = 0.4;

        #region Old parameters

        /// <summary>
        /// Soil temperature above which there is no further effect on mineralisation and nitrification (oC)
        /// </summary>
        [XmlIgnore]
        public double[] opt_temp = { 32.0, 32.0 };

        /// <summary>Index specifying water content for computing moisture factor for mineralisation</summary>
        [XmlIgnore]
        public double[] wfmin_index = { 0.0, 0.5, 1.0, 2.0 };

        /// <summary>Value of moisture factor (for mineralisation) function at given index values</summary>
        [XmlIgnore]
        public double[] wfmin_values = { 0.0, 1.0, 1.0, 0.5 };

        #endregion

        #region New parameters

        /// <summary>Data to calculate the temperature effect on SOM mineralisation</summary>
        private BentStickData TempFactorData_MinerSOM = new BentStickData();

        /// <summary>Optimum temperature for OM mineralisation</summary>
        /// <value>The STF miner_ topt.</value>

        public double[] stfMiner_Topt
        {
            get { return TempFactorData_MinerSOM.xValueForOptimum; }
            set { TempFactorData_MinerSOM.xValueForOptimum = value; }
        }

        /// <summary>Temperature factor for OM mineralisation at zero degree</summary>
        /// <value>The STF miner_ FCTR zero.</value>

        public double[] stfMiner_FctrZero
        {
            get { return TempFactorData_MinerSOM.yValueAtZero; }
            set { TempFactorData_MinerSOM.yValueAtZero = value; }
        }

        /// <summary>Curve exponent to calculate temperature factor for OM mineralisation</summary>
        /// <value>The STF miner_ cv exp.</value>

        public double[] stfMiner_CvExp
        {
            get { return TempFactorData_MinerSOM.CurveExponent; }
            set { TempFactorData_MinerSOM.CurveExponent = value; }
        }

        /// <summary>Parameters to calculate soil moisture factor for OM mineralisation</summary>
        /// <remarks>These are pairs of points representing a broken stick function</remarks>
        private BrokenStickData MoistFactorData_MinerSOM = new BrokenStickData();

        /// <summary>Values of the modified soil water content at which misture factor is know</summary>
        /// <value>The SWF miner_x.</value>

        public double[] swfMiner_x
        {
            get { return MoistFactorData_MinerSOM.xVals; }
            set { MoistFactorData_MinerSOM.xVals = value; }
        }

        /// <summary>Values of the moisture factor at the given modified water content</summary>
        /// <value>The SWF miner_y.</value>

        public double[] swfMiner_y
        {
            get { return MoistFactorData_MinerSOM.yVals; }
            set { MoistFactorData_MinerSOM.yVals = value; }
        }

        #region Parameters for each OM type

        #region Humic pool

        /// <summary>Parameters to calculate the temperature effects on mineralisation - humus</summary>
        private BentStickData TempFactorData_MinerSOM_Hum = new BentStickData();

        /// <summary>Optimum temperature for mineralisation of humus</summary>
        /// <value>The STF miner hum_ topt.</value>

        public double[] stfMinerHum_Topt
        {
            get { return TempFactorData_MinerSOM_Hum.xValueForOptimum; }
            set { TempFactorData_MinerSOM_Hum.xValueForOptimum = value; }
        }

        /// <summary>Temperature factor for mineralisation of humus at zero degrees</summary>
        /// <value>The STF miner hum_ FCTR zero.</value>

        public double[] stfMinerHum_FctrZero
        {
            get { return TempFactorData_MinerSOM_Hum.yValueAtZero; }
            set { TempFactorData_MinerSOM_Hum.yValueAtZero = value; }
        }

        /// <summary>Curve exponent for calculating the temperature factor for mineralisation of humus</summary>
        /// <value>The STF miner hum_ cv exp.</value>

        public double[] stfMinerHum_CvExp
        {
            get { return TempFactorData_MinerSOM_Hum.CurveExponent; }
            set { TempFactorData_MinerSOM_Hum.CurveExponent = value; }
        }

        /// <summary>Parameters to calculate the soil moisture factor for mineralisation of humus</summary>
        private BrokenStickData MoistFactorData_MinerSOM_Hum = new BrokenStickData();

        /// <summary>Values of the modified soil water content at which the moisture factor is know</summary>
        /// <value>The SWF miner hum_x.</value>

        public double[] swfMinerHum_x
        {
            get { return MoistFactorData_MinerSOM_Hum.xVals; }
            set { MoistFactorData_MinerSOM_Hum.xVals = value; }
        }

        /// <summary>Values of the moisture factor at given water content values</summary>
        /// <value>The SWF miner hum_y.</value>

        public double[] swfMinerHum_y
        {
            get { return MoistFactorData_MinerSOM_Hum.yVals; }
            set { MoistFactorData_MinerSOM_Hum.yVals = value; }
        }

        # endregion

        #region M biomass pool

        /// <summary>Parameters to calculate the temperature effects on mineralisation - biom</summary>
        private BentStickData TempFactorData_MinerSOM_Biom = new BentStickData();

        /// <summary>Optimum temperature for mineralisation of biom</summary>
        /// <value>The STF miner biom_ topt.</value>

        public double[] stfMinerBiom_Topt
        {
            get { return TempFactorData_MinerSOM_Biom.xValueForOptimum; }
            set { TempFactorData_MinerSOM_Biom.xValueForOptimum = value; }
        }

        /// <summary>Temperature factor for mineralisation of biom at zero degrees</summary>
        /// <value>The STF miner biom_ FCTR zero.</value>

        public double[] stfMinerBiom_FctrZero
        {
            get { return TempFactorData_MinerSOM_Biom.yValueAtZero; }
            set { TempFactorData_MinerSOM_Biom.yValueAtZero = value; }
        }

        /// <summary>Curve exponent for calculating the temperature factor for mineralisation of biom</summary>
        /// <value>The STF miner biom_ cv exp.</value>

        public double[] stfMinerBiom_CvExp
        {
            get { return TempFactorData_MinerSOM_Biom.CurveExponent; }
            set { TempFactorData_MinerSOM_Biom.CurveExponent = value; }
        }

        /// <summary>Parameters to calculate the soil moisture factor for mineralisation of biom</summary>
        private BrokenStickData MoistFactorData_MinerSOM_Biom = new BrokenStickData();

        /// <summary>Values of the modified soil water content at which the moisture factor is know</summary>
        /// <value>The SWF miner biom_x.</value>

        public double[] swfMinerBiom_x
        {
            get { return MoistFactorData_MinerSOM_Biom.xVals; }
            set { MoistFactorData_MinerSOM_Biom.xVals = value; }
        }

        /// <summary>Values of the moisture factor at given water content values</summary>
        /// <value>The SWF miner biom_y.</value>

        public double[] swfMinerBiom_y
        {
            get { return MoistFactorData_MinerSOM_Biom.yVals; }
            set { MoistFactorData_MinerSOM_Biom.yVals = value; }
        }

        # endregion

        #endregion

        #endregion


        #endregion

        #region Parameters for urea hydrolisys process

        /// <summary>Parameters to calculate the temperature effect on urea hydrolysis</summary>
        private BentStickData TempFactorData_UHydrol = new BentStickData();

        /// <summary>Optimum temperature for urea hydrolisys</summary>
        /// <value>The STF hydrol_ topt.</value>

        public double[] stfHydrol_Topt
        {
            get { return TempFactorData_UHydrol.xValueForOptimum; }
            set { TempFactorData_UHydrol.xValueForOptimum = value; }
        }

        /// <summary>Temperature factor for urea hydrolisys at zero degrees</summary>
        /// <value>The STF hydrol_ FCTR zero.</value>

        public double[] stfHydrol_FctrZero
        {
            get { return TempFactorData_UHydrol.yValueAtZero; }
            set { TempFactorData_UHydrol.yValueAtZero = value; }
        }

        /// <summary>Curve exponent to calculate the temperature factor for urea hydrolisys</summary>
        /// <value>The STF hydrol_ cv exp.</value>

        public double[] stfHydrol_CvExp
        {
            get { return TempFactorData_UHydrol.CurveExponent; }
            set { TempFactorData_UHydrol.CurveExponent = value; }
        }

        /// <summary>Parameters to calculate the moisture effect on urea hydrolysis</summary>
        private BrokenStickData MoistFactorData_UHydrol = new BrokenStickData();

        /// <summary>Values of the modified soil water content at which factor is known</summary>
        /// <value>The SWF hydrol_x.</value>

        public double[] swfHydrol_x
        {
            get { return MoistFactorData_UHydrol.xVals; }
            set { MoistFactorData_UHydrol.xVals = value; }
        }

        /// <summary>Values of the modified moisture factor at given water content</summary>
        /// <value>The SWF hydrol_y.</value>

        public double[] swfHydrol_y
        {
            get { return MoistFactorData_UHydrol.yVals; }
            set { MoistFactorData_UHydrol.yVals = value; }
        }

        /// <summary>Minimum value for hydrolysis rate</summary>
        /// Parameters for calculating the potential urea hydrolisys

        [XmlIgnore]
        public double potHydrol_min = 0.0;

        /// <summary>Paramter A of the function determining potential urea hydrolysis</summary>

        [XmlIgnore]
        public double potHydrol_parmA = 0.0;

        /// <summary>Paramter B of the function determining potential urea hydrolysis</summary>

        [XmlIgnore]
        public double potHydrol_parmB = 0.0;

        /// <summary>Paramter C of the function determining potential urea hydrolysis</summary>

        [XmlIgnore]
        public double potHydrol_parmC = 0.0;

        /// <summary>Paramter D of the function determining potential urea hydrolysis</summary>

        [XmlIgnore]
        public double potHydrol_parmD = 0.0;

        #endregion

        #region Parameters for nitrification process

        /// <summary>Maximum potential nitrification (ppm/day)</summary>
        /// <remarks>
        /// This is the parameter M on Michaelis-Menten equation
        /// r = MC/(k+C)
        /// </remarks>
        [XmlIgnore]
        public double nitrification_pot = 40;

        /// <summary>NH4 conc. at half potential rate (ppm)</summary>
        /// <remarks>
        /// This is the parameter k on Michaelis-Menten equation
        /// r = MC/(k+C)
        /// </remarks>
        [XmlIgnore]
        public double nh4_at_half_pot = 90;

        #region Old parameters

        /// <summary>Index specifying water content for water factor for nitrification</summary>
        [XmlIgnore]
        public double[] wfnit_index = { 0.0, 0.25, 1.0, 2.0 };

        /// <summary>Value of water factor (for nitrification) function at given index values</summary>
        [XmlIgnore]
        public double[] wfnit_values = { 0.0, 1.0, 1.0, 0.0 };

        /// <summary>pH values for specifying pH factor for nitrification</summary>
        [XmlIgnore]
        public double[] pHf_nit_pH = { 4.5, 6, 8, 9 };

        /// <summary>Value of pH factor (for nitrification) function for given pH values</summary>
        [XmlIgnore]
        public double[] pHf_nit_values = { 0.0, 1.0, 1.0, 0.0 };

        #endregion

        #region New parameters

        /// <summary>Parameters to calculate the temperature effect on nitrification</summary>
        private BentStickData TempFactorData_Nitrif = new BentStickData();

        /// <summary>Optimum temperature for nitrification</summary>
        /// <value>The STF nitrif_ topt.</value>

        public double[] stfNitrif_Topt
        {
            get { return TempFactorData_Nitrif.xValueForOptimum; }
            set { TempFactorData_Nitrif.xValueForOptimum = value; }
        }

        /// <summary>Temperature factor for nitrification at zero degrees</summary>
        /// <value>The STF nitrif_ FCTR zero.</value>

        public double[] stfNitrif_FctrZero
        {
            get { return TempFactorData_Nitrif.yValueAtZero; }
            set { TempFactorData_Nitrif.yValueAtZero = value; }
        }

        /// <summary>Curve exponent for calculating the temperature factor for nitrification</summary>
        /// <value>The STF nitrif_ cv exp.</value>

        public double[] stfNitrif_CvExp
        {
            get { return TempFactorData_Nitrif.CurveExponent; }
            set { TempFactorData_Nitrif.CurveExponent = value; }
        }

        /// <summary>Parameters to calculate the soil moisture factor for nitrification</summary>
        private BrokenStickData MoistFactorData_Nitrif = new BrokenStickData();

        /// <summary>Values of the modified soil water content at which the moisture factor is known</summary>
        /// <value>The SWF nitrif_x.</value>

        public double[] swfNitrif_x
        {
            get { return MoistFactorData_Nitrif.xVals; }
            set { MoistFactorData_Nitrif.xVals = value; }
        }

        /// <summary>Values of the moisture factor at given water content</summary>
        /// <value>The SWF nitrif_y.</value>

        public double[] swfNitrif_y
        {
            get { return MoistFactorData_Nitrif.yVals; }
            set { MoistFactorData_Nitrif.yVals = value; }
        }

        /// <summary>Parameters to calculate the soil pH factor for nitrification</summary>
        private BrokenStickData pHFactorData_Nitrif = new BrokenStickData();

        /// <summary>Values of pH at which factors is known</summary>
        /// <value>The SPHF nitrif_x.</value>

        public double[] sphfNitrif_x
        {
            get { return pHFactorData_Nitrif.xVals; }
            set { pHFactorData_Nitrif.xVals = value; }
        }

        /// <summary>Values of pH factor ar given pH values</summary>
        /// <value>The SPHF nitrif_y.</value>

        public double[] sphfNitrif_y
        {
            get { return pHFactorData_Nitrif.yVals; }
            set { pHFactorData_Nitrif.yVals = value; }
        }

        #endregion

        #endregion

        #region Parameters for denitrification and N2O emission processes

        /// <summary>Denitrification rate coefficient (kg/mg)</summary>
        [XmlIgnore]
        public double dnit_rate_coeff = 0.0006;

        /// <summary>Fraction of nitrification lost as denitrification</summary>
        [XmlIgnore]
        public double dnit_nitrf_loss = 0.0;

        /// <summary>Parameter k1 from Thorburn et al (2010) for N2O model</summary>
        [XmlIgnore]
        public double dnit_k1 = 25.1;

        #region Old parameters

        /// <summary>Power term to calculate water factor for denitrification</summary>
        [XmlIgnore]
        public double dnit_wf_power = 1.0;

        /// <summary>Values of WFPS for calculating the N2O fraction of denitrification</summary>
        [XmlIgnore]
        public double[] dnit_wfps = { 21.333, 100 };  //Alert these parameter values need to be checked

        /// <summary>Values of WFPS factor for N2O fraction of denitrification</summary>
        [XmlIgnore]
        public double[] dnit_n2o_factor = { 0, 1.18 };  //Alert these parameter values need to be checked

        #endregion

        #region New parameters

        /// <summary>Parameter A to compute active carbon (for denitrification)</summary>

        [XmlIgnore]
        public double actC_parmB = 0;

        /// <summary>Parameter B to compute active carbon (for denitrification)</summary>

        [XmlIgnore]
        public double actC_parmA = 0;

        /// <summary>Parameters to calculate the temperature effect on denitrification</summary>
        private BentStickData TempFactorData_Denit = new BentStickData();

        /// <summary>Optimum temperature for denitrification</summary>
        /// <value>The STF denit_ topt.</value>

        public double[] stfDenit_Topt
        {
            get { return TempFactorData_Denit.xValueForOptimum; }
            set { TempFactorData_Denit.xValueForOptimum = value; }
        }

        /// <summary>Temperature factor for denitrification at zero degrees</summary>
        /// <value>The STF denit_ FCTR zero.</value>

        public double[] stfDenit_FctrZero
        {
            get { return TempFactorData_Denit.yValueAtZero; }
            set { TempFactorData_Denit.yValueAtZero = value; }
        }

        /// <summary>Curve exponent for calculating the temperature factor for denitrification</summary>
        /// <value>The STF denit_ cv exp.</value>

        public double[] stfDenit_CvExp
        {
            get { return TempFactorData_Denit.CurveExponent; }
            set { TempFactorData_Denit.CurveExponent = value; }
        }

        /// <summary>Parameters to calculate the soil moisture factor for denitrification</summary>
        private BrokenStickData MoistFactorData_Denit = new BrokenStickData();

        /// <summary>Values of modified soil water content at which the moisture factor is known</summary>
        /// <value>The SWF denit_x.</value>

        public double[] swfDenit_x
        {
            get { return MoistFactorData_Denit.xVals; }
            set { MoistFactorData_Denit.xVals = value; }
        }

        /// <summary>Values of the moisture factor at given water content values</summary>
        /// <value>The SWF denit_y.</value>

        public double[] swfDenit_y
        {
            get { return MoistFactorData_Denit.yVals; }
            set { MoistFactorData_Denit.yVals = value; }
        }

        /// <summary>Parameter A in the N2N2O function</summary>
        [XmlIgnore]
        public double N2N2O_parmA = 0.16;

        /// <summary>Parameter B in the N2N2O function</summary>

        [XmlIgnore]
        public double N2N2O_parmB = -0.80;

        /// <summary>Parameters to calculate the soil moisture factor for denitrification gas ratio</summary>
        private BrokenStickData WFPSFactorData_N2N2O = new BrokenStickData();

        /// <summary>Values of modified soil water content at which the moisture factor is known</summary>
        /// <value>The WFPS n2 n2 o_x.</value>

        public double[] wfpsN2N2O_x
        {
            get { return WFPSFactorData_N2N2O.xVals; }
            set { WFPSFactorData_N2N2O.xVals = value; }
        }

        /// <summary>Values of the moisture factor at given water content values</summary>
        /// <value>The WFPS n2 n2 o_y.</value>

        public double[] wfpsN2N2O_y
        {
            get { return WFPSFactorData_N2N2O.yVals; }
            set { WFPSFactorData_N2N2O.yVals = value; }
        }

        #endregion

        #endregion

        #endregion

        #region Parameters that do or may change during simulation

        #region Soil physics data

        /// <summary>Soil layers' thichness (mm)</summary>
        [Units("mm")]
        private double[] dlayer;

        /// <summary>Soil bulk density for each layer (g/cm3)</summary>
        [Units("g/cm^3")]
        private double[] bd;
        //private float[] bd;
        //private double[] SoilDensity;

        /// <summary>Soil water amount at saturation (mm)</summary>
        [Units("mm")]
        private double[] sat_dep;

        /// <summary>Soil water amount at drainage upper limit (mm)</summary>
        [Units("mm")]
        private double[] dul_dep;

        /// <summary>Soil water amount at drainage lower limit (mm)</summary>
        [Units("mm")]
        private double[] ll15_dep;

        /// <summary>Today's soil water amount (mm)</summary>
        [Units("mm")]
        private double[] sw_dep;

        /// <summary>Soil albedo (0-1)</summary>
        private double salb;
        //{ get; private set; }

        /// <summary>Soil temperature (oC), as computed by an external module (SoilTemp)</summary>
        [Units("oC")]
        public double[] ave_soil_temp;

        #endregion

        #region Soil pH data

        /// <summary>pH of soil (assumed equivalent to a 1:1 soil-water slurry)</summary>
        private double[] ph;

        #endregion

        #region Values for soil organic matter (som)

        /// <summary>
        /// Stores initial OC values until dlayer is available, can be used for a Reset operation
        /// </summary>
        private double[] OC_reset;

        /// <summary>Total soil organic carbon content (%)</summary>
        /// <value>The oc.</value>


        [Units("%")]
        [Description("Soil organic carbon (exclude FOM)")]
        public double[] oc
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[dlayer.Length];
                    for (int k = 0; k < Patch.Count; k++)
                        for (int layer = 0; layer < dlayer.Length; ++layer)
                            result[layer] += Patch[k].oc[layer] * Patch[k].RelativeArea;
                }
                else
                {
                    // no value has been asigned yet, return null
                    result = OC_reset;
                }
                return result;
            }
            set
            {
                if (initDone)
                {
                    Summary.WriteMessage(this, " Attempt to assign values for OC during simulation, "
                                     + "this operation is not valid and will be ignored");
                }
                else
                {
                    // Store initial values, initialisation of C pools is done on InitCalc. Can be used OnReset
                    OC_reset = value;
                }
            }
        }

        #endregion

        #region Values for soil mineral N

        /// <summary>
        /// Stores initial values until dlayer is available, can be used for a Reset operation
        /// </summary>
        private double[] ureappm_reset;

        /// <summary>Soil urea nitrogen content (ppm)</summary>
        /// <value>The ureappm.</value>
        [Units("mg/kg")]
        [Description("Soil urea nitrogen content")]
        [XmlIgnore]
        public double[] ureappm
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[dlayer.Length];
                    for (int k = 0; k < Patch.Count; k++)
                        for (int layer = 0; layer < dlayer.Length; ++layer)
                            result[layer] += Patch[k].urea[layer] * convFactor_kgha2ppm(layer) * Patch[k].RelativeArea;
                }
                else
                    result = ureappm_reset;
                return result;
            }
            set
            {
                if (initDone)
                {
                    double sumOld = MathUtilities.Sum(urea);      // original amount

                    for (int layer = 0; layer < value.Length; ++layer)
                        value[layer] = MathUtilities.Divide(value[layer], convFactor_kgha2ppm(layer), 0.0);       //Convert from ppm to kg/ha
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].urea = value;

                    if (!inReset)
                        SendExternalMassFlowN(MathUtilities.Sum(urea) - sumOld);

                }
                else
                    ureappm_reset = value;
            }
        }

        /// <summary>Soil urea nitrogen amount (kgN/ha)</summary>
        /// <value>The urea.</value>

        [Units("kg/ha")]
        [Description("Soil urea nitrogen amount")]
        [XmlIgnore]
        public double[] urea
        {
            get
            {
                if (dlayer != null)
                {
                    double[] result = new double[dlayer.Length];
                    for (int k = 0; k < Patch.Count; k++)
                        for (int layer = 0; layer < dlayer.Length; ++layer)
                            result[layer] += Patch[k].urea[layer] * Patch[k].RelativeArea;
                    return result;
                }
                return null;
            }
            set  // should this be private?
            {
                double sumOld = MathUtilities.Sum(urea);

                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].urea = value;

                SendExternalMassFlowN(MathUtilities.Sum(urea) - sumOld);
            }
        }

        /// <summary>
        /// Stores initial values until dlayer is available, can be used for a Reset operation
        /// </summary>
        private double[] nh4ppm_reset;
        /// <summary>Soil ammonium nitrogen content (ppm)</summary>
        /// <value>The NH4PPM.</value>
        [Units("mg/kg")]
        [Description("Soil ammonium nitrogen content")]
        [XmlIgnore]
        public double[] NH4ppm
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[dlayer.Length];
                    for (int k = 0; k < Patch.Count; k++)
                        for (int layer = 0; layer < dlayer.Length; ++layer)
                            result[layer] += Patch[k].nh4[layer] * convFactor_kgha2ppm(layer) * Patch[k].RelativeArea;
                }
                else
                    result = nh4ppm_reset;
                return result;
            }
            set
            {
                if (initDone)
                {
                    double sumOld = MathUtilities.Sum(NH4);   // original values

                    for (int layer = 0; layer < value.Length; ++layer)
                        value[layer] = MathUtilities.Divide(value[layer], convFactor_kgha2ppm(layer), 0.0);       //Convert from ppm to kg/ha
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].nh4 = value;

                    if (!inReset)
                        SendExternalMassFlowN(MathUtilities.Sum(NH4) - sumOld);
                }
                else
                    nh4ppm_reset = value;
            }
        }

        /// <summary>Soil ammonium nitrogen amount (kg/ha)</summary>
        /// <value>The NH4.</value>

        [Units("kg/ha")]
        [Description("Soil ammonium nitrogen amount")]
        [XmlIgnore]
        public double[] NH4
        {
            get
            {
                if (dlayer != null)
                {
                    double[] result = new double[dlayer.Length];
                    for (int k = 0; k < Patch.Count; k++)
                        for (int layer = 0; layer < dlayer.Length; ++layer)
                            result[layer] += Patch[k].nh4[layer] * Patch[k].RelativeArea;
                    return result;
                }
                return null;
            }
            set  // should this be private?
            {
                double sumOld = MathUtilities.Sum(NH4);

                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].nh4 = value;

                SendExternalMassFlowN(MathUtilities.Sum(NH4) - sumOld);
            }
        }

        /// <summary>
        /// Stores initial values until dlayer is available, can be used for a Reset operation
        /// </summary>
        private double[] no3ppm_reset;

        /// <summary>Soil nitrate nitrogen content (ppm)</summary>
        /// <value>The no3ppm.</value>
        [Units("mg/kg")]
        [Description("Soil nitrate nitrogen content")]
        [XmlIgnore]
        public double[] NO3ppm
        {
            get
            {
                double[] result;
                if (initDone)
                {
                    result = new double[dlayer.Length];
                    for (int k = 0; k < Patch.Count; k++)
                        for (int layer = 0; layer < dlayer.Length; ++layer)
                            result[layer] += Patch[k].no3[layer] * convFactor_kgha2ppm(layer) * Patch[k].RelativeArea;
                }
                else
                    result = no3ppm_reset;
                return result;
            }
            set
            {
                if (initDone)
                {
                    double sumOld = MathUtilities.Sum(NO3);   // original values
                    for (int layer = 0; layer < value.Length; ++layer)
                        value[layer] = MathUtilities.Divide(value[layer], convFactor_kgha2ppm(layer), 0.0);       //Convert from ppm to kg/ha
                    for (int k = 0; k < Patch.Count; k++)
                        Patch[k].no3 = value;

                    if (!inReset)
                        SendExternalMassFlowN(MathUtilities.Sum(NO3) - sumOld);
                }
                else
                    no3ppm_reset = value;
            }
        }

        /// <summary>Soil nitrate nitrogen amount (kgN/ha)</summary>
        /// <value>The no3.</value>

        [Units("kg/ha")]
        [Description("Soil nitrate nitrogen amount")]
        [XmlIgnore]
        public double[] NO3
        {
            get
            {
                if (dlayer != null)
                {
                    double[] result = new double[dlayer.Length];
                    for (int layer = 0; layer < dlayer.Length; ++layer)
                        for (int k = 0; k < Patch.Count; k++)
                            result[layer] += Patch[k].no3[layer] * Patch[k].RelativeArea;
                    return result;
                }
                return null;
            }
            set  // should this be private? or not exist at all?
            {
                double sumOld = MathUtilities.Sum(NO3);
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].no3 = value;

                SendExternalMassFlowN(MathUtilities.Sum(NO3) - sumOld);
            }
        }

        #endregion

        #region Soil loss data

        /// <summary>Indicates whether soil profile reduction is allowed (from erosion)</summary>
        private bool allowProfileReduction = false;
        /// <summary>Sets the n_reduction.</summary>
        /// <value>The n_reduction.</value>
        private string n_reduction
        { set { allowProfileReduction = value.StartsWith("on"); } }

        /// <summary>Soil loss, due to erosion (?)</summary>
        [Units("t/ha")]
        private double soil_loss = 0;

        #endregion

        #region Pond data

        /// <summary>Indicates whether pond is active or not</summary>
        private Boolean isPondActive = false;
        /// <summary>Sets the pond_active.</summary>
        /// <value>The pond_active.</value>
        private string pond_active
        { set { isPondActive = (value == "yes"); } }

        /// <summary>Amount of C decomposed in pond that is added to soil m. biomass</summary>
        [Units("kg/ha")]
        private double pond_biom_C = 0;
        //{ set {PondC_to_BiomC}; }

        /// <summary>Amount of C decomposed in pond that is added to soil humus</summary>
        [Units("kg/ha")]
        private double pond_hum_C = 0;
        //{ set {PondC_to_HumC}; }

        #endregion

        #region Inhibitors data

        // factor reducing urea hydrolysis due to the presence of an inhibitor - not implemented yet
        /// <summary>The inhibition factor_ u hydrolysis</summary>
        private double[] InhibitionFactor_UHydrolysis = null;
        /// <summary>Gets or sets the hydrolysis_inhibition.</summary>
        /// <value>The hydrolysis_inhibition.</value>
        [Units("0-1")]
        private double[] hydrolysis_inhibition
        {
            get { return InhibitionFactor_UHydrolysis; }
            set
            {
                InhibitionFactor_UHydrolysis = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                {
                    if (layer < value.Length)
                    {
                        InhibitionFactor_UHydrolysis[layer] = value[layer];
                        if (InhibitionFactor_UHydrolysis[layer] < 0.0)
                        {
                            InhibitionFactor_UHydrolysis[layer] = 0.0;
                            Summary.WriteMessage(this, "Value for hydrolysis inhibition is below lower limit, value will be adjusted to 0.0");
                        }
                        else if (InhibitionFactor_UHydrolysis[layer] > 1.0)
                        {
                            InhibitionFactor_UHydrolysis[layer] = 1.0;
                            Summary.WriteMessage(this, "Value for hydrolysis inhibition is above upper limit, value will be adjusted to 1.0");
                        }
                    }
                    else
                        InhibitionFactor_UHydrolysis[layer] = 0.0;
                }
            }
        }

        /// <summary>Factor reducing nitrification due to the presence of a inhibitor</summary>
        private double[] InhibitionFactor_Nitrification = null;
        /// <summary>Sets the nitrification_inhibition.</summary>
        /// <value>The nitrification_inhibition.</value>
        [Units("0-1")]
        double[] nitrification_inhibition
        {
            set
            {
                InhibitionFactor_Nitrification = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                {
                    if (layer < value.Length)
                    {
                        InhibitionFactor_Nitrification[layer] = value[layer];
                        if (InhibitionFactor_Nitrification[layer] < 0.0)
                        {
                            InhibitionFactor_Nitrification[layer] = 0.0;
                            Summary.WriteMessage(this, "Value for nitrification inhibition is below lower limit, value will be adjusted to 0.0");
                        }
                        else if (InhibitionFactor_Nitrification[layer] > 1.0)
                        {
                            InhibitionFactor_Nitrification[layer] = 1.0;
                            Summary.WriteMessage(this, "Value for nitrification inhibition is above upper limit, value will be adjusted to 1.0");
                        }
                    }
                    else
                        InhibitionFactor_Nitrification[layer] = 0.0;
                }
            }
        }

        // factor reducing urea hydrolysis due to the presence of an inhibitor - not implemented yet
        /// <summary>The inhibition factor_ denitrification</summary>
        private double[] InhibitionFactor_Denitrification = null;
        /// <summary>Gets or sets the denitrification_inhibition.</summary>
        /// <value>The denitrification_inhibition.</value>
        [Units("0-1")]
        private double[] Denitrification_inhibition
        {
            get { return InhibitionFactor_UHydrolysis; }
            set
            {
                InhibitionFactor_Denitrification = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                {
                    if (layer < value.Length)
                    {
                        InhibitionFactor_Denitrification[layer] = value[layer];
                        if (InhibitionFactor_Denitrification[layer] < 0.0)
                        {
                            InhibitionFactor_Denitrification[layer] = 0.0;
                            Summary.WriteMessage(this, "Value for denitrification inhibition is below lower limit, "
                                + "value will be adjusted to 0.0");
                        }
                        else if (InhibitionFactor_Denitrification[layer] > 1.0)
                        {
                            InhibitionFactor_Denitrification[layer] = 1.0;
                            Summary.WriteMessage(this, "Value for denitrification inhibition is above upper limit, "
                                + "value will be adjusted to 1.0");
                        }
                    }
                    else
                        InhibitionFactor_Denitrification[layer] = 0.0;
                }
            }
        }

        // factor reducing mineralisation processes due to the presence of an inhibitor - not implemented yet
        /// <summary>The inhibition factor_ mineralisation</summary>
        private double[] InhibitionFactor_Mineralisation = null;
        /// <summary>Gets or sets the mineralisation_inhibition.</summary>
        /// <value>The mineralisation_inhibition.</value>
        [Units("0-1")]
        private double[] mineralisation_inhibition
        {
            get { return InhibitionFactor_Mineralisation; }
            set
            {
                InhibitionFactor_Mineralisation = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                {
                    if (layer < value.Length)
                    {
                        InhibitionFactor_Mineralisation[layer] = value[layer];
                        if (InhibitionFactor_Mineralisation[layer] < 0.0)
                        {
                            InhibitionFactor_Mineralisation[layer] = 0.0;
                            Summary.WriteMessage(this, "Value for mineralisation inhibition is below lower limit, value will be adjusted to 0.0");
                        }
                        else if (InhibitionFactor_Mineralisation[layer] > 1.0)
                        {
                            InhibitionFactor_Mineralisation[layer] = 1.0;
                            Summary.WriteMessage(this, "Value for mineralisation inhibition is above upper limit, value will be adjusted to 1.0");
                        }
                    }
                    else
                        InhibitionFactor_Mineralisation[layer] = 0.0;
                }
            }
        }

        #endregion

        #endregion

        #region Settable variables

        #region Mineral nitrogen

        /// <summary>Variations in ureappm as given by another component</summary>
        /// <value>The dlt_ureappm.</value>

        [Units("mg/kg")]
        private double[] dlt_ureappm
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this will have to be handled differently in the future
                for (int layer = 0; layer < value.Length; ++layer)
                    value[layer] = MathUtilities.Divide(value[layer], convFactor_kgha2ppm(layer), 0.0);  // convert from ppm to kg/ha
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_urea = value;
            }
        }

        /// <summary>Variations in urea as given by another component</summary>
        /// <value>The dlt_urea.</value>

        [Units("kg/ha")]
        private double[] dlt_urea
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this will have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_urea = value;
            }
        }

        /// <summary>Variations in nh4ppm as given by another component</summary>
        /// <value>The DLT_NH4PPM.</value>

        [Units("mg/kg")]
        private double[] dlt_nh4ppm
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this will have to be handled differently in the future
                for (int layer = 0; layer < value.Length; ++layer)
                    value[layer] = MathUtilities.Divide(value[layer], convFactor_kgha2ppm(layer), 0.0);  // convert from ppm to kg/ha
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_nh4 = value;
            }
        }

        /// <summary>Variations in nh4 as given by another component</summary>
        /// <value>The DLT_NH4.</value>

        [Units("kg/ha")]
        private double[] dlt_nh4
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this will have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_nh4 = value;
            }
        }

        /// <summary>Variations in no3ppm as given by another component</summary>
        /// <value>The dlt_no3ppm.</value>

        [Units("mg/kg")]
        private double[] dlt_no3ppm
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this will have to be handled differently in the future
                for (int layer = 0; layer < value.Length; ++layer)
                    value[layer] = MathUtilities.Divide(value[layer], convFactor_kgha2ppm(layer), 0.0);  // convert from ppm to kg/ha
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_no3 = value;
            }
        }

        /// <summary>Variations in no3 as given by another component</summary>
        /// <value>The dlt_no3.</value>

        [Units("kg/ha")]
        private double[] dlt_no3
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this will have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_no3 = value;
            }
        }

        #endregion

        #region Organic N and C

        /// <summary>Variations in org_n as given by another component</summary>
        /// <value>The dlt_org_n.</value>

        [Units("kg/ha")]
        private double[] dlt_org_n
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this might have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_org_n = value;
            }
        }

        /// <summary>Variations in org_c_pool1 as given by another component</summary>
        /// <value>The dlt_org_c_pool1.</value>


        [Units("kg/ha")]
        private double[] dlt_org_c_pool1
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this might have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_org_c_pool1 = value;
            }
        }

        /// <summary>Variations in org_c_pool2 as given by another component</summary>
        /// <value>The dlt_org_c_pool2.</value>


        [Units("kg/ha")]
        private double[] dlt_org_c_pool2
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this might have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_org_c_pool2 = value;
            }
        }

        /// <summary>Variations in org_c_pool3 as given by another component</summary>
        /// <value>The dlt_org_c_pool3.</value>


        [Units("kg/ha")]
        private double[] dlt_org_c_pool3
        {
            set
            {
                // for now any incoming dlt is passed to all patches, this might have to be handled differently in the future
                for (int k = 0; k < Patch.Count; k++)
                    Patch[k].dlt_org_c_pool3 = value;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Outputs we make available to other components

        #region Values that other components can get or set

        /// <summary>Amount of C in pool1 of FOM - doesn't seem to be fully implemented</summary>
        /// <value>The org_c_pool1.</value>
        /// <exception cref="System.Exception">Value given for fom_c_pool1 is negative</exception>

        [Units("kg/ha")]
        [Description("Not fully implemented")]
        double[] org_c_pool1
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c_pool1[layer] * Patch[k].RelativeArea;
                return result;
            }
            set
            {
                if (value.Length == dlayer.Length)
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        if (value[layer] < 0)
                            throw new Exception("Value given for fom_c_pool1 is negative");
                        else
                            for (int k = 0; k < Patch.Count; k++)
                                Patch[k].fom_c_pool1[layer] = value[layer];
                    }
                }
            }
        }

        /// <summary>Amount of C in pool2 of FOM - doesn't seem to be fully implemented</summary>
        /// <value>The org_c_pool2.</value>
        /// <exception cref="System.Exception">Value given for fom_c_pool2 is negative</exception>

        [Units("kg/ha")]
        [Description("Not fully implemented")]
        double[] org_c_pool2
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c_pool2[layer] * Patch[k].RelativeArea;
                return result;
            }
            set
            {
                if (value.Length == dlayer.Length)
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        if (value[layer] < 0)
                            throw new Exception("Value given for fom_c_pool2 is negative");
                        else
                            for (int k = 0; k < Patch.Count; k++)
                                Patch[k].fom_c_pool2[layer] = value[layer];
                    }
                }
            }
        }

        /// <summary>Amount of C in pool3 of FOM - doesn't seem to be fully implemented</summary>
        /// <value>The org_c_pool3.</value>
        /// <exception cref="System.Exception">Value given for fom_c_pool3 is negative</exception>

        [Units("kg/ha")]
        [Description("Not fully implemented")]
        double[] org_c_pool3
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c_pool3[layer] * Patch[k].RelativeArea;
                return result;
            }
            set
            {
                if (value.Length == dlayer.Length)
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        if (value[layer] < 0)
                            throw new Exception("Value given for fom_c_pool3 is negative");
                        else
                            for (int k = 0; k < Patch.Count; k++)
                                Patch[k].fom_c_pool3[layer] = value[layer];
                    }
                }
            }
        }

        /// <summary>Amount of N in FOM - doesn't seem to be fully implemented</summary>
        /// <value>The org_n.</value>
        /// <exception cref="System.Exception">Value given for fom_n is negative</exception>

        [Units("kg/ha")]
        [Description("Not fully implemented")]
        double[] org_n
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n[layer] * Patch[k].RelativeArea;
                return result;
            }
            set
            {
                if (value.Length == dlayer.Length)
                {
                    for (int layer = 0; layer < value.Length; ++layer)
                    {
                        if (value[layer] < 0)
                            throw new Exception("Value given for fom_n is negative");
                        else
                            for (int k = 0; k < Patch.Count; k++)
                                Patch[k].fom_n[layer] = value[layer];

                    }
                }
            }
        }

        #endregion

        #region Values that other components can only get

        #region Outputs for Nitrogen

        #region General values

        /// <summary>Minimum allowable urea amount in each layer</summary>

        [Units("kg/ha")]
        [Description("Minimum allowable urea")]
        public double[] urea_min;

        /// <summary>Minimum allowable NH4 amount in each layer</summary>

        [Units("kg/ha")]
        [Description("Minimum allowable NH4")]
        public double[] nh4_min;

        /// <summary>Minimum allowable NO3 amount in each layer</summary>

        [Units("kg/ha")]
        [Description("Minimum allowable NO3")]
        public double[] no3_min;

        #endregion

        #region Changes for today - deltas

        /// <summary>N carried out in sediment via runoff/erosion</summary>
        /// <value>The dlt_n_loss_in_sed.</value>
        [Units("kg")]
        double dlt_n_loss_in_sed
        {
            get
            {
                double result = 0.0;
                for (int k = 0; k < Patch.Count; k++)
                    result += Patch[k].dlt_n_loss_in_sed * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net nh4 change today</summary>
        /// <value>The dlt_nh4_net.</value>

        [Units("kg/ha")]
        double[] dlt_nh4_net
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nh4_net[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net NH4 transformation today</summary>
        /// <value>The nh4_transform_net.</value>

        [Units("kg/ha")]
        double[] nh4_transform_net
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].nh4_transform_net[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net no3 change today</summary>
        /// <value>The dlt_no3_net.</value>

        [Units("kg/ha")]
        double[] dlt_no3_net
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_no3_net[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net NO3 transformation today</summary>
        /// <value>The no3_transform_net.</value>
        [Units("kg/ha")]
        double[] no3_transform_net
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].no3_transform_net[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net mineralisation today</summary>
        /// <value>The dlt_n_min.</value>

        [Units("kg/ha")]
        public double[] MineralisedN
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_min[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net N mineralisation from residue decomposition</summary>
        /// <value>The dlt_n_min_res.</value>

        [Units("kg/ha")]
        public double[] dlt_n_min_res
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_min_res[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net NH4 mineralisation from residue decomposition</summary>
        /// <value>The dlt_res_nh4_min.</value>

        [Units("kg/ha")]
        double[] dlt_res_nh4_min
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nh4_decomp[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net NO3 mineralisation from residue decomposition</summary>
        /// <value>The dlt_res_no3_min.</value>

        [Units("kg/ha")]
        double[] dlt_res_no3_min
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_no3_decomp[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net fom N mineralised (negative for immobilisation)</summary>
        /// <value>The dlt_fom_n_min.</value>

        [Units("kg/ha")]
        double[] dlt_fom_n_min
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_fom_2_min[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net N mineralised for humic pool</summary>
        /// <value>The dlt_hum_n_min.</value>

        [Units("kg/ha")]
        double[] dlt_hum_n_min
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_hum_2_min[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Net N mineralised from m. biomass pool</summary>
        /// <value>The dlt_biom_n_min.</value>

        [Units("kg/ha")]
        double[] dlt_biom_n_min
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_biom_2_min[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Total net N mineralised (residues plus soil OM)</summary>
        /// <value>The dlt_n_min_tot.</value>

        [Units("kg/ha")]
        public double[] dlt_n_min_tot
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_n_min_tot[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Nitrogen coverted by hydrolysis (from urea to NH4)</summary>
        /// <value>The dlt_urea_hydrol.</value>

        [Units("kg/ha")]
        double[] dlt_urea_hydrol
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_urea_hydrolised[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>
        /// Nitrogen coverted by nitrification (from NH4 to either NO3 or N2O) - alias of nitrification
        /// </summary>
        /// <value>The DLT_RNTRF.</value>

        [Units("kg/ha")]
        double[] dlt_rntrf
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nitrification[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Nitrogen coverted by nitrification (from NH4 to either NO3 or N2O)</summary>
        /// <value>The nitrification.</value>

        [Units("kg/ha")]
        public double[] Nitrification
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nitrification[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Effective, or net, nitrogen coverted by nitrification (from NH4 to NO3)</summary>
        /// <value>The effective_nitrification.</value>

        [Units("kg/ha")]
        double[] effective_nitrification
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].effective_nitrification[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>NH4 N denitrified</summary>
        /// <value>The dlt_nh4_dnit.</value>

        [Units("kg/ha")]
        double[] dlt_nh4_dnit
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nh4_dnit[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>NO3 N denitrified</summary>
        /// <value>The dlt_no3_dnit.</value>

        [Units("kg/ha")]
        double[] dlt_no3_dnit
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_no3_dnit[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Total N2O amount produced today</summary>
        /// <value>The n2o_atm.</value>

        [Units("kg/ha")]
        public double[] n2o_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].n2o_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of N2O produced by nitrification</summary>
        /// <value>The n2o_atm_nitrification.</value>

        [Units("kg/ha")]
        double[] n2o_atm_nitrification
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_nh4_dnit[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of N2O produced by denitrification</summary>
        /// <value>The n2o_atm_dentrification.</value>

        [Units("kg/ha")]
        double[] n2o_atm_dentrification
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += (Patch[k].n2o_atm[layer] - Patch[k].dlt_nh4_dnit[layer]) * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of N2 produced</summary>
        /// <value>The n2_atm.</value>

        [Units("kg/ha")]
        double[] n2_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].n2_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>N converted by denitrification</summary>
        /// <value>The dnit.</value>

        [Units("kg/ha")]
        public double[] Denitrification
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; layer++)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dnit[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Excess N required above NH4 supply (for immobilisation)</summary>
        /// <value>The nh4_deficit_immob.</value>

        [Units("kg/ha")]
        double[] nh4_deficit_immob
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].nh4_deficit_immob[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        #endregion

        #region Amounts in various pools

        /// <summary>Total nitrogen in FOM</summary>
        /// <value>The fom_n.</value>

        [Units("kg/ha")]
        public double[] FOMN
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Nitrogen in FOM pool 1</summary>
        /// <value>The fom_n_pool1.</value>

        [Units("kg/ha")]
        double[] fom_n_pool1
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n_pool1[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Nitrogen in FOM pool 2</summary>
        /// <value>The fom_n_pool2.</value>

        [Units("kg/ha")]
        double[] fom_n_pool2
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n_pool2[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Nitrogen in FOM pool 3</summary>
        /// <value>The fom_n_pool3.</value>

        [Units("kg/ha")]
        double[] fom_n_pool3
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_n_pool3[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Soil humic N</summary>
        /// <value>The hum_n.</value>

        [Units("kg/ha")]
        public double[] HumicN
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].hum_n[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Soil biomass nitrogen</summary>
        /// <value>The biom_n.</value>

        [Units("kg/ha")]
        public double[] MicrobialN
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].biom_n[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Total N in soil</summary>
        /// <value>The nit_tot.</value>

        [Units("kg/ha")]
        public double[] TotalN
        {
            get
            {
                double[] result = null;
                if (dlayer != null)
                {
                    result = new double[dlayer.Length];
                    for (int layer = 0; layer < dlayer.Length; ++layer)
                        for (int k = 0; k < Patch.Count; k++)
                            result[layer] += Patch[k].nit_tot[layer] * Patch[k].RelativeArea;
                }
                return result;
            }
        }

        #endregion

        #region Nitrogen balance

        /// <summary>Balance of nitrogen: deltaN - losses</summary>
        /// <value>The nitrogenbalance.</value>

        double nitrogenbalance
        {
            get
            {
                double deltaN = SumDoubleArray(TotalN) - dailyInitialN;  // variation in N today
                double losses = SumDoubleArray(dlt_nh4_dnit) + SumDoubleArray(dlt_no3_dnit);
                return -(losses + deltaN);
                // why leaching losses are not accounted and what about the inputs?
            }
        }

        #endregion

        #endregion

        #region Outputs for Carbon

        #region General values

        /// <summary>Number of fom types read - is this really needed?</summary>
        /// <value>The num_fom_types.</value>

        private int num_fom_types
        { get { return fom_types.Length; } }

        /// <summary>Carbohydrate fraction of FOM (0-1)</summary>
        /// <value>The fr_carb.</value>

        private double fr_carb
        { get { return fract_carb[fom_type]; } }

        /// <summary>Cellulose fraction of FOM (0-1)</summary>
        /// <value>The fr_cell.</value>

        private double fr_cell
        { get { return fract_cell[fom_type]; } }

        /// <summary>Lignin fraction of FOM (0-1)</summary>
        /// <value>The fr_lign.</value>

        private double fr_lign
        { get { return fract_lign[fom_type]; } }

        #endregion

        #region Changes for today - deltas

        /// <summary>Carbon loss in sediment, via runoff/erosion</summary>
        /// <value>The dlt_c_loss_in_sed.</value>

        [Units("kg")]
        double dlt_c_loss_in_sed
        {
            get
            {
                double result = 0.0;
                for (int k = 0; k < Patch.Count; k++)
                    result += Patch[k].dlt_c_loss_in_sed * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C converted from FOM to humic (kg/ha)</summary>
        /// <value>The dlt_fom_c_hum.</value>

        [Units("kg/ha")]
        double[] dlt_fom_c_hum
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_fom_c_hum[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C converted from FOM to m. biomass (kg/ha)</summary>
        /// <value>The dlt_fom_c_biom.</value>

        [Units("kg/ha")]
        double[] dlt_fom_c_biom
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_fom_c_biom[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C lost to atmosphere from FOM</summary>
        /// <value>The dlt_fom_c_atm.</value>

        [Units("kg/ha")]
        double[] dlt_fom_c_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_fom_c_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Humic C converted to biomass</summary>
        /// <value>The dlt_hum_c_biom.</value>

        [Units("kg/ha")]
        double[] dlt_hum_c_biom
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_hum_2_biom[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Humic C lost to atmosphere</summary>
        /// <value>The dlt_hum_c_atm.</value>

        [Units("kg/ha")]
        double[] dlt_hum_c_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_hum_2_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Biomass C converted to humic</summary>
        /// <value>The dlt_biom_c_hum.</value>

        [Units("kg/ha")]
        double[] dlt_biom_c_hum
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_biom_2_hum[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Biomass C lost to atmosphere</summary>
        /// <value>The dlt_biom_c_atm.</value>

        [Units("kg/ha")]
        double[] dlt_biom_c_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_c_biom_2_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Carbon from residues converted to biomass C</summary>
        /// <value>The dlt_res_c_biom.</value>

        [Units("kg/ha")]
        double[] dlt_res_c_biom
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_res_c_biom[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Carbon from residues converted to humic C</summary>
        /// <value>The dlt_res_c_hum.</value>

        [Units("kg/ha")]
        double[] dlt_res_c_hum
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_res_c_hum[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Carbon from residues lost to atmosphere</summary>
        /// <value>The dlt_res_c_atm.</value>

        [Units("kg/ha")]
        double[] dlt_res_c_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_res_c_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Delta C in pool 1 of FOM</summary>
        /// <value>The dlt_fom_c_pool1.</value>

        [Units("kg/ha")]
        double[] dlt_fom_c_pool1
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_fom_c_pool1[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Delta C in pool 2 of FOM</summary>
        /// <value>The dlt_fom_c_pool2.</value>

        [Units("kg/ha")]
        double[] dlt_fom_c_pool2
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_fom_c_pool2[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Delta C in pool 3 of FOM</summary>
        /// <value>The dlt_fom_c_pool3.</value>

        [Units("kg/ha")]
        double[] dlt_fom_c_pool3
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].dlt_fom_c_pool3[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Carbon lost from all residues to atmosphere</summary>
        /// <value>The soilp_dlt_res_c_atm.</value>

        [Units("kg/ha")]
        double[] soilp_dlt_res_c_atm
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].soilp_dlt_res_c_atm[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Carbon from all residues to humic pool</summary>
        /// <value>The soilp_dlt_res_c_hum.</value>

        [Units("kg/ha")]
        double[] soilp_dlt_res_c_hum
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].soilp_dlt_res_c_hum[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Carbon from all residues to m. biomass</summary>
        /// <value>The soilp_dlt_res_c_biom.</value>

        [Units("kg/ha")]
        double[] soilp_dlt_res_c_biom
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].soilp_dlt_res_c_biom[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        #endregion

        #region Amounts in various pools

        /// <summary>Fresh organic C - FOM</summary>
        /// <value>The fom_c.</value>

        [Units("kg/ha")]
        public double[] FOMC
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C in pool 1 of FOM</summary>
        /// <value>The fom_c_pool1.</value>

        [Units("kg/ha")]
        double[] fom_c_pool1
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c_pool1[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C in pool 2 of FOM</summary>
        /// <value>The fom_c_pool2.</value>

        [Units("kg/ha")]
        double[] fom_c_pool2
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c_pool2[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C in pool 3 of FOM</summary>
        /// <value>The fom_c_pool3.</value>

        [Units("kg/ha")]
        double[] fom_c_pool3
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].fom_c_pool3[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C in humic pool</summary>
        /// <value>The hum_c.</value>

        [Units("kg/ha")]
        public double[] HumicC
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].hum_c[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C in inert humic pool</summary>
        /// <value>The inert_c.</value>

        [Units("kg/ha")]
        public double[] InertC
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].inert_c[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Amount of C in m. biomass pool</summary>
        /// <value>The biom_c.</value>

        [Units("kg/ha")]
        public double[] MicrobialC
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].biom_c[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Total carbon amount in the soil</summary>
        /// <value>The carbon_tot.</value>

        [Units("kg/ha")]
        public double[] TotalC
        {
            get
            {
                double[] result = null;
                if (dlayer != null)
                {
                    result = new double[dlayer.Length];
                    for (int layer = 0; layer < dlayer.Length; ++layer)
                        for (int k = 0; k < Patch.Count; k++)
                            result[layer] += Patch[k].carbon_tot[layer] * Patch[k].RelativeArea;
                }
                return result;
            }
        }

        #endregion

        #region Carbon Balance

        /// <summary>Balance of C in soil: deltaC - losses</summary>
        /// <value>The carbonbalance.</value>

        double carbonbalance
        {
            get
            {
                double deltaC = SumDoubleArray(TotalC) - dailyInitialC;     // variation in C today
                double losses = SumDoubleArray(dlt_res_c_atm) + SumDoubleArray(dlt_fom_c_atm) + SumDoubleArray(dlt_hum_c_atm) + SumDoubleArray(dlt_biom_c_atm);
                return -(losses + deltaC);
            }
        }

        #endregion

        #endregion

        #region Factors and other outputs

        /// <summary>amount of P coverted by residue mineralisation</summary>
        /// <value>The soilp_dlt_org_p.</value>

        [Units("kg/ha")]
        double[] soilp_dlt_org_p
        {
            get
            {
                double[] result = new double[dlayer.Length];
                for (int layer = 0; layer < dlayer.Length; ++layer)
                    for (int k = 0; k < Patch.Count; k++)
                        result[layer] += Patch[k].soilp_dlt_org_p[layer] * Patch[k].RelativeArea;
                return result;
            }
        }

        /// <summary>Soil temperature (oC), values actually used in the model</summary>
        private double[] Tsoil;

        /// <summary>SoilN's simple soil temperature</summary>
        /// <value>The st.</value>

        [Units("oC")]
        public double[] st
        {
            get
            {
                double[] Result = new double[0];
                if (!use_external_st)
                    Result = Tsoil;
                return Result;
            }
        }

        /// <summary>Temperature factor for nitrification and mineralisation</summary>
        /// <value>The tf.</value>

        double[] tf
        {
            get
            {
                double[] result = new double[dlayer.Length];
                // RCichota: deactivated
                //int index = (!is_pond_active) ? 1 : 2;
                //for (int layer = 0; layer < dlayer.Length; layer++)
                //    result[layer] = (soiltype == "rothc") ? RothcTF(layer, index) : TF(layer, index);
                return result;
            }
        }

        /// <summary>Number of internal patches</summary>
        /// <value>The number patches.</value>

        int numPatches
        { get { return Patch.Count; } }

        /// <summary>Relative area of each internal patch</summary>
        /// <value>The patch area.</value>

        double[] PatchArea
        {
            get
            {
                double[] result = new double[Patch.Count];
                for (int k = 0; k < Patch.Count; k++)
                    result[k] = Patch[k].RelativeArea;
                return result;
            }
        }


        /// <summary>The patches data</summary>
        private Dictionary<string, double[]> PatchesData = new Dictionary<string, double[]>();

        /// <summary>Relative area of each internal patch</summary>
        /// <value>The cn patch_no3.</value>

        public double[][] CNPatch_no3
        {
            get
            {
                double[][] result = new double[Patch.Count][];
                for (int k = 0; k < Patch.Count; k++)
                {
                    //result[k] = new double[2];
                    result[k] = Patch[k].no3;
                    //                result[k][1] = Patch[k].hum_c;
                }
                return result;
            }
        }

        /// <summary>Relative area of each internal patch</summary>
        /// <value>The cn patch_hum_c.</value>

        public double[][] CNPatch_hum_c
        {
            get
            {
                double[][] result = new double[Patch.Count][];
                for (int k = 0; k < Patch.Count; k++)
                {
                    //result[k] = new double[2];
                    result[k] = Patch[k].hum_c;
                    //                result[k][1] = Patch[k].hum_c;
                }
                return result;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Useful constants

        /// <summary>Value to evaluate precision against floating point variables</summary>
        private double EPSILON = Math.Pow(2, -24);

        #endregion

        #region Internal variables

        #region Components

        /// <summary>List of all existing patches (internal instances of C and N processes)</summary>
        List<soilCNPatch> Patch;

        /// <summary>The internal soil temp module - to be avoided (deprecated)</summary>
        private simpleSoilTemp simpleST;

        #endregion

        #region Decision auxiliary variables

        /// <summary>Marker for whether initialisation has been finished or not</summary>
        private bool initDone = false;

        /// <summary>Marker for whether a reset is going on</summary>
        private bool inReset = false;

        /// <summary>Marker for whether external soil temperature is supplied, otherwise use internal</summary>
        private bool use_external_st = false;

        /// <summary>Marker for whether external ph is supplied, otherwise default is used</summary>
        private bool use_external_ph = false;

        /// <summary>
        /// Marker for whether there is pond water, decomposition of surface OM will be done by that model
        /// </summary>
        private bool is_pond_active = false;

        #endregion

        #region Miscelaneous

        /// <summary>Total C content at the beginning of the day</summary>
        private double dailyInitialC;

        /// <summary>Total N content at the beginning of the day</summary>
        private double dailyInitialN;

        /// <summary>Type of fom</summary>
        private int fom_type;

        /// <summary>Number of surface residues whose decomposition is being calculated</summary>
        private int num_residues = 0;

        #endregion

        #region Parameters related to computing approaches

        /// <summary>Approach to be used when computing urea hydrolysis</summary>
        UreaHydrolysisApproaches UreaHydrolysisApproach = UreaHydrolysisApproaches.APSIMdefault;

        /// <summary>Approach to be used when computing nitrification</summary>
        NitrificationApproaches NitrificationApproach = NitrificationApproaches.APSIMdefault;

        /// <summary>Approach to be used when computing denitrification</summary>
        DenitrificationApproaches DenitrificationApproach = DenitrificationApproaches.APSIMdefault;

        #endregion

        #endregion

        #region Types and structures

        /// <summary>List with patch ids to merge</summary>
        [Serializable]
        private struct PatchIDs
        {

            /// <summary>IDs of disappearing patches</summary>
            public List<int> disappearing;
            /// <summary>IDs of patches receiving the area and status of disappearing patches</summary>
            public List<int> recipient;
        }

        /// <summary>
        /// The parameters to compute a exponential type function (used for example for temperature factor)
        /// </summary>
        [Serializable]
        private struct BentStickData
        {
            // this is a bending stick type data

            /// <summary>Optimum temperature, when factor is equal to one</summary>
            public double[] xValueForOptimum;
            /// <summary>Value of factor when temperature is equal to zero celsius</summary>
            public double[] yValueAtZero;
            /// <summary>Exponent defining the curvature of the factors</summary>
            public double[] CurveExponent;
        }

        /// <summary>
        /// Lists of x and y values used to describe certain a 'broken stick' function (e.g. moisture factor)
        /// </summary>
        [Serializable]
        private struct BrokenStickData
        {
            /// <summary>The values in the x-axis</summary>
            public double[] xVals;
            /// <summary>The values in the y-axis</summary>
            public double[] yVals;
        }

        /// <summary>List of approaches available for computing urea hydrolysis</summary>
        private enum UreaHydrolysisApproaches { APSIMdefault, RCichota };

        /// <summary>List of approaches available for computing nitrification</summary>
        private enum NitrificationApproaches { APSIMdefault, RCichota };

        /// <summary>List of approaches available for computing denitrification</summary>
        private enum DenitrificationApproaches { APSIMdefault, RCichota };

        #endregion

    }


}