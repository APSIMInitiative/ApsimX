using static Models.GrazPlan.GrazType;

namespace Models.GrazPlan
{
    using APSIM.Shared.Utilities;
    using DocumentFormat.OpenXml.EMMA;
    using MathNet.Numerics.Random;
    using Models.Climate;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Surface;
    using StdUnits;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Models.Core;
    using Models.Interfaces;
    using static Models.GrazPlan.GrazType;
    using CMPServices;

    public class TSoilInstance : Core.Model
    {
        protected int FNoLayers;                                                        // Layer profile used by this component
        protected double[] FLayerProfile = new double[GrazType.MaxSoilLayers + 1];      // [1..  [0] is unused

        protected int FNoInputLayers;                                                   // Temporary arrays                      

        protected double[] FInputProfile = new double[GrazType.MaxSoilLayers + 1];      // [1..
        protected double[] FLayerValues = new double[GrazType.MaxSoilLayers + 1];       // [1..
        protected double[] FSoilValues = new double[GrazType.MaxSoilLayers + 1];        // [SURFACE..MaxSoilLayers] SURFACE = 0
        protected double[] FSoilDepths = new double[GrazType.MaxSoilLayers + 1];        // [SURFACE..MaxSoilLayers]
        protected double[] FLayer2Soil = new double[GrazType.MaxSoilLayers + 1];        // [1..
    }

