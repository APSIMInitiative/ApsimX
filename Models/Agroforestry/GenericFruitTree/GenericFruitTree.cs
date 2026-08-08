using System;
using System.Collections.Generic;
using APSIM.Core;
using Newtonsoft.Json;
using Models.Core;
using Models.Interfaces;
using Models.Soils.Arbitrator;
using Models.PMF;
using Models.PMF.Phen;
using Models.PMF.Organs;
using Models.Functions.SupplyFunctions;
using Models.PMF.Interfaces;
using System.Linq;
using Models.Functions;
using Models.Soils;
using Models;


namespace Models.Agroforestry
{
    /// <summary>Policy for handling fruit after maturity when no external manager overrides behavior.</summary>
    public enum FruitFatePolicy
    {
        /// <summary>Keep fruit on the tree until management removes it.</summary>
        Persist = 0,
        /// <summary>Apply smooth post-maturity abscission by default.</summary>
        Abscise = 1,
        /// <summary>Automatically call Harvest() once harvest criteria are met.</summary>
        AutoHarvest = 2,
        /// <summary>Move post-maturity fruit biomass into senesced pools smoothly.</summary>
        SenesceToDead = 3
    }

    /// <summary>
    /// A generic, mechanistic fruit‑tree model for APSIM‑X.
    /// Can be parameterised to represent different fruit tree species via cultivar and trait settings.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GenericFruitTreeView")]
    [PresenterName("UserInterface.Presenters.GenericFruitTreePresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class GenericFruitTree : Plant
    {
        private const double GPerM2ToKgPerHa = 10.0;
        private const double CmPerHourToMPerDay = 0.24;
        private const int KrefMedianWindowDays = 30;
        private const double KrefMedianScale = 0.2;
        private const double SupplyPawSlope = 8.0;
        private const double FruitWaterStateTauDays = 5.0;
        private const double NoFruitPoolDecayTauDays = 12.0;
        private const double FruitQualityOutputWeightEpsilon = 0.2;

        #region Parameters
        // === Plant Structure ===

        /// <summary>Initial height of the plant at emergence.</summary>
        [Description("Initial height of the plant at emergence")]
        [Units("m")]
        public double InitialHeight { get; set; }

        /// <summary>Default stomatal conductance at 350 ppm CO2 (m/s) if not specified.</summary>
        [Description("Default Gsmax350 (m/s) if leaf organ value is missing/zero")]
        [Units("m/s")]
        public double DefaultGsmax350 { get; set; } = 0.015;

        /// <summary>Default R50 (W/m2) if not specified.</summary>
        [Description("Default R50 (W/m2) if leaf organ value is missing/zero")]
        [Units("W/m^2")]
        public double DefaultR50 { get; set; } = 150.0;

        /// <summary>Plant hydraulic conductance trait used with soil conductance in series.</summary>
        [Description("Plant hydraulic conductance trait (combined in series with rooted-zone soil conductance)")]
        [Units("m/day")]
        public double PlantHydraulicConductance { get; set; } = 1.0;

        /// <summary>Characteristic response time for supply effective decline (fast-down).</summary>
        [Description("Supply signal fast-down time constant (days)")]
        [Units("d")]
        public double SupplyTauDownDays { get; set; } = 2.0;

        /// <summary>Characteristic response time for supply effective recovery (slow-up).</summary>
        [Description("Supply signal slow-up time constant (days)")]
        [Units("d")]
        public double SupplyTauUpDays { get; set; } = 6.0;

        /// <summary>Exponent mapping effective supply to growth stress modifier.</summary>
        [Description("Growth stress exponent applied to effective supply")]
        [Units("-")]
        public double GrowthStressExponent { get; set; } = 1.5;

        /// <summary>PAW fraction midpoint for the logistic supply mapping.</summary>
        [Description("PAW fraction midpoint for supply logistic response")]
        [Units("0-1")]
        public double PAW50 { get; set; } = 0.5;

        // === Light Interception & Canopy Geometry ===

        /// <summary>Number of horizontal canopy layers for radiative discretisation only.</summary>
        [Description("Number of horizontal canopy layers for radiative discretisation only (do not calibrate)")]
        [Units("count")]
        public int LayerCount { get; set; } = 10;

        /// <summary>Geometric shape used to represent the crown.</summary>
        [Description("Geometric shape used to represent the crown")]
        public CrownGeometryHelper.CrownShape CrownShape { get; set; } = CrownGeometryHelper.CrownShape.Ellipsoid;

        /// <summary>Alpha shape parameter for the Beta LAD profile (higher -> more top-heavy).</summary>
        [Description("Alpha shape parameter for the Beta LAD profile (higher -> more top-heavy)")]
        [Units("-")]
        public double LAD_Alpha { get; set; } = 2.0;

        /// <summary>Beta shape parameter for the Beta LAD profile (higher -> more bottom-heavy).</summary>
        [Description("Beta shape parameter for the Beta LAD profile (higher -> more bottom-heavy)")]
        [Units("-")]
        public double LAD_Beta { get; set; } = 2.0;

        /// <summary>Ratio of crown width to tree height.</summary>
        [Description("Ratio of crown width to tree height")]
        [Units("-")]
        public double CrownWidthHeightRatio { get; set; } = 0.8;

        /// <summary>Ratio of crown depth to tree height.</summary>
        [Description("Ratio of crown depth to tree height")]
        [Units("-")]
        public double CrownDepthHeightRatio { get; set; } = 0.8;

        /// <summary>Clumping index to adjust effective light extinction.</summary>
        [Description("Clumping index to adjust effective light extinction")]
        [Units("-")]
        public double ClumpingIndex { get; set; } = 1.0;

        /// <summary>Structural pruning effect on effective crown width and depth.</summary>
        [Description("Structural pruning effect on effective crown width and depth")]
        [Units("-")]
        public double PruningStructuralSizeSensitivity { get; set; } = 0.5;

        /// <summary>Structural pruning effect on light-equivalent LAI.</summary>
        [Description("Structural pruning effect on light-equivalent LAI")]
        [Units("-")]
        public double PruningStructuralLightSensitivity { get; set; } = 0.5;

        // === Management Events ===

        /// <summary>Fraction of live leaf biomass and area removed by pruning events.</summary>
        [Description("Fraction of live leaf biomass and area removed by pruning events")]
        [Units("0-1")]
        public double PruningLeafFraction { get; set; }

        /// <summary>Fractional structural canopy effect applied by pruning events.</summary>
        [Description("Fractional structural canopy effect applied by pruning events")]
        [Units("0-1")]
        public double PruningStructuralFraction { get; set; }

        /// <summary>Fraction of fruit crop removed by thinning events.</summary>
        [Description("Fraction of fruit crop removed by thinning events")]
        [Units("0-1")]
        public double ThinningFraction { get; set; }

        /// <summary>Total fraction of live leaf biomass to remove over the leaf-fall window (0-1).</summary>
        [Description("Total fraction of live leaf biomass to remove over dormancy leaf-fall (0-1)")]
        [Units("0-1")]
        public double LeafFallTotalFraction { get; set; } = 0.8;

        /// <summary>Duration of dormancy leaf-fall window.</summary>
        [Description("Duration of dormancy leaf-fall window")]
        [Units("d")]
        public double LeafFallDurationDays { get; set; } = 40.0;

        /// <summary>Shape exponent for cumulative dormancy leaf-fall curve.</summary>
        [Description("Shape exponent for cumulative dormancy leaf-fall curve")]
        [Units("-")]
        public double LeafFallShape { get; set; } = 2.0;

        /// <summary>Reserve capacity as a fraction of live wood structural dry matter.</summary>
        [Description("Reserve capacity as a fraction of live wood structural dry matter")]
        [Units("0-1")]
        public double ReserveCapacityFracOfWoodDM { get; set; } = 0.15;

        /// <summary>Maximum daily mobilisation fraction of the reserve pool.</summary>
        [Description("Maximum daily mobilisation fraction of the reserve pool")]
        [Units("1/d")]
        public double ReserveMobilisationRate { get; set; } = 0.02;

        /// <summary>
        /// Maximum daily storage fraction of free reserve capacity.
        /// Values &lt;= 0 mean storage is only capacity-limited.
        /// </summary>
        [Description("Maximum daily storage fraction of free reserve capacity (<=0 means unlimited)")]
        [Units("1/d")]
        public double ReserveStorageRate { get; set; } = 0.0;

        /// <summary>Minimum days between successive bud-break events.</summary>
        [Description("Minimum days between successive bud-break events (species default / derived behavior; excluded from compact calibration)")]
        [Units("d")]
        public int MinDaysBetweenBudBreak { get; set; } = 365;

        /// <summary>Initial reserve pool as a fraction of live wood structural dry matter.</summary>
        [Description("Initial reserve pool as a fraction of live wood structural dry matter")]
        [Units("0-1")]
        public double ReserveInitFrac { get; set; } = 0.10;

        /// <summary>Initial reserve nitrogen concentration (g N / g DM).</summary>
        [Description("Initial reserve nitrogen concentration (g N / g DM)")]
        [Units("g/g")]
        public double InitialReserveNConc { get; set; } = 0.02;

        // === Phenology & Dormancy ===

        /// <summary>Name of the dormancy stage start tag (optional).</summary>
        [Description("Name of the dormancy stage start tag (optional)")]
        public string DormancyStage { get; set; } = string.Empty;

        /// <summary>Name of the bud-break stage start tag.</summary>
        [Description("Name of the bud-break stage start tag")]
        public string BudBreakStage { get; set; } = "BudBreak";

        /// <summary>Name of the flowering stage start tag.</summary>
        [Description("Name of the flowering stage start tag")]
        public string FloweringStage { get; set; } = "Flowering";

        /// <summary>"Name of the fruit-set stage start tag.</summary>
        [Description("Name of the fruit-set stage start tag")]
        public string FruitSetStage { get; set; } = "FruitSet";

        /// <summary>Name of the fruit-fill stage start tag.</summary>
        [Description("Name of the fruit-fill stage start tag")]
        public string FruitFillStage { get; set; } = "FruitFill";

        /// <summary>Name of the maturity stage start tag.</summary>
        [Description("Name of the maturity stage start tag")]
        public string MaturityStage { get; set; } = "Maturity";

        /// <summary>Name of the post-dormancy forcing phase start tag (optional).</summary>
        [Description("Name of the post-dormancy forcing phase start tag (optional)")]
        public string ForcingStage { get; set; } = string.Empty;

        /// <summary>Base temperature for GDD accumulation after dormancy.</summary>
        [Description("Base temperature for GDD accumulation after dormancy")]
        [Units("C")]
        public double ForcingBaseTemperature { get; set; } = 0.0;

        /// <summary>Heat-unit sum (GDD) required to trigger bud-break after chilling.</summary>
        [Description("Heat-unit sum (GDD) required after chilling to trigger bud-break")]
        [Units("C * day")]
        public double ForcingUnitsRequired { get; set; } = 0.0;

        /// <summary>Chill units (hours) required to break dormancy.</summary>
        [Description("Chill units (hours) required to break dormancy")]
        [Units("h")]
        public double ChillUnitsRequired { get; set; } = 0.0;

        /// <summary>Reset phenology to the seasonal reset stage after harvest readiness is reached.</summary>
        [Description("Reset phenology to the seasonal reset stage after harvest readiness is reached")]
        public bool ResetPhenologyAfterHarvest { get; set; } = false;

        /// <summary>
        /// Stage to reset phenology to after harvest. If blank, DormancyStage is used when set, otherwise BudBreakStage.
        /// </summary>
        [Description("Stage to reset phenology to after harvest; blank uses DormancyStage or BudBreakStage")]
        public string SeasonalPhenologyResetStage { get; set; } = string.Empty;

        // === Harvest Control ===

        /// <summary>Enable harvest as soon as maturity phenology stage is reached.</summary>
        [Description("Enable harvest as soon as maturity stage is reached")]
        public bool HarvestByPhenology { get; set; } = true;

        /// <summary>Policy controlling default fruit fate after maturity when no manager intervention is present.</summary>
        [Description("Policy controlling default fruit fate after maturity when no manager intervention is present")]
        public FruitFatePolicy FruitFatePolicy { get; set; } = global::Models.Agroforestry.FruitFatePolicy.Abscise;

        /// <summary>Minimum Brix to allow quality‑based harvest (0 disables).</summary>
        [Description("Minimum Brix to allow quality‑based harvest (0 disables)")]
        [Units("%")]
        public double HarvestBrixThreshold { get; set; } = 0.0;

        /// <summary>Maximum acid (%) to allow quality‑based harvest (0 disables).</summary>
        [Description("Maximum acid (%) to allow quality-based harvest (0 disables)")]
        [Units("%")]
        public double HarvestAcidThreshold { get; set; } = 0.0;
        #endregion

        // === Fruit Quality ===

        /// <summary>Sugar production per fruit dry matter gain.</summary>
        [Description("Sugar production per fruit dry matter gain")]
        [Units("g/g")]
        public double SugarPerDM { get; set; } = 0.75;

        /// <summary>Acid production per fruit dry matter gain.</summary>
        [Description("Acid production per fruit dry matter gain")]
        [Units("g/g")]
        public double AcidPerDM { get; set; } = 0.05;

        /// <summary>Daily fractional acid degradation rate during late fruit filling.</summary>
        [Description("Daily fractional acid degradation rate during late fruit filling")]
        [Units("1/d")]
        public double AcidDegradationRate { get; set; } = 0.01;

        /// <summary>Optional late-season sugar concentration rate.</summary>
        [Description("Optional late-season sugar concentration rate")]
        [Units("g/m^2/d")]
        public double SugarConcRate { get; set; } = 0.0;

        /// <summary>Minimum fruit water fraction.</summary>
        [Description("Minimum fruit water fraction (species default / derived bound; excluded from compact calibration)")]
        [Units("0-1")]
        public double WC_Min { get; set; } = 0.75;

        /// <summary>Maximum fruit water fraction.</summary>
        [Description("Maximum fruit water fraction (species default / derived bound; excluded from compact calibration)")]
        [Units("0-1")]
        public double WC_Max { get; set; } = 0.92;

        /// <summary>Leaf water stress midpoint for fruit water fraction response.</summary>
        [Description("Leaf water stress midpoint for fruit water fraction response (species default / derived behavior; excluded from compact calibration)")]
        [Units("0-1")]
        public double WC_Fw50 { get; set; } = 0.5;

        /// <summary>Leaf water stress slope for fruit water fraction response.</summary>
        [Description("Leaf water stress slope for fruit water fraction response (species default / derived behavior; excluded from compact calibration)")]
        [Units("-")]
        public double WC_FwSlope { get; set; } = 12.0;

        /// <summary>Phenology midpoint for fruit water fraction response.</summary>
        [Description("Phenology midpoint for fruit water fraction response (species default / derived behavior; excluded from compact calibration)")]
        [Units("0-1")]
        public double WC_Stage50 { get; set; } = 0.5;

        /// <summary>Phenology slope for fruit water fraction response.</summary>
        [Description("Phenology slope for fruit water fraction response (species default / derived behavior; excluded from compact calibration)")]
        [Units("-")]
        public double WC_StageSlope { get; set; } = 10.0;

        /// <summary>Soft saturation ceiling for Brix.</summary>
        [Description("Soft saturation ceiling for Brix (species default / safety bound; excluded from compact calibration)")]
        [Units("%")]
        public double BrixMax { get; set; } = 30.0;

        /// <summary>Soft saturation ceiling for acidity.</summary>
        [Description("Soft saturation ceiling for acidity (species default / safety bound; excluded from compact calibration)")]
        [Units("%")]
        public double AcidMax { get; set; } = 3.0;

        /// <summary>Shape factor for soft concentration saturation.</summary>
        [Description("Shape factor for soft concentration saturation (species default / safety bound; excluded from compact calibration)")]
        [Units("-")]
        public double CapSharpness { get; set; } = 3.0;

        #region Links

        // === Core simulation services ===

        /// <summary>The summary</summary>
        [Link]
        public ISummary Summary = null;

        /// <summary>Clock for date-based gating of phenology events.</summary>
        [Link]
        private IClock clock = null!;

        /// <summary>Link to the weather component for driving climate variables.</summary>
        [Link]
        private IWeather weather = null!;

        // === Phenology and microclimate drivers ===

        /// <summary>Phenology driver for developmental stages.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private Phenology phenology = null!;

        /// <summary>Function returning today's vapour pressure deficit (hPa).</summary>
        [Link(ByName = true)]
        private IFunction VPDCalculator = null!;

        // === Soil resources ===

        /// <summary>Soil water balance interface, for water uptake.</summary>
        [Link]
        private ISoilWater soilWater = null!;

        /// <summary>Soil physical properties (layer thickness, bulk density, etc.).</summary>
        [Link]
        private IPhysical soilPhysical = null!;

        // === Sink strength functions driven by phenology ===

        /// <summary>Fruit sink strength (TableFunction on phenology.FractionInCurrentPhase).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction FruitSinkStrength = null!;

        /// <summary>Leaf sink strength (TableFunction on phenology.FractionInCurrentPhase).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction LeafSinkStrength = null!;

        /// <summary>Fruit-organ water content function (for keeping a single daily wc source of truth).</summary>
        [Link(Type = LinkType.Path, Path = "Arbitrator.fruitOrgan.WaterContent", IsOptional = true)]
        private IFunction fruitOrganWaterContentFn = null!;

        // === Dormancy / chilling function (optional) ===

        /// <summary>
        /// Function returning daily chill accumulation (Optional: if absent, no chilling is applied).
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction ChillRate = null!;
        #endregion

        #region Events

        /// <summary>Fired as soon as the tree becomes ready for harvest, but before any biomass is removed.</summary>
        public event EventHandler ReadyToHarvest = delegate { };
        #endregion

        #region Organ Sub-models
        /// <summary>Leaf organ sub-model (perennial leaves).</summary>
        [Link(Type = LinkType.Path, Path = "Arbitrator.leafOrgan")]
        private PerennialLeaf leafOrgan = null!;

        /// <summary>Fruit or reproductive organ sub-model.</summary>
        [Link(Type = LinkType.Path, Path = "Arbitrator.fruitOrgan")]
        private FruitOrgan fruitOrgan = null!;

        /// <summary>Wood or structural organ sub-model.</summary>
        [Link(Type = LinkType.Path, Path = "Arbitrator.woodOrgan")]
        private GenericOrgan woodOrgan = null!;

        /// <summary>Root organ sub-model.</summary>
        [Link(Type = LinkType.Path, Path = "Arbitrator.rootOrgan")]
        private Root rootOrgan = null!;

        /// <summary>Optional bud organ used to derive reproductive potential from bud load.</summary>
        [Link(Type = LinkType.Path, Path = "Arbitrator.budOrgan", IsOptional = true)]
        private BudOrgan budOrgan = null!;

        #endregion

        #region State Variables

        /// <summary>PENDING.</summary>
        public double FruitSinkStrengthOutput => FruitSinkStrength?.Value() ?? 0.0;

        /// <summary>PENDING.</summary>
        public double LeafSinkStrengthOutput => LeafSinkStrength?.Value() ?? 0.0;

        /// <summary>Leaf growth modifier (supply-effective mapped through GrowthStressExponent).</summary>
        [Units("0-1")]
        public double LeafGrowthModifierOutput => GrowthStressModifierOutput;

        /// <summary>Fruit growth modifier (softer penalty than leaf growth).</summary>
        [Units("0-1")]
        public double FruitGrowthModifierOutput => Math.Sqrt(LeafGrowthModifierOutput);

        /// <summary>Alias for leaf growth modifier using standardized naming.</summary>
        [Units("0-1")]
        public double GrowthModifierLeafOutput => LeafGrowthModifierOutput;

        /// <summary>Alias for fruit growth modifier using standardized naming.</summary>
        [Units("0-1")]
        public double GrowthModifierFruitOutput => FruitGrowthModifierOutput;

        /// <summary>Leaf sink strength seen by demand creation after supply-stress regulation.</summary>
        public double LeafSinkStrengthEffectiveOutput => LeafSinkStrengthOutput * LeafGrowthModifierOutput;

        /// <summary>Fruit sink strength seen by demand creation after supply-stress regulation.</summary>
        public double FruitSinkStrengthEffectiveOutput => FruitSinkStrengthOutput * FruitGrowthModifierOutput;

        /// <summary>Current leaf area index (m^2/m^2).</summary>
        [JsonIgnore]
        public double LeafAreaIndex => leafOrgan?.LAI ?? 0.0;

        /// <summary>Raw daily leaf water stress from PMF leaf organ (0-1).</summary>
        [JsonIgnore]
        public double LeafWaterStressRawOutput => Clamp01(leafOrgan?.Fw ?? 0.0);

        /// <summary>Raw daily mechanistic supply index from Ktotal and rooted PAW state (0-1).</summary>
        [JsonIgnore]
        public double SupplyIndexRaw { get; private set; } = 1.0;

        /// <summary>Asymmetrically smoothed daily supply index (0-1).</summary>
        [JsonIgnore]
        public double SupplyIndexEffective { get; private set; } = 1.0;

        /// <summary>Growth stress modifier derived from effective supply (0-1).</summary>
        [JsonIgnore]
        public double GrowthStressModifier { get; private set; } = 1.0;

        /// <summary>Rooted-zone PAW fraction (0-1).</summary>
        [JsonIgnore]
        public double RootZonePAWFraction { get; private set; }

        /// <summary>Daily thermal-time increment used for fruit cohort age tracking.</summary>
        [JsonIgnore]
        public double DailyFruitThermalTimeIncrement => Math.Max(0.0, phenology?.thermalTime?.Value() ?? 0.0);

        /// <summary>Allow reproductive events for the active seasonal cycle.</summary>
        public bool AllowReproductivePhase(string stageName)
        {
            if (string.IsNullOrWhiteSpace(stageName))
                return false;

            if (!string.IsNullOrWhiteSpace(MaturityStage) &&
                stageName.Equals(MaturityStage, StringComparison.OrdinalIgnoreCase))
                return true; // always allow maturity transition handling

            return reproductiveCycleActive;
        }

        /// <summary>Current height of the plant (m).</summary>
        [JsonIgnore] private double currentHeight;

        /// <summary>Current structural pruning effect applied to canopy geometry and light interception (0-1).</summary>
        [JsonIgnore] private double activeStructuralPruningFraction = 0.0;

        /// <summary>Current structural multiplier applied to crown width and depth.</summary>
        [JsonIgnore] private double currentStructuralSizeFactor = 1.0;

        /// <summary>Current structural multiplier applied to light-equivalent LAI.</summary>
        [JsonIgnore] private double currentStructuralLightFactor = 1.0;

        /// <summary>Leaf dry matter (g/m^2).</summary>
        [JsonIgnore] private double leafDryMatter_g_m2;

        /// <summary>Fruit dry matter (g/m^2).</summary>
        [JsonIgnore] private double fruitDryMatter_g_m2;

        /// <summary>Zone-by-zone records of water and nitrogen uptakes.</summary>
        [JsonIgnore] public List<ZoneWaterAndN> Uptakes { get; private set; } = new();

        /// <summary>Flag indicating whether we’ve already fired ReadyToHarvest.</summary>
        [JsonIgnore] private bool isHarvested = false;

        /// <summary>Is dormancy leaf-fall currently active?</summary>
        [JsonIgnore] private bool inLeafFallWindow = false;

        /// <summary>Normalised leaf-fall progress (0-1) within current window.</summary>
        [JsonIgnore] private double leafFallProgress = 0.0;

        /// <summary>Cumulative fraction removed from initial live leaf biomass (0-1).</summary>
        [JsonIgnore] private double leafFallRemovedSoFarFrac = 0.0;

        /// <summary>Date when the current leaf-fall window started.</summary>
        [JsonIgnore] private DateTime leafFallStartDate = DateTime.MinValue;

        /// <summary>Ensure initial reserves are only seeded once.</summary>
        [JsonIgnore] private bool reservesSeeded = false;

        /// <summary>Ensure reserve flows are applied at most once per day.</summary>
        [JsonIgnore] private DateTime lastReserveBalanceDate = DateTime.MinValue;

        /// <summary>Warn once if structural-demand fallback logic is used.</summary>
        [JsonIgnore] private bool reserveDemandFallbackWarningIssued = false;

        /// <summary>Cached leaf photosynthesis function used as supply fallback.</summary>
        [JsonIgnore] private IFunction leafPhotosynthesisFn = null;

        /// <summary>Tracks last accepted bud-break date.</summary>
        [JsonIgnore] private DateTime lastBudBreakDate = DateTime.MinValue;

        /// <summary>Is the reproductive cycle currently active?</summary>
        [JsonIgnore] private bool reproductiveCycleActive = false;

        /// <summary>Bulk soil-plant hydraulic conductance (m/day).</summary>
        [JsonIgnore] public double SoilPlantConductance { get; private set; }

        /// <summary>Root-zone soil hydraulic conductance diagnostic (m/day).</summary>
        [JsonIgnore] public double HydraulicSoilConductance { get; private set; }

        /// <summary>Plant hydraulic conductance diagnostic after safety bounds (m/day).</summary>
        [JsonIgnore] public double HydraulicPlantConductance { get; private set; }

        /// <summary>Dynamic K reference used in the supply-index transform (m/day).</summary>
        [JsonIgnore] public double HydraulicReferenceConductance { get; private set; }

        /// <summary>Conductance term used by the supply-index transform (0-1).</summary>
        [JsonIgnore] public double HydraulicKTerm { get; private set; } = 1.0;

        /// <summary>Rooted-zone PAW term used by the supply-index transform (0-1).</summary>
        [JsonIgnore] public double HydraulicPAWTerm { get; private set; } = 1.0;

        /// <summary>Rooted-zone plant available water capacity used by the supply-index transform (mm).</summary>
        [JsonIgnore] public double HydraulicRootedPAWCmm { get; private set; }

        /// <summary>Daily mean vapour pressure deficit (hPa).</summary>
        [JsonIgnore] public double VPD { get; private set; }

        /// <summary>Cumulative sugar mass in fruit (g/m^2).</summary>
        [JsonIgnore] private double fruitSugarMass_g_m2 = 0;

        /// <summary>Cumulative acid mass in fruit (g/m^2).</summary>
        [JsonIgnore] private double fruitAcidMass_g_m2 = 0;

        /// <summary>Smoothed fruit water fraction state (0-1).</summary>
        [JsonIgnore] private double fruitWaterFractionState = 0.85;

        /// <summary>Stored canopy potential evapotranspiration state used before link resolution.</summary>
        [JsonIgnore] private double potentialEP;

        /// <summary>Stored canopy water-demand state used before link resolution.</summary>
        [JsonIgnore] private double waterDemand;

        /// <summary>Yesterday fruit dry matter (g/m^2).</summary>
        [JsonIgnore] private double prevFruitDM_g_m2 = 0;

        /// <summary>Yesterday fruit fresh mass (g/m^2).</summary>
        [JsonIgnore] private double prevFruitFresh_g_m2 = 0;

        /// <summary>Current fruit quality: Brix percentage.</summary>
        [JsonIgnore] public double QualityBrix { get; private set; }

        /// <summary>Current fruit quality: dry‑matter percentage.</summary>
        [JsonIgnore] public double QualityDMPct { get; private set; }

        /// <summary>Current fruit quality: acid percentage.</summary>
        [JsonIgnore] public double QualityAcidPct { get; private set; }

        /// <summary>Ensure non-mutable fruit water content function warning is only logged once.</summary>
        [JsonIgnore] private bool fruitWaterSyncWarningIssued = false;

        /// <summary>Warn once if clumping index is outside the supported calibration range.</summary>
        [JsonIgnore] private bool clumpingBoundsWarningIssued = false;

        /// <summary>Ensure hydraulics diagnostics are emitted at most once per day.</summary>
        [JsonIgnore] private DateTime lastHydraulicsDiagnosticDate = DateTime.MinValue;

        /// <summary>Warn once if soil hydraulic arrays are missing/misaligned.</summary>
        [JsonIgnore] private bool hydraulicsArrayShapeWarningIssued = false;

        /// <summary>Warn once if PSI refresh for K fails.</summary>
        [JsonIgnore] private bool hydraulicsPsiRefreshWarningIssued = false;

        /// <summary>Warn once if rooted PAW arrays are missing/misaligned.</summary>
        [JsonIgnore] private bool supplyArrayShapeWarningIssued = false;

        /// <summary>Keep a rolling window of positive Ktotal values to estimate dynamic Kref.</summary>
        [JsonIgnore] private Queue<double> positiveKtotalWindow = new();

        /// <summary>Tracks whether the effective supply EWMA has been initialized.</summary>
        [JsonIgnore] private bool supplyStateInitialised = false;

        /// <summary>Ensures supply-state updates occur at most once per day.</summary>
        [JsonIgnore] private DateTime lastSupplyUpdateDate = DateTime.MinValue;

        /// <summary>Last date phenology was reset for a seasonal cycle.</summary>
        [JsonIgnore] private DateTime lastSeasonalPhenologyResetDate = DateTime.MinValue;

        /// <summary>Accumulated chill units so far this dormancy.</summary>
        [JsonIgnore]
        private double chillUnitsAccumulated;

        /// <summary>Accumulated heat units since end of dormancy (GDD).</summary>
        [JsonIgnore]
        private double forcingUnitsAccumulated = 0.0;

        /// <summary>Chill units recorded when the chill gate last triggered.</summary>
        [JsonIgnore]
        private double chillUnitsAtLastTransition = 0.0;

        /// <summary>Forcing units recorded when the forcing gate last triggered.</summary>
        [JsonIgnore]
        private double forcingUnitsAtLastTransition = 0.0;

        /// <summary>Whether the chill gate triggered today.</summary>
        [JsonIgnore]
        private double chillRequirementSatisfiedToday = 0.0;

        /// <summary>Whether the forcing gate triggered today.</summary>
        [JsonIgnore]
        private double forcingRequirementSatisfiedToday = 0.0;

        /// <summary>Daily carbon supply signal used for reserve control.</summary>
        [JsonIgnore] public double ReserveSupplyDM { get; private set; }

        /// <summary>Daily critical structural demand signal used for reserve control.</summary>
        [JsonIgnore] public double ReserveCriticalDemandDM { get; private set; }

        /// <summary>Daily carbon surplus signal.</summary>
        [JsonIgnore] public double ReserveSurplusDM { get; private set; }

        /// <summary>Daily carbon deficit signal.</summary>
        [JsonIgnore] public double ReserveDeficitDM { get; private set; }

        /// <summary>Reserve pool capacity (g/m^2).</summary>
        [JsonIgnore] public double ReserveCapacityDM { get; private set; }

        /// <summary>Current reserve pool (g/m^2).</summary>
        [JsonIgnore] public double ReservePoolDM { get; private set; }

        /// <summary>Daily reserve storage demand signal (g/m^2/day).</summary>
        [JsonIgnore] public double ReserveStoredDM { get; private set; }

        /// <summary>Daily reserve mobilisation flow (g/m^2/day).</summary>
        [JsonIgnore] public double ReserveMobilisedDM { get; private set; }

        /// <summary>Daily retranslocation factor supplied to wood organ (0-1).</summary>
        [JsonIgnore] public double ReserveDMRetranslocationFactor { get; private set; }

        /// <summary>Leaf dry matter (kg/ha).</summary>
        [Units("kg/ha")]
        public double BiomassLeavesOutput => leafDryMatter_g_m2 * GPerM2ToKgPerHa;

        /// <summary>Fruit dry matter (kg/ha).</summary>
        [Units("kg/ha")]
        public double BiomassFruitsOutput => fruitDryMatter_g_m2 * GPerM2ToKgPerHa;

        /// <summary>Current fruit dry‑matter percentage.</summary>
        [Units("%")]
        public double QualityDMPctOutput => QualityDMPct;

        /// <summary>Current fruit acid percentage.</summary>
        [Units("%")]
        public double QualityAcidPctOutput => QualityAcidPct;

        /// <summary>Bulk soil-plant hydraulic conductance (m/day).</summary>
        [Units("m/day")]
        public double SoilPlantConductanceOutput => SoilPlantConductance;

        /// <summary>Root-zone soil hydraulic conductance diagnostic (m/day).</summary>
        [Units("m/day")]
        public double HydraulicSoilConductanceOutput => HydraulicSoilConductance;

        /// <summary>Plant hydraulic conductance diagnostic after safety bounds (m/day).</summary>
        [Units("m/day")]
        public double HydraulicPlantConductanceOutput => HydraulicPlantConductance;

        /// <summary>Dynamic K reference used in the supply-index transform (m/day).</summary>
        [Units("m/day")]
        public double HydraulicReferenceConductanceOutput => HydraulicReferenceConductance;

        /// <summary>Conductance term used by the supply-index transform (0-1).</summary>
        [Units("0-1")]
        public double HydraulicKTermOutput => HydraulicKTerm;

        /// <summary>Rooted-zone PAW term used by the supply-index transform (0-1).</summary>
        [Units("0-1")]
        public double HydraulicPAWTermOutput => HydraulicPAWTerm;

        /// <summary>Rooted-zone plant available water capacity used by the supply-index transform (mm).</summary>
        [Units("mm")]
        public double HydraulicRootedPAWCmmOutput => HydraulicRootedPAWCmm;

        /// <summary>Diagnostic flag: 1 when plant conductance is lower than soil conductance, otherwise 0.</summary>
        [Units("0-1")]
        public double HydraulicPlantLimitedOutput => HydraulicSoilConductance > 1e-9 && HydraulicPlantConductance < HydraulicSoilConductance ? 1.0 : 0.0;

        /// <summary>Daily mean vapour pressure deficit (hPa).</summary>
        [Units("hPa")]
        public double VPDOutput => VPD;

        /// <summary>Current phenology phase name for diagnostics.</summary>
        public string PhenologyPhaseNameOutput => phenology?.CurrentPhaseName ?? string.Empty;

        /// <summary>Whether phenology is currently in the configured dormancy phase.</summary>
        [Units("0-1")]
        public double DormancyPhaseActiveOutput => IsCurrentPhenologyPhaseForStage(DormancyStage) ? 1.0 : 0.0;

        /// <summary>Whether phenology is currently in the configured forcing phase.</summary>
        [Units("0-1")]
        public double ForcingPhaseActiveOutput => IsCurrentPhenologyPhaseForStage(ForcingStage) ? 1.0 : 0.0;

        /// <summary>Accumulated chill units in the current dormancy phase.</summary>
        public double ChillUnitsAccumulatedOutput => chillUnitsAccumulated;

        /// <summary>Chill units required to leave dormancy.</summary>
        public double ChillUnitsRequiredOutput => ChillUnitsRequired;

        /// <summary>Chill units recorded at the most recent chill-triggered transition.</summary>
        public double ChillUnitsAtLastTransitionOutput => chillUnitsAtLastTransition;

        /// <summary>Whether the chill requirement triggered a transition today.</summary>
        [Units("0-1")]
        public double ChillRequirementSatisfiedTodayOutput => chillRequirementSatisfiedToday;

        /// <summary>Accumulated forcing units in the current forcing phase.</summary>
        public double ForcingUnitsAccumulatedOutput => forcingUnitsAccumulated;

        /// <summary>Forcing units required to reach bud break.</summary>
        public double ForcingUnitsRequiredOutput => ForcingUnitsRequired;

        /// <summary>Forcing units recorded at the most recent forcing-triggered transition.</summary>
        public double ForcingUnitsAtLastTransitionOutput => forcingUnitsAtLastTransition;

        /// <summary>Whether the forcing requirement triggered a transition today.</summary>
        [Units("0-1")]
        public double ForcingRequirementSatisfiedTodayOutput => forcingRequirementSatisfiedToday;

        /// <summary>Raw daily supply index from Ktotal and rooted PAW term.</summary>
        [Units("0-1")]
        public double SupplyIndexRawOutput => SupplyIndexRaw;

        /// <summary>Effective daily supply index after asymmetric smoothing.</summary>
        [Units("0-1")]
        public double SupplyIndexEffectiveOutput => SupplyIndexEffective;

        /// <summary>Daily growth-stress modifier derived from effective supply.</summary>
        [Units("0-1")]
        public double GrowthStressModifierOutput => GrowthStressModifier;

        /// <summary>Rooted-zone PAW fraction (0-1).</summary>
        [Units("0-1")]
        public double RootZonePAWFractionOutput => RootZonePAWFraction;

        /// <summary>Derived root activation diagnostic from root depth relative to soil profile depth (0-1).</summary>
        [Units("0-1")]
        public double RootActivationOutput
        {
            get
            {
                double profileDepth_m = (soilPhysical?.Thickness ?? Array.Empty<double>()).Sum() / 1000.0;
                if (profileDepth_m <= 1e-9)
                    return 0.0;

                double rootDepth_m = Math.Max(0.0, (rootOrgan?.Depth ?? 0.0) / 1000.0);
                return Clamp01(rootDepth_m / profileDepth_m);
            }
        }

        /// <summary>Optional bud load diagnostic (buds/m^2) used by reproductive potential calculations.</summary>
        [Units("buds/m^2")]
        public double BudLoadPerAreaOutput => Math.Max(0.0, budOrgan?.NodeNumber ?? 0.0);

        /// <summary>Effective stress signal exposed to reproduction and quality pathways.</summary>
        [Units("0-1")]
        public double StressForReproOutput => SupplyIndexEffectiveOutput;

        /// <summary>Daily fruit-set flux (fruit/m^2/day).</summary>
        [Units("fruit/m^2/day")]
        public double ReproSetRateOutput => fruitOrgan?.ReproSetRateOutput ?? 0.0;

        /// <summary>Daily fruit-drop/senescence flux (fruit/m^2/day).</summary>
        [Units("fruit/m^2/day")]
        public double ReproDropRateOutput => fruitOrgan?.ReproDropRateOutput ?? 0.0;

        /// <summary>Smoothed fruit water-fraction state used by quality calculations (0-1).</summary>
        [Units("0-1")]
        public double FruitWaterFractionOutput => fruitWaterFractionState;

        /// <summary>Crown width (m).</summary>
        [Units("m")]
        public double CrownWidthOutput => Width / 1000.0;

        /// <summary>Crown depth (m).</summary>
        [Units("m")]
        public double CrownDepthOutput => Depth / 1000.0;

        /// <summary>Leaf fraction requested for the current pruning event.</summary>
        [Units("0-1")]
        public double PruningLeafFractionOutput => Clamp01(PruningLeafFraction);

        /// <summary>Structural fraction requested for the current pruning event.</summary>
        [Units("0-1")]
        public double PruningStructuralFractionOutput => Clamp01(PruningStructuralFraction);

        /// <summary>Active structural pruning state currently applied to canopy calculations.</summary>
        [Units("0-1")]
        public double ActiveStructuralPruningFractionOutput => activeStructuralPruningFraction;

        /// <summary>Current structural multiplier applied to crown width and depth.</summary>
        [Units("-")]
        public double CanopyStructuralSizeFactorOutput => currentStructuralSizeFactor;

        /// <summary>Current structural multiplier applied to light-equivalent LAI.</summary>
        [Units("-")]
        public double CanopyStructuralLightFactorOutput => currentStructuralLightFactor;

        /// <summary>Daily carbon supply signal for reserve control (g/m^2/day).</summary>
        [Units("g/m^2/d")]
        public double ReserveSupplyDMOutput => ReserveSupplyDM;

        /// <summary>Daily critical structural demand for reserve control (g/m^2/day).</summary>
        [Units("g/m^2/d")]
        public double ReserveCriticalDemandDMOutput => ReserveCriticalDemandDM;

        /// <summary>Daily reserve pool size (g/m^2).</summary>
        [Units("g/m^2")]
        public double ReservePoolDMOutput => ReservePoolDM;

        /// <summary>Daily reserve capacity (g/m^2).</summary>
        [Units("g/m^2")]
        public double ReserveCapacityDMOutput => ReserveCapacityDM;

        /// <summary>Daily reserve storage demand signal (g/m^2/day).</summary>
        [Units("g/m^2/d")]
        public double ReserveStoredDMOutput => ReserveStoredDM;

        /// <summary>Daily reserve mobilisation flow (g/m^2/day).</summary>
        [Units("g/m^2/d")]
        public double ReserveMobilisedDMOutput => ReserveMobilisedDM;

        #endregion

        #region Plant Interface

        /// <summary>
        /// Aggregates structural and storage biomass (dry matter and nitrogen) from the tree’s key organs:
        /// leaf, wood and fruit.  Hides the base <c>Plant.AboveGround</c> implementation so that we only
        /// include these three organ pools (Live + Dead) in our fruit‑tree model, ensuring accurate
        /// mass accounting for perennial trees with distinct fruit and wood components.
        /// </summary>
        new public IBiomass AboveGround
        {
            get
            {
                var total = new Biomass();
                IOrgan[] organs = { leafOrgan, woodOrgan, fruitOrgan };

                foreach (var organ in organs)
                {
                    if (organ == null)
                        continue;

                    IBiomass live = organ switch
                    {
                        PerennialLeaf leaf => leaf.Live,
                        GenericOrgan genericOrgan => genericOrgan.Live,
                        ReproductiveOrgan reproductiveOrgan => reproductiveOrgan.Live,
                        _ => organ is INodeModel organModel
                            ? Node.FindChild<IBiomass>("Live", relativeTo: organModel)
                            : null
                    };

                    IBiomass dead = organ switch
                    {
                        PerennialLeaf leaf => leaf.Dead,
                        GenericOrgan genericOrgan => genericOrgan.Dead,
                        ReproductiveOrgan reproductiveOrgan => reproductiveOrgan.Dead,
                        _ => organ is INodeModel organModel
                            ? Node.FindChildren<IBiomass>("Dead", relativeTo: organModel).FirstOrDefault()
                            : null
                    };

                    // sum live pools
                    if (live != null)
                    {
                        total.StructuralWt += live.StructuralWt;
                        total.StorageWt += live.StorageWt;
                        total.StructuralN += live.StructuralN;
                        total.StorageN += live.StorageN;
                    }

                    // sum dead pools if present
                    if (dead != null)
                    {
                        total.StructuralWt += dead.StructuralWt;
                        total.StorageWt += dead.StorageWt;
                        total.StructuralN += dead.StructuralN;
                        total.StorageN += dead.StorageN;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Daily water uptake by soil layer (mm), summed across all soil zones for this tree.
        /// Hides Plant.WaterUptake so that when multiple plants compete in different zones, we
        /// aggregate each zone’s uptake rather than relying on a single-root profile.
        /// </summary>
        [Units("mm")]
        new public IReadOnlyList<double> WaterUptake
        {
            get
            {
                int maxLayers = Uptakes.Count > 0 ? Uptakes.Max(u => u.Water?.Length ?? 0) : 0;

                // sum up each zone’s uptake into one array
                double[] total = new double[maxLayers];
                foreach (var u in Uptakes)
                {
                    if (u.Water == null) continue;
                    for (int i = 0; i < u.Water.Length; i++)
                        total[i] += u.Water[i];
                }

                return total;
            }
        }

        /// <summary>
        /// Daily nitrogen uptake by soil layer (NO3 + NH4) (kg/ha), summed across all soil zones for this tree.
        /// Hides Plant.NitrogenUptake so that when multiple plants compete in different zones, we
        /// aggregate NO3 and NH4 uptake per zone rather than using a single-root profile.
        /// </summary>
        [Units("kg/ha")]
        new public IReadOnlyList<double> NitrogenUptake
        {
            get
            {
                int maxNO3 = Uptakes.Count > 0 ? Uptakes.Max(u => u.NO3N?.Length ?? 0) : 0;
                int maxNH4 = Uptakes.Count > 0 ? Uptakes.Max(u => u.NH4N?.Length ?? 0) : 0;
                int maxLayers = Math.Max(maxNO3, maxNH4);

                double[] total = new double[maxLayers];
                foreach (var u in Uptakes)
                {
                    if (u.NO3N != null)
                        for (int i = 0; i < u.NO3N.Length; i++)
                            total[i] += u.NO3N[i];
                    if (u.NH4N != null)
                        for (int i = 0; i < u.NH4N.Length; i++)
                            total[i] += u.NH4N[i];
                }

                return total;
            }
        }

        /// <summary>
        /// Ready to harvest when either the phenology stage is past maturity,
        /// or (if enabled) the fruit quality thresholds are met.
        /// </summary>
        new public bool IsReadyForHarvesting
        {
            get
            {
                bool harvestByQuality = HarvestBrixThreshold > 0 && HarvestAcidThreshold > 0;
                bool phenReady = HarvestByPhenology && phenology.Beyond(MaturityStage);

                bool qualReady = harvestByQuality
                              && fruitDryMatter_g_m2 > 0
                              && QualityBrix >= HarvestBrixThreshold
                              && QualityAcidPct <= HarvestAcidThreshold;

                // If neither phenology‑ nor quality‑based harvesting is enabled,
                // fall back to “any fruit present”
                if (!HarvestByPhenology && !harvestByQuality)
                    return fruitDryMatter_g_m2 > 0;

                return phenReady || qualReady;
            }
        }
        #endregion

        #region Canopy proxies to leafOrgan

        /// <summary>Type identifier for the canopy.</summary>
        public string CanopyType => leafOrgan.CanopyType;

        /// <summary>Canopy albedo.</summary>
        public double Albedo => leafOrgan.Albedo;

        /// <summary>Maximum stomatal conductance.</summary>
        public double Gsmax => leafOrgan.Gsmax;

        /// <summary>Resistance to water vapour transfer (R50).</summary>
        public double R50 => leafOrgan.R50;

        /// <summary>Canopy width (mm).</summary>
        public double Width { get; private set; }

        /// <summary>Canopy depth (mm).</summary>
        public double Depth { get; private set; }

        /// <summary>Canopy height (mm).</summary>
        public double Height
        {
            get
            {
                if (leafOrgan != null)
                    return leafOrgan.Height; // mm (PerennialLeaf uses mm)

                return Math.Max(0.0, currentHeight * 1000.0);
            }
            set
            {
                double height = Math.Max(0.0, value);
                currentHeight = height / 1000.0;

                if (leafOrgan != null)
                    leafOrgan.Height = height; // mm
            }
        }

        /// <summary>Potential evapotranspiration (mm).</summary>
        public double PotentialEP
        {
            get => leafOrgan?.PotentialEP ?? potentialEP;
            set
            {
                potentialEP = value;
                if (leafOrgan != null)
                    leafOrgan.PotentialEP = value;
            }
        }

        /// <summary>Actual water demand (mm).</summary>
        public double WaterDemand
        {
            get => leafOrgan?.WaterDemand ?? waterDemand;
            set
            {
                waterDemand = value;
                if (leafOrgan != null)
                    leafOrgan.WaterDemand = value;
            }
        }

        #endregion

        // Put this in GenericFruitTree
        private void PrimeRootWaterUptakeArrays()
        {
            if (rootOrgan == null) return;

            // 1) PlantZone exists after Root.OnSimulationCommencing:
            var pz = rootOrgan.PlantZone;
            int nLayersPZ = pz?.Physical?.Thickness?.Length ?? 0;
            if (nLayersPZ > 0)
            {
                if (pz.WaterUptake == null)
                    pz.WaterUptake = new double[nLayersPZ]; // zeros
                if (pz.NitUptake == null)
                    pz.NitUptake = new double[nLayersPZ]; // zeros
                if (pz.NO3Uptake == null)
                    pz.NO3Uptake = new double[nLayersPZ]; // zeros
                if (pz.NH4Uptake == null)
                    pz.NH4Uptake = new double[nLayersPZ]; // zeros
            }

            // 2) After Root.InitialiseZones() (runs on PlantSowing), additional zones exist:
            if (rootOrgan.Zones != null && rootOrgan.Zones.Count > 0)
            {
                foreach (var zs in rootOrgan.Zones)
                {
                    int n = zs?.Physical?.Thickness?.Length ?? 0;
                    if (n > 0)
                    {
                        if (zs.WaterUptake == null)
                            zs.WaterUptake = new double[n]; // zeros
                        if (zs.NitUptake == null)
                            zs.NitUptake = new double[n]; // zeros
                        if (zs.NO3Uptake == null)
                            zs.NO3Uptake = new double[n]; // zeros
                        if (zs.NH4Uptake == null)
                            zs.NH4Uptake = new double[n]; // zeros
                    }
                }
            }
        }

        private void EnsureCanopyConductanceDefaults()
        {
            if (leafOrgan == null)
                return;

            if (leafOrgan.Gsmax350 <= 0 && DefaultGsmax350 > 0)
                leafOrgan.Gsmax350 = DefaultGsmax350;

            if (leafOrgan.R50 <= 0 && DefaultR50 > 0)
                leafOrgan.R50 = DefaultR50;
        }

        private void EnsureLeafMinNDemand()
        {
            if (leafOrgan == null)
                return;
            if (Node == null)
                return;

            var nDemands = Node.FindChild<NutrientPoolFunctions>("nDemands", relativeTo: leafOrgan);
            if (nDemands?.Structural == null)
                return;

            if (nDemands.Structural is LeafMinNConcDemandFunction)
                return;

            nDemands.Structural = new LeafMinNConcDemandFunction
            {
                Name = "LeafMinNConcDemand",
                BaseDemand = nDemands.Structural,
                Leaf = leafOrgan
            };
        }

        private static bool IsZeroOrMissing(IFunction fn)
        {
            if (fn == null)
                return true;
            if (fn is Constant constant)
                return constant.FixedValue <= 0.0;
            return false;
        }

        private void EnsureReserveMechanism()
        {
            if (woodOrgan == null || leafOrgan == null)
                return;
            if (Node == null)
                return;

            var dmDemands = Node.FindChild<NutrientDemandFunctions>("dmDemands", relativeTo: woodOrgan);
            bool needsStorageDemand = dmDemands != null
                                      && (dmDemands.Storage is ReserveStorageDemandFunction
                                          || IsZeroOrMissing(dmDemands.Storage));
            if (needsStorageDemand)
            {
                dmDemands.Storage = new ReserveStorageDemandFunction
                {
                    Name = "ReserveStorageDemand",
                    Tree = this
                };
            }

            var nDemands = Node.FindChild<NutrientDemandFunctions>("nDemands", relativeTo: woodOrgan);
            bool needsStorageNDemand = nDemands != null
                                       && (nDemands.Storage is ReserveStorageNDemandFunction
                                           || IsZeroOrMissing(nDemands.Storage));
            if (needsStorageNDemand)
            {
                IFunction baseDemand = nDemands.Storage;
                if (baseDemand is ReserveStorageNDemandFunction reserveStorageNDemand)
                    baseDemand = reserveStorageNDemand.BaseDemand;
                if (IsZeroOrMissing(baseDemand))
                    baseDemand = null;

                nDemands.Storage = new ReserveStorageNDemandFunction
                {
                    Name = "ReserveStorageNDemand",
                    BaseDemand = baseDemand,
                    Tree = this,
                    Wood = woodOrgan
                };
            }

            if (ReserveMobilisationRate > 0 && IsZeroOrMissing(woodOrgan.DMRetranslocationFactor))
            {
                woodOrgan.DMRetranslocationFactor = new ReserveMobilisationFactorFunction
                {
                    Name = "ReserveMobilisationFactor",
                    Tree = this
                };
            }
        }

        private void SeedInitialReserves()
        {
            if (reservesSeeded || woodOrgan == null || !IsAlive)
                return;

            double structuralWood = Math.Max(0.0, woodOrgan.Live.StructuralWt);
            if (structuralWood <= 0.0)
                return;

            double capacityFrac = Clamp01(ReserveCapacityFracOfWoodDM);
            double capacity = capacityFrac * structuralWood;
            double target = Math.Max(0.0, ReserveInitFrac) * structuralWood;
            if (capacity > 0.0)
                target = Math.Min(target, capacity);

            double current = Math.Max(0.0, woodOrgan.Live.StorageWt);
            double dm = Math.Max(0.0, target - current);
            double nConc = Math.Max(0.0, InitialReserveNConc);

            if (dm > 0.0)
            {
                woodOrgan.Live.StorageWt += dm;
                woodOrgan.Live.StorageN += dm * nConc;
            }

            reservesSeeded = true;

            Summary?.WriteMessage(this,
                $"[Reserves] Seeded wood storage from wood size: target={target:F2}, add={dm:F2}, Nadd={dm * nConc:F3} g/m^2",
                MessageType.Diagnostic);
        }

        internal double GetReserveDMRetranslocationFactor()
        {
            UpdateReserveBalanceSignals();
            return ReserveDMRetranslocationFactor;
        }

        internal double GetReserveStorageDemandDM()
        {
            UpdateReserveBalanceSignals();
            return ReserveStoredDM;
        }

        private void UpdateReserveBalanceSignals()
        {
            DateTime today = clock?.Today ?? DateTime.MinValue;
            if (lastReserveBalanceDate == today)
                return;
            lastReserveBalanceDate = today;

            ReserveSupplyDM = 0.0;
            ReserveCriticalDemandDM = 0.0;
            ReserveSurplusDM = 0.0;
            ReserveDeficitDM = 0.0;
            ReserveCapacityDM = 0.0;
            ReservePoolDM = Math.Max(0.0, woodOrgan?.Live.StorageWt ?? 0.0);
            ReserveStoredDM = 0.0;
            ReserveMobilisedDM = 0.0;
            ReserveDMRetranslocationFactor = 0.0;

            if (woodOrgan == null || leafOrgan == null || !IsAlive)
                return;

            SeedInitialReserves();

            bool usedFallback = false;
            ReserveSupplyDM = Math.Max(0.0, GetSupplyDMSignal());
            ReserveCriticalDemandDM = Math.Max(0.0, GetCriticalDemandDMSignal(ref usedFallback));

            if (usedFallback && !reserveDemandFallbackWarningIssued)
            {
                Summary?.WriteMessage(this,
                    "[Reserves] Demand signal fell back to organ DMDemand.Structural state because one or more demand functions were unavailable.",
                    MessageType.Diagnostic);
                reserveDemandFallbackWarningIssued = true;
            }

            double structuralWood = Math.Max(0.0, woodOrgan.Live.StructuralWt);
            ReserveCapacityDM = Clamp01(ReserveCapacityFracOfWoodDM) * structuralWood;

            double pool = Math.Max(0.0, woodOrgan.Live.StorageWt);
            ReservePoolDM = pool;

            ReserveSurplusDM = Math.Max(0.0, ReserveSupplyDM - ReserveCriticalDemandDM);
            ReserveDeficitDM = Math.Max(0.0, ReserveCriticalDemandDM - ReserveSupplyDM);

            double freeCapacity = Math.Max(0.0, ReserveCapacityDM - pool);
            double store = Math.Min(ReserveSurplusDM, freeCapacity);
            if (ReserveStorageRate > 0.0)
                store = Math.Min(store, ReserveStorageRate * freeCapacity);
            ReserveStoredDM = Math.Max(0.0, store);
            ReservePoolDM = pool;

            double maxMobilisation = Math.Max(0.0, ReserveMobilisationRate) * pool;
            ReserveMobilisedDM = Math.Min(ReserveDeficitDM, maxMobilisation);

            double eps = 1e-9;
            ReserveDMRetranslocationFactor = Clamp01(ReserveMobilisedDM / (ReserveCriticalDemandDM + eps));
        }

        private double GetSupplyDMSignal()
        {
            // Prefer direct function evaluation to avoid stale DMSupply state from the previous day.
            leafPhotosynthesisFn ??= leafOrgan == null || Node == null
                ? null
                : Node.FindChild<IFunction>("Photosynthesis", relativeTo: leafOrgan);
            double supply = Math.Max(0.0, leafPhotosynthesisFn?.Value() ?? 0.0);
            if (supply > 0.0)
                return supply;

            return Math.Max(0.0, leafOrgan?.DMSupply?.Total ?? 0.0);
        }

        private double GetCriticalDemandDMSignal(ref bool usedFallback)
        {
            return
                GetLeafStructuralDemandDM(ref usedFallback) +
                GetRootStructuralDemandDM(ref usedFallback) +
                GetFruitStructuralDemandDM(ref usedFallback) +
                GetWoodStructuralDemandDM(ref usedFallback);
        }

        private double EvaluateStructuralDemand(IFunction structuralDemandFn, IFunction dMCEFn, IFunction remobilisationCostFn = null)
        {
            if (structuralDemandFn == null)
                return -1.0;

            double demand = Math.Max(0.0, structuralDemandFn.Value());
            if (dMCEFn != null)
            {
                double dMCE = dMCEFn.Value();
                if (dMCE <= 0.0)
                    return 0.0;
                demand /= dMCE;
            }

            if (remobilisationCostFn != null)
                demand += Math.Max(0.0, remobilisationCostFn.Value());

            return Math.Max(0.0, demand);
        }

        private double GetLeafStructuralDemandDM(ref bool usedFallback)
        {
            IFunction demandFn = leafOrgan == null || Node == null
                ? null
                : Node.FindChild<NutrientPoolFunctions>("dmDemands", relativeTo: leafOrgan)?.Structural;
            double functionDemand = EvaluateStructuralDemand(demandFn, dMCEFn: null);
            if (functionDemand >= 0.0)
                return functionDemand;

            usedFallback = true;
            return Math.Max(0.0, leafOrgan?.DMDemand?.Structural ?? 0.0);
        }

        private double GetRootStructuralDemandDM(ref bool usedFallback)
        {
            IFunction demandFn = rootOrgan == null || Node == null
                ? null
                : Node.FindChild<NutrientDemandFunctions>("dmDemands", relativeTo: rootOrgan)?.Structural;
            IFunction dMCE = rootOrgan == null || Node == null
                ? null
                : Node.FindChild<IFunction>("DMConversionEfficiency", relativeTo: rootOrgan);
            IFunction remobilisationCost = rootOrgan == null || Node == null
                ? null
                : Node.FindChild<IFunction>("RemobilisationCost", relativeTo: rootOrgan)
                  ?? Node.FindChild<IFunction>("remobilisationCost", relativeTo: rootOrgan);
            double functionDemand = EvaluateStructuralDemand(demandFn, dMCE, remobilisationCost);
            if (functionDemand >= 0.0)
                return functionDemand;

            usedFallback = true;
            return Math.Max(0.0, rootOrgan?.DMDemand?.Structural ?? 0.0);
        }

        private double GetFruitStructuralDemandDM(ref bool usedFallback)
        {
            return Math.Max(0.0, fruitOrgan?.DMDemand?.Structural ?? 0.0);
        }

        private double GetWoodStructuralDemandDM(ref bool usedFallback)
        {
            IFunction demandFn = woodOrgan == null || Node == null
                ? null
                : Node.FindChild<NutrientDemandFunctions>("dmDemands", relativeTo: woodOrgan)?.Structural;
            IFunction dMCE = woodOrgan == null || Node == null
                ? null
                : Node.FindChild<IFunction>("DMConversionEfficiency", relativeTo: woodOrgan);
            IFunction remobilisationCost = woodOrgan == null || Node == null
                ? null
                : Node.FindChild<IFunction>("RemobilisationCost", relativeTo: woodOrgan)
                  ?? Node.FindChild<IFunction>("remobilisationCost", relativeTo: woodOrgan);
            double functionDemand = EvaluateStructuralDemand(demandFn, dMCE, remobilisationCost);
            if (functionDemand >= 0.0)
                return functionDemand;

            usedFallback = true;
            return Math.Max(0.0, woodOrgan?.DMDemand?.Structural ?? 0.0);
        }

        #region Event Handlers

        /// <summary>Initialise all state at the start of the simulation.</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // Guard against PerennialLeaf.Clear() NRE on first sow:
            // PerennialLeaf expects these pools to be non-null when Plant.Sow fires.
            if (leafOrgan.Detached == null)
                leafOrgan.Detached = new Biomass();
            if (leafOrgan.Removed == null)
                leafOrgan.Removed = new Biomass();

            // If HeightFunction hasn't run yet, give the leaf a minimal height (mm)
            if (leafOrgan.Height <= 0)
                leafOrgan.Height = Math.Max(1.0, InitialHeight * 1000.0);

            EnsureCanopyConductanceDefaults();
            EnsureLeafMinNDemand();
            EnsureReserveMechanism();

            PrimeRootWaterUptakeArrays();
            InitialiseState();

            Summary.WriteMessage(this, $"[Probe@Commencing] leafOrgan: Height={leafOrgan.Height:F1} mm, Depth={leafOrgan.Depth:F1} mm, LAI={leafOrgan.LAI:F3}", MessageType.Diagnostic);
        }

        /// <summary>Daily initialisation: update VPD and handle chilling.</summary>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            VPD = VPDCalculator.Value();
            Summary.WriteMessage(this, $"[Init] VPDCalculator -> VPD = {VPD:F2} hPa", MessageType.Diagnostic);
        }

        /// <summary>Accumulate chill units and advance phenology when threshold reached.</summary>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation_Chill(object sender, EventArgs e)
        {
            chillRequirementSatisfiedToday = 0.0;

            if (string.IsNullOrWhiteSpace(DormancyStage)
             || string.IsNullOrWhiteSpace(BudBreakStage)
             || ChillUnitsRequired <= 0
             || ChillRate == null)
                return;

            if (IsCurrentPhenologyPhaseForStage(DormancyStage))
            {
                chillUnitsAccumulated += ChillRate.Value();
                if (chillUnitsAccumulated >= ChillUnitsRequired)
                {
                    chillUnitsAtLastTransition = chillUnitsAccumulated;
                    chillRequirementSatisfiedToday = 1.0;

                    if (!string.IsNullOrWhiteSpace(ForcingStage) && ForcingUnitsRequired > 0.0)
                    {
                        Summary?.WriteMessage(this, $"[Phenology] Chill requirement met: accumulated={chillUnitsAccumulated:F3}, required={ChillUnitsRequired:F3}; setting stage to '{ForcingStage}'.", MessageType.Diagnostic);
                        phenology.SetToStage(ForcingStage);
                    }
                    else
                    {
                        Summary?.WriteMessage(this, $"[Phenology] Chill requirement met: accumulated={chillUnitsAccumulated:F3}, required={ChillUnitsRequired:F3}; setting stage to '{BudBreakStage}'.", MessageType.Diagnostic);
                        phenology.SetToStage(BudBreakStage);
                    }

                    chillUnitsAccumulated = 0.0;
                }
            }
        }

        /// <summary>Accumulate forcing heat units and advance to bud-break when threshold reached.</summary>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation_Forcing(object sender, EventArgs e)
        {
            forcingRequirementSatisfiedToday = 0.0;

            if (string.IsNullOrWhiteSpace(ForcingStage)
             || string.IsNullOrWhiteSpace(BudBreakStage)
             || ForcingUnitsRequired <= 0.0)
                return;

            if (IsCurrentPhenologyPhaseForStage(ForcingStage))
            {
                double tmean = (weather.MaxT + weather.MinT) / 2.0;
                double gdd = Math.Max(0.0, tmean - ForcingBaseTemperature);
                forcingUnitsAccumulated += gdd;

                if (forcingUnitsAccumulated >= ForcingUnitsRequired)
                {
                    forcingUnitsAtLastTransition = forcingUnitsAccumulated;
                    forcingRequirementSatisfiedToday = 1.0;
                    Summary?.WriteMessage(this, $"[Phenology] Forcing requirement met: accumulated={forcingUnitsAccumulated:F3}, required={ForcingUnitsRequired:F3}; setting stage to '{BudBreakStage}'.", MessageType.Diagnostic);
                    phenology.SetToStage(BudBreakStage);
                    forcingUnitsAccumulated = 0.0;
                }
            }
        }

        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters s)
        {
            if (leafOrgan.Height <= 0)
                leafOrgan.Height = Math.Max(1.0, InitialHeight * 1000.0);

            EnsureCanopyConductanceDefaults();
            EnsureLeafMinNDemand();
            EnsureReserveMechanism();
            ResetLeafFallState();
            activeStructuralPruningFraction = 0.0;
            currentStructuralSizeFactor = 1.0;
            currentStructuralLightFactor = 1.0;
            reservesSeeded = false;
            lastReserveBalanceDate = DateTime.MinValue;
            PrimeRootWaterUptakeArrays(); // call it again just in case
        }

        /// <summary>Daily growth: sync height, update hydraulics, photosynthesis, allocation, quality, harvest status.</summary>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            UpdateReserveBalanceSignals();
            // Sync height from the leaf organ (which has just been updated by its own HeightFunction).
            // tree.Height forwards into leafOrgan.Height (mm), and Height getter reads leafOrgan.Height/1000.
            // Keep at least the initial height until the leaf organ reports something >0
            // Height (ICanopy) is mm → convert to m for internal geometry
            currentHeight = Math.Max(this.Height / 1000.0, InitialHeight);

            // Refresh hydraulics after potential growth updates.
            SoilPlantConductance = UpdateHydraulics();
            UpdateSupplyState(SoilPlantConductance);
        }

        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth_AfterPartition(object sender, EventArgs e)
        {
            ApplyDailyLeafFall();
            fruitOrgan?.SyncCohortFromLivePools();

            // Refresh state after the arbitrator has allocated
            var leafLive = leafOrgan.Live;
            var fruitLive = fruitOrgan.Live;

            leafDryMatter_g_m2 = leafLive.Wt;
            fruitDryMatter_g_m2 = fruitLive.Wt;

            UpdateFruitQuality(updatePools: true);
            UpdateHarvestStatus();
        }


        [EventSubscribe("PostHarvesting")]
        private void OnPostHarvesting(object sender, EventArgs e)
        {
            double decay = Math.Exp(-1.0 / Math.Max(1e-6, NoFruitPoolDecayTauDays));
            fruitSugarMass_g_m2 *= decay;
            fruitAcidMass_g_m2 *= decay;
            fruitOrgan?.ResetCohortIfNoFruit();
            fruitDryMatter_g_m2 = Math.Max(0.0, fruitOrgan?.Live?.Wt ?? 0.0);
            prevFruitDM_g_m2 = fruitDryMatter_g_m2;
            double wc = Clamp(fruitWaterFractionState, 0.01, 0.99);
            prevFruitFresh_g_m2 = fruitDryMatter_g_m2 / Math.Max(1e-6, 1.0 - wc);
            TryResetSeasonalPhenologyAfterHarvest();
        }

        /// <summary>Ensure hydraulics are up-to-date before microclimate routines.</summary>
        [EventSubscribe("DoUpdateWaterDemand")]
        private void OnDoUpdateWaterDemand(object sender, EventArgs e)
        {
            SoilPlantConductance = UpdateHydraulics();
            UpdateSupplyState(SoilPlantConductance);
        }

        /// <summary>Prune event: remove live leaf area and apply structural canopy pruning.</summary>
        [EventSubscribe("Prune")]
        private void OnPrune(object sender, EventArgs e)
        {
            double leafFraction = Clamp01(PruningLeafFraction);
            double structuralFraction = Clamp01(PruningStructuralFraction);
            if (leafFraction <= 0.0 && structuralFraction <= 0.0) return;

            if (leafFraction > 0.0)
            {
                leafOrgan.RemoveBiomass(
                    liveToRemove: 0.0,
                    deadToRemove: 0.0,
                    liveToResidue: leafFraction,
                    deadToResidue: 0.0);
            }

            activeStructuralPruningFraction = Math.Max(activeStructuralPruningFraction, structuralFraction);
        }


        /// <summary>Thin event: remove fraction of live fruit biomass.</summary>
        [EventSubscribe("Thin")]
        private void OnThin(object sender, EventArgs e)
        {
            if (ThinningFraction <= 0) return;

            fruitOrgan.RemoveBiomass(
                liveToRemove: 0.0,
                deadToRemove: 0.0,
                liveToResidue: ThinningFraction,
                deadToResidue: 0.0);
        }

        /// <summary>
        /// Build/update the canopy geometry and light-interception profile at the start of the day,
        /// before MicroClimate runs. This guarantees a non-empty LightProfile so intercepted radiation
        /// isn’t zero on day 1
        /// </summary>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInit_Canopy(object s, EventArgs e)
        {
            UpdateCanopyStructure();
            Summary.WriteMessage(this, $"[Probe@DailyInit] leafOrgan: Height={leafOrgan.Height:F1} mm, Depth={leafOrgan.Depth:F1} mm, LAI={leafOrgan.LAI:F3}", MessageType.Diagnostic);
        }

        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType args)
        {
            if (sender != this) return;

            string stage = args.StageName?.Trim() ?? string.Empty;
            string dormancyStage = !string.IsNullOrWhiteSpace(DormancyStage) ? DormancyStage : "Dormancy";

            if (!string.IsNullOrWhiteSpace(dormancyStage) &&
                stage.Equals(dormancyStage, StringComparison.OrdinalIgnoreCase))
            {
                HandleDormancyStageReached();
            }
            else if (!string.IsNullOrWhiteSpace(BudBreakStage) &&
                     stage.Equals(BudBreakStage, StringComparison.OrdinalIgnoreCase))
            {
                HandleBudBreakStageReached();
            }
            else if (!string.IsNullOrWhiteSpace(MaturityStage) &&
                     stage.Equals(MaturityStage, StringComparison.OrdinalIgnoreCase))
            {
                HandleMaturityStageReached();
            }
        }


        /// <summary>After phenology advances: update sinks, canopy, quality, harvest status.</summary>
        [EventSubscribe("PostPhenology")]
        private void OnPostPhenology(object sender, EventArgs e)
        {
            UpdateFruitQuality(updatePools: false);
            UpdateHarvestStatus();
        }

        #endregion

        #region Core Methods

        private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

        private static double Clamp01(double value) => Clamp(value, 0.0, 1.0);

        private static double Logistic(double x, double x50, double slope)
        {
            double exponent = Clamp(-slope * (x - x50), -60.0, 60.0);
            return 1.0 / (1.0 + Math.Exp(exponent));
        }

        /// <summary>Median helper for small rolling windows.</summary>
        private static double Median(IEnumerable<double> values)
        {
            double[] sorted = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
                return 0.0;

            int mid = sorted.Length / 2;
            if (sorted.Length % 2 == 1)
                return sorted[mid];

            return 0.5 * (sorted[mid - 1] + sorted[mid]);
        }

        /// <summary>Convert a time constant (days) to a stable EWMA update coefficient.</summary>
        private static double TauToAlpha(double tauDays)
        {
            double tau = Math.Max(1e-6, tauDays);
            return Clamp01(1.0 - Math.Exp(-1.0 / tau));
        }

        /// <summary>Check whether the current phenology phase starts at the configured stage tag.</summary>
        private bool IsCurrentPhenologyPhaseForStage(string stageName)
        {
            if (phenology?.CurrentPhase == null || string.IsNullOrWhiteSpace(stageName))
                return false;

            string name = stageName.Trim();
            IPhase currentPhase = phenology.CurrentPhase;
            return string.Equals(currentPhase.Start, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentPhase.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Compute rooted-zone PAW fraction as rooted PAW / rooted PAWC.</summary>
        private double ComputeRootZonePAWFraction(out double rootedPawcMm)
        {
            rootedPawcMm = 0.0;
            double rootDepth_m = Math.Max(0.0, (rootOrgan?.Depth ?? 0.0) / 1000.0);
            if (rootDepth_m <= 0.0)
                return 0.0;

            double[] dz_m = (soilPhysical?.Thickness ?? Array.Empty<double>()).Select(mm => mm / 1000.0).ToArray();
            double[] pawmm = soilWater?.PAWmm ?? Array.Empty<double>();
            double[] pawcmm = soilPhysical?.PAWCmm ?? Array.Empty<double>();
            int layerCount = Math.Min(dz_m.Length, Math.Min(pawmm.Length, pawcmm.Length));

            if (!supplyArrayShapeWarningIssued &&
                (layerCount == 0 || pawmm.Length != dz_m.Length || pawcmm.Length != dz_m.Length))
            {
                Summary?.WriteMessage(
                    this,
                    $"[Supply] Rooted PAW array mismatch: PAWmm.Length={pawmm.Length}, PAWCmm.Length={pawcmm.Length}, Thickness.Length={dz_m.Length}. Integrating over min length={layerCount}.",
                    MessageType.Warning);
                supplyArrayShapeWarningIssued = true;
            }

            double depthAccumulated_m = 0.0;
            double rootedPawMm = 0.0;
            rootedPawcMm = 0.0;
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (depthAccumulated_m >= rootDepth_m)
                    break;

                double layerThickness_m = dz_m[layer];
                if (layerThickness_m <= 0.0)
                    continue;

                double thicknessInZone_m = Math.Min(layerThickness_m, rootDepth_m - depthAccumulated_m);
                if (thicknessInZone_m <= 0.0)
                    continue;

                double layerFraction = Clamp01(thicknessInZone_m / layerThickness_m);
                rootedPawMm += Math.Max(0.0, pawmm[layer]) * layerFraction;
                rootedPawcMm += Math.Max(0.0, pawcmm[layer]) * layerFraction;
                depthAccumulated_m += thicknessInZone_m;
            }

            return rootedPawcMm > 1e-9 ? Clamp01(rootedPawMm / rootedPawcMm) : 0.0;
        }

        /// <summary>Update daily supply state from Ktotal and rooted-zone PAW fraction.</summary>
        private void UpdateSupplyState(double ktotal)
        {
            DateTime today = clock?.Today ?? DateTime.MinValue;
            if (today != DateTime.MinValue && lastSupplyUpdateDate == today)
                return;
            lastSupplyUpdateDate = today;

            double rootDepth_m = Math.Max(0.0, (rootOrgan?.Depth ?? 0.0) / 1000.0);
            if (rootDepth_m <= 0.0)
            {
                RootZonePAWFraction = 0.0;
                HydraulicRootedPAWCmm = 0.0;
                HydraulicReferenceConductance = 0.0;
                HydraulicKTerm = 0.0;
                HydraulicPAWTerm = 1.0;
                SupplyIndexRaw = 1.0;
                SupplyIndexEffective = 1.0;
                GrowthStressModifier = 1.0;
                supplyStateInitialised = true;
                return;
            }

            RootZonePAWFraction = ComputeRootZonePAWFraction(out double rootedPawcMm);
            HydraulicRootedPAWCmm = rootedPawcMm;
            if (ktotal > 1e-9)
            {
                positiveKtotalWindow.Enqueue(ktotal);
                while (positiveKtotalWindow.Count > KrefMedianWindowDays)
                    positiveKtotalWindow.Dequeue();
            }

            double kref;
            if (positiveKtotalWindow.Count > 0)
                kref = Math.Max(1e-6, Median(positiveKtotalWindow) * KrefMedianScale);
            else if (ktotal > 1e-9)
                kref = Math.Max(1e-6, ktotal * KrefMedianScale);
            else
                kref = 1e-6;
            HydraulicReferenceConductance = kref;
            HydraulicKTerm = ktotal > 0.0 ? ktotal / (ktotal + kref) : 0.0;
            HydraulicPAWTerm = rootedPawcMm > 1e-9 ? Logistic(RootZonePAWFraction, PAW50, SupplyPawSlope) : 1.0;
            SupplyIndexRaw = Clamp01(HydraulicKTerm * HydraulicPAWTerm);

            if (!supplyStateInitialised)
            {
                SupplyIndexEffective = SupplyIndexRaw;
                supplyStateInitialised = true;
            }
            else
            {
                double tau = SupplyIndexRaw < SupplyIndexEffective ? SupplyTauDownDays : SupplyTauUpDays;
                double alpha = TauToAlpha(tau);
                SupplyIndexEffective = Clamp01(SupplyIndexEffective + alpha * (SupplyIndexRaw - SupplyIndexEffective));
            }

            double exponent = Math.Max(0.0, GrowthStressExponent);
            GrowthStressModifier = Clamp01(Math.Pow(Math.Max(0.0, SupplyIndexEffective), exponent));
        }

        /// <summary>Begin a new dormancy leaf-fall window.</summary>
        private void StartLeafFallWindow()
        {
            inLeafFallWindow = true;
            leafFallProgress = 0.0;
            leafFallRemovedSoFarFrac = 0.0;
            leafFallStartDate = clock?.Today ?? DateTime.MinValue;
        }

        /// <summary>Close and reset dormancy leaf-fall state.</summary>
        private void StopLeafFallWindow()
        {
            ResetLeafFallState();
        }

        /// <summary>Apply tree state changes associated with entering dormancy.</summary>
        private void HandleDormancyStageReached()
        {
            StartLeafFallWindow();
            reproductiveCycleActive = false;
            chillUnitsAccumulated = 0.0;
            forcingUnitsAccumulated = 0.0;
        }

        /// <summary>Apply tree state changes associated with entering bud-break.</summary>
        private void HandleBudBreakStageReached()
        {
            StopLeafFallWindow();

            bool allowBudBreak = true;
            if (MinDaysBetweenBudBreak > 0 && clock != null && lastBudBreakDate != DateTime.MinValue)
            {
                double daysSince = (clock.Today - lastBudBreakDate).TotalDays;
                if (daysSince < MinDaysBetweenBudBreak)
                    allowBudBreak = false;
            }

            if (allowBudBreak)
            {
                reproductiveCycleActive = true;
                if (clock != null)
                    lastBudBreakDate = clock.Today;
            }
            else
            {
                reproductiveCycleActive = false;
            }
        }

        /// <summary>Apply tree state changes associated with maturity.</summary>
        private void HandleMaturityStageReached()
        {
            reproductiveCycleActive = false;
        }

        /// <summary>Get the configured stage used when rewinding phenology for the next season.</summary>
        private string ResolveSeasonalPhenologyResetStage()
        {
            if (!string.IsNullOrWhiteSpace(SeasonalPhenologyResetStage))
                return SeasonalPhenologyResetStage.Trim();

            if (!string.IsNullOrWhiteSpace(DormancyStage))
                return DormancyStage.Trim();

            if (phenology?.StageNames?.Any(stage => stage.Equals("Dormancy", StringComparison.OrdinalIgnoreCase)) == true)
                return "Dormancy";

            return BudBreakStage?.Trim() ?? string.Empty;
        }

        /// <summary>Rewind phenology to the configured seasonal stage after the harvest point.</summary>
        private bool TryResetSeasonalPhenologyAfterHarvest()
        {
            if (!ResetPhenologyAfterHarvest)
                return false;

            DateTime today = clock?.Today ?? DateTime.MinValue;
            if (today != DateTime.MinValue && lastSeasonalPhenologyResetDate == today)
            {
                isHarvested = false;
                return true;
            }

            string resetStage = ResolveSeasonalPhenologyResetStage();
            if (string.IsNullOrWhiteSpace(resetStage))
                throw new Exception($"{Name} cannot reset seasonal phenology because no reset stage has been configured.");

            if (phenology?.StageNames?.Any(stage => stage.Equals(resetStage, StringComparison.OrdinalIgnoreCase)) != true)
                throw new Exception($"{Name} cannot reset seasonal phenology to '{resetStage}' because the stage is not present in the Phenology child.");

            phenology.SetToStage(resetStage);
            lastSeasonalPhenologyResetDate = today;
            ApplySeasonalPhenologyResetSideEffects(resetStage);
            isHarvested = false;

            Summary?.WriteMessage(this, $"[Phenology] Reset seasonal cycle to '{resetStage}' after harvest readiness.", MessageType.Information);
            return true;
        }

        /// <summary>Apply local stage side effects because Phenology.SetToStage rewinds via StageWasReset, not PhaseChanged.</summary>
        private void ApplySeasonalPhenologyResetSideEffects(string resetStage)
        {
            string dormancyStage = !string.IsNullOrWhiteSpace(DormancyStage) ? DormancyStage : "Dormancy";

            if (!string.IsNullOrWhiteSpace(dormancyStage) &&
                resetStage.Equals(dormancyStage, StringComparison.OrdinalIgnoreCase))
                HandleDormancyStageReached();
            else if (!string.IsNullOrWhiteSpace(BudBreakStage) &&
                     resetStage.Equals(BudBreakStage, StringComparison.OrdinalIgnoreCase))
                HandleBudBreakStageReached();
            else if (!string.IsNullOrWhiteSpace(MaturityStage) &&
                     resetStage.Equals(MaturityStage, StringComparison.OrdinalIgnoreCase))
                HandleMaturityStageReached();
        }

        /// <summary>Reset dormancy leaf-fall state variables.</summary>
        private void ResetLeafFallState()
        {
            inLeafFallWindow = false;
            leafFallProgress = 0.0;
            leafFallRemovedSoFarFrac = 0.0;
            leafFallStartDate = DateTime.MinValue;
        }

        /// <summary>Apply smooth daily dormancy leaf-fall.</summary>
        private void ApplyDailyLeafFall()
        {
            if (!inLeafFallWindow)
                return;

            double duration = Math.Max(1.0, LeafFallDurationDays);
            double totalFraction = Clamp01(LeafFallTotalFraction);
            double shape = Math.Max(0.01, LeafFallShape);

            double oldProgress = Clamp01(leafFallProgress);
            double progressIncrement = 1.0 / duration;
            double newProgress = Clamp01(oldProgress + progressIncrement);

            double cumulativeOld = totalFraction * Math.Pow(oldProgress, shape);
            double cumulativeNew = totalFraction * Math.Pow(newProgress, shape);
            double dF = Math.Max(0.0, cumulativeNew - cumulativeOld);

            double remainingLiveFraction = Math.Max(1e-9, 1.0 - leafFallRemovedSoFarFrac);
            double dailyRemoveFrac = Clamp01(dF / remainingLiveFraction);
            if (dailyRemoveFrac > 0.0)
            {
                leafOrgan.RemoveBiomass(
                    liveToRemove: 0.0,
                    deadToRemove: 0.0,
                    liveToResidue: dailyRemoveFrac,
                    deadToResidue: 0.0);

                leafFallRemovedSoFarFrac = Clamp01(leafFallRemovedSoFarFrac + dailyRemoveFrac * remainingLiveFraction);
            }

            leafFallProgress = newProgress;

            if (leafFallProgress >= 1.0 - 1e-9 || leafFallRemovedSoFarFrac >= totalFraction - 1e-9)
                inLeafFallWindow = false;
        }

        private double GetFruitFillProgress()
        {
            if ((string.IsNullOrWhiteSpace(FruitSetStage) || !phenology.InPhase(FruitSetStage)) &&
                (string.IsNullOrWhiteSpace(FruitFillStage) || !phenology.InPhase(FruitFillStage)))
                return 0.0;

            return Clamp01(phenology.FractionInCurrentPhase);
        }

        private double ComputeFruitWaterFraction(double stageProgress)
        {
            double stressForRepro = StressForReproOutput;
            double fwTerm = Logistic(stressForRepro, WC_Fw50, WC_FwSlope);
            double stageTerm = Logistic(stageProgress, WC_Stage50, WC_StageSlope);

            double blend = Clamp01(0.7 * fwTerm + 0.3 * stageTerm);
            double wcMin = Math.Min(WC_Min, WC_Max);
            double wcMax = Math.Max(WC_Min, WC_Max);
            double wc = wcMin + (wcMax - wcMin) * blend;
            return Clamp(wc, 0.01, 0.99);
        }

        private void SyncFruitOrganWaterContent(double wc)
        {
            if (fruitOrganWaterContentFn is Constant wcConstant)
            {
                wcConstant.FixedValue = wc;
                return;
            }

            if (fruitOrganWaterContentFn != null && !fruitWaterSyncWarningIssued)
            {
                Summary?.WriteMessage(this,
                    "[Quality] fruitOrgan.WaterContent is not a Constant; cannot force daily sync to computed wc.",
                    MessageType.Warning);
                fruitWaterSyncWarningIssued = true;
            }
        }

        private double ApplySoftCap(double rawConcentrationPct, double maxConcentrationPct)
        {
            if (maxConcentrationPct <= 0.0)
                return Math.Max(0.0, rawConcentrationPct);

            double ratio = Math.Max(0.0, rawConcentrationPct) / maxConcentrationPct;
            double sharpness = Math.Max(0.1, CapSharpness);
            double saturation = 1.0 - Math.Exp(-Math.Pow(ratio, sharpness));
            return maxConcentrationPct * saturation;
        }

        private double ComputeQualityOutputWeight(double fruitDM)
        {
            double eps = Math.Max(1e-6, FruitQualityOutputWeightEpsilon);
            return Clamp01(fruitDM / (fruitDM + eps));
        }

        /// <summary>Update fruit sugar, acid and water pools and compute quality metrics.</summary>
        private void UpdateFruitQuality(bool updatePools)
        {
            var liveFruit = fruitOrgan.Live;
            double fruitDM = liveFruit.StructuralWt + liveFruit.StorageWt; // g/m^2
            double stageProgress = GetFruitFillProgress();
            double wcEquilibrium = ComputeFruitWaterFraction(stageProgress);
            double alphaWc = TauToAlpha(FruitWaterStateTauDays);
            fruitWaterFractionState = Clamp(fruitWaterFractionState + alphaWc * (wcEquilibrium - fruitWaterFractionState), 0.01, 0.99);
            SyncFruitOrganWaterContent(fruitWaterFractionState);

            // Fresh mass derived from DM and smoothed water fraction state.
            double fresh = fruitDM / Math.Max(1e-6, 1.0 - fruitWaterFractionState);

            double dDM = 0.0;
            double dSugarProd = 0.0;
            double dAcidProd = 0.0;
            double dSugarConc = 0.0;
            double dAcidLoss = 0.0;

            if (updatePools)
            {
                if (fruitDM > 1e-9)
                {
                    dDM = Math.Max(0.0, fruitDM - prevFruitDM_g_m2);
                    dSugarProd = SugarPerDM * dDM;
                    dAcidProd = AcidPerDM * dDM;

                    double gLate = Logistic(stageProgress, 0.5, 12.0);
                    double supplyIndex = StressForReproOutput;
                    dSugarConc = Math.Max(0.0, SugarConcRate) * supplyIndex * gLate;
                    dAcidLoss = Math.Max(0.0, AcidDegradationRate) * gLate * Math.Max(0.0, fruitAcidMass_g_m2);

                    fruitSugarMass_g_m2 = Math.Max(0.0, fruitSugarMass_g_m2 + Math.Max(0.0, dSugarProd) + dSugarConc);
                    fruitAcidMass_g_m2 = Math.Max(0.0, fruitAcidMass_g_m2 + Math.Max(0.0, dAcidProd) - dAcidLoss);
                }
                else
                {
                    double decay = Math.Exp(-1.0 / Math.Max(1e-6, NoFruitPoolDecayTauDays));
                    fruitSugarMass_g_m2 *= decay;
                    fruitAcidMass_g_m2 *= decay;
                }

                prevFruitDM_g_m2 = fruitDM;
                prevFruitFresh_g_m2 = fresh;
            }

            // Convert to percentages of fresh mass with soft saturation.
            double freshSafe = Math.Max(1e-6, fresh);
            double brixRaw = 100.0 * fruitSugarMass_g_m2 / freshSafe;
            double acidRaw = 100.0 * fruitAcidMass_g_m2 / freshSafe;

            double outputWeight = ComputeQualityOutputWeight(fruitDM);
            double dmPctState = (1.0 - fruitWaterFractionState) * 100.0;
            double brixState = ApplySoftCap(brixRaw, BrixMax);
            double acidState = ApplySoftCap(acidRaw, AcidMax);

            QualityDMPct = outputWeight * dmPctState;
            QualityBrix = outputWeight * brixState;
            QualityAcidPct = outputWeight * acidState;

            Summary.WriteMessage(this,
                $"[Quality] DM={fruitDM:F3} g/m², dDM={dDM:F3} g/m²/day, fresh={fresh:F3} g/m², wcEq={wcEquilibrium:F3}, wcState={fruitWaterFractionState:F3}, w={outputWeight:F3}, " +
                $"dSugarProd={dSugarProd:F3}, dSugarConc={dSugarConc:F3}, dAcidProd={dAcidProd:F3}, dAcidLoss={dAcidLoss:F3}, " +
                $"BrixRaw={brixRaw:F2}%, AcidRaw={acidRaw:F2}%, Brix={QualityBrix:F2}%, Acid%={QualityAcidPct:F2}%, DM%={QualityDMPct:F1}%",
                MessageType.Diagnostic);
        }

        /// <summary>If the tree hasn't been harvested, and its ready, do a one‐time harvest.</summary>
        private void UpdateHarvestStatus()
        {
            Summary.WriteMessage(this, $"[Harvest] IsAlive={IsAlive}, isHarvested={isHarvested}, ReadyForHarvesting={IsReadyForHarvesting}", MessageType.Diagnostic);

            if (IsAlive && !isHarvested && IsReadyForHarvesting)
            {
                ReadyToHarvest.Invoke(this, EventArgs.Empty);
                Summary.WriteMessage(this, $"[Harvest] Fired ReadyToHarvest event", MessageType.Information);

                if (FruitFatePolicy == global::Models.Agroforestry.FruitFatePolicy.AutoHarvest)
                {
                    Harvest();
                    Summary.WriteMessage(this, "[Harvest] AutoHarvest policy executed Harvest()", MessageType.Information);
                }

                isHarvested = true;
                TryResetSeasonalPhenologyAfterHarvest();
            }
        }

        /// <summary>Reset all state variables to their initial values.</summary>
        private void InitialiseState()
        {
            currentHeight = InitialHeight;
            activeStructuralPruningFraction = 0.0;
            currentStructuralSizeFactor = 1.0;
            currentStructuralLightFactor = 1.0;
            leafDryMatter_g_m2 = 0;
            fruitDryMatter_g_m2 = 0;
            Uptakes.Clear();
            chillUnitsAccumulated = 0.0;
            forcingUnitsAccumulated = 0.0;
            isHarvested = false;
            fruitSugarMass_g_m2 = 0;
            fruitAcidMass_g_m2 = 0;
            fruitWaterFractionState = Clamp(0.5 * (Math.Min(WC_Min, WC_Max) + Math.Max(WC_Min, WC_Max)), 0.01, 0.99);
            prevFruitDM_g_m2 = 0;
            prevFruitFresh_g_m2 = 0;
            ResetLeafFallState();
            reservesSeeded = false;
            lastReserveBalanceDate = DateTime.MinValue;
            reserveDemandFallbackWarningIssued = false;
            leafPhotosynthesisFn = null;
            ReserveSupplyDM = 0.0;
            ReserveCriticalDemandDM = 0.0;
            ReserveSurplusDM = 0.0;
            ReserveDeficitDM = 0.0;
            ReserveCapacityDM = 0.0;
            ReservePoolDM = 0.0;
            ReserveStoredDM = 0.0;
            ReserveMobilisedDM = 0.0;
            ReserveDMRetranslocationFactor = 0.0;
            lastBudBreakDate = DateTime.MinValue;
            lastSeasonalPhenologyResetDate = DateTime.MinValue;
            reproductiveCycleActive = false;
            clumpingBoundsWarningIssued = false;
            lastHydraulicsDiagnosticDate = DateTime.MinValue;
            hydraulicsArrayShapeWarningIssued = false;
            hydraulicsPsiRefreshWarningIssued = false;
            supplyArrayShapeWarningIssued = false;
            positiveKtotalWindow.Clear();
            supplyStateInitialised = false;
            lastSupplyUpdateDate = DateTime.MinValue;
            RootZonePAWFraction = 0.0;
            HydraulicSoilConductance = 0.0;
            HydraulicPlantConductance = 0.0;
            HydraulicReferenceConductance = 0.0;
            HydraulicKTerm = 1.0;
            HydraulicPAWTerm = 1.0;
            HydraulicRootedPAWCmm = 0.0;
            SupplyIndexRaw = 1.0;
            SupplyIndexEffective = 1.0;
            GrowthStressModifier = 1.0;
        }

        #endregion

        /// <summary>Compute bulk soil–plant hydraulic conductance (m/day).</summary>
        private double UpdateHydraulics()
        {
            // Get layer thicknesses in m
            double[] dz_m = soilPhysical.Thickness.Select(mm => mm / 1000.0).ToArray();

            try
            {
                // WaterBalance updates K as a side-effect of reading PSI.
                _ = soilWater.PSI;
            }
            catch (Exception err)
            {
                if (!hydraulicsPsiRefreshWarningIssued)
                {
                    Summary.WriteMessage(
                        this,
                        $"[Hydraulics] Unable to refresh PSI before reading K ({err.Message}). Proceeding with current K values.",
                        MessageType.Warning);
                    hydraulicsPsiRefreshWarningIssued = true;
                }
            }

            double[] kLayers = soilWater.K ?? Array.Empty<double>();
            int layerCount = Math.Min(dz_m.Length, kLayers.Length);

            if (!hydraulicsArrayShapeWarningIssued && (kLayers.Length == 0 || kLayers.Length != dz_m.Length))
            {
                Summary.WriteMessage(
                    this,
                    $"[Hydraulics] soilWater.K/thickness mismatch: K.Length={kLayers.Length}, Thickness.Length={dz_m.Length}. Integrating over min length={layerCount}.",
                    MessageType.Warning);
                hydraulicsArrayShapeWarningIssued = true;
            }

            // Pull dynamic root depth (mm -> m)
            double dynamicRootDepth_m = Math.Max(0.0, rootOrgan.Depth / 1000.0);

            double integratedK = 0.0;
            double depthAccumulated = 0.0;
            double minKLayer_m_per_day = double.PositiveInfinity;
            int minKLayerIndex = -1;

            // Only integrate down to the current root depth
            for (int layer = 0; layer < layerCount; layer++)
            {
                if (depthAccumulated >= dynamicRootDepth_m)
                    break;

                double layerThickness = dz_m[layer];
                // If only part of this layer is in the rooting zone, truncate it
                double thicknessInZone = Math.Min(layerThickness, dynamicRootDepth_m - depthAccumulated);

                // soilWater.K is cm/h; convert to m/day before integrating.
                double kLayer_m_per_day = kLayers[layer] * CmPerHourToMPerDay;
                integratedK += kLayer_m_per_day * thicknessInZone;
                depthAccumulated += thicknessInZone;

                if (thicknessInZone > 0.0 && kLayer_m_per_day < minKLayer_m_per_day)
                {
                    minKLayer_m_per_day = kLayer_m_per_day;
                    minKLayerIndex = layer;
                }
            }

            // Keep existing rooted-depth integration for soil conductance.
            double Ksoil = depthAccumulated > 0.0 ? integratedK / depthAccumulated : 0.0;
            double Kplant = Math.Max(1e-6, PlantHydraulicConductance);
            HydraulicSoilConductance = Ksoil;
            HydraulicPlantConductance = Kplant;

            double Ktotal;
            if (Ksoil <= 1e-9)
                Ktotal = 0.0;
            else
                Ktotal = 1.0 / (1.0 / Ksoil + 1.0 / Kplant);

            SoilPlantConductance = Ktotal;

            bool shouldLog = clock == null || clock.Today.Date != lastHydraulicsDiagnosticDate.Date;
            if (shouldLog)
            {
                if (clock != null)
                    lastHydraulicsDiagnosticDate = clock.Today.Date;

                string dominantLayer = minKLayerIndex >= 0
                    ? $"L{minKLayerIndex + 1} ({minKLayer_m_per_day:F4} m/day)"
                    : "none";
                string limiter = Ksoil <= Kplant ? "soil" : "plant";

                Summary.WriteMessage(
                    this,
                    $"[Hydraulics] Ksoil={Ksoil:F4} m/day, Kplant={Kplant:F4} m/day, Ktotal={Ktotal:F4} m/day, rootDepth={dynamicRootDepth_m:F3} m, dominantLayer={dominantLayer}, limiter={limiter}",
                    MessageType.Diagnostic);
            }

            return Ktotal;
        }

        /// <summary>
        /// Build/update canopy geometry and provide MicroClimate with a per-layer LAI profile.
        /// IMPORTANT: LightProfile.AmountOnGreen is used by MicroClimate as layer "green amount".
        /// Here it is set to light-equivalent LAI (after clumping), while physical LAI remains leafOrgan.LAI.
        /// MicroClimate will later overwrite those values with intercepted radiation (MJ m⁻² day⁻¹).
        /// Units:
        ///   • Internals in metres; ICanopy.Height is mm (PerennialLeaf.Depth == Height; Width is unused in 1-D).
        ///   • LightProfile[i].thickness is metres; AmountOnGreen is LAI in that layer (m²/m²).
        /// </summary>
        private void UpdateCanopyStructure()
        {
            if (leafOrgan == null) return;

            // --- Geometry (m) → PerennialLeaf.Height in mm for MC
            double crownH_m = (this.Height > 0 ? this.Height / 1000.0
                                               : Math.Max(InitialHeight, currentHeight));
            if (crownH_m <= 0) crownH_m = Math.Max(InitialHeight, 0.5);

            var dims = CrownGeometryHelper.ComputeCrownDimensions(
                crownH_m, CrownWidthHeightRatio, CrownDepthHeightRatio);
            double structuralFraction = Clamp01(activeStructuralPruningFraction);
            const double minStructuralFactor = 0.2;
            currentStructuralSizeFactor = Clamp(
                1.0 - Math.Max(0.0, PruningStructuralSizeSensitivity) * structuralFraction,
                minStructuralFactor,
                1.0);
            currentStructuralLightFactor = Clamp(
                1.0 - Math.Max(0.0, PruningStructuralLightSensitivity) * structuralFraction,
                minStructuralFactor,
                1.0);
            double effectiveWidth_m = Math.Max(0.0, dims.Width) * currentStructuralSizeFactor;
            double effectiveDepth_m = Math.Max(0.0, dims.Depth) * currentStructuralSizeFactor;

            leafOrgan.Height = Math.Max(1.0, crownH_m * 1000.0); // mm required by MC
            this.Width = effectiveWidth_m * 1000.0; // mm (reporting only)
            this.Depth = effectiveDepth_m * 1000.0; // mm (reporting only)

            // --- Keep physical LAI unchanged; clumping only modifies what light "sees".
            double laiPhysical = Math.Max(0.0, leafOrgan.LAI);
            const double minClumping = 0.2;
            const double maxClumping = 2.0;
            double omega = Math.Max(minClumping, Math.Min(maxClumping, ClumpingIndex));
            if (!clumpingBoundsWarningIssued && (ClumpingIndex < minClumping || ClumpingIndex > maxClumping))
            {
                Summary?.WriteMessage(
                    this,
                    $"ClumpingIndex={ClumpingIndex:F3} is outside [{minClumping:F1}, {maxClumping:F1}] and was clamped for radiative transfer.",
                    MessageType.Warning);
                clumpingBoundsWarningIssued = true;
            }

            // --- Build vertical layers from helper using fixed discretisation.
            int nLayers = Math.Max(1, LayerCount);
            var segs = CrownGeometryHelper.GetLayerSegments(
                crownH_m, effectiveWidth_m, effectiveDepth_m, nLayers, CrownShape, CrownGeometryHelper.DensityDistribution.Uniform, 0.0);

            if (segs.Count == 0 || segs.TrueForAll(s => (s.Top - s.Bottom) <= 0))
            {
                // safety fallback: one full-height layer
                segs = new List<CrownGeometryHelper.LayerSegment> {
            new CrownGeometryHelper.LayerSegment { Bottom = 0.0, Top = Math.Max(1e-3, crownH_m) }
        };
            }

            // --- Hand per-layer thickness (m) + LAI to MicroClimate
            var profile = new CanopyEnergyBalanceInterceptionlayerType[segs.Count];
            double[] laiFractions = ComputeBetaLadFractionsForSegments(segs, crownH_m, LAD_Alpha, LAD_Beta);
            double laiPhysicalCheck = 0.0;
            double laiLightCheck = 0.0;

            for (int i = 0; i < segs.Count; i++)
            {
                double thick = Math.Max(1e-6, segs[i].Top - segs[i].Bottom); // m
                double laiPhysical_i = laiPhysical * laiFractions[i]; // m^2/m^2 in this layer (physical)
                double laiLight_i = omega * currentStructuralLightFactor * laiPhysical_i; // light-equivalent LAI for radiative transfer

                profile[i] = new CanopyEnergyBalanceInterceptionlayerType
                {
                    thickness = thick,
                    AmountOnGreen = laiLight_i,
                    AmountOnDead = 0.0
                };
                laiPhysicalCheck += laiPhysical_i;
                laiLightCheck += laiLight_i;
            }

            // MC will overwrite AmountOnGreen with MJ m⁻² d⁻¹ after its radiative transfer
            leafOrgan.LightProfile = profile;

            Summary?.WriteMessage(
                this,
                $"[Canopy->MC] H={leafOrgan.Height:F0}mm, layers={profile.Length}, omega={omega:F2}, structural={structuralFraction:F2}, structuralSize={currentStructuralSizeFactor:F2}, structuralLight={currentStructuralLightFactor:F2}, LAD(a,b)=({LAD_Alpha:F2},{LAD_Beta:F2}), LAIphysical={laiPhysical:F3}, LAIlight={laiLightCheck:F3}, SumPhys={laiPhysicalCheck:F3}",
                MessageType.Diagnostic);
        }

        /// <summary>
        /// Compute normalized layer LAI fractions from a continuous Beta LAD profile over normalized height z in [0,1]
        /// using each segment's actual [Bottom, Top] bounds.
        /// </summary>
        private static double[] ComputeBetaLadFractionsForSegments(
            IReadOnlyList<CrownGeometryHelper.LayerSegment> segments,
            double crownHeight,
            double ladAlpha,
            double ladBeta)
        {
            int layerCount = segments?.Count ?? 0;
            if (layerCount <= 0)
                return Array.Empty<double>();

            const double minShape = 1e-3;
            double alpha = Math.Max(minShape, ladAlpha);
            double beta = Math.Max(minShape, ladBeta);

            var weights = new double[layerCount];
            double total = 0.0;

            double invHeight = 1.0 / Math.Max(1e-12, crownHeight);

            for (int i = 0; i < layerCount; i++)
            {
                double z0 = Math.Max(0.0, Math.Min(1.0, segments[i].Bottom * invHeight));
                double z1 = Math.Max(0.0, Math.Min(1.0, segments[i].Top * invHeight));
                if (z1 < z0)
                {
                    double tmp = z0;
                    z0 = z1;
                    z1 = tmp;
                }

                double w = IntegrateBetaLad(z0, z1, alpha, beta, 8);
                w = Math.Max(0.0, w);
                weights[i] = w;
                total += w;
            }

            if (total <= 0.0)
            {
                double uniform = 1.0 / layerCount;
                for (int i = 0; i < layerCount; i++)
                    weights[i] = uniform;
                return weights;
            }

            for (int i = 0; i < layerCount; i++)
                weights[i] /= total;

            return weights;
        }

        /// <summary>
        /// Midpoint-rule integration of an unnormalized Beta pdf over [z0,z1].
        /// </summary>
        private static double IntegrateBetaLad(double z0, double z1, double alpha, double beta, int samples)
        {
            if (z1 <= z0)
                return 0.0;

            int n = Math.Max(2, samples);
            double dz = (z1 - z0) / n;
            double sum = 0.0;
            const double eps = 1e-9;

            for (int j = 0; j < n; j++)
            {
                double z = z0 + (j + 0.5) * dz;
                z = Math.Max(eps, Math.Min(1.0 - eps, z));
                sum += Math.Pow(z, alpha - 1.0) * Math.Pow(1.0 - z, beta - 1.0);
            }

            return sum * dz;
        }
    }
}
