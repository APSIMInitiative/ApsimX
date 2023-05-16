namespace Models.GrazPlan
{
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils;
    using StdUnits;
    using System;
    using System.Collections.Generic;
    using static Models.GrazPlan.GrazType;
    using static Models.GrazPlan.PastureUtil;

    /// <summary>
    /// The soil interface for the Pasture model
    /// </summary>
    [Serializable]
    public class TSoilInstance : Core.Model
    {
        protected int FNoLayers;                                                        // Layer profile used by this component
        protected double[] FLayerProfile = new double[GrazType.MaxSoilLayers + 1];      // [1..  [0] is unused

        protected int FNoInputLayers;                                                   // Temporary arrays                      

        /// <summary>Layer thicknesses</summary>
        protected double[] FInputProfile = new double[GrazType.MaxSoilLayers + 1];      // [1..

        /// <summary>Layer values</summary>
        protected double[] FLayerValues = new double[GrazType.MaxSoilLayers + 1];       // [1..

        /// <summary></summary>
        protected double[] FSoilValues = new double[GrazType.MaxSoilLayers + 1];        // [SURFACE..MaxSoilLayers] SURFACE = 0
        protected double[] FSoilDepths = new double[GrazType.MaxSoilLayers + 1];        // [SURFACE..MaxSoilLayers]
        protected double[] FLayer2Soil = new double[GrazType.MaxSoilLayers + 1];        // [1..

        /// <summary>
        /// Set the layer count and thicknesses
        /// </summary>
        /// <param name="profile"></param>
        protected void SetLayerProfile(double[] profile)
        {
            FNoLayers = profile.Length;
            Value2LayerArray(profile, ref FLayerProfile);
        }

        /// <summary>
        /// Fill a layer array where [0] is unused
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="LayerA"></param>
        protected void Value2LayerArray(double[] profile, ref double[] LayerA)
        {
            if (profile != null)
            {
                PastureUtil.FillArray(LayerA, 0.0);
                for (uint Ldx = 1; Ldx < profile.Length; Ldx++)
                {
                    LayerA[Ldx] = profile[Ldx];
                }
            }
        }
    }



    /// <summary>
    /// # Pasture class that models temperate Australian pastures
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MarkdownView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Pasture : TSoilInstance
    {
        private TPasturePopulation PastureModel;
        private TWeatherHandler FWeather;
        private TPastureInputs FInputs;

        private double[] F_BulkDensity = new double[GrazType.MaxSoilLayers + 1];    // [1..
        private double[] F_SandPropn = new double[GrazType.MaxSoilLayers + 1];      // [1..
        private double[] F_DUL = new double[GrazType.MaxSoilLayers + 1];            // [1..
        private double[] F_LL15 = new double[GrazType.MaxSoilLayers + 1];           // [1..

        private double FFieldGreenDM;
        private double FFieldGAI;
        private double FFieldDAI;
        private double FFieldCoverSum;

        private double FFieldArea;
        private double FHarvestHeight;
        private int FToday;             // StdDate.Date
        private double FIntercepted;
        private double[] FLightAbsorbed = new double[GrazType.stSENC + 1];      // [stSEEDL..stSENC] - [1..3]
        private double[][] FSoilPropn = new double[GrazType.stSENC + 1][];      // [stSEEDL..stSENC] - [1..3][1..]
        private double[][] FTranspiration = new double[GrazType.stSENC + 1][];  // [TOTAL..stSENC]   - [0..3][1..]

        private bool FLightAllocated;
        private bool FWaterAllocated;
        private bool FSoilAllocated;
        private string FSoilResidueDest = "";                     // Destination for OM outputs            
        private string FSurfaceResidueDest = "";
        private bool FBiomassRemovedFound;
        private bool FWaterValueReqd;

        // Stages within the timestep
        public const int evtINITSTEP = 1;
        public const int evtWATER = 2;
        public const int evtGROW = 3;
        public const int evtENDSTEP = 4; 

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

        /// <summary>Link to the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        private ISoilWater water = null;

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

            FLightAllocated = false;    // default to no other plant populations present
            FWaterAllocated = false;
            FSoilAllocated = false;

            for (int Ldx = 1; Ldx <= GrazType.MaxSoilLayers; Ldx++)
            {
                FInputs.pH[Ldx] = 7.0;  // Default value for pH          
            }

            FInputs.CO2_PPM = GrazEnv.REFERENCE_CO2;
            FInputs.Windspeed = 2.0;
            FFieldArea = 1.0;
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
        public double MaxRtDep { get; set; } = 1.0;

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

        /// <summary>Canopy characteristics of a child APSIM-Plant module. The array has one member per sub-canopy</summary>
        [Units("-")]
        public Canopy[] Canopy
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                
                Canopy[] result = MakeCanopy(PastureModel);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="iComp"></param>
        /// <returns>A canopy object</returns>
        private Canopy MakeCanopyElement(TPasturePopulation Model, int iComp)
        {
            Canopy anElement = new Canopy();

            if (iComp == GrazType.sgLITTER)    
                anElement.Name = sCOMPNAME[stLITT1];
            else
                anElement.Name = sCOMPNAME[iComp];
            anElement.PlantType = "pasture";
            anElement.Layer = new CanopyLayer[1];

            anElement.Layer[0].Thickness = 0.001 * Model.Height_MM();             // thickness (in metres)    
            anElement.Layer[0].AreaIndex = Model.AreaIndex(iComp);            
            if (iComp == stSEEDL || iComp == stESTAB || iComp == stSENC)
            {
                anElement.Layer[0].CoverGreen = Model.Cover(iComp);
                anElement.Layer[0].CoverTotal = anElement.Layer[0].CoverGreen;
            }
            else // stDEAD, sgLITTER
            {
                anElement.Layer[0].CoverGreen = 0.0;               
                anElement.Layer[0].CoverTotal = Model.Cover(iComp);
            }

            return anElement;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        private Canopy[] MakeCanopy(TPasturePopulation Model)
        {
            int iCount;
            int iComp;
            uint Jdx;

            iCount = 0;
            for (iComp = stSEEDL; iComp <= stDEAD; iComp++)
            {
                if (Model.AreaIndex(iComp) > 0.0)
                {
                    iCount++;
                }
            }

            if (Model.AreaIndex(sgLITTER) > 0.0)
            {
                iCount++;
            }

            Canopy[] result = new Canopy[iCount];

            Jdx = 0;
            for (iComp = stSEEDL; iComp <= stDEAD; iComp++)
            {
                if (Model.AreaIndex(iComp) > 0.0)
                {
                    Jdx++;
                    result[Jdx - 1] = MakeCanopyElement(Model, iComp);
                }
            }

            if (Model.AreaIndex(sgLITTER) > 0.0)
            {
                Jdx++;
                result[Jdx - 1] = MakeCanopyElement(Model, sgLITTER);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        private WaterInfo[] MakeWaterInfo(TPasturePopulation Model)
        {
            int iCount;
            int iComp;
            double[] fRootLD;
            double[] fRootRad;
            double[] fSupply;
            int Jdx, Ldx;

            iCount = 0;
            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                if (Model.WaterDemand(iComp) > 0.0)
                {
                    iCount++;
                }
            }

            WaterInfo[] result = new WaterInfo[iCount];
            
            iCount = Model.SoilLayerCount;
            Jdx = 0;
            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                if (Model.WaterDemand(iComp) > 0.0)
                {
                    fSupply = Model.WaterMaxSupply(iComp);
                    fRootLD = Model.EffRootLengthD(iComp);
                    fRootRad = Model.RootRadii(iComp);

                    Jdx++;
                    result[Jdx - 1].Name = sCOMPNAME[iComp];
                    result[Jdx - 1].PlantType = "pasture";
                    result[Jdx - 1].Demand = Model.WaterDemand(iComp);
                    result[Jdx - 1].Layer = new WaterLayer[iCount];
                    for (Ldx = 1; Ldx <= iCount; Ldx++)
                    {
                        result[Jdx - 1].Layer[Ldx-1].Thickness = Model.SoilLayer_MM[Ldx];
                        result[Jdx - 1].Layer[Ldx - 1].MaxSupply = fSupply[Ldx];
                        result[Jdx - 1].Layer[Ldx - 1].RLD = 1.0E-6 * fRootLD[Ldx]; // converted from m/m^3 to mm/mm^3 
                        result[Jdx - 1].Layer[Ldx - 1].Radius = fRootRad[Ldx];    
                    }
                }
            }

            return result;
        }

        /// <summary>Water demand and supply attributes (one member per sub-population)</summary>
        [Units("-")]
        public WaterInfo[] WaterDemandSupply
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                WaterInfo[] result = MakeWaterInfo(PastureModel);

                PastureModel.MassUnit = sUnit;
                return result;
            }
        }

        /// <summary>Green area index</summary>
        [Units("m^2/m^2")]
        public double GAI
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AreaIndex(GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dead area index</summary>
        [Units("m^2/m^2")]
        public double DAI
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AreaIndex(GrazType.sgDRY);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Green cover</summary>
        [Units("m^2/m^2")]
        public double CoverGreen
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Cover(GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Total cover</summary>
        [Units("m^2/m^2")]
        public double CoverTotal
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Cover(GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Cover of standing dead and litter</summary>
        [Units("m^2/m^2")]
        public double CoverResidue
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Cover(GrazType.sgDRY);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Cover of standing dead and litter</summary>
        [Units("mm")]
        public double Height
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Height_MM();
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Ammonium-N uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] NH4Uptake { get { return NutrUptake(TPlantNutrient.pnNH4); } }

        /// <summary>Nitrate-N uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] NO3Uptake { get { return NutrUptake(TPlantNutrient.pnNO3); } }

        /// <summary>Phosphate-P uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] POXUptake { get { return NutrUptake(TPlantNutrient.pnPOx); } }

        /// <summary>Sulphate-S uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] SO4Uptake { get { return NutrUptake(TPlantNutrient.pnSO4); } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nutr"></param>
        /// <returns></returns>
        private SoilLayer[] NutrUptake(TPlantNutrient nutr)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";

            double[][] Uptakes = PastureModel.GetNutrUptake(nutr);
            SoilLayer[] result = new SoilLayer[PastureModel.SoilLayerCount];
            double value;
            for (uint Ldx = 1; Ldx <= PastureModel.SoilLayerCount; Ldx++)
            {
                value = 0.0;
                for (int Kdx = 0; Kdx <= MAXNUTRAREAS - 1; Kdx++)
                    value = value + Uptakes[Kdx][Ldx];

                result[Ldx - 1].thickness = PastureModel.SoilLayer_MM[Ldx];
                result[Ldx - 1].amount = value;
            }

            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Parameters used by the Paddock component to determine water uptake</summary>
        [Units("")]
        public double[] WaterParams
        {
            get
            {
                double[] result;
                if ((PastureModel.fWater_KL[1] > 0.0) && (PastureModel.fPlant_LL[1] > 0.0))
                    result = new double[0];
                else if (PastureModel.Params.iWaterUptakeVersion == 1)
                    result = new double[2];
                else
                    result = new double[4];
                for (int Idx = 1; Idx <= result.Length; Idx++)
                    result[Idx - 1] = PastureModel.Params.WaterUseK[Idx];

                return result;
            }
        }

        /// <summary>
        /// Total water demand
        /// </summary>
        [Units("mm")]
        public double WaterDemand
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double fValue = 0.0;
                for (int Idx = stSEEDL; Idx <= stSENC; Idx++)
                    fValue += PastureModel.WaterDemand(Idx);
                double result = fValue;

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /*
         Data exchange vars
          AddOneVariable(ref Idx, PastureProps.prpPLANT2STOCK, "plant2stock", PastureProps.typePLANT2STOCK, "Description of the pasture for use by the ruminant model", "");
        //AddOneVariable(ref Idx, PastureProps.prpAVAILANIMAL, "availabletoanimal",typeCOHORTAVAIL,      "Characteristics of herbage available for defoliation",   "");
        */

        /// <summary>Total dry weight of all herbage</summary>
        [Units("kg/ha")]
        public double ShootDM { get { return GetDM(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of all herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] shootDMQ { get { return GetDMQ(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of all herbage</summary>
        [Units("g/g")]
        public double ShootDMD { get { return GetDMD(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of all herbage</summary>
        [Units("g/g")]
        public double ShootCP { get { return GetPlantCP(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of all herbage</summary>
        [Units("g/g")]
        public double ShootN { get { return GetPlantNutr(GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorous content of all herbage</summary>
        [Units("g/g")]
        public double ShootP { get { return GetPlantNutr(GrazType.TOTAL, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of all herbage</summary>
        [Units("g/g")]
        public double ShootS { get { return GetPlantNutr(GrazType.TOTAL, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of seedlings</summary>
        [Units("kg/ha")]
        public double SeedlDM { get { return GetDM(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of seedlings in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] SeedlDMQ { get { return GetDMQ(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of seedlings</summary>
        [Units("g/g")]
        public double SeedlDMD { get { return GetDMD(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of seedlings</summary>
        [Units("g/g")]
        public double SeedlCP { get { return GetPlantCP(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of seedlings</summary>
        [Units("g/g")]
        public double SeedlN { get { return GetPlantNutr(GrazType.stSEEDL, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of seedlings</summary>
        [Units("g/g")]
        public double SeedlP { get { return GetPlantNutr(GrazType.stSEEDL, GrazType.TOTAL, TPlantElement.P); } }
        /// <summary>Average sulphur content of seedlings</summary>
        [Units("g/g")]
        public double SeedlS { get { return GetPlantNutr(GrazType.stSEEDL, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of herbage of established plants</summary>
        [Units("kg/ha")]
        public double EstabDM { get { return GetDM(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of established plants in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] EstabDMQ { get { return GetDMQ(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabDMD { get { return GetDMD(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabCP { get { return GetPlantCP(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabN { get { return GetPlantNutr(GrazType.stESTAB, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabP { get { return GetPlantNutr(GrazType.stESTAB, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabS { get { return GetPlantNutr(GrazType.stESTAB, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of herbage of senescing plants</summary>
        [Units("kg/ha")]
        public double SencDM { get { return GetDM(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of senescing plants in each digestibility clas</summary>
        [Units("kg/ha")]
        public double[] SencDMQ { get { return GetDMQ(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of herbage of senescing plants</summary>
        [Units("g/g")]
        public double SencDMD { get { return GetDMD(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of herbage of senescing plants</summary>
        [Units("g/g")]
        public double SencCP { get { return GetPlantCP(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of senescing plants</summary>
        [Units("g/g")]
        public double SencN { get { return GetPlantNutr(GrazType.stSENC, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of senescing plants</summary>
        [Units("g/g")]
        public double SencP { get { return GetPlantNutr(GrazType.stSENC, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of senescing plants</summary>
        [Units("g/g")]
        public double SencS { get { return GetPlantNutr(GrazType.stSENC, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of standing dead herbage</summary>
        [Units("kg/ha")]
        public double DeadDM { get { return GetDM(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Dry weight of standing dead herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] DeadDMQ { get { return GetDMQ(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadDMD { get { return GetDMD(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadCP { get { return GetPlantCP(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadN { get { return GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadP { get { return GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadS { get { return GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of litter</summary>
        [Units("kg/ha")]
        public double LitterDM { get { return GetDM(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of litter in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] LitterDMQ { get { return GetDMQ(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of litter</summary>
        [Units("g/g")]
        public double LitterDMD { get { return GetDMD(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of litter</summary>
        [Units("g/g")]
        public double LitterCP { get { return GetPlantCP(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of litter</summary>
        [Units("g/g")]
        public double LitterN { get { return GetPlantNutr(GrazType.sgLITTER, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of litter</summary>
        [Units("g/g")]
        public double LitterP { get { return GetPlantNutr(GrazType.sgLITTER, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of litter</summary>
        [Units("g/g")]
        public double LitterS { get { return GetPlantNutr(GrazType.sgLITTER, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of green herbage</summary>
        [Units("kg/ha")]
        public double GreenDM { get { return GetDM(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Dry weight of green herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] GreenDMQ { get { return GetDMQ(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of green herbage</summary>
        [Units("g/g")]
        public double GreenDMD { get { return GetDMD(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of green herbage (seedlings+established+senescing)</summary>
        [Units("g/g")]
        public double GreenCP { get { return GetPlantCP(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of green herbage</summary>
        [Units("g/g")]
        public double GreenN { get { return GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of green herbage</summary>
        [Units("g/g")]
        public double GreenP { get { return GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of green herbage</summary>
        [Units("g/g")]
        public double GreenS { get { return GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of dry herbage</summary>
        [Units("kg/ha")]
        public double DryDM { get { return GetDM(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Dry weight of dry herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] DryDMQ { get { return GetDMQ(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of dry herbage</summary>
        [Units("g/g")]
        public double DryDMD { get { return GetDMD(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of dry herbage (standing dead+litter)</summary>
        [Units("g/g")]
        public double DryCP { get { return GetPlantCP(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of dry herbage</summary>
        [Units("g/g")]
        public double DryN { get { return GetPlantNutr(GrazType.sgDRY, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of dry herbage</summary>
        [Units("g/g")]
        public double DryP { get { return GetPlantNutr(GrazType.sgDRY, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of dry herbage</summary>
        [Units("g/g")]
        public double DryS { get { return GetPlantNutr(GrazType.sgDRY, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of all leaves</summary>
        [Units("kg/ha")]
        public double LeafDM { get { return GetDM(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Dry weight of all leaves in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] LeafDMQ { get { return GetDMQ(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Average DM digestibility of all leaves</summary>
        [Units("g/g")]
        public double LeafDMD { get { return GetDMD(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Average crude protein content of all leaves</summary>
        [Units("g/g")]
        public double LeafCP { get { return GetPlantCP(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Average nitrogen content of all leaves</summary>
        [Units("g/g")]
        public double LeafN { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.N); } }

        /// <summary>Average phosphorus content of all leaves</summary>
        [Units("g/g")]
        public double LeafP { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.P); } }

        /// <summary>Average sulphur content of all leaves</summary>
        [Units("g/g")]
        public double LeafS { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.S); } }

        /// <summary>Total dry weight of all stems</summary>
        [Units("kg/ha")]
        public double StemDM { get { return GetDM(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Dry weight of all stems in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] StemDMQ { get { return GetDMQ(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Average DM digestibility of all stems</summary>
        [Units("g/g")]
        public double StemDMD { get { return GetDMD(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Average crude protein content of all stems</summary>
        [Units("g/g")]
        public double StemCP { get { return GetPlantCP(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Average nitrogen content of all stems</summary>
        [Units("g/g")]
        public double StemN { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.N); } }

        /// <summary>Average phosphorus content of all stems</summary>
        [Units("g/g")]
        public double StemP { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.P); } }

        /// <summary>Average sulphur content of all stems</summary>
        [Units("g/g")]
        public double StemS { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.S); } }

        /// <summary>Total dry weight of herbage available for grazing</summary>
        [Units("kg/ha")]
        public double AvailDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AvailHerbage(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;
                return result;
            }
        }

        /// <summary>Dry weight of herbage available for grazing in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] AvailDMQ
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[GrazType.HerbClassNo]; ;
                for (int i = 1; i <= GrazType.HerbClassNo; i++)
                    result[i - 1] = PastureModel.AvailHerbage(GrazType.TOTAL, GrazType.TOTAL, i);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average DM digestibility of herbage available for grazing</summary>
        [Units("g/g")]
        public double AvailDMD
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double result = WeightAverage(PastureModel.Digestibility(GrazType.sgGREEN, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL),
                                     PastureModel.Digestibility(GrazType.sgDRY, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgDRY, GrazType.TOTAL, GrazType.TOTAL));
                
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }
        
        /// <summary>Average crude protein content of herbage available for grazing</summary>
        [Units("g/g")]
        public double AvailCP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double result = WeightAverage(PastureModel.CrudeProtein(GrazType.sgGREEN, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL),
                                     PastureModel.CrudeProtein(GrazType.sgDRY, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgDRY, GrazType.TOTAL, GrazType.TOTAL));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Weight of green (seedling+established+senescing) herbage available for grazing</summary>
        [Units("kg/ha")]
        public double AvailGreen
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AvailHerbage(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Weight of dry (standing dead+litter) herbage available for grazing</summary>
        [Units("kg/ha")]
        public double AvailDry
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AvailHerbage(GrazType.sgDRY, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Height profile of green herbage (by plant parts)</summary>
        [Units("kg/ha")]
        public HerbageProfile GreenProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                HerbageProfile result = makeHerbageProfile(PastureModel, GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Height profile of all herbage (by plant parts)</summary>
        [Units("-")]
        public HerbageProfile ShootProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                HerbageProfile result = makeHerbageProfile(PastureModel, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        private HerbageProfile makeHerbageProfile(TPasturePopulation Model, int iComp)
        {
            double[] fPartMasses = new double[5];         // [ptLEAF..ptSEED] 
            int iHeightProfileCount = 0;
            double[] fHeightProfile = new double[MaxSoilLayers + 1];
            double[][] fProfilePropns = new double[5][];     // [ptLEAF..ptSEED] of LayerArray;
            int iPart;
            int iLayer;
            
            
            for (int i = 0; i <= 5; i++)
            {
                fProfilePropns[i] = new double[MaxSoilLayers + 1];
            }
         
            // init fProfilePropns
            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                for (iLayer = 1; iLayer <= MaxSoilLayers; iLayer++)
                    fProfilePropns[iPart][iLayer] = 0;
            }

            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                fPartMasses[iPart] = Model.GetHerbageMass(iComp, iPart, TOTAL);
            }
            
            fPartMasses[ptSEED] = Model.GetSeedMass(TOTAL, TOTAL, 1);

            if (fPartMasses[ptLEAF] + fPartMasses[ptSTEM] + fPartMasses[ptSEED] <= 0.0)
            {
                iHeightProfileCount = 0;
            }
            else
            {
                Model.GetHeightProfile(iComp, ptSEED, ref iHeightProfileCount, ref fHeightProfile, ref fProfilePropns[ptSEED]);
                Model.GetHeightProfile(iComp, ptSTEM, ref iHeightProfileCount, ref fHeightProfile, ref fProfilePropns[ptSTEM]);
                Model.GetHeightProfile(iComp, ptLEAF, ref iHeightProfileCount, ref fHeightProfile, ref fProfilePropns[ptLEAF]);
            }

            HerbageProfile result = new HerbageProfile(iHeightProfileCount);
            
            for (iLayer = 1; iLayer <= iHeightProfileCount; iLayer++)
            {
                if (iLayer == 1)
                    result.bottom[iLayer-1] = 0.0;
                else
                    result.bottom[iLayer - 1] = fHeightProfile[iLayer - 1];
                result.top[iLayer-1] = fHeightProfile[iLayer];
                result.leaf[iLayer-1] = fPartMasses[ptLEAF] * fProfilePropns[ptLEAF][iLayer];
                result.stem[iLayer-1] = fPartMasses[ptSTEM] * fProfilePropns[ptSTEM][iLayer];
                result.head[iLayer-1] = fPartMasses[ptSEED] * fProfilePropns[ptSEED][iLayer];
            } 

            return result;
        }

        /// <summary>Total dry weight of all roots</summary>
        [Units("kg/ha")]
        public double RootDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average nitrogen content of all roots</summary>
        [Units("g/g")]
        public double RootN
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average phosphorus content of all roots</summary>
        [Units("g/g")]
        public double RootP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.P);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average sulphur content of all roots</summary>
        [Units("g/g")]
        public double RootS
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.S);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight of all roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootDMProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.TOTAL, i);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average nitrogen content of roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootDMProfileN
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, i, TPlantElement.N);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average phosphorus content of roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootDMProfileP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, i, TPlantElement.P);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average sulphur content of roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootDMProfileS
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, i, TPlantElement.S);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Total dry weight of effective roots</summary>
        [Units("kg/ha")]
        public double RootEffDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.EFFR, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight of effective roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootEffDMProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.EFFR, i);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Current depth of the rooting front</summary>
        [Units("mm")]
        public double RootDep
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootDepth(GrazType.sgGREEN, TPasturePopulation.ALL_COHORTS) ;
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average radius of all roots</summary>
        [Units("mm")]
        public double RootRadius
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.RootRadii(GrazType.sgGREEN)[1];
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Length density of effective roots in each soil layer</summary>
        [Units("mm/mm^3")]
        public double[] RootLVProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.EffRootLengthD(GrazType.sgGREEN)[i] * 1e-6;

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Total dry weight of seeds in all soil layers</summary>
        [Units("kg/ha")]
        public double SeedDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetSeedMass(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average crude protein content of seeds</summary>
        [Units("g/g")]
        public double SeedCP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.SeedCrudeProtein(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average nitrogen content of seeds</summary>
        [Units("g/g")]
        public double SeedN
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetSeedNutr(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average phosphorus content of seeds</summary>
        [Units("g/g")]
        public double SeedP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetSeedNutr(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.P);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average sulphur content of seeds</summary>
        [Units("g/g")]
        public double SeedS
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetSeedNutr(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.S);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Weighted average value of the establishment index for seedlings</summary>
        [Units("-")]
        public double EstIndex
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.EstablishIndex();
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Weighted average value of the seedling stress index</summary>
        [Units("-")]
        public double StressIndex
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.SeedlingStress();
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Water uptake from each soil layer</summary>
        [Units("mm")]
        public double[] SWUptake
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double[] result = LayerArray2Value(PastureModel.Transpiration(GrazType.sgGREEN), PastureModel.SoilLayerCount);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        private double[] LayerArray2Value(double[] fLayers, int iNoLayers)
        {
            double[] result = new double[iNoLayers];
            
            for (int Ldx = 1; Ldx <= iNoLayers; Ldx++)
                result[Ldx-1] = fLayers[Ldx];

            return result;
        }

        /// <summary>Whole-plant net primary productivity</summary>
        [Units("kg/ha/d")]
        public double NPP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.NetPP(GrazType.sgGREEN, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Net primary productivity of shoots</summary>
        [Units("kg/ha/d")]
        public double ShootNPP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.NetPP(GrazType.sgGREEN, GrazType.TOTAL) - PastureModel.NetPP(GrazType.sgGREEN, GrazType.ptROOT);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Gross whole-plant assimilation rate</summary>
        [Units("kg/ha/d")]
        public double Assimilation
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Assimilation(GrazType.sgGREEN, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Whole-plant respiration rate, in dry weight equivalent terms</summary>
        [Units("kg/ha/d")]
        public double Respiration
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Respiration(GrazType.sgGREEN, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Daily shoot growth rate</summary>
        [Units("kg/ha/d")]
        public double Growth
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.TopGrowth();
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Green leaf area index</summary>
        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AreaIndex(GrazType.sgGREEN, GrazType.ptLEAF);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Light interception growth-limiting factor</summary>
        [Units("-")]
        public double GLF_GAI { get { return GetGLF(TGrowthLimit.glGAI); } }

        /// <summary>VPD growth-limiting factor</summary>
        [Units("-")]
        public double GLF_VPD { get { return GetGLF(TGrowthLimit.glVPD); } }

        /// <summary>Soil moisture growth-limiting factor</summary>
        [Units("-")]
        public double GLF_SM { get { return GetGLF(TGrowthLimit.glSM); } }

        /// <summary>Temperature growth-limiting factor</summary>
        [Units("-")]
        public double GLF_Temp { get { return GetGLF(TGrowthLimit.glLowT); } }

        /// <summary>Waterlogging growth-limiting factor</summary>
        [Units("-")]
        public double GLF_WL { get { return GetGLF(TGrowthLimit.glWLog); } }

        /// <summary>Nitrogen growth-limiting factor</summary>
        [Units("-")]
        public double GLF_N { get { return GetGLF(TGrowthLimit.gl_N); } }

        /// <summary>Phosphorus growth-limiting factor</summary>
        [Units("-")]
        public double GLF_P { get { return GetGLF(TGrowthLimit.gl_P); } }

        /// <summary>Sulphur growth-limiting factor</summary>
        [Units("-")]
        public double GLF_S { get { return GetGLF(TGrowthLimit.gl_S); } }

        private double GetGLF(TGrowthLimit factor)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GrowthLimit(GrazType.sgGREEN, factor);   // glGAI, glVPD, glSM, glLowT, glWLog, gl_N, gl_P, gl_S 
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Nutrient growth-limiting factor</summary>
        [Units("-")]
        public double GLF_Nutr
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = Math.Min(PastureModel.GrowthLimit(GrazType.sgGREEN, TGrowthLimit.gl_N), Math.Min(PastureModel.GrowthLimit(GrazType.sgGREEN, TGrowthLimit.gl_P), PastureModel.GrowthLimit(GrazType.sgGREEN, TGrowthLimit.gl_S)));
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Nitrogen fixation rate</summary>
        [Units("kg/ha/d")]
        public double N_Fixed
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.N_Fixation(GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Ammonium-N uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_NH4  { get { return UptakeByLayer(TPlantNutrient.pnNH4); } }

        /// <summary>Nitrate-N uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_NO3 { get { return UptakeByLayer(TPlantNutrient.pnNO3); } }

        /// <summary>Phosphate-P uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_POx { get { return UptakeByLayer(TPlantNutrient.pnPOx); } }

        /// <summary>Sulphate-S uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_SO4 { get { return UptakeByLayer(TPlantNutrient.pnSO4); } }

        /// <summary>
        /// Get the plant nutrient uptake from each soil layer
        /// </summary>
        /// <param name="nutr"></param>
        /// <returns></returns>
        private double[] UptakeByLayer(TPlantNutrient nutr)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";

            double[][] Uptakes = PastureModel.GetNutrUptake(nutr);   // TSoilUptakeDistn
            double[] result = new double[PastureModel.SoilLayerCount];

            for (int layer = 1; layer <= PastureModel.SoilLayerCount; layer++)
            {
                double value = 0.0;
                for (int area = 0; area <= MAXNUTRAREAS - 1; area++)
                    value = value + Uptakes[area][layer];

                result[layer - 1] = value;
            }

            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Allocation of assimilate to each plant part</summary>
        [Units("-")]
        public Allocation PlantAllocation
        {
            get
            {
                Allocation result = new Allocation();
                result.Leaf = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptLEAF);
                result.Stem = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptSTEM);
                result.Root = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptROOT);
                result.Seed = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptSEED);

                return result;
            }
        }

        /// <summary>Dry weight and quality of residues incorporated into the soil in this time step (one member per soil layer). [0] is surface</summary>
        [Units("-")]
        public Residue[] ResiduePlant
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue[] result = new Residue[PastureModel.SoilLayerCount];

                DM_Pool surfPool = PastureModel.GetResidueFlux(GrazType.ptLEAF);
                AddDMPool(PastureModel.GetResidueFlux(GrazType.ptSTEM), surfPool);
                AddDMPool(PastureModel.GetResidueFlux(GrazType.ptROOT, 1), surfPool);
                result[0].CopyFrom(surfPool);
                for (int i = 2; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1].CopyFrom(PastureModel.GetResidueFlux(ptROOT, i));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight and quality of leaf residues incorporated into the soil in this time step</summary>
        [Units("-")]
        public Residue ResidueLeaf
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue result = new Residue();
                result.CopyFrom(PastureModel.GetResidueFlux(GrazType.ptLEAF));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight and quality of stem residues incorporated into the soil in this time step</summary>
        [Units("-")]
        public Residue ResidueStem
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue result = new Residue();
                result.CopyFrom(PastureModel.GetResidueFlux(GrazType.ptSTEM));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight and quality of root residues incorporated into the soil in this time step (one member per soil layer)</summary>
        [Units("-")]
        public Residue[] ResidueRoot
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue[] result = new Residue[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i-1].CopyFrom(PastureModel.GetResidueFlux(GrazType.ptROOT, i));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Mass of organic nutrients leached from dead pasture and litter by rainfall</summary>
        [Units("-")]
        public Leachate PlantLeachate
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Leachate result = new Leachate();
                DM_Pool pool = PastureModel.GetLeachate();
                result.N = pool.Nu[(int)TPlantElement.N];
                result.P = pool.Nu[(int)TPlantElement.P];
                result.S = pool.Nu[(int)TPlantElement.S];

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Rate of volatilization of tissue N into the atmosphere</summary>
        [Units("kg/ha/d")]
        public double GasNLoss
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetGaseousLoss(TPlantElement.N);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Herbage bulk density of green shoots</summary>
        [Units("kg/m^3")]
        public double GreenBD
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = REF_HERBAGE_BD / PastureModel.Params.HeightRatio;
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Relative height of two 'horizons' affecting the impact of defoliation on phenology</summary>
        [Units("-")]
        public double[] PhenHorizon
        {
            get
            {
                double[] result = new double[2];
                result[0] = PastureModel.fPhenoHorizon[0];
                result[1] = PastureModel.fPhenoHorizon[1];

                return result;
            }
        }

        private int[] DefoliationCompMap = { 0, sgGREEN, stDEAD, sgLITTER };    // 1..3
        private int[] DefoliationPartMap = { 0, ptLEAF, ptSTEM };               // 1..2

        /// <summary>Amount of herbage defoliated from each of green/standing dead/litter (1st index) x leaf/stem (2nd index) x DMD class (3rd index, 1=DMD 80-85pc, 12=DMD 35-40pc)</summary>
        [Units("kg/ha")]
        public double[,,] Defoliation
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[,,] result = new double[3, 2, 12];

                for (int Idx = 1; Idx <= 3; Idx++)
                {
                    for (int Jdx = 1; Jdx <= 2; Jdx++)
                    {
                        for (int Kdx = 1; Kdx <= 12; Kdx++)
                        {
                            result[Idx-1, Jdx-1, Kdx-1] = PastureModel.Removal(DefoliationCompMap[Idx], DefoliationPartMap[Jdx], Kdx);
                        }
                    }
                }

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Rate of death of green herbage from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc). Does not include defoliation or death due to kill, cultivate or cut events</summary>
        [Units("kg/ha/d")]
        public double[,] DeathRate { get { return Loss(GrazType.sgGREEN); } }

        /// <summary>Rate of fall of standing dead herbage from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc)</summary>
        [Units("kg/ha/d")]
        public double[,] FallRate { get { return Loss(GrazType.stDEAD); } }

        /// <summary>Rate of incorporation of litter from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc)</summary>
        [Units("kg/ha/d")]
        public double[,] IncorpRate { get { return Loss(GrazType.stLITT2); } }

        private double[,] Loss(int comp)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double[,] result = new double[2, 12];

            for (int Jdx = 1; Jdx <= 2; Jdx++)
            {
                for (int Kdx = 1; Kdx <= 12; Kdx++)
                {
                    result[Jdx - 1, Kdx - 1] = PastureModel.BiomassExit(comp, DefoliationPartMap[Jdx], Kdx);
                }
            }

            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Amount of death of green herbage as a result of kill or cultivate events from each of leaf and stem</summary>
        [Units("kg/ha")]
        public double[] Killed
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double[] result = new double[2];
                result[0] = PastureModel.ShootKilled(GrazType.ptLEAF, GrazType.TOTAL);
                result[1] = PastureModel.ShootKilled(GrazType.ptSTEM, GrazType.TOTAL);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }
        
        /// <summary>
        /// Total dry weight of herbage of a plant
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetDM(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GetHerbageMass(comp, part, GrazType.TOTAL);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get the average digestibility of this herbage
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetDMD(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.Digestibility(comp, part);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get the dry weight of a plant in each digestibility class
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double[] GetDMQ(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double[] result = new double[GrazType.HerbClassNo];
            for (int i = 1; i <= GrazType.HerbClassNo; i++)
                result[i - 1] = PastureModel.GetHerbageMass(comp, part, i);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get average nutrient content of a plant
        /// </summary>
        /// <param name="comp">Herbage</param>
        /// <param name="part">Plant part</param>
        /// <param name="elem">Nutrient element</param>
        /// <returns></returns>
        private double GetPlantNutr(int comp, int part, TPlantElement elem)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GetHerbageConc(comp, part, GrazType.TOTAL, elem);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get average crude protein of plant
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetPlantCP(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.CrudeProtein(comp, part, GrazType.TOTAL);
            PastureModel.MassUnit = sUnit;

            return result;
        }
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
            // =========================================================
            // Some initialisation that will be on the user interface
            //

            this.Species = "Phalaris";
            this.MaxRtDep = 650;

            // =========================================================

            StartSimulation();
            
        }

        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            InitStep();
        }

        [EventSubscribe("DoPastureWater")]
        private void OnDoPastureWater(object sender, EventArgs e)
        {
            DoPastureWater();
        }

        /// <summary>
        /// Grow pasture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            DoPastureGrowth();
        }

        [EventSubscribe("DoEndPasture")]
        private void OnDoEndPasture(object sender, EventArgs e)
        {
            EndStep();
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

        /// <summary>
        /// This responds to the OnStartOfSimulation event.
        /// Set up the model and initial values for the pasture. 
        /// </summary>
        private void StartSimulation()
        {
            FWeather = new TWeatherHandler();
            PastureModel = new TPasturePopulation();
            PastureModel.MassUnit = "g/m^2";

            // required at initialisation
            FWeather.fLatDegrees = locWtr.Latitude;                             // Location. South is -ve
            SetLayerProfile(soilPhysical.Thickness);                            // Layers
            Value2LayerArray(soilPhysical.BD, ref F_BulkDensity);               // Soil bulk density profile Mg/m^3
            Value2LayerArray(soilPhysical.DUL, ref F_DUL);                      // Profile of water content at drained upper limit
            Value2LayerArray(soilPhysical.LL15, ref F_LL15);                    // Profile of water content at (soil) lower limit
            Value2LayerArray(soilPhysical.ParticleSizeSand, ref F_SandPropn);   // Sand content profile //// TODO: check this

            // Light interception profiles of plant populations
            LightProfile lightProfile = GetLightProfile();
            storeLightPropn(lightProfile);

            FWaterValueReqd = false;

            // Water uptake by plant populations from the allocator (paddock)
            WaterUptake[] water = null;     //// TODO: populate this
            storeWaterSupply(water);

            // Proportion of the soil volume occupied by roots of plant populations (paddock)
            SoilFract[] soilFract = GetSoilInfo();       //// TODO: populate this
            StoreSoilPropn(soilFract);

            NutrAvail nutrNH4 = null;      //// TODO: populate this
            Value2SoilNutrient(nutrNH4, ref FInputs.Nutrients[(int)TPlantNutrient.pnNH4]);   // Soil ammonium availability
            // if using ppm then
            // Value2LayerArray(aValue, ref FLayerValues);
            // LayerArray2SoilNutrient(FLayerValues, ref FInputs.Nutrients[(int)TPlantNutrient.pnNH4]);

            NutrAvail nutrNO3 = null;      //// TODO: populate this
            Value2SoilNutrient(nutrNO3, ref FInputs.Nutrients[(int)TPlantNutrient.pnNO3]);   // Soil nitrate availability

            this.LocateDestinations();

            if (FSurfaceResidueDest != "")
            { } // TODO: set biomassremoved to this component also

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

                FToday = systemClock.Today.Day + (systemClock.Today.Month * 0x100) + (systemClock.Today.Year * 0x10000);    //stddate
            }

            /* 
            // TODO: connect these
            if (FSoilResidueDest != "")
                addEvent(FSoilResidueDest + ".add_fom", evtADD_FOM, TypeSpec.KIND_PUBLISHEDEVENT, PastureProps.typeADD_FOM, "", "", 0);
            if ((FSurfaceResidueDest != "") && FBiomassRemovedFound)
                addEvent(FSurfaceResidueDest + ".BiomassRemoved", evtBIOMASS_OUT, TypeSpec.KIND_PUBLISHEDEVENT, PastureProps.typeBIOMASSREMOVED, "", "", 0);
             */
        }

        /// <summary>
        /// Initial step = 100
        /// </summary>
        private void InitStep()
        {
            resetDrivers();

            FFieldGreenDM = PastureModel.GetHerbageMass(sgGREEN, TOTAL, TOTAL);
            FFieldGAI = PastureModel.AreaIndex(sgGREEN);
            FFieldDAI = PastureModel.AreaIndex(sgDRY);
            FFieldCoverSum = PastureModel.Cover(TOTAL);

            // TODO: get values from sibling components
            // From each sibling:
            //  - cover (add to FFieldCoverSum)
            //  - green DM (add to FFieldGreenDM)
            //  - GAI (add to FFieldGAI)
            //  - DAI (add to FFieldDAI)

            PastureModel.BeginTimeStep();
            passDrivers(evtINITSTEP);
        }

        private void GetWtrDrivers()
        {
            FInputs.CO2_PPM = locWtr.CO2;            // atmospheric CO2 ppm
            FInputs.MaxTemp = locWtr.MaxT;
            FInputs.Precipitation = locWtr.Rain;
            FInputs.MinTemp = locWtr.MinT;
            FInputs.Radiation = locWtr.Radn;    // Will need "radn" when "light_profile" is read later  
            FInputs.Windspeed = locWtr.Wind;
            FInputs.VP_Deficit = locWtr.VPD;
            // TODO: FInputs.SurfaceEvap = found in grazplan soilwater
        }

        /// <summary>
        /// Get climate and water = 4000
        /// </summary>
        private void DoPastureWater()
        {
            GetWtrDrivers();
             
            if (FLightAllocated)
            {
                // get the canopy details for all populations and computeLight() 
                LightProfile lightProfile = GetLightProfile();
                storeLightPropn(lightProfile);
            }

            /*
            if (FDriverThere[drvSW_L])                                              // Soil water is obtained *before* soil water dynamics calculations are made      
                sendDriverRequest(drvSW_L, eventID);                                
            else                                                                                                   
                sendDriverRequest(drvSW, eventID);
            FWaterFromSWIM = false;
            */
            passDrivers(evtWATER);
            if (!FLightAllocated)
            {
                // If the light_profile has not been calculated based on this paddock's plant populations.
                PastureModel.SetMonocultureLight();
            }

            if (!FWaterAllocated)
            {
                PastureModel.ComputeWaterUptake();
            }
        }

        /// <summary>
        /// Do the growth = 6000
        /// </summary>
        private void DoPastureGrowth()
        {
            // TODO: sendDriverRequest(drvTIME, eventID);            // Time driver                           
            GetWtrDrivers(); // Weather drivers                       

            // TODO: sendDriverRequest(drvINTCPD, eventID);

            FFieldGreenDM = PastureModel.GetHerbageMass(sgGREEN, TOTAL, TOTAL);
            FFieldGAI = PastureModel.AreaIndex(sgGREEN);
            FFieldDAI = PastureModel.AreaIndex(sgDRY);
            FFieldCoverSum = PastureModel.Cover(TOTAL);


            // TODO: get values from sibling components
            // From each sibling:
            //  - cover (add to FFieldCoverSum)
            //  - green DM (add to FFieldGreenDM)
            //  - GAI (add to FFieldGAI)
            //  - DAI (add to FFieldDAI)

            FWaterValueReqd = false;
            /* if (FWaterAllocated && !FWaterFromSWIM)                                 // Paddock drivers                       
                sendDriverRequest(drvWATER_UPTAKE, eventID);
            if (FSoilAllocated)
                sendDriverRequest(drvSOIL_FRACT, eventID);

            FInputs.TrampleRate = 0.0;
            sendDriverRequest(drvTRAMPLE, eventID);                                 // Animal drivers                        

            if (FModel.ElementSet.Length == 0)                                      // Nutrient drivers                      
            {
                //retrieve the fertility scalar if it exists elsewhere
                if (base.driverList[drvFERTSCL] == null)
                    addDriver("fert_scalar", drvFERTSCL, 0, 1, "-", false, TTypedValue.STYPE_DOUBLE, "Fertility scalar", "Default is the value of fertility. Only used when nutrients=''.", 0);

                sendDriverRequest(drvFERTSCL, eventID);
            }
            else
            {
                var values = Enum.GetValues(typeof(TPlantNutrient)).Cast<TPlantNutrient>().ToArray();
                foreach (var _Nutr in values)
                {
                    if (FModel.ElementSet.Contains(NutrToElement[(int)_Nutr]))
                    {
                        if (FDriverThere[NutrDriver[(int)_Nutr, 1]])
                            sendDriverRequest(NutrDriver[(int)_Nutr, 1], eventID);
                        else if (NutrDriver[(int)_Nutr, 2] >= 0)
                            sendDriverRequest(NutrDriver[(int)_Nutr, 2], eventID);
                    }
                }
            }
            if (FDriverThere[drvPH_L])
                sendDriverRequest(drvPH_L, eventID);
            else
                sendDriverRequest(drvPH, eventID);
        }

        */

            passDrivers(evtINITSTEP);
            passDrivers(evtGROW);
            if (!FSoilAllocated)
                PastureModel.SetMonocultureSoil();

            PastureModel.ComputeRates();
        }

        /// <summary>
        /// Publish biomass values = 9900
        /// </summary>
        private void EndStep()
        {
            PastureModel.UpdateState();

            if (FSoilResidueDest != "")                                             // Publish an "add_fom" event            
            {
                /*publParams = eventParams(evtADD_FOM);
                if (makeFOMValue(ref publParams))
                    sendPublishEvent(evtADD_FOM, false);*/
            }

            if ((FSurfaceResidueDest != "") && FBiomassRemovedFound)
            {
                /*publParams = eventParams(evtBIOMASS_OUT);
                if (transferLitterToValue(ref publParams))
                    sendPublishEvent(evtBIOMASS_OUT, false);*/
            }
        }

        private SoilFract[] GetSoilInfo()
        {
            SoilFract[] soil = new SoilFract[0];
            
            // for each plant population
            WaterInfo[] water_info = WaterDemandSupply;
            double[] water_params = WaterParams;
            double[] plant_kl = KL;
            double[] root_ll = LL;
            // paddock.storeWaterInfo()
            

            // paddock get soil_fract


            return null;
        }

        /// <summary>
        /// Get the canopy details for all populations and computeLight()
        /// </summary>
        /// <returns></returns>
        private LightProfile GetLightProfile()
        {
            // iterate through each plant population in this paddock and get the canopy structure
            
            // calculate the light profile

            LightProfile lightProfile = new LightProfile();
            
            lightProfile = null;

            return lightProfile;
        }

        /// <summary>
        /// Pass driving values to the pasture model
        /// </summary>
        /// <param name="iEventID"></param>
        /// <exception cref="Exception"></exception>
        private void passDrivers(int iEventID)
        {
            if (PastureModel != null)
            {
                if (iEventID == evtINITSTEP)
                {
                    PastureModel.PastureGreenDM = FFieldGreenDM;
                    PastureModel.PastureGAI = FFieldGAI;
                    PastureModel.PastureAreaIndex = FFieldGAI + FFieldDAI;
                    PastureModel.PastureCoverSum = FFieldCoverSum;
                }

                else if (iEventID == evtWATER)
                {
                    completeInputs(evtWATER);
                    PastureModel.Inputs = FInputs;
                    if (FLightAllocated)
                    {
                        for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
                        {
                            if (FInputs.Radiation > 0.0)
                                PastureModel.SetLightPropn(iComp, FLightAbsorbed[iComp] / FInputs.Radiation);
                            else
                                PastureModel.SetLightPropn(iComp, 0.0);
                        }
                    }
                }

                else if (iEventID == evtGROW)
                {
                    completeInputs(evtGROW);
                    PastureModel.SetFertility(this.Fertility);
                    PastureModel.Inputs = FInputs;

                    for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
                    {
                        if (FSoilAllocated)
                            PastureModel.SetSoilPropn(iComp, FSoilPropn[iComp]);
                        if (FWaterAllocated)
                            PastureModel.SetTranspiration(iComp, FTranspiration[iComp]);
                    }
                }
            }
            else
                throw new Exception("PastureModel == null in Pasture.passDrivers().");
        }

        /// <summary>
        /// Computes the derived values that go to make up the FInputs record            
        /// </summary>
        /// <param name="iEventID"></param>
        private void completeInputs(int iEventID)
        {
            int Ldx;

            if (iEventID == evtWATER)
            {
                for (Ldx = 1; Ldx <= FNoLayers; Ldx++)
                {
                    FInputs.ASW[Ldx] = (FInputs.Theta[Ldx] - F_LL15[Ldx]) / (F_DUL[Ldx] - F_LL15[Ldx]);
                    FInputs.ASW[Ldx] = Math.Max(0.0, Math.Min(FInputs.ASW[Ldx], 1.0));
                    FInputs.WFPS[Ldx] = FInputs.Theta[Ldx] / (1.0 - F_BulkDensity[Ldx] / 2.65);
                }
            }

            else if (iEventID == evtGROW)
            {
                if (FWeather != null)
                {
                    FWeather.setToday(StdDate.DayOf(FToday), StdDate.MonthOf(FToday), StdDate.YearOf(FToday));  // also clears FWeather data list
                    FWeather[TWeatherData.wdtRain] = FInputs.Precipitation;
                    FWeather[TWeatherData.wdtMaxT] = FInputs.MaxTemp;
                    FWeather[TWeatherData.wdtMinT] = FInputs.MinTemp;
                    FWeather[TWeatherData.wdtRadn] = FInputs.Radiation;
                    FWeather[TWeatherData.wdtWind] = FInputs.Windspeed;
                    FWeather[TWeatherData.wdtEpan] = locWtr.PanEvap;
                    FWeather[TWeatherData.wdtVP] = locWtr.VP;

                    FInputs.MeanTemp = FWeather.MeanTemp();
                    FInputs.MeanDayTemp = FWeather.MeanDayTemp();
                    FInputs.DayLength = FWeather.Daylength(true);
                    FInputs.DayLenIncreasing = FWeather.DaylengthIncreasing();
                    FInputs.PotentialET = FWeather.PotentialET(GrazEnv.HERBAGE_ALBEDO);       // Reference evapotranspiration mm

                    if (FInputs.Precipitation > 0.0)
                        FInputs.RainIntercept = Math.Min(1.0, FIntercepted / FInputs.Precipitation);
                    else
                        FInputs.RainIntercept = 1.0;
                }
                else
                    throw new Exception("FWeather is null in Pasture.completeInputs().");
            }
        }


        /// <summary>
        /// Configure connections to other models
        /// </summary>
        private void LocateDestinations()
        {
            // Locate the single destination for "add_fom", sibling closest or descendant or in the same system
            // set FSurfaceResidueDest

            // Locate the single destination for "BiomassRemoved"
            // This is a two-step process;  (i) locate nearby "surfaceom_c" - set FSurfaceResidueDest    & then
            //                              (ii) locate "BiomassRemoved", because Pasture modules
            //                                  also subscribe to "BiomassRemoved" - set FBiomassRemovedFound

            // Sibling modules with "cover_tot" - add the source
            // Sibling modules with "green_dm" - add the source
            // Sibling modules with "dai" - add the source
            // Sibling modules with "gai" - add the source
        }

        private void resetDrivers()
        {
            FLightAbsorbed = new double[GrazType.stSENC + 1];
            FSoilPropn = new double[GrazType.stSENC + 1][];
            FTranspiration = new double[GrazType.stSENC + 1][];
            for (int i = 0; i <= GrazType.stSENC; i++)
            {
                FSoilPropn[i] = new double[MaxSoilLayers + 1];
                FTranspiration[i] = new double[MaxSoilLayers + 1];
            }
        }


        public static string[] sCOMPNAME = { "", "seedling", "established", "senescing", "dead", "litter" };    // [0, stSEEDL..stLITT1]  - [0, 1..5]

        /// <summary>
        /// This function is used to store the light intercepted as calculated from the allocation object (paddock)
        /// </summary>
        /// <param name="lightValues"></param>
        private void storeLightPropn(LightProfile lightValues)
        {
            Population[] intcpValue;
            PopulationItem[] popnValue;
            PopulationItem compValue;
            int iComp;

            if (lightValues != null)
            {
                FLightAllocated = true; // if light source values found

                for (iComp = stSEEDL; iComp <= stSENC; iComp++)
                    FLightAbsorbed[iComp] = 0.0;

                intcpValue = lightValues.interception;
                popnValue = null;
                int Idx = 0;
                while (Idx < intcpValue.Length && (popnValue == null))
                {
                    if (intcpValue[Idx].population == this.Name)    ///// TODO: check this name
                        popnValue = intcpValue[Idx].element;
                    else
                        Idx++;
                }

                if (popnValue != null)
                {
                    for (Idx = 0; Idx < popnValue.Length; Idx++)                                // One entry per component for which a FPC was given                     
                    {
                        compValue = popnValue[Idx];

                        iComp = stSEEDL;
                        while ((iComp <= stSENC) && (compValue.name != sCOMPNAME[iComp]))
                            iComp++;
                        if (iComp == stSEEDL || iComp == stESTAB || iComp == stSENC)
                        {
                            for (uint Ldx = 0; Ldx <= compValue.layer.Length; Ldx++)
                                FLightAbsorbed[iComp] = FLightAbsorbed[iComp] + compValue.layer[Ldx].amount;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="water"></param>
        /// <exception cref="Exception"></exception>
        private void storeWaterSupply(WaterUptake[] water)
        {
            uint Idx, Jdx;
            int iComp;
            WaterPopItem[] popnValue;
            WaterPopItem compValue;

            if (water != null)
            {
                FWaterAllocated = true;
                popnValue = null;
                Idx = 0;
                while ((Idx < water.Length) && (popnValue == null))
                {
                    if (water[Idx].population == this.Name)                //// TODO: check this name                 
                        popnValue = water[Idx].element;
                    else
                        Idx++;
                }

                if (FWaterValueReqd && (popnValue == null))
                {
                    throw new Exception("Water uptake value not located for module " + this.Name);
                }

                if (popnValue != null)
                {
                    for (Jdx = 0; Jdx < popnValue.Length; Jdx++)                            // One entry per component for which a demand was given                       
                    {
                        compValue = popnValue[Jdx];

                        iComp = stSEEDL;
                        while ((iComp <= stSENC) && (compValue.name != sCOMPNAME[iComp]))
                            iComp++;
                        if (iComp == stSEEDL || iComp == stESTAB || iComp == stSENC)
                            Layers2LayerArray(compValue.layer, ref FTranspiration[iComp], true);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="LayerA"></param>
        /// <param name="bLayersOuter"></param>
        /// <param name="iDataField"></param>
        protected void Layers2LayerArray(SoilLayer[] layers, ref double[] LayerA, bool bLayersOuter = false, int iDataField = 2)
        {
            int Ldx;
            SoilLayer soilLayer;

            if (bLayersOuter)
            {
                FNoInputLayers = layers.Length;
                for (Ldx = 0; Ldx < FNoInputLayers; Ldx++)
                {
                    soilLayer = layers[Ldx];
                    FInputProfile[Ldx + 1] = soilLayer.thickness;
                    if (iDataField == 2)
                        FLayerValues[Ldx + 1] = soilLayer.amount;
                }
            }
            else
            {   //// TODO: check if unused - uses layer[], value[]
                ////setInputProfile(aValue[1]);
                ////Value2LayerArray(aValue[2], ref FLayerValues);
            }
            Input2LayerProfile(FLayerValues, true, ref LayerA);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Values1"></param>
        /// <param name="bAsAverage"></param>
        /// <param name="Values2"></param>
        protected void Input2LayerProfile(double[] Values1, bool bAsAverage, ref double[] Values2)
        {
            if (bAsAverage)
                MakeLayersAsAverage(FInputProfile, FNoInputLayers, Values1, FLayerProfile, FNoLayers, ref Values2);
            else
                MakeLayersAsFlow(FInputProfile, FNoInputLayers, Values1, FLayerProfile, FNoLayers, ref Values2);
        }

        protected const double EPS = 1.0E-5;
        protected const double GM2_KGHA = 10.0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Layers1"></param>
        /// <param name="iNoLayers1"></param>
        /// <param name="Values1"></param>
        /// <param name="Layers2"></param>
        /// <param name="iNoLayers2"></param>
        /// <param name="Values2"></param>
        protected void MakeLayersAsAverage(double[] Layers1,
                               int iNoLayers1,
                               double[] Values1,
                               double[] Layers2,
                               int iNoLayers2,
                               ref double[] Values2)
        {
            int iSameLayers;
            double fTopDepth;
            double fBaseDepth1;
            double fBaseDepth2;
            int Ldx1, Ldx2;

            Values2 = new double[GrazType.MaxSoilLayers + 1];

            // Deal quickly with the case where layers are the same     
            Ldx1 = 1;
            while ((Ldx1 <= iNoLayers1)
                  && (Ldx1 <= iNoLayers2)
                  && (Math.Abs(Layers1[Ldx1] - Layers2[Ldx1]) < 2.0 * EPS))
            {
                Values2[Ldx1] = Values1[Ldx1];
                Ldx1++;
            }
            iSameLayers = Ldx1 - 1;

            if (iSameLayers < iNoLayers2)
            {
                // We have found a layer mismatch.
                fTopDepth = 0.0;
                for (Ldx1 = 1; Ldx1 <= iSameLayers; Ldx1++)
                {
                    fTopDepth = fTopDepth + Layers1[Ldx1];
                }

                fBaseDepth1 = fTopDepth + Layers1[iSameLayers + 1];
                fBaseDepth2 = fTopDepth + Layers2[iSameLayers + 1];

                Ldx1 = iSameLayers + 1;
                Ldx2 = iSameLayers + 1;
                while ((Ldx1 <= iNoLayers1) && (Ldx2 <= iNoLayers2))
                {
                    if (fBaseDepth1 < fBaseDepth2 + EPS)
                    {
                        // Base of data layer shallower than or equal to base of soil budget layer     
                        Values2[Ldx2] = Values2[Ldx2] + Values1[Ldx1] * (fBaseDepth1 - fTopDepth);

                        Ldx1++;
                        fTopDepth = fBaseDepth1;
                        fBaseDepth1 = fTopDepth + Values1[Ldx1];

                        if (Math.Abs(fBaseDepth1 - fBaseDepth2) < 2.0 * EPS)
                        {
                            // Layer bases at same depth - increment both
                            Ldx2++;
                            fBaseDepth2 = fBaseDepth2 + Layers2[Ldx2];
                        }
                    }
                    else
                    {
                        // Base of data layer deeper than base of soil budget layer                  
                        Values2[Ldx2] = Values2[Ldx2] + Values1[Ldx1] * (fBaseDepth2 - fTopDepth);
                        fTopDepth = fBaseDepth2;
                        Ldx2++;
                        fBaseDepth2 = fBaseDepth2 + Layers2[Ldx2];
                    }

                    // Complete the average                  
                    for (Ldx2 = iSameLayers + 1; Ldx2 <= iNoLayers2; Ldx2++)
                    {
                        Values2[Ldx2] = Values2[Ldx2] / Layers2[Ldx2];
                    }
                } // _ if (iSameLayers < Model.Params.LastLayer) _
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Layers1"></param>
        /// <param name="iNoLayers1"></param>
        /// <param name="Values1"></param>
        /// <param name="Layers2"></param>
        /// <param name="iNoLayers2"></param>
        /// <param name="Values2"></param>
        protected void MakeLayersAsFlow(double[] Layers1, int iNoLayers1,
                               double[] Values1,
                               double[] Layers2, int iNoLayers2,
                               ref double[] Values2)
        {
            double fBaseDepth1;
            double fBaseDepth2;
            int Ldx1, Ldx2;

            Values2 = new double[GrazType.MaxSoilLayers + 1];

            Ldx1 = 0;
            fBaseDepth1 = 0.0;
            fBaseDepth2 = 0.0;
            for (Ldx2 = 1; Ldx2 <= iNoLayers2; Ldx2++)
            {
                fBaseDepth2 = fBaseDepth2 + Layers2[Ldx2];
                while ((Ldx1 <= iNoLayers1) && (fBaseDepth2 > fBaseDepth1 + EPS))
                {
                    Ldx1++;
                    fBaseDepth1 = fBaseDepth1 + Layers1[Ldx1];
                }
                Values2[Ldx2] = Values1[Ldx1];
            }
        }

        /// <summary>
        /// Store proportion of the soil volume occupied by roots
        /// </summary>
        /// <param name="aValue"></param>
        private void StoreSoilPropn(SoilFract[] aValue)
        {
            uint Idx;
            uint Jdx;
            int iComp;
            SoilPopItem[] popnValue;
            SoilFract aValueItem;
            SoilPopItem compValue;

            if (aValue != null)
            {
                FSoilAllocated = true;

                popnValue = null;
                Idx = 0;
                while ((Idx < aValue.Length) && (popnValue == null))
                {
                    aValueItem = aValue[Idx];
                    if (aValueItem.population == this.Name)   //// TODO: check this name
                    {
                        popnValue = aValueItem.element;
                    }
                    else
                        Idx++;
                }

                if (popnValue != null)
                {
                    for (Jdx = 0; Jdx < popnValue.Length; Jdx++)                         // One entry per component for which a demand was given                      
                    {
                        compValue = popnValue[Jdx];
                        iComp = stSEEDL;
                        while ((iComp <= stSENC) && (compValue.name != sCOMPNAME[iComp]))
                            iComp++;
                        if (iComp == stSEEDL || iComp == stESTAB || iComp == stSENC)
                            Layers2LayerArray(compValue.layer, ref FSoilPropn[iComp], true);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nutrValue"></param>
        /// <param name="NutrD"></param>
        protected void Value2SoilNutrient(NutrAvail nutrValue, ref GrazType.TSoilNutrientDistn NutrD)
        {
            Nutrient areaValue;
            uint Idx;

            if (nutrValue != null)
            {
                setInputProfile(nutrValue.layers);

                NutrD.NoAreas = nutrValue.nutrient.Length;
                for (Idx = 0; Idx <= NutrD.NoAreas - 1; Idx++)
                {
                    areaValue = nutrValue.nutrient[Idx + 1];
                    if (NutrD.NoAreas > 1)
                        NutrD.RelAreas[Idx] = areaValue.area_fract;
                    else
                        NutrD.RelAreas[Idx] = 1.0;
                    Value2LayerArray(areaValue.soiln_conc, ref FLayerValues);
                    Input2LayerProfile(FLayerValues, true, ref NutrD.SolnPPM[Idx]);
                    Value2LayerArray(areaValue.avail_nutr, ref FLayerValues);
                    Input2LayerProfile(FLayerValues, true, ref NutrD.AvailKgHa[Idx]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aValue"></param>
        protected void setInputProfile(double[] aValue)
        {
            FNoInputLayers = aValue.Length;
            Value2LayerArray(aValue, ref FInputProfile);
        }

        #endregion
    }
}