    /// <summary>
    /// # Pasture
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MarkdownView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Pasture : TSoilInstance
    {
        private TPasturePopulation PastureModel;
        private TWeatherHandler FWeather;
        private TPastureInputs FInputs;

        private double[] F_BulkDensity = new double[GrazType.MaxSoilLayers + 1];    // [1..
        private double[] F_SandPropn = new double[GrazType.MaxSoilLayers + 1];      // [1..
        private double[] F_DUL = new double[GrazType.MaxSoilLayers + 1];            // [1..
        private double[] F_LL15 = new double[GrazType.MaxSoilLayers + 1];           // [1..

        private double FFieldArea;
        private double FHarvestHeight;
        private int FToday;             // StdDate.Date
        private double FFertility;
        private double FIntercepted;

        #region Class links
        /// <summary>
        /// The simulation clock
        /// </summary>
        [Link]
        private Clock systemClock = null;

        /// <summary>
        /// The simulation weather component
        /// </summary>
        [Link]
        private IWeather locWtr = null;

        /// <summary>
        /// The supplement component
        /// </summary>
        [Link(IsOptional = true)]
        private Supplement suppFeed = null;

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary outputSummary = null;

        [Link]
        private List<Zone> paddocks = null;

        #endregion


        /// <summary>
        /// The Pasture class constructor
        /// </summary>
        public Pasture() : base()
        {
            FInputs = new TPastureInputs();

        }

        #region Initialisation properties ====================================================

        /// <summary>
        /// Name of the pasture species for which parameters are to be used
        /// </summary>
        public string Species { get; set; }

        /// <summary>
        /// Determines which of the plant nutrient submodels to activate.
        /// Feasible values are:
        /// 
        /// '' - 'Simple' nutrient mode,
        /// 'N' - Nitrogen submodel only, 
        /// 'NP' - Nitrogen and phosphorus submodels, 
        /// 'NS' - Nitrogen and sulphur submodels, 
        /// 'NPS' - All three plant nutrients.
        /// </summary>
        public string Nutrients { get; set; } = "N";

        /// <summary>
        /// Fertility scalar. Only meaningful in 'simple' mode. Default is 1.0.
        /// </summary>
        public double Fertility { get; set; } = 1.0;

        /// <summary>
        /// Depth of each soil layer referenced in specifying root and seed pools. 
        /// Must be given if soil profiles for root or seed pools are given, 
        /// otherwise the profile depths will be requested (as layers) from the rest of the simulation.
        /// mm
        /// </summary>
        public double[] Layers { get; set; }

        /// <summary>
        /// Maximum rooting depth. 
        /// The default value is calculated from soil bulk density and sand content
        /// mm
        /// </summary>
        public double MaxRtDep { get; set; }

        /// <summary>
        /// Lagged daytime temperature.
        /// Default value is -999.9, which denotes that the value of daytime 
        /// temperature in the first time step should be used
        /// oC
        /// </summary>
        public double LaggedDayT { get; set; } = -999.9;

        /// <summary>
        /// Value denoting the phenological stage of the species.
        /// (0-1) Vernalizing. 
        /// (1-2) Vegetative. 
        /// (2-3) Reproductive. 
        /// (3-4) Summer-dormant perennials. 
        /// (4.0) Senescent annuals. 
        /// (5-6) Spray-topped. 
        /// (6.0) Winter-dormant perennials. See Help for more info.
        /// </summary>
        public double Phenology { get; set; }

        /// <summary>
        /// Current maximum length of the flowering period.
        /// Ignored if the species is modelled with no seed pools or the phenological stage is not reproductive.  
        /// Default depends on the phenological stage;
        /// d
        /// </summary>
        public double FlowerLen { get; set; }

        /// <summary>
        /// Time since the start of flowering.
        /// Default depends on the phenological stage.
        /// d
        /// </summary>
        public double FlowerTime { get; set; }

        /// <summary>
        /// Number of preceding days of 'dry' conditions.
        /// Only meaningful if the pasture population is vulnerable to senescence. Default value is 0
        /// d
        /// </summary>
        public double SencIndex { get; set; } = 0;

        /// <summary>
        /// Number of preceding days of 'cool', moist conditions.
        /// Only meaningful if the pasture population is summer-dormant. Default value is 0
        /// d
        /// </summary>
        public int DormIndex { get; set; } = 0;

        /// <summary>
        /// Lagged mean temperature used in summer-dormancy calculations.
        /// Default value is -999.9, which denotes that the value of mean temperature in the first time step should be used.
        /// oC
        /// </summary>
        public double DormT { get; set; } = -999.9;

        /// <summary>
        /// Apparent extinction coefficients.
        /// Apparent extinction coefficients of seedlings, established plants and senescing plants.
        /// </summary>
        public double[] ExtinctCoeff { get; set; }

        /// <summary>
        /// Each element specifies the state of a cohort of green (living) herbage
        /// </summary>
        public GreenInit[] Green { get; set; }

        /// <summary>
        /// Each element specifies the state of a cohort of dry herbage (standing dead or litter)
        /// </summary>
        public DryInit[] Dry { get; set; }

        /// <summary>
        /// Mass of seeds in each soil layer
        /// </summary>
        public SeedInit Seeds { get; set; }

        /// <summary>
        /// Time since commencement of embryo dormancy.
        /// Only meaningful if unripe seeds are present. Default is 0.0
        /// d
        /// </summary>
        public double SeedDormTime { get; set; } = 0;

        /// <summary>
        /// Germination index.
        /// Only meaningful if the species is modelled with seed pools. Default is 0.0
        /// d
        /// </summary>
        public double GermIndex { get; set; } = 0;

        /// <summary>
        /// Rate parameter for the optional Monteith water uptake sub-model
        /// /d
        /// </summary>
        public double[] KL { get; set; } // [1..

        /// <summary>
        /// Minimum water content parameter for the optional Monteith water uptake sub-model
        /// mm/mm
        /// </summary>
        public double[] LL { get; set; } // [1..

        #endregion

        #region Readable properties ====================================================
        /*
         Data exchange vars
            AddOneVariable(ref Idx, PastureProps.prpCANOPY, "canopy", PastureProps.typeCANOPY, "Canopy characteristics of a child APSIM-Plant module. The array has one member per sub-canopy", "");
            AddOneVariable(ref Idx, PastureProps.prpWATER_INFO, "water_info", PastureProps.typeWATER_INFO, "Water demand and supply attributes of a child APSIM-Plant module (one member per sub-population)", "");
            AddOneVariable(ref Idx, PastureProps.prpPLANT2STOCK, "plant2stock", PastureProps.typePLANT2STOCK, "Description of the pasture for use by the ruminant model", "");
            AddOneVariable(ref Idx, PastureProps.prpGAI, "gai", "<type kind=\"double\" unit=\"m^2/m^2\"/>", "Green area index", "");
            AddOneVariable(ref Idx, PastureProps.prpDAI, "dai", "<type kind=\"double\" unit=\"m^2/m^2\"/>", "Dead area index", "");
            AddOneVariable(ref Idx, PastureProps.prpCOVER_G, "cover_green", "<type kind=\"double\" unit=\"m^2/m^2\"/>", "Green cover", "");
            AddOneVariable(ref Idx, PastureProps.prpCOVER_T, "cover_tot", "<type kind=\"double\" unit=\"m^2/m^2\"/>", "Total cover", "");
            AddOneVariable(ref Idx, PastureProps.prpCOVER_R, "residue_cover", "<type kind=\"double\" unit=\"m^2/m^2\"/>", "Cover of standing dead and litter", "");
            AddOneVariable(ref Idx, PastureProps.prpHEIGHT, "height", "<type kind=\"double\" unit=\"mm\"/>", "Average height of the pasture", "");
            AddOneVariable(ref Idx, PastureProps.prpNH4_UPTAKE, "nh4_uptake", PastureProps.typeNUTR_UPTAKE, "Ammonium-N uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpNO3_UPTAKE, "no3_uptake", PastureProps.typeNUTR_UPTAKE, "Nitrate-N uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpPOX_UPTAKE, "pox_uptake", PastureProps.typeNUTR_UPTAKE, "Phosphate-P uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpSO4_UPTAKE, "so4_uptake", PastureProps.typeNUTR_UPTAKE, "Sulphate-S uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpWATERPARAMS, "water_params", "<type kind=\"double\" unit=\"\"      array=\"T\"/>", "Parameters used by the Paddock component to determine water uptake", "");
            AddOneVariable(ref Idx, PastureProps.prpWATERDEMAND, "WaterDemand", "<type kind=\"double\" unit=\"mm\"/>", "Total water demand", "");
            //AddOneVariable(ref Idx, PastureProps.prpAVAILANIMAL, "availabletoanimal",typeCOHORTAVAIL,                                   "Characteristics of herbage available for defoliation",   "");
        */
        // readable properties

        /// <summary>
        /// Total dry weight of all herbage
        /// </summary>
        [Units("kg/ha")]
        public double ShootDM { get; }


        /// <summary>
        /// Dry weight of herbage of seedlings in each digestibility class
        /// </summary>
        [Units("kg/ha")]
        public double[] shootDMq { get; }

        /// <summary>
        /// Average DM digestibility of all herbage
        /// </summary>
        [Units("g/g")]
        public double ShootDMD { get; }

        /// <summary>
        /// Average crude protein content of all herbage
        /// </summary>
        [Units("g/g")]
        public double ShootCP { get; }

        /// <summary>
        /// Average nitrogen content of all herbage
        /// </summary>
        [Units("g/g")]
        public double ShootN { get; }

        /// <summary>
        /// Average phosphorous content of all herbage
        /// </summary>
        [Units("g/g")]
        public double ShootP { get; }

        /// <summary>
        /// Average sulphur content of all herbage
        /// </summary>
        [Units("g/g")]
        public double ShootS { get; }

        /// <summary>
        /// Total dry weight of seedlings
        /// </summary>
        [Units("kg/ha")]
        public double SeedlDM { get; }

        /// <summary>
        /// Dry weight of herbage of seedlings in each digestibility class
        /// </summary>
        [Units("kg/ha")]
        public double[] SeedlMDq { get; }

        /// <summary>
        /// Average DM digestibility of seedlings
        /// </summary>
        [Units("g/g")]
        public double SeedlDMD { get; }

        /// <summary>
        /// Average crude protein content of seedlings
        /// </summary>
        [Units("g/g")]
        public double SeedlCP { get; }

        /// <summary>
        /// Average nitrogen content of seedlings
        /// </summary>
        [Units("g/g")]
        public double SeedlN { get; }
        /// <summary>
        /// Average phosphorus content of seedlings
        /// </summary>
        [Units("g/g")]
        public double SeedlP { get; }
        /// <summary>
        /// Average sulphur content of seedlings
        /// </summary>
        [Units("g/g")]
        public double SeedlS { get; }

        /*        
            AddOneVariable(ref Idx, PastureProps.prpESTAB_DM, "estab_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of herbage of established plants", "");
            AddOneVariable(ref Idx, PastureProps.prpESTAB_Q, "estab_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of herbage of established plants in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpESTAB_DMD, "estab_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of herbage of established plants", "");
            AddOneVariable(ref Idx, PastureProps.prpESTAB_CP, "estab_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of herbage of established plants.", "");
            AddOneVariable(ref Idx, PastureProps.prpESTAB_N, "estab_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of herbage of established plants", "");
            AddOneVariable(ref Idx, PastureProps.prpESTAB_P, "estab_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of herbage of established plants", "");
            AddOneVariable(ref Idx, PastureProps.prpESTAB_S, "estab_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of herbage of established plants", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_DM, "senc_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of herbage of senescing plants", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_Q, "senc_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of herbage of senescing plants in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_DMD, "senc_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of herbage of senescing plants", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_CP, "senc_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of herbage of senescing plants", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_N, "senc_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of seedlings", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_P, "senc_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of seedlings", "");
            AddOneVariable(ref Idx, PastureProps.prpSENC_S, "senc_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of seedlings", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_DM, "dead_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of standing dead herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_Q, "dead_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of standing dead herbage in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_DMD, "dead_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of standing dead herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_CP, "dead_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of standing dead herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_N, "dead_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of standing dead herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_P, "dead_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of standing dead herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDEAD_S, "dead_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of standing dead herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_DM, "litter_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of litter", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_Q, "litter_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of herbage of litter in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_DMD, "litter_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of litter", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_CP, "litter_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of litter", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_N, "litter_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of litter", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_P, "litter_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of litter", "");
            AddOneVariable(ref Idx, PastureProps.prpLITT_S, "litter_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of litter", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_DM, "green_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of green herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_Q, "green_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of green herbage in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_DMD, "green_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of green herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_CP, "green_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of green herbage (seedlings+established+senescing)", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_N, "green_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of green herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_P, "green_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of green herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_S, "green_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of green herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_DM, "dry_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of dry herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_Q, "dry_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of dry herbage in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_DMD, "dry_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of dry herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_CP, "dry_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of dry herbage (standing dead+litter)", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_N, "dry_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of dry herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_P, "dry_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of dry herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpDRY_S, "dry_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of dry herbage", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_DM, "leaf_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of all leaves", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_Q, "leaf_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of all leaves in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_DMD, "leaf_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of all leaves", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_CP, "leaf_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of all leaves", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_N, "leaf_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of all leaves", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_P, "leaf_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of all leaves", "");
            AddOneVariable(ref Idx, PastureProps.prpLEAF_S, "leaf_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of all leaves", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_DM, "stem_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of all stems", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_Q, "stem_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of all stems in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_DMD, "stem_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of all stems", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_CP, "stem_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of all stems", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_N, "stem_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of all stems", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_P, "stem_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of all stems", "");
            AddOneVariable(ref Idx, PastureProps.prpSTEM_S, "stem_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of all stems", "");
            AddOneVariable(ref Idx, PastureProps.prpAVAIL_DM, "avail_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of herbage available for grazing", "");
            AddOneVariable(ref Idx, PastureProps.prpAVAIL_Q, "avail_dm_q", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of herbage available for grazing in each digestibility class", "");
            AddOneVariable(ref Idx, PastureProps.prpAVAIL_DMD, "avail_dmd", "<type kind=\"double\" unit=\"g/g\"/>", "Average DM digestibility of herbage available for grazing", "");
            AddOneVariable(ref Idx, PastureProps.prpAVAIL_CP, "avail_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of herbage available for grazing", "");
            AddOneVariable(ref Idx, PastureProps.prpAVAIL_G, "avail_green", "<type kind=\"double\" unit=\"kg/ha\"/>", "Weight of green (seedling+established+senescing) herbage available for grazing", "");
            AddOneVariable(ref Idx, PastureProps.prpAVAIL_D, "avail_dry", "<type kind=\"double\" unit=\"kg/ha\"/>", "Weight of dry (standing dead+litter) herbage available for grazing", "");
            AddOneVariable(ref Idx, PastureProps.prpGREENPROFILE, "green_profile", PastureProps.typeHERBAGEPROFILE, "Height profile of green herbage (by plant parts)", "");
            AddOneVariable(ref Idx, PastureProps.prpSHOOTPROFILE, "shoot_profile", PastureProps.typeHERBAGEPROFILE, "Height profile of all herbage (by plant parts)", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_DM, "root_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of all roots", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_PROF, "root_dm_dep", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of all roots in each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_N, "root_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of all roots", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_PROF_N, "root_n_dep", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Average nitrogen content of roots in each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_P, "root_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of all roots", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_PROF_P, "root_p_dep", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Average phosphorus content of roots in each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_S, "root_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of all roots", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_PROF_S, "root_s_dep", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Average sulphur content of roots in each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpEFFR_DM, "eff_root_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of effective roots", "");
            AddOneVariable(ref Idx, PastureProps.prpEFFR_PROF, "eff_root_dm_dep", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Dry weight of effective roots in each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpLAI, "lai", "<type kind=\"double\" unit=\"m^2/m^2\"/>", "Green leaf area index", "");
            AddOneVariable(ref Idx, PastureProps.prpRTDEP, "rtdep", "<type kind=\"double\" unit=\"mm\"/>", "Current depth of the rooting front", "");
            AddOneVariable(ref Idx, PastureProps.prpROOT_RAD, "root_radius", "<type kind=\"double\" unit=\"mm\"/>", "Average radius of all roots", "");
            AddOneVariable(ref Idx, PastureProps.prpRLV, "rlv", "<type kind=\"double\" unit=\"mm/mm^3\" array=\"T\"/>", "Length density of effective roots in each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpSEED_DM, "seed_dm", "<type kind=\"double\" unit=\"kg/ha\"/>", "Total dry weight of seeds in all soil layers", "");
            AddOneVariable(ref Idx, PastureProps.prpSEED_CP, "seed_cp", "<type kind=\"double\" unit=\"g/g\"/>", "Average crude protein content of seeds", "");
            AddOneVariable(ref Idx, PastureProps.prpSEED_N, "seed_n", "<type kind=\"double\" unit=\"g/g\"/>", "Average nitrogen content of seeds", "");
            AddOneVariable(ref Idx, PastureProps.prpSEED_P, "seed_p", "<type kind=\"double\" unit=\"g/g\"/>", "Average phosphorus content of seeds", "");
            AddOneVariable(ref Idx, PastureProps.prpSEED_S, "seed_s", "<type kind=\"double\" unit=\"g/g\"/>", "Average sulphur content of seeds", "");
            AddOneVariable(ref Idx, PastureProps.prpEST_IDX, "est_index", "<type kind=\"double\" unit=\"-\"/>", "Weighted average value of the establishment index for seedlings", "");
            AddOneVariable(ref Idx, PastureProps.prpSTRESS_IDX, "stress_index", "<type kind=\"double\" unit=\"-\"/>", "Weighted average value of the seedling stress index", "");
            AddOneVariable(ref Idx, PastureProps.prpRWU, "sw_uptake", "<type kind=\"double\" unit=\"mm\" array=\"T\"/>", "Water uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpNPP, "npp", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Whole-plant net primary productivity", "");
            AddOneVariable(ref Idx, PastureProps.prpNPP_SHOOT, "shoot_npp", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Net primary productivity of shoots", "");
            AddOneVariable(ref Idx, PastureProps.prpASSIM, "assimilation", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Gross whole-plant assimilation rate", "");
            AddOneVariable(ref Idx, PastureProps.prpRESPIRE, "respiration", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Whole-plant respiration rate, in dry weight equivalent terms", "");
            AddOneVariable(ref Idx, PastureProps.prpGROWTH, "growth", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Daily shoot growth rate", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_GAI, "glf_gai", "<type kind=\"double\" unit=\"-\"/>", "Light interception growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_VPD, "glf_vpd", "<type kind=\"double\" unit=\"-\"/>", "VPD growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_SM, "glf_sm", "<type kind=\"double\" unit=\"-\"/>", "Soil moisture growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_TMP, "glf_tmp", "<type kind=\"double\" unit=\"-\"/>", "Temperature growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_WLOG, "glf_wl", "<type kind=\"double\" unit=\"-\"/>", "Waterlogging growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_N, "glf_nitr", "<type kind=\"double\" unit=\"-\"/>", "Nitrogen growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_P, "glf_phos", "<type kind=\"double\" unit=\"-\"/>", "Phosphorus growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_S, "glf_sulf", "<type kind=\"double\" unit=\"-\"/>", "Sulphur growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpGLF_NUTR, "glf_nutr", "<type kind=\"double\" unit=\"-\"/>", "Nutrient growth-limiting factor", "");
            AddOneVariable(ref Idx, PastureProps.prpNFIX, "n_fixed", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Nitrogen fixation rate", "");
            AddOneVariable(ref Idx, PastureProps.prpUPT_NH4, "uptake_nh4", "<type kind=\"double\" unit=\"kg/ha/d\" array=\"T\"/>", "Ammonium-N uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpUPT_NO3, "uptake_no3", "<type kind=\"double\" unit=\"kg/ha/d\" array=\"T\"/>", "Nitrate-N uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpUPT_POX, "uptake_pox", "<type kind=\"double\" unit=\"kg/ha/d\" array=\"T\"/>", "Phosphate-P uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpUPT_SO4, "uptake_so4", "<type kind=\"double\" unit=\"kg/ha/d\" array=\"T\"/>", "Sulphate-S uptake from each soil layer", "");
            AddOneVariable(ref Idx, PastureProps.prpALLOCATION, "allocation", PastureProps.typeALLOCATION, "Allocation of assimilate to each plant part", "");
            AddOneVariable(ref Idx, PastureProps.prpRESIDUE, "residues", PastureProps.typeRESIDUES, "Dry weight and quality of residues incorporated into the soil in this time step (one member per soil layer)", "");
            AddOneVariable(ref Idx, PastureProps.prpRESID_LEAF, "leaf_residues", PastureProps.typeRESIDUE, "Dry weight and quality of leaf residues incorporated into the soil in this time step", "");
            AddOneVariable(ref Idx, PastureProps.prpRESID_STEM, "stem_residues", PastureProps.typeRESIDUE, "Dry weight and quality of stem residues incorporated into the soil in this time step", "");
            AddOneVariable(ref Idx, PastureProps.prpRESID_ROOT, "root_residues", PastureProps.typeRESIDUES, "Dry weight and quality of root residues incorporated into the soil in this time step (one member per soil layer)", "");
            AddOneVariable(ref Idx, PastureProps.prpLEACHATE, "leachate", PastureProps.typeLEACHATE, "Mass of organic nutrients leached from dead pasture and litter by rainfall", "");
            AddOneVariable(ref Idx, PastureProps.prpGAS_N_LOSS, "n_gas_loss", "<type kind=\"double\" unit=\"kg/ha/d\"/>", "Rate of volatilization of tissue N into the atmosphere", "");
            AddOneVariable(ref Idx, PastureProps.prpGREEN_BD, "green_bd", "<type kind=\"double\" unit=\"kg/m^3\"/>", "Herbage bulk density of green shoots", "");
            AddOneVariable(ref Idx, PastureProps.prpPHEN_HRZ, "pheno_horizon", "<type kind=\"double\" unit=\"-\"       array=\"T\"/>", "Relative height of two 'horizons' affecting the impact of defoliation on phenology", "");
            AddOneVariable(ref Idx, PastureProps.prpDEFOLIATION, "defoliation", PastureProps.typeDEFOLIATION, "Amount of herbage defoliated", "Amount of herbage defoliated from each of green/standing dead/litter (1st index) x leaf/stem (2nd index) x DMD class (3rd index, 1=DMD 80-85pc, 12=DMD 35-40pc)");
            AddOneVariable(ref Idx, PastureProps.prpLOSS_GREEN, "death_rate", PastureProps.typeLOSS_RATE, "Rate of death of green herbage", "Rate of death of green herbage from each of leaf/stem (1st index) x DMD class (2rd index, 1=DMD 80-85pc, 12=DMD 35-40pc). Does not include defoliation or death due to kill, cultivate or cut events");
            AddOneVariable(ref Idx, PastureProps.prpLOSS_DEAD, "fall_rate", PastureProps.typeLOSS_RATE, "Rate of fall of standing dead herbage", "Rate of fall of standing dead herbage from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc)");
            AddOneVariable(ref Idx, PastureProps.prpLOSS_LITTER, "incorp_rate", PastureProps.typeLOSS_RATE, "Rate of incorporation of litter", "Rate of incorporation of litter from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc)");
            AddOneVariable(ref Idx, PastureProps.prpKILLED, "killed", "<type kind=\"double\" unit=\"kg/ha\" array=\"T\"/>", "Amount of death of green herbage", "Amount of death of green herbage as a result of kill or cultivate events from each of leaf and stem");


         */
        #endregion

        #region Subscribed events ====================================================

        /// <summary>
        /// At the start of the simulation.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            FWeather = new TWeatherHandler();
            PastureModel = new TPasturePopulation();
            PastureModel.MassUnit = "g/m^2";

            PastureModel.ReadParamsFromValues(this.Nutrients, this.Species, this.Fertility, this.MaxRtDep, this.KL, this.LL); // initialise the model with initial values

            double[] fCampbell = new double[GrazType.MaxSoilLayers + 1];
            for (int Ldx = 1; Ldx <= FNoLayers; Ldx++)
            {
                fCampbell[Ldx] = Math.Log(15.0 / (0.1)) / Math.Log(F_DUL[Ldx] / F_LL15[Ldx]);
            }

            if (PastureModel != null)
            {
                PastureModel.SetSoilParams(FLayerProfile, F_BulkDensity, F_SandPropn, fCampbell);
                if (PastureModel.fPlant_LL[1] == 0.0)
                {
                    PastureModel.fPlant_LL = F_LL15;
                }

                PastureModel.ReadStateFromValues(this.LaggedDayT, this.Phenology, this.FlowerLen, this.FlowerTime, this.SencIndex, this.DormIndex, this.DormT, this.ExtinctCoeff, this.Green, this.Dry, this.Seeds, this.SeedDormTime, this.GermIndex);

                int currentDay = systemClock.Today.Day + (systemClock.Today.Month * 0x100) + (systemClock.Today.Year * 0x10000);
            }

            /* 
             if (FSoilResidueDest != "")
                    addEvent(FSoilResidueDest + ".add_fom", evtADD_FOM, TypeSpec.KIND_PUBLISHEDEVENT, PastureProps.typeADD_FOM, "", "", 0);
                if ((FSurfaceResidueDest != "") && FBiomassRemovedFound)
                    addEvent(FSurfaceResidueDest + ".BiomassRemoved", evtBIOMASS_OUT, TypeSpec.KIND_PUBLISHEDEVENT, PastureProps.typeBIOMASSREMOVED, "", "", 0);
             */
        }

        /// <summary>
        /// At the end of the simulation
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
        {

        }

        #endregion

        #region Management methods ============================================

        #endregion

        #region Private functions ============================================

        #endregion
    }
}
