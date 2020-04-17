using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System.Linq;
using Models.Functions.DemandFunctions;
using Models.Functions.SupplyFunctions;

namespace Models.PMF.Organs
{

    /// <summary>
    /// This organ is simulated using a SimpleLeaf organ type.  It provides the core functions of intercepting radiation, producing biomass
    ///  through photosynthesis, and determining the plant's transpiration demand.  The model also calculates the growth, senescence, and
    ///  detachment of leaves.  SimpleLeaf does not distinguish leaf cohorts by age or position in the canopy.
    /// 
    /// Radiation interception and transpiration demand are computed by the MicroClimate model.  This model takes into account
    ///  competition between different plants when more than one is present in the simulation.  The values of canopy Cover, LAI, and plant
    ///  Height (as defined below) are passed daily by SimpleLeaf to the MicroClimate model.  MicroClimate uses an implementation of the
    ///  Beer-Lambert equation to compute light interception and the Penman-Monteith equation to calculate potential evapotranspiration.  
    ///  These values are then given back to SimpleLeaf which uses them to calculate photosynthesis and soil water demand.
    /// </summary>
    /// <remarks>
    /// NOTE: the summary above is used in the Apsim's autodoc.
    /// 
    /// SimpleLeaf has two options to define the canopy: the user can either supply a function describing LAI or a function describing canopy cover directly.  From either of these functions SimpleLeaf can obtain the other property using the Beer-Lambert equation with the specified value of extinction coefficient.
    /// The effect of growth rate on transpiration is captured by the Fractional Growth Rate (FRGR) function, which is passed to the MicroClimate model.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SorghumLeaf : Model, IHasWaterDemand, IOrgan, IArbitration, ICustomDocumentation, IOrganDamage, ICanopy
    {
        //IHasWaterDemand, removing to see if it's necessary

        #region Canopy interface

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return Plant.CropType; } }

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>The effect of CO2 on stomatal conductance</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction StomatalConductanceCO2Modifier = null;

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax
        {
            get
            {
                return Gsmax350 * FRGR * StomatalConductanceCO2Modifier.Value();
            }
        }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Maximum stomatal conductance at CO2 concentration of 350 ppm (m/s)")]
        public double Gsmax350 { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50")]
        public double R50 { get; set; }

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double BaseHeight { get; set; }

        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth { get { return Math.Max(0, Height - BaseHeight); } }

        /// <summary>The width of an individual plant</summary>
        [Units("mm")]
        public double Width { get; set; }

        /// <summary>Gets or sets the FRGR.</summary>
        [Units("mm")]
        public double FRGR { get; set; }

        private double _PotentialEP = 0;
        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP
        {
            get { return _PotentialEP; }
            set
            {
                _PotentialEP = value;
                MicroClimatePresent = true;
            }
        }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }
        #endregion

        /// <summary>
        /// The SMM2SM
        /// </summary>
        public const double smm2sm = 1.0 / 1000000.0;      //! conversion factor of mm^2 to m^2

        /// <summary>The plant</summary>
        [Link]
        public Plant Plant = null; //todo change back to private

        [Link]
        ISummary Summary = null;

        [Link]
        private LeafCulms culms = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather MetData = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction maxLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction TTEmergToFI = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafInitRate = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private IFunction LeafNumSeed = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction SDRatio = null;

        [Link(Type = LinkType.Path, Path = "[Phenology].DltTT")]
        private IFunction DltTT { get; set; }

        #region Canopy interface

        /// <summary>
        /// Number of leaf primordia present in seed.
        /// </summary>
        public double SeedNo
        {
            get
            {
                return LeafNumSeed?.Value() ?? 0;
            }
        }

        /// <summary>
        /// Degree days to initiate each leaf primordium until floral init (deg day).
        /// </summary>
        public double LeafInitRate
        {
            get
            {
                return leafInitRate?.Value() ?? 0;
            }
        }

        /// <summary>
        /// Min Leaf Number.
        /// </summary>
        public double MinLeafNo
        {
            get
            {
                return minLeafNo.Value();
            }
        }

        /// <summary>
        /// Max Leaf Number.
        /// </summary>
        public double MaxLeafNo
        {
            get
            {
                return maxLeafNo.Value();
            }
        }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double DltLAI { get; set; }
        
        /// <summary>Gets the Potential DltLAI</summary>
        [Units("m^2/m^2")]
        public double dltPotentialLAI { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double dltStressedLAI { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal { get { return LAI + LAIDead; } }

        /// <summary>Gets the LAI</summary>
        public double SLN { get; set; }

        /// <summary>Used in metabolic ndemand calc.</summary>
        public double SLN0 { get; set; }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen { get; set; }

        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get; set; }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal
        {
            get { return 1.0 - (1 - CoverGreen) * (1 - CoverDead); }
        }

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        [Units("mm")]
        public double WaterDemand { get; set; }

        /// <summary>
        /// Flag to test if Microclimate is present
        /// </summary>
        public bool MicroClimatePresent { get; set; } = false;

        #endregion

        #region Parameters

        /// <summary>The extinction coefficient function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        IFunction ExtinctionCoefficientFunction = null;
        
        /// <summary>The photosynthesis</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction Photosynthesis = null;

        /// <summary>External calculation for largest leaf size.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction LargestLeafSize = null;

        /// <summary>The height function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction HeightFunction = null;

        /// <summary>Water Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        IFunction WaterDemandFunction = null;

        /// <summary>DM Fixation Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction DMSupplyFixation = null;

        /// <summary>DM Fixation Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PotentialBiomassTEFunction = null;
        
        /// <summary>Input for NewLeafSLN</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction NewLeafSLN = null;
        
        /// <summary>Input for TargetSLN</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction TargetSLN = null;

        /// <summary>Input for SenescedLeafSLN.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction SenescedLeafSLN = null;

        /// <summary>Intercept for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction NDilutionIntercept = null;

        /// <summary>Slope for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction NDilutionSlope = null;

        /// <summary>Slope for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction MinPlantWt = null;

        /// <summary>/// The aX0 for this Culm </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction AX0 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senLightTimeConst = null;

        /// <summary>Temperature threshold for leaf death.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frostKill = null;

        /// <summary>Delay factor for water senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senWaterTimeConst = null;

        /// <summary>supply:demand ratio for onset of water senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senThreshold = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nPhotoStress = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoDeadIntercept = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoDeadSlope = null;

        /// <summary>Potential Biomass via Radiation Use Efficientcy.</summary>
        public double BiomassRUE { get; set; }

        /// <summary>Potential Biomass via Radiation Use Efficientcy.</summary>
        public double BiomassTE { get; set; }

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";

        #endregion

        #region States and variables

        /// <summary>Gets or sets the k dead.</summary>
        public double KDead { get; set; }                  // Extinction Coefficient (Dead)
        /// <summary>Calculates the water demand.</summary>
        public double CalculateWaterDemand()
        {
            if (WaterDemandFunction != null)
                return WaterDemandFunction.Value();
            else
            {
                return WaterDemand;
            }
        }
        /// <summary>Gets the transpiration.</summary>
        public double Transpiration { get { return WaterAllocation; } }
        
        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }


        /// <summary>Gets the RAD int tot.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadIntTot
        {
            get
            {
                return CoverGreen * MetData.Radn;
            }
        }

        /// <summary>Stress.</summary>
        [Description("Nitrogen Photosynthesis Stress")]
        public double NitrogenPhotoStress { get; set; }

        /// <summary>Stress.</summary>
        [Description("Nitrogen Phenology Stress")]
        public double NitrogenPhenoStress { get; set; }

        /// <summary>Stress.</summary>
        [Description("Phosphorus Stress")]
        public double PhosphorusStress { get; set; }

        /// <summary>Final Leaf Number.</summary>
        public double FinalLeafNo { get { return culms.FinalLeafNo; } }

        /// <summary>Leaf number.</summary>
        public double LeafNo { get { return culms.LeafNo; } }

        /// <summary> /// Sowing Density (Population). /// </summary>
        public double SowingDensity { get; set; }

        private bool LeafInitialised = false;
        #endregion

        #region Arbitrator Methods
        /// <summary>Gets or sets the water allocation.</summary>
        [XmlIgnore]
        public double WaterAllocation { get; set; }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        private void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.Reallocation = AvailableDMReallocation();
            DMSupply.Retranslocation = AvailableDMRetranslocation();
            DMSupply.Uptake = 0;
            DMSupply.Fixation = DMSupplyFixation.Value();
        }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        public double MaintenanceRespiration { get; }

        /// <summary>Remove maintenance respiration from live component of organs.</summary>
        public void RemoveMaintenanceRespiration(double respiration)
        { }

        #endregion

        #region Events

        /// <summary>
        /// Calculates final leaf number. Doesn't update any globals.
        /// </summary>
        /// <returns></returns>
        private double CalcFinalLeafNo()
        {
            double ttFi = TTEmergToFI.Value();
            return MathUtilities.Bound(MathUtilities.Divide(ttFi, LeafInitRate, 0) + SeedNo, MinLeafNo, MaxLeafNo);
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == LeafInitialisationStage)
            {
                LeafInitialised = true;

                Live.StructuralWt = initialWtFunction.Value() * SowingDensity;
                Live.StorageWt = 0.0;
                LAI = initialLAIFunction.Value() * smm2sm * SowingDensity;
                SLN = initialSLNFunction.Value();

                Live.StructuralN = LAI * SLN;
                Live.StorageN = 0;
            }
        }

        #endregion

        #region Component Process Functions

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
            DMSupply.Clear();
            NSupply.Clear();
            DMDemand.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
            Height = 0;

            LeafInitialised = false;
            laiEqlbLightTodayQ = new Queue<double>(10);
            //sdRatio = 0.0;
            totalLaiEqlbLight = 0.0;
            avgLaiEquilibLight = 0.0;

            laiEquilibWaterQ = new Queue<double>(10);
            sdRatioQ = new Queue<double>(5);
            totalLaiEquilibWater = 0;
            totalSDRatio = 0.0;
            avSDRatio = 0.0;

            LAI = 0;
            SLN = 0;
            SLN0 = 0;
            Live.StructuralN = 0;
            Live.StorageN = 0;

            DltSenescedLaiN = 0.0;
            SenescedLai = 0.0;
            CoverGreen = 0.0;
            CoverDead = 0.0;
            LAIDead = 0.0;
            LossFromExpansionStress = 0.0;
            culms.Initialize();
        }
        #endregion

        #region Top Level time step functions

        [EventSubscribe("StartOfDay")]
        private void ResetDailyVariables(object sender, EventArgs e)
        {
            BiomassRUE = 0;
            BiomassTE = 0;
            DltLAI = 0;
            DltSenescedLai = 0;
            DltSenescedLaiAge = 0;
            DltSenescedLaiFrost = 0;
            DltSenescedLaiLight = 0;
            DltSenescedLaiN = 0;
            DltSenescedLaiWater = 0;
            DltSenescedN = 0;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (parentPlant.IsEmerged)
                StartLive = ReflectionUtilities.Clone(Live) as Biomass;
            dltPotentialLAI = 0;
            dltStressedLAI = 0;
            if (LeafInitialised)
            {
                culms.CalcPotentialArea();

                //old model calculated BiomRUE at the end of the day
                //this is done at strat of the day
                BiomassRUE = Photosynthesis.Value();
                //var bimT = 0.009 / waterFunction.VPD / 0.001 * Arbitrator.WSupply;
                BiomassTE = PotentialBiomassTEFunction.Value();

                Height = HeightFunction.Value();

                LAIDead = SenescedLai;
            }
        }

        /// <summary>Update area.</summary>
        public void UpdateArea()
        {
            if (Plant.IsEmerged)
            {
                //areaActual in old model
                // culms.AreaActual() will update this.DltLAI
                culms.AreaActual();
                senesceArea();
            }
        }


        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            // if (!parentPlant.IsAlive) return; wtf
            if (!Plant.IsAlive) return;
            if (!LeafInitialised) return;

            calcSenescence();

            //double slnToday = MathUtilities.Divide(Live.N, laiToday, 0.0);
            //DltSenescedN = DltSenescedLai * Math.Max((slnToday - SenescedLeafSLN.Value()), 0.0);
            //double slnToday = laiToday > 0.0 ? Live.N / laiToday : 0.0;
            //var DltSenescedN = DltSenescedLai * Math.Max((slnToday - senescedLeafSLN), 0.0);

            //UpdateVars
            SenescedLai += DltSenescedLai;
            nDeadLeaves += dltDeadLeaves;
            dltDeadLeaves = 0;

            LAI += DltLAI - DltSenescedLai;

            int flag = 6; //= phenology.StartStagePhaseIndex("FlagLeaf");
            if (phenology.Stage >= flag)
            {
                if (LAI - DltSenescedLai < 0.1)
                {
                    string message = "Crop failed due to loss of leaf area \r\n";
                    Summary.WriteMessage(this, message);
                //scienceAPI.write(" ********** Crop failed due to loss of leaf area ********");
                Plant.EndCrop();
                    return;
                }
            }
            LAIDead = SenescedLai; // drew todo
            SLN = MathUtilities.Divide(Live.N, LAI, 0);
            
            CoverGreen = MathUtilities.Bound(1.0 - Math.Exp(-ExtinctionCoefficientFunction.Value() * LAI), 0.0, 0.999999999);// limiting to within 10^-9, so MicroClimate doesn't complain
            CoverDead = MathUtilities.Bound(1.0 - Math.Exp(-ExtinctionCoefficientFunction.Value() * LAIDead), 0.0, 0.999999999);
            //var photoStress = (2.0 / (1.0 + Math.Exp(-6.05 * (SLN - 0.41))) - 1.0);

            NitrogenPhotoStress = nPhotoStress.Value(); // Math.Max(photoStress, 0.0);

            NitrogenPhenoStress = 1.0;
            if (phenology.Between("Emergence", "Flowering"))
            {
                var phenoStress = (1.0 / 0.7) * SLN * 1.25 - (3.0 / 7.0);
                NitrogenPhenoStress = MathUtilities.Bound(phenoStress, 0.0, 1.0);
            }
        }

        /// <summary>Radiation level for onset of light senescence.</summary>
        public double SenRadnCrit
        {
            get
            {
                return senRadnCrit.Value();
            }
        }

        /// <summary>sen_light_time_const.</summary>
        public double SenLightTimeConst { get { return senLightTimeConst.Value(); } }

        /// <summary>temperature threshold for leaf death.</summary>
        public double FrostKill { get { return frostKill.Value(); } }

        /// <summary>supply:demand ratio for onset of water senescence.</summary>
        public double SenThreshold { get { return senThreshold.Value(); } }

        /// <summary>Delay factor for water senescence.</summary>
        public double SenWaterTimeConst { get { return senWaterTimeConst.Value(); } }

        /// <summary>Only water stress at this stage.</summary>
        /// Diff between potentialLAI and stressedLAI
        public double LossFromExpansionStress { get; set; }

        /// <summary>Total LAII as a result of senescence.</summary>
        public double SenescedLai { get; set; }

        /// <summary>Amount of N retranslocated today.</summary>
        public double DltRetranslocatedN { get; set; }
        /// <summary>Delta of N removed due to Senescence.</summary>
        public double DltSenescedN { get; set; }
        /// <summary>Delta of LAI removed due to N Senescence.</summary>
        public double DltSenescedLaiN { get; set; }
        /// <summary>Delta of LAI removed due to Senescence.</summary>
        public double DltSenescedLai { get; set; }
        /// <summary>Delta of LAI removed due to Light Senescence.</summary>
        public double DltSenescedLaiLight { get; set; }
        /// <summary>Delta of LAI removed due to Water Senescence.</summary>
        public double DltSenescedLaiWater { get; set; }
        /// <summary>Delta of LAI removed due to Frost Senescence.</summary>
        public double DltSenescedLaiFrost { get; set; }
        /// <summary>Delta of LAI removed due to age senescence.</summary>
        public double DltSenescedLaiAge { get; set; }

        private double nDeadLeaves;
        private double dltDeadLeaves;
        
        private double totalLaiEqlbLight;
        private double avgLaiEquilibLight;
        private Queue<double> laiEqlbLightTodayQ;
        private double updateAvLaiEquilibLight(double laiEqlbLightToday, int days)
        {
            totalLaiEqlbLight += laiEqlbLightToday;
            laiEqlbLightTodayQ.Enqueue(laiEqlbLightToday);
            if (laiEqlbLightTodayQ.Count > days)
            {
                totalLaiEqlbLight -= laiEqlbLightTodayQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEqlbLight, laiEqlbLightTodayQ.Count, 0);
        }

        private double totalLaiEquilibWater;
        private double avLaiEquilibWater;
        private Queue<double> laiEquilibWaterQ;
        private double updateAvLaiEquilibWater(double valToday, int days)
        {
            totalLaiEquilibWater += valToday;
            laiEquilibWaterQ.Enqueue(valToday);
            if (laiEquilibWaterQ.Count > days)
            {
                totalLaiEquilibWater -= laiEquilibWaterQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEquilibWater, laiEquilibWaterQ.Count, 0);
        }

        private double totalSDRatio;
        private double avSDRatio;
        private Queue<double> sdRatioQ;
        private double updateAvSDRatio(double valToday, int days)
        {
            totalSDRatio += valToday;
            sdRatioQ.Enqueue(valToday);
            if (sdRatioQ.Count > days)
            {
                totalSDRatio -= sdRatioQ.Dequeue();
            }
            return MathUtilities.Divide(totalSDRatio, sdRatioQ.Count, 0);
        }

        /// <summary>Senesce the LEaf Area.</summary>
        private void senesceArea()
        {
            DltSenescedLai = 0.0;
            DltSenescedLaiN = 0.0;

            DltSenescedLaiAge = 0;
            if (phenology.Between("Emergence", "HarvestRipe"))
                DltSenescedLaiAge = CalcLaiSenescenceAge();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiAge);

            //sLai - is the running total of dltSLai
            //could be a stage issue here. should only be between fi and flag
            LossFromExpansionStress += (dltPotentialLAI - dltStressedLAI);
            var maxLaiPossible = LAI + SenescedLai - LossFromExpansionStress;

            DltSenescedLaiLight = calcLaiSenescenceLight();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiLight);

            DltSenescedLaiWater = calcLaiSenescenceWater();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiWater);

            DltSenescedLaiFrost = calcLaiSenescenceFrost();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiFrost);

            DltSenescedLai = Math.Min(DltSenescedLai, LAI);
        }

        private double calcLaiSenescenceFrost()
        {
            //  calculate senecence due to frost
            double dltSlaiFrost = 0.0;
            if (MetData.MinT < FrostKill)
            {
                if(phenology.Between("Germination", "FloralInitiation"))
                {
                    dltSlaiFrost = Math.Max(0.0, LAI - 0.01);
                }
                else
                {
                    dltSlaiFrost = LAI;
                }

            }

            return dltSlaiFrost;
        }

        private double calcLaiSenescenceWater()
        {
            //watSupply is calculated in SorghumArbitrator:StoreWaterVariablesForNitrogenUptake
            //Arbitrator.WatSupply = Plant.Root.PlantAvailableWaterSupply();
            double dlt_dm_transp = PotentialBiomassTEFunction.Value();

            //double radnCanopy = divide(plant->getRadnInt(), coverGreen, plant->today.radn);
            double effectiveRue = MathUtilities.Divide(Photosynthesis.Value(), RadIntTot, 0);

            double radnCanopy = MathUtilities.Divide(RadIntTot, CoverGreen, MetData.Radn);
            if (MathUtilities.FloatsAreEqual(CoverGreen, 0))
                radnCanopy = 0;

            double sen_radn_crit = MathUtilities.Divide(dlt_dm_transp, effectiveRue, radnCanopy);
            double intc_crit = MathUtilities.Divide(sen_radn_crit, radnCanopy, 1.0);
            if (MathUtilities.FloatsAreEqual(sen_radn_crit, 0))
                intc_crit = 0;

            //            ! needs rework for row spacing
            double laiEquilibWaterToday;
            if (intc_crit < 1.0)
                laiEquilibWaterToday = -Math.Log(1.0 - intc_crit) / ExtinctionCoefficientFunction.Value();
            else
                laiEquilibWaterToday = LAI;

            avLaiEquilibWater = updateAvLaiEquilibWater(laiEquilibWaterToday, 10);

            avSDRatio = updateAvSDRatio(SDRatio.Value(), 5);
            //// average of the last 10 days of laiEquilibWater`
            //laiEquilibWater.push_back(laiEquilibWaterToday);
            //double avLaiEquilibWater = movingAvgVector(laiEquilibWater, 10);

            //// calculate a 5 day moving average of the supply demand ratio
            //avSD.push_back(plant->water->getSdRatio());

            double dltSlaiWater = 0.0;
            if (SDRatio.Value() < senThreshold.Value())
                dltSlaiWater = Math.Max(0.0, MathUtilities.Divide((LAI - avLaiEquilibWater), senWaterTimeConst.Value(), 0.0));
            dltSlaiWater = Math.Min(LAI, dltSlaiWater);
            return dltSlaiWater;
            //return 0.0;
        }
        
        /// <summary>Return the lai that would senesce on the current day from natural ageing</summary>
        private double CalcLaiSenescenceAge()
        {
            dltDeadLeaves = CalcDltDeadLeaves();
            double deadLeaves = nDeadLeaves + dltDeadLeaves;
            double laiSenescenceAge = 0;
            if (MathUtilities.IsPositive(deadLeaves))
            {
                int leafDying = (int)Math.Ceiling(deadLeaves);
                double areaDying = (deadLeaves % 1.0) * culms.LeafSizes[leafDying - 1];
                laiSenescenceAge = (culms.LeafSizes.Take(leafDying - 1).Sum() + areaDying) * smm2sm * SowingDensity;
            }
            return Math.Max(laiSenescenceAge - SenescedLai, 0);
        }

        private double CalcDltDeadLeaves()
        {
            double nDeadYesterday = nDeadLeaves;
            double nDeadToday = FinalLeafNo * (leafNoDeadIntercept.Value() + leafNoDeadSlope.Value() * phenology.AccumulatedEmergedTT);
            nDeadToday = MathUtilities.Bound(nDeadToday, nDeadYesterday, FinalLeafNo);
            return nDeadToday - nDeadYesterday;
        }

        private double calcLaiSenescenceLight()
        {
            double critTransmission = MathUtilities.Divide(SenRadnCrit, MetData.Radn, 1);
            /* TODO : Direct translation - needs cleanup */
            //            ! needs rework for row spacing
            double laiEqlbLightToday;
            if (critTransmission > 0.0)
            {
                laiEqlbLightToday = -Math.Log(critTransmission) / ExtinctionCoefficientFunction.Value();
            }
            else
            {
                laiEqlbLightToday = LAI;
            }
            // average of the last 10 days of laiEquilibLight
            avgLaiEquilibLight = updateAvLaiEquilibLight(laiEqlbLightToday, 10);//senLightTimeConst?

            // dh - In old apsim, we had another variable frIntcRadn which is always set to 0.
            // Set Plant::radnInt(void) in Plant.cpp.
            double radnInt = MetData.Radn * CoverGreen;
            double radnTransmitted = MetData.Radn - radnInt;
            double dltSlaiLight = 0.0;
            if (radnTransmitted < SenRadnCrit)
                dltSlaiLight = Math.Max(0.0, MathUtilities.Divide(LAI - avgLaiEquilibLight, SenLightTimeConst, 0.0));
            dltSlaiLight = Math.Min(dltSlaiLight, LAI);
            return dltSlaiLight;
        }

        private void calcSenescence()
        {
            // Derives seneseced plant dry matter (g/m^2) for the day
            //Should not include any retranloocated biomass
            // dh - old apsim does not take into account DltSenescedLai for this laiToday calc
            double laiToday = LAI + DltLAI/* - DltSenescedLai*/; // how much LAI we will end up with at end of day
            double slaToday = MathUtilities.Divide(laiToday, Live.Wt, 0.0); // m2/g?

            if (MathUtilities.IsPositive(Live.Wt))
            {
                // This is equivalent to dividing by slaToday
                double DltSenescedBiomass = Live.Wt * MathUtilities.Divide(DltSenescedLai, laiToday, 0);
                double SenescingProportion = DltSenescedBiomass / Live.Wt;

                if (MathUtilities.IsGreaterThan(DltSenescedBiomass, Live.Wt))
                    throw new Exception($"Attempted to senesce more biomass than exists on leaf '{Name}'");
                if (MathUtilities.IsPositive(DltSenescedBiomass))
                {
                    var structuralWtSenescing = Live.StructuralWt * SenescingProportion;
                    Live.StructuralWt -= structuralWtSenescing;
                    Dead.StructuralWt += structuralWtSenescing;
                    Senesced.StructuralWt += structuralWtSenescing;

                    var metabolicWtSenescing = Live.MetabolicWt * SenescingProportion;
                    Live.MetabolicWt -= metabolicWtSenescing;
                    Dead.MetabolicWt += metabolicWtSenescing;
                    Senesced.MetabolicWt += metabolicWtSenescing;

                    var storageWtSenescing = Live.StorageWt * SenescingProportion;
                    Live.StorageWt -= storageWtSenescing;
                    Dead.StorageWt += storageWtSenescing;
                    Senesced.StorageWt += storageWtSenescing;

                    double slnToday = MathUtilities.Divide(Live.N, laiToday, 0.0);
                    DltSenescedN += DltSenescedLai * Math.Max((slnToday - SenescedLeafSLN.Value()), 0.0);

                    SenescingProportion = DltSenescedN / Live.N;

                    if (MathUtilities.IsGreaterThan(DltSenescedN, Live.N))
                        throw new Exception($"Attempted to senesce more N than exists on leaf '{Name}'");

                    var structuralNSenescing = Live.StructuralN * SenescingProportion;
                    Live.StructuralN -= structuralNSenescing;
                    Dead.StructuralN += structuralNSenescing;
                    Senesced.StructuralN += structuralNSenescing;

                    var metabolicNSenescing = Live.MetabolicN * SenescingProportion;
                    Live.MetabolicN -= metabolicNSenescing;
                    Dead.MetabolicN += metabolicNSenescing;
                    Senesced.MetabolicN += metabolicNSenescing;

                    var storageNSenescing = Live.StorageN * SenescingProportion;
                    Live.StorageN -= storageNSenescing;
                    Dead.StorageN += storageNSenescing;
                    Senesced.StorageN += storageNSenescing;
                }
            }
        }
        

        #endregion

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double BiomassToleranceValue = 0.0000000001;

        /// <summary>The parent plant</summary>
        [Link]
        private Plant parentPlant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The senescence rate function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        protected IFunction SenescenceRate = null;

        /// <summary>Radiation level for onset of light senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("Mj/m^2")]
        private IFunction senRadnCrit = null;

        ///// <summary>The N retranslocation factor</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("/d")]
        //protected IFunction NRetranslocationFactor = null;

        ///// <summary>The N reallocation factor</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("/d")]
        //protected IFunction nReallocationFactor = null;

        // NOT CURRENTLY USED /// <summary>The nitrogen demand switch</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //private IFunction nitrogenDemandSwitch = null;

        ///// <summary>The DM retranslocation factor</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("/d")]
        //private IFunction dmRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand dmDemands = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand nDemands = null;

        /// <summary>The initial biomass dry matter weight</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        private IFunction initialWtFunction = null;

        /// <summary>The initial biomass dry matter weight</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        private IFunction initialLAIFunction = null;

        /// <summary>The initial SLN value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2")]
        private IFunction initialSLNFunction = null;

        /// <summary>The maximum N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction maximumNConc = null;

        /// <summary>The minimum N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction minimumNConc = null;

        /// <summary>The critical N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction criticalNConc = null;

//#pragma warning disable 414
        /// <summary>Carbon concentration</summary>
        /// [Units("-")]
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction CarbonConcentration = null;
//#pragma warning restore 414

        /// <summary>The live biomass state at start of the computation round</summary>
        public Biomass StartLive = null;

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemandPriorityFactor { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }

        /// <summary>Constructor</summary>
        public SorghumLeaf()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>The live biomass</summary>
        [XmlIgnore]
        public Biomass Live { get; private set; }

        /// <summary>The dead biomass</summary>
        [XmlIgnore]
        public Biomass Dead { get; private set; }

        /// <summary>Gets the total biomass</summary>
        [XmlIgnore]
        public Biomass Total { get { return Live + Dead; } }


        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [XmlIgnore]
        public Biomass Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [XmlIgnore]
        public Biomass Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [XmlIgnore]
        public Biomass Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [XmlIgnore]
        public Biomass Removed { get; private set; }

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation { get { return potentialDMAllocation; } }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public virtual double NFixationCost { get { return 0; } }

        /// <summary>Gets the maximum N concentration.</summary>
        [XmlIgnore]
        public double MaxNconc { get { return maximumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [XmlIgnore]
        public double MinNconc { get { return minimumNConc.Value(); } }

        /// <summary>Gets the minimum N concentration.</summary>
        [XmlIgnore]
        public double CritNconc { get { return criticalNConc.Value(); } }

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [XmlIgnore]
        public double Wt { get { return Live.Wt + Dead.Wt; } }

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [XmlIgnore]
        public double N { get { return Live.N + Dead.N; } }

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [XmlIgnore]
        public double Nconc
        {
            get
            {
                if (Wt > 0.0)
                    return N / Wt;
                else
                    return 0.0;
            }
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public virtual void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        public double AvailableDMRetranslocation()
        {
            var leafWt = StartLive.Wt + potentialDMAllocation.Total;
            var leafWtAvail = leafWt - MinPlantWt.Value() * SowingDensity;

            double availableDM = Math.Max(0.0,  leafWtAvail);

            // Don't retranslocate more DM than we have available.
            availableDM = Math.Min(availableDM, StartLive.Wt);
            return availableDM;
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            double availableDM = StartLive.StorageWt * SenescenceRate.Value() * dmReallocationFactor.Value();
            if (availableDM < -BiomassToleranceValue)
                throw new Exception("Negative DM reallocation value computed for " + Name);

            return availableDM;
        }

        /// <summary>
        /// calculates todays LAI values - can change during retranslocation calculations
        /// </summary>
        /// <returns></returns>
        public double calcLAI()
        {
            return Math.Max(0.0, LAI + DltLAI - DltSenescedLai);
        }
        private double calcSLN(double laiToday, double nGreenToday)
        {
            return MathUtilities.Divide(nGreenToday, laiToday, 0.0);
        }

        /// <summary>
        /// Adjustment function for calculating leaf demand.
        /// This should always be equal to -1 * structural N Demand.
        /// </summary>
        public double calculateClassicDemandDelta()
        {
            if (MathUtilities.IsNegative(Live.N))
                throw new Exception($"Negative N in sorghum leaf '{Name}'");
            //n demand as calculated in apsim classic is different ot implementation of structural and metabolic
            // Same as metabolic demand in new apsim.
            var classicLeafDemand = Math.Max(0.0, calcLAI() * TargetSLN.Value() - Live.N);
            //need to remove pmf nDemand calcs from totalDemand to then add in what it should be from classic
            var pmfLeafDemand = nDemands.Structural.Value() + nDemands.Metabolic.Value();

            var structural = nDemands.Structural.Value();
            var diff = classicLeafDemand - pmfLeafDemand;

            return classicLeafDemand - pmfLeafDemand;
        }

        /// <summary>Calculate the amount of N to retranslocate</summary>
        public double provideNRetranslocation(BiomassArbitrationType BAT, double requiredN, bool forLeaf)
        {
            int leafIndex = 2;
            double laiToday = calcLAI();
            //whether the retranslocation is added or removed is confusing
            //Leaf::CalcSLN uses - dltNRetranslocate - but dltNRetranslocate is -ve
            double dltNGreen = BAT.StructuralAllocation[leafIndex] + BAT.MetabolicAllocation[leafIndex];
            double nGreenToday = Live.N + dltNGreen + DltRetranslocatedN; //dltRetranslocation is -ve
            //double nGreenToday = Live.N + BAT.TotalAllocation[leafIndex] + BAT.Retranslocation[leafIndex];
            double slnToday = calcSLN(laiToday, nGreenToday);

            var dilutionN = DltTT.Value() * (NDilutionSlope.Value() * slnToday + NDilutionIntercept.Value()) * laiToday;
            dilutionN = Math.Max(dilutionN, 0);
            if(phenology.Between("Germination", "Flowering"))
            {
                // pre anthesis, get N from dilution, decreasing dltLai and senescence
                double nProvided = Math.Min(dilutionN, requiredN / 2.0);
                DltRetranslocatedN -= nProvided;
                nGreenToday -= nProvided; //jkb
                requiredN -= nProvided;
                if (requiredN <= 0.0001)
                    return nProvided;

                // decrease dltLai which will reduce the amount of new leaf that is produced
                if (MathUtilities.IsPositive(DltLAI))
                {
                    // Only half of the requiredN can be accounted for by reducing DltLAI
                    // If the RequiredN is large enough, it will result in 0 new growth
                    // Stem and Rachis can technically get to this point, but it doesn't occur in all of the validation data sets
                    double n = DltLAI * NewLeafSLN.Value();
                    double laiN = Math.Min(n, requiredN / 2.0);
                    // dh - we don't make this check in old apsim
                    if (MathUtilities.IsPositive(laiN))
                    {
                        DltLAI = (n - laiN) / NewLeafSLN.Value();
                        if (forLeaf)
                        {
                            // should we update the StructuralDemand?
                            //BAT.StructuralDemand[leafIndex] = nDemands.Structural.Value();
                            requiredN -= laiN;
                        }
                    }
                }

                // recalc the SLN after this N has been removed
                laiToday = calcLAI();
                slnToday = calcSLN(laiToday, nGreenToday);

                var maxN = DltTT.Value() * (NDilutionSlope.Value() * slnToday + NDilutionIntercept.Value()) * laiToday;
                maxN = Math.Max(maxN, 0);
                requiredN = Math.Min(requiredN, maxN);

                double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - SenescedLeafSLN.Value()), 0.0), 0.0);

                // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                senescenceLAI = Math.Min(senescenceLAI, Live.N * SenescedLeafSLN.Value() - DltSenescedN);

                double newN = Math.Max(senescenceLAI * (slnToday - SenescedLeafSLN.Value()), 0.0);
                DltRetranslocatedN -= newN;
                nGreenToday += newN; // local variable
                nProvided += newN;
                DltSenescedLaiN += senescenceLAI;

                DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                DltSenescedN += senescenceLAI * SenescedLeafSLN.Value();

                return nProvided;
            }
            else
            {
                // if sln > 1, dilution then senescence
                if(slnToday > 1.0)
                {
                    double nProvided = Math.Min(dilutionN, requiredN);
                    requiredN -= nProvided;
                    nGreenToday -= nProvided; //jkb
                    DltRetranslocatedN -= nProvided;

                    if (requiredN <= 0.0001)
                        return nProvided;

                    // rest from senescence
                    laiToday = calcLAI();
                    slnToday = calcSLN(laiToday, nGreenToday);

                    var maxN = DltTT.Value() * (NDilutionSlope.Value() * slnToday + NDilutionIntercept.Value()) * laiToday;
                    requiredN = Math.Min(requiredN, maxN);

                    double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - SenescedLeafSLN.Value()), 0.0), 0.0);

                    // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                    senescenceLAI = Math.Min(senescenceLAI, Live.N * SenescedLeafSLN.Value() - DltSenescedN);

                    double newN = Math.Max(senescenceLAI * (slnToday - SenescedLeafSLN.Value()), 0.0);
                    DltRetranslocatedN -= newN;
                    nGreenToday += newN;
                    nProvided += newN;
                    DltSenescedLaiN += senescenceLAI;
                    
                    DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                    DltSenescedN += senescenceLAI * SenescedLeafSLN.Value();
                    return nProvided;
                }
                else
                {
                    // half from dilution and half from senescence
                    double nProvided = Math.Min(dilutionN, requiredN / 2.0);
                    requiredN -= nProvided;
                    nGreenToday -= nProvided; //jkb // dh - this should be subtracted, not added
                    DltRetranslocatedN -= nProvided;

                    // rest from senescence
                    laiToday = calcLAI();
                    slnToday = calcSLN(laiToday, nGreenToday);

                    var maxN = DltTT.Value() * (NDilutionSlope.Value() * slnToday + NDilutionIntercept.Value()) * laiToday;
                    requiredN = Math.Min(requiredN, maxN);

                    double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - SenescedLeafSLN.Value()), 0.0), 0.0);

                    // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                    senescenceLAI = Math.Min(senescenceLAI, Live.N * SenescedLeafSLN.Value() - DltSenescedN);

                    double newN = Math.Max(senescenceLAI * (slnToday - SenescedLeafSLN.Value()), 0.0);
                    DltRetranslocatedN -= newN;
                    nGreenToday += newN;
                    nProvided += newN;
                    DltSenescedLaiN += senescenceLAI;
                    
                    DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                    DltSenescedN += senescenceLAI * SenescedLeafSLN.Value();
                    return nProvided;
                }
            }
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        protected virtual void SetNSupply(object sender, EventArgs e)
        {
            var availableLaiN = DltLAI * NewLeafSLN.Value();

            double laiToday = calcLAI();
            double nGreenToday = Live.N;
            double slnToday = MathUtilities.Divide(nGreenToday, laiToday, 0.0);

            var dilutionN = DltTT.Value() * ( NDilutionSlope.Value() * slnToday + NDilutionIntercept.Value()) * laiToday;

            NSupply.Retranslocation = Math.Max(0, Math.Min(StartLive.N, availableLaiN + dilutionN));

            //NSupply.Retranslocation = Math.Max(0, (StartLive.StorageN + StartLive.MetabolicN) * (1 - SenescenceRate.Value()) * NRetranslocationFactor.Value());
            if (NSupply.Retranslocation < -BiomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        protected virtual void SetDMDemand(object sender, EventArgs e)
        {
            DMDemand.Structural = dmDemands.Structural.Value(); // / dmConversionEfficiency.Value() + remobilisationCost.Value();
            DMDemand.Metabolic = Math.Max(0, dmDemands.Metabolic.Value());
            DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value()); // / dmConversionEfficiency.Value());
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        protected virtual void SetNDemand(object sender, EventArgs e)
        {
            //happening in potentialPlantPartitioning
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = nDemands.Metabolic.Value();
            NDemand.Storage = nDemands.Storage.Value();
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Sets the dry matter allocation.</summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public virtual void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // Check retranslocation
            if (MathUtilities.IsGreaterThan(dryMatter.Retranslocation, StartLive.StructuralWt))
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural, DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            Live.StructuralWt -= dryMatter.Retranslocation;
            Allocated.StructuralWt -= dryMatter.Retranslocation;

        }

        /// <summary>Sets the n allocation.</summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public virtual void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            SLN0 = MathUtilities.Divide(Live.N, LAI, 0);

            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            // Retranslocation
            ////TODO check what this is guarding - not sure on the relationship between NSupply and nitrogen
            //if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, StartLive.StorageN + StartLive.MetabolicN - NSupply.Retranslocation))
            //    throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            //sorghum can utilise structural as well
            //if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, StartLive.StorageN + StartLive.MetabolicN))
            //    throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            if (nitrogen.Retranslocation > Live.StorageN + Live.MetabolicN)
            {
                var strucuralNLost = nitrogen.Retranslocation - (Live.StorageN + Live.MetabolicN);
                Live.StructuralN -= strucuralNLost;
                Allocated.StructuralN -= strucuralNLost;

                Live.StorageN = 0.0;
                Live.MetabolicN = 0.0;
                Allocated.StorageN = 0;
                Allocated.MetabolicN = 0.0;
            }
            else if (nitrogen.Retranslocation > Live.StorageN)
            {
                var metabolicNLost = nitrogen.Retranslocation - Live.StorageN;
                Live.MetabolicN -= metabolicNLost;
                Allocated.MetabolicN -= metabolicNLost;
                Live.StorageN = 0.0;
                Allocated.StorageN = 0;
            }
            else
            {
                Live.StorageN -= nitrogen.Retranslocation;
                Allocated.StorageN -= nitrogen.Retranslocation;
            }

            // No Reallocation at present
            //if (MathUtilities.IsGreaterThan(nitrogen.Reallocation, StartLive.StorageN + StartLive.MetabolicN))
            //    throw new Exception("N reallocation exceeds storage + metabolic nitrogen in organ: " + Name);
            //double StorageNReallocation = Math.Min(nitrogen.Reallocation, StartLive.StorageN * SenescenceRate.Value() * nReallocationFactor.Value());
            //Live.StorageN -= StorageNReallocation;
            //Live.MetabolicN -= (nitrogen.Reallocation - StorageNReallocation);
            //Allocated.StorageN -= nitrogen.Reallocation;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            NDemand = new BiomassPoolType();
            DMDemand = new BiomassPoolType();
            DMDemandPriorityFactor = new BiomassPoolType();
            DMDemandPriorityFactor.Structural = 1.0;
            DMDemandPriorityFactor.Metabolic = 1.0;
            DMDemandPriorityFactor.Storage = 1.0;
            NSupply = new BiomassSupplyType();
            DMSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            StartLive = new Biomass();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
            Live = new Biomass();
            Dead = new Biomass();

            Clear();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                Allocated.Clear();
                Senesced.Clear();
                Detached.Clear();
                Removed.Clear();

                //clear local variables
                // dh - DltLAI cannot be cleared here. It needs to retain its value from yesterday,
                // for when leaf retranslocates to itself in provideN().
                dltPotentialLAI = 0.0;
                DltRetranslocatedN = 0.0;
                DltSenescedLai = 0.0;
                DltSenescedLaiN = 0.0;
                DltSenescedN = 0.0;
                dltStressedLAI = 0.0;
                
            }
        }

        /// <summary>Called when crop is being sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == parentPlant)
            {
                //OnPlantSowing let structure do the clear so culms isn't cleared before initialising the first one
                //Clear();
                SowingDensity = data.Population;
                nDeadLeaves = 0;
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        protected void DoPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, parentPlant.CropType, Name);
            }

            Clear();
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading, the name of this organ
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write the basic description of this class, given in the <summary>
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write the memos
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                //// List the parameters, properties, and processes from this organ that need to be documented:

                // document initial DM weight
                IModel iniWt = Apsim.Child(this, "initialWtFunction");
                AutoDocumentation.DocumentModel(iniWt, tags, headingLevel + 1, indent);

                // document DM demands
                tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", indent));
                IModel DMDemand = Apsim.Child(this, "dmDemands");
                AutoDocumentation.DocumentModel(DMDemand, tags, headingLevel + 2, indent);

                // document N demands
                tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", indent));
                IModel NDemand = Apsim.Child(this, "nDemands");
                AutoDocumentation.DocumentModel(NDemand, tags, headingLevel + 2, indent);

                // document N concentration thresholds
                IModel MinN = Apsim.Child(this, "MinimumNConc");
                AutoDocumentation.DocumentModel(MinN, tags, headingLevel + 2, indent);
                IModel CritN = Apsim.Child(this, "CriticalNConc");
                AutoDocumentation.DocumentModel(CritN, tags, headingLevel + 2, indent);
                IModel MaxN = Apsim.Child(this, "MaximumNConc");
                AutoDocumentation.DocumentModel(MaxN, tags, headingLevel + 2, indent);
                IModel NDemSwitch = Apsim.Child(this, "NitrogenDemandSwitch");
                if (NDemSwitch is Constant)
                {
                    if ((NDemSwitch as Constant).Value() == 1.0)
                    {
                        //Don't bother documenting as is does nothing
                    }
                    else
                    {
                        tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor of " + (NDemSwitch as Constant).Value() + " as specified by the NitrogenDemandSwitch", indent));
                    }
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch.", indent));
                    AutoDocumentation.DocumentModel(NDemSwitch, tags, headingLevel + 2, indent);
                }

                // document DM supplies
                tags.Add(new AutoDocumentation.Heading("Dry Matter Supply", headingLevel + 1));
                IModel DMReallocFac = Apsim.Child(this, "DMReallocationFactor");
                if (DMReallocFac is Constant)
                {
                    if ((DMReallocFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate DM when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + (DMReallocFac as Constant).Value() * 100 + "% of DM that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(DMReallocFac, tags, headingLevel + 2, indent);
                }
                IModel DMRetransFac = Apsim.Child(this, "DMRetranslocationFactor");
                if (DMRetransFac is Constant)
                {
                    if ((DMRetransFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural DM.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + (DMRetransFac as Constant).Value() * 100 + "% of non-structural DM each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(DMRetransFac, tags, headingLevel + 2, indent);
                }

                // document photosynthesis
                IModel PhotosynthesisModel = Apsim.Child(this, "Photosynthesis");
                AutoDocumentation.DocumentModel(PhotosynthesisModel, tags, headingLevel + 2, indent);

                // document N supplies
                tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
                IModel NReallocFac = Apsim.Child(this, "NReallocationFactor");
                if (NReallocFac is Constant)
                {
                    if ((NReallocFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NReallocFac, tags, headingLevel + 2, indent);
                }
                IModel NRetransFac = Apsim.Child(this, "NRetranslocationFactor");
                if (NRetransFac is Constant)
                {
                    if ((NRetransFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NRetransFac, tags, headingLevel + 2, indent);
                }

                // document canopy
                tags.Add(new AutoDocumentation.Heading("Canopy Properties", headingLevel + 1));
                IModel laiF = Apsim.Child(this, "LAIFunction");
                IModel coverF = Apsim.Child(this, "CoverFunction");
                if (laiF != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has been defined with a LAIFunction, cover is calculated using the Beer-Lambert equation.", indent));
                    AutoDocumentation.DocumentModel(laiF, tags, headingLevel + 2, indent);
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has been defined with a CoverFunction. LAI is calculated using an inverted Beer-Lambert equation", indent));
                    AutoDocumentation.DocumentModel(coverF, tags, headingLevel + 2, indent);
                }
                IModel exctF = Apsim.Child(this, "ExtinctionCoefficientFunction");
                AutoDocumentation.DocumentModel(exctF, tags, headingLevel + 2, indent);
                IModel heightF = Apsim.Child(this, "HeightFunction");
                AutoDocumentation.DocumentModel(heightF, tags, headingLevel + 2, indent);

                // document senescence and detachment
                tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
                IModel SenRate = Apsim.Child(this, "SenescenceRate");
                if (SenRate is Constant)
                {
                    if ((SenRate as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has senescence parameterised to zero so all biomass in this organ will remain alive.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " senesces " + (SenRate as Constant).Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate.", indent));
                    AutoDocumentation.DocumentModel(SenRate, tags, headingLevel + 2, indent);
                }

                IModel DetRate = Apsim.Child(this, "DetachmentRateFunction");
                if (DetRate is Constant)
                {
                    if ((DetRate as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " detaches " + (DetRate as Constant).Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.", indent));
                    AutoDocumentation.DocumentModel(DetRate, tags, headingLevel + 2, indent);
                }

                if (biomassRemovalModel != null)
                    biomassRemovalModel.Document(tags, headingLevel + 1, indent);
            }
        }
    }
}
