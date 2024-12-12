using System;
using System.Linq;
using System.Collections.Generic;
using Models.PMF;
using Models.Core;
using Models.Soils;
using Models.Surface;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.ForageDigestibility;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Soils.NutrientPatching;
using Models.Core.ApsimFile;
using MathNet.Numerics;

namespace Models.AgPasture
{

    /// <summary>
    /// A model for cutting pasture / plants and calculating and returning excreta to the
    /// soil based on the biomass cut. If this model is put at the top level of the simulation
    /// all child zones (paddocks) are treated uniformly (e.g. for urine patch modelling)
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Simulation))]
    public class SimpleGrazing : Model
    {
        [Link] IClock clock = null;
        [Link] ISummary summary = null;
        [Link] Forages forages = null;
        [Link] ScriptCompiler compiler = null;

        private double residualBiomass;
        private IBooleanFunction expressionFunction;
        private int simpleGrazingFrequency;
        private List<ZoneWithForage> zones;
        private UrineDungPatches urineDungPatches;
        private readonly UrineReturnTypes urineReturnType = UrineReturnTypes.FromHarvest;
        private double[] speciesCutProportions { get; set; }


        /// <summary>Average potential ME concentration in herbage material (MJ/kg)</summary>
        private const double potentialMEOfHerbage = 16.0;

        private const double maxEffectiveNConcentration = 3;

        /// <summary>Grazing rotation type enum for drop down.</summary>
        public enum GrazingRotationTypeEnum
        {
            /// <summary>A simple rotation.</summary>
            SimpleRotation,

            /// <summary>A rotation based on a target mass.</summary>
            TargetMass,

            /// <summary>Timing of grazing is controlled elsewhere.</summary>
            TimingControlledElsewhere,

            /// <summary>Flexible grazing using an expression.</summary>
            Flexible
        }

        /// <summary>class for encapsulating a urine return.</summary>
        public class UrineDungReturnType : EventArgs
        {
            /// <summary>Grazed dry matter (kg/ha)</summary>
            public double GrazedDM { get; set; }

            /// <summary>N in grazed dry matter (kg/ha).</summary>
            public double GrazedN { get; set; }

            /// <summary>Metabolisable energy in grazed dry matter.</summary>
            public double GrazedME { get; set; }
        }

        /// <summary>Urine return patterns.</summary>
        public enum UrineReturnPatterns
        {
            /// <summary>Rotating in order</summary>
            RotatingInOrder,
            /// <summary>Not enabled Random</summary>
            Random,
            /// <summary>Not enabled Pseudo-random</summary>
            PseudoRandom
        }

        /// <summary>Urine return types.</summary>
        public enum UrineReturnTypes
        {
            /// <summary>FromHarvest</summary>
            FromHarvest,
            /// <summary>SetMonthly</summary>
            SetMonthly
        }

        /// <summary>Invoked when a grazing occurs.</summary>
        public event EventHandler Grazed;

        ////////////// GUI parameters shown to user //////////////

        /// <summary>Use a strict rotation, a target pasture mass, or both?</summary>
        [Separator("Grazing parameters")]
        [Description("Use a simple rotation, a target pasture mass, or both?")]
        [Units("-")]
        public GrazingRotationTypeEnum GrazingRotationType { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Simple Rotation'")]
        [Description("Frequency of grazing (days) or \"end of month\"")]
        [Units("days")]
        [Display(VisibleCallback = nameof(IsSimpleGrazingTurnedOn))]
        public string SimpleGrazingFrequencyString { get; set; }

        /// <summary></summary>
        [Description("Minimum grazeable dry matter to trigger grazing (kgDM/ha). Set to zero to turn off.")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleMinGrazable { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleGrazingResidual { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Target Mass' - all values by month from January")]
        [Description("Target mass of pasture to trigger grazing event (single value or 12 monthly values) (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsTargetMassTurnedOn")]
        public double[] PreGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Residual mass of pasture post grazing (single value or 12 monthly values) (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsTargetMassTurnedOn")]
        public double[] PostGrazeDMArray { get; set; }

        /// <summary></summary>
        [Separator("Settings for flexible grazing")]
        [Description("Expression for timing of grazing (e.g. AGPRyegrass.CoverTotal > 0.95)")]
        [Display(VisibleCallback = "IsFlexibleGrazingTurnedOn")]
        public string FlexibleExpressionForTimingOfGrazing { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsFlexibleGrazingTurnedOn")]
        public double FlexibleGrazePostDM { get; set; }

        /// <summary></summary>
        [Separator("Optional rules for rotation length")]
        [Description("Monthly maximum rotation length (days)")]
        [Units("days")]
        [Display(VisibleCallback = nameof(IsMaximumRotationLengthArrayTurnedOn))]
        public double[] MaximumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Description("Monthly minimum rotation length (days)")]
        [Units("days")]
        [Display(VisibleCallback = nameof(IsMinimumRotationLengthArrayTurnedOn))]
        public double[] MinimumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Separator("Optional no-grazing window")]
        [Description("Start of the no-grazing window (dd-mmm)")]
        [Display(VisibleCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingStartString { get; set; }

        /// <summary></summary>
        [Description("End of the no-grazing window (dd-mmm)")]
        [Display(VisibleCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingEndString { get; set; }

        /// <summary></summary>
       // [Separator("Urine and Dung.")]

        // [Description("Use patching to return excreta to the soil?")]
        // public bool UsePatching { get; set; }
        [JsonIgnore] public bool UsePatching = false;

        /// <summary>Fraction of defoliated Biomass going to soil</summary>
        [Description("Fraction of defoliated Biomass going to soil. Remainder is exported as animal product or to lanes/camps (0-1).")]
        [Display(VisibleCallback = "DontUsePatching")]
        public double[] FractionDefoliatedBiomassToSoil { get; set; } = new double[] { 1 };

        /// <summary></summary>
        [Description("Fraction of defoliated N going to soil. Remainder is exported as animal product or to lanes/camps (0-1).")]
        [Display(VisibleCallback = "DontUsePatching")]
        public double[] FractionDefoliatedNToSoil { get; set; }

        /// <summary></summary>
        [Description("Proportion of excreted N going to dung (0-1). Yearly or 12 monthly values. Blank means use C:N ratio of dung.")]
        [Display(VisibleCallback = "IsFractionExcretedNToDungEnabled")]
        public double[] FractionExcretedNToDung { get; set; }

        /// <summary></summary>
        [Description("C:N ratio of biomass for dung. If set to zero it will calculate the C:N using digestibility. ")]
        [Display(VisibleCallback = "IsCNRatioDungEnabled")]
        public double CNRatioDung { get; set; }

        /// <summary></summary>
        [Description("Depth that urine is added (mm)")]
        [Units("mm")]
        [Display(VisibleCallback = "DontUsePatching")]
        public double DepthUrineIsAdded { get; set; }

        /// <summary></summary>
        [Description("Send some fraction of the calculated dung off-paddock - usually this should be zero (0-1)")]
        [Units("0-1")]
        public double SendDungElsewhere { get; set; }

        /// <summary></summary>
        [Description("Send some fraction of the  calculated urine off-paddock - usually this should be zero (0-1)")]
        [Units("0-1")]
        public double SendUrineElsewhere { get; set; }

        // Patching variables.

        /// <summary>Create pseudo patches?</summary>
        [Description("Should this simulation create pseudo patches? If not then explict zones (slow!) will be created")]
        [Display(VisibleCallback = "UsePatching")]
        public bool PseudoPatches { get; set; }

        /// <summary>Number of patches or zones to create.</summary>
        [Description("How many patches or zones should be created?")]
        [Display(VisibleCallback = "UsePatching")]
        public int ZoneCount { get; set; } = 20;

        /// <summary>Urine return pattern.</summary>
        [Description("Pattern (spatial) of nutrient return")]
        [Display(VisibleCallback = "UsePatching")]
        public UrineReturnPatterns UrineReturnPattern { get; set; }

        /// <summary>Seed to use for pseudo random number generator.</summary>
        [Description("Seed to use for pseudo random number generator")]
        [Display(VisibleCallback = "IsPseudoRandom")]
        public int PseudoRandomSeed { get; set; }

        /// <summary>Depth of urine penetration (mm)</summary>
        [Description("Depth of urine penetration (mm)")]
        [Display(VisibleCallback = "UsePatching")]
        public double UrineDepthPenetration { get; set; }

        // End patching variables.

        /// <summary></summary>
        [Separator("Plant population modifier")]
        [Description("Enter the fraction of population decline due to defoliation (0-1):")]
        public double FractionPopulationDecline { get; set; }

        /// <summary> </summary>
        [Separator("Trampling")]
        [Description("Turn trampling on?")]
        public bool TramplingOn { get; set; }

        /// <summary> </summary>
        [Description("Maximum proportion of litter moved to the soil")]
        [Display(VisibleCallback = "IsTramplingTurnedOn")]
        public double MaximumPropLitterMovedToSoil { get; set; } = 0.1;

        /// <summary> </summary>
        [Description("Pasture removed at the maximum rate (e.g. 900 for heavy cattle, 1200 for ewes)")]
        [Display(VisibleCallback = "IsTramplingTurnedOn")]
        public double PastureConsumedAtMaximumRateOfLitterRemoval { get; set; } = 1200;

        ////////////// Callbacks to enable/disable GUI parameters //////////////

        /// <summary></summary>
        public bool IsSimpleGrazingTurnedOn => GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation;

        /// <summary></summary>
        public bool IsTargetMassTurnedOn => GrazingRotationType == GrazingRotationTypeEnum.TargetMass;

        /// <summary></summary>
        public bool IsNotTimingControlledElsewhere => GrazingRotationType != GrazingRotationTypeEnum.TimingControlledElsewhere;

        /// <summary></summary>
        public bool IsFlexibleGrazingTurnedOn => GrazingRotationType == GrazingRotationTypeEnum.Flexible;

        /// <summary>Is CN ratio dung enable?</summary>
        public bool IsCNRatioDungEnabled => DontUsePatching && FractionExcretedNToDung == null;

        /// <summary>Is pseudo random return pattern selected?</summary>
        public bool IsPseudoRandom => UrineReturnPattern == UrineReturnPatterns.PseudoRandom;

        /// <summary>Is fraction ExcretedN to dung enabled?</summary>
        public bool IsFractionExcretedNToDungEnabled => DontUsePatching && (double.IsNaN(CNRatioDung) || CNRatioDung == 0);

        /// <summary>Return true if don't use patching for excreta return.</summary>
        public bool DontUsePatching => !UsePatching;

        /// <summary>
        /// Is maximum rotation length input array enabled in the GUI?
        /// </summary>
        public bool IsMaximumRotationLengthArrayTurnedOn => IsTargetMassTurnedOn || IsFlexibleGrazingTurnedOn;

        /// <summary>
        /// Is minimum rotation length array enabled in the GUI?
        /// </summary>
        public bool IsMinimumRotationLengthArrayTurnedOn => IsTargetMassTurnedOn || IsFlexibleGrazingTurnedOn;

        /// <summary></summary>
        public bool IsTramplingTurnedOn { get { return TramplingOn; } }

        ////////////// Outputs //////////////

        /// <summary>Number of days since grazing.</summary>
        [JsonIgnore]
        public int DaysSinceGraze { get; private set; }

        /// <summary></summary>
        [JsonIgnore]
        public int GrazingInterval { get; private set; }

        /// <summary>DM grazed</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double GrazedDM => zones.Sum(z => z.GrazedDM);

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double GrazedN => zones.Sum(z => z.GrazedN);

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("MJME/ha")]
        public double GrazedME => zones.Sum(z => z.GrazedME);

        /// <summary>N in urine returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountUrineNReturned => urineDungPatches == null ? zones.Sum(z => z.AmountUrineNReturned) : urineDungPatches.AmountUrineNReturned;

        /// <summary>C in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double AmountDungWtReturned => zones.Sum(z => z.AmountDungWtReturned);

        /// <summary>N in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountDungNReturned => urineDungPatches == null ? zones.Sum(z => z.AmountDungNReturned) : urineDungPatches.AmountDungNReturned;

        /// <summary>Mass of herbage just before grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PreGrazeDM { get; private set; }

        /// <summary>Mass of harvestable herbage just before grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PreGrazeHarvestableDM { get; private set; }

        /// <summary>Mass of herbage just after grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PostGrazeDM { get; private set; }

        /// <summary>Proportion of each species biomass to the total biomass.</summary>
        [JsonIgnore]
        [Units("0-1")]
        public double[] ProportionOfTotalDM { get; private set; }

        /// <summary>Did grazing happen today?</summary>
        [JsonIgnore]
        [Units("0-1")]
        public bool GrazedToday { get; private set; }

        // Patching specific outputs.

        /// <summary>Zone or patch that urine will be applied to</summary>
        public int ZoneNumForUrine => urineDungPatches == null ? 0 : urineDungPatches.ZoneNumForUrine;

        /// <summary>Number of zones for applying urine</summary>
        public int NumZonesForUrine => urineDungPatches == null ? 0 : urineDungPatches.NumZonesForUrine;

        /// <summary>Divisor for reporting</summary>
        public double DivisorForReporting => urineDungPatches == null ? 0 : urineDungPatches.DivisorForReporting;

        /// <summary>Amount of dung carbon returned to soil for whole paddock (kg/ha)</summary>
        public double AmountDungCReturned => urineDungPatches == null ? 0 : urineDungPatches.AmountDungCReturned;

        ////////////// Methods //////////////

        /// <summary>
        /// Invoked by the infrastructure before the simulation gets created in memory.
        /// Use this to create patches.
        /// </summary>
        public override void OnPreLink()
        {
            if (UsePatching)
            {
                urineDungPatches = new UrineDungPatches(this, PseudoPatches, ZoneCount, urineReturnType,
                                                        UrineReturnPattern, PseudoRandomSeed, UrineDepthPenetration, maxEffectiveNConcentration);
                urineDungPatches.OnPreLink();
            }
        }

        /// <summary>This method is invoked at the beginning of the simulation.</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (forages == null)
                throw new Exception("No forages component found in simulation.");
            var parentZone = Parent as Zone;
            if (parentZone == null)
                summary.WriteMessage(this, "When SimpleGrazing is in the top level of the simulation (above the paddocks) it is assumed that the child paddocks are zones within a paddock.",
                                     MessageType.Information);
            double areaOfAllZones = forages.ModelsWithDigestibleBiomass.Select(f => f.Zone)
                                                                       .Distinct()
                                                                       .Sum(z => z.Area);
            zones = forages.ModelsWithDigestibleBiomass.GroupBy(f => f.Zone,
                                                                f => f,
                                                                (z, f) => new ZoneWithForage(z, f.ToList(), areaOfAllZones, summary))
                                                       .ToList();

            if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
            {
                if (PreGrazeDMArray == null || (PreGrazeDMArray.Length != 1 && PreGrazeDMArray.Length != 12))
                    throw new Exception("There must be a single value or monthly values specified for 'target mass of pasture to trigger grazing'");
                if (PostGrazeDMArray == null || (PostGrazeDMArray.Length != 1 && PostGrazeDMArray.Length != 12))
                    throw new Exception("There must be a single value or monthly values specified for 'residual mass of pasture post grazing'");
            }
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
            {
                if (string.IsNullOrEmpty(FlexibleExpressionForTimingOfGrazing))
                    throw new Exception("You must specify an expression for timing of grazing.");
                if (CSharpExpressionFunction.Compile(FlexibleExpressionForTimingOfGrazing, this, compiler, out IBooleanFunction f, out string errors))
                    expressionFunction = f;
                else
                    throw new Exception(errors);
            }

            if (FractionExcretedNToDung != null && FractionExcretedNToDung.Length != 1 && FractionExcretedNToDung.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of defoliated nitrogen going to dung' or 12 monthly values.");

            // If we are at the top level of the simulation then look in first zone for number of forages.
            int numForages;
            if (Parent is Simulation)
                numForages = zones.First().NumForages;
            else
                numForages = zones.Where(z => z.Zone == this.Parent).First().NumForages;

            speciesCutProportions = MathUtilities.CreateArrayOfValues(1.0, numForages);

            if (SimpleGrazingFrequencyString != null && SimpleGrazingFrequencyString.Equals("end of month", StringComparison.InvariantCultureIgnoreCase))
                simpleGrazingFrequency = 0;
            else
                simpleGrazingFrequency = Convert.ToInt32(SimpleGrazingFrequencyString);

            if (FractionDefoliatedNToSoil == null || FractionDefoliatedNToSoil.Length == 0)
                FractionDefoliatedNToSoil = new double[] { 1 };

            if (FractionDefoliatedBiomassToSoil == null || FractionDefoliatedBiomassToSoil.Length == 0)
                FractionDefoliatedBiomassToSoil = new double[] { 0 };

            // Initialise the days since grazing.
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                DaysSinceGraze = simpleGrazingFrequency;
            else if ((GrazingRotationType == GrazingRotationTypeEnum.TargetMass ||
                      GrazingRotationType == GrazingRotationTypeEnum.Flexible) &&
                      MinimumRotationLengthArray != null)
                DaysSinceGraze = Convert.ToInt32(MinimumRotationLengthArray[clock.Today.Month - 1]);

            urineDungPatches?.OnStartOfSimulation();
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DaysSinceGraze += 1;
            ProportionOfTotalDM = new double[zones.First().NumForages];
            PostGrazeDM = 0;

            foreach (var zone in zones)
                zone.OnStartOfDay();
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            PreGrazeDM = zones.Sum(z => z.TotalDM);
            PreGrazeHarvestableDM = zones.Sum(z => z.HarvestableDM);

            // Determine if we can graze today.
            GrazedToday = false;
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                GrazedToday = SimpleRotation();
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
                GrazedToday = TargetMass();
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
                GrazedToday = FlexibleTiming();

            if (NoGrazingStartString != null && NoGrazingStartString.Length > 0 &&
                NoGrazingEndString != null && NoGrazingEndString.Length > 0 &&
                DateUtilities.WithinDates(NoGrazingStartString, clock.Today, NoGrazingEndString))
                GrazedToday = false;

            // Perform grazing if necessary.
            if (GrazedToday)
                GrazeToResidual(residualBiomass);
        }

        /// <summary>Perform grazing.</summary>
        /// <param name="residual">The residual biomass to graze to (kg/ha).</param>
        public void GrazeToResidual(double residual)
        {
            GrazingInterval = DaysSinceGraze;  // i.e. yesterday's value
            DaysSinceGraze = 0;

            foreach (var zone in zones)
                zone.RemoveDMFromPlants(residual, speciesCutProportions);

            DoUrineDungTrampling();

            // Calculate post-grazed dry matter.
            PostGrazeDM = zones.Sum(z => z.TotalDM);

            // Calculate proportions of each species to the total biomass.
            for (int i = 0; i < zones.First().NumForages; i++)
                ProportionOfTotalDM[i] = zones.Select(z => z.ProportionsToTotal[i]).Average();

            summary.WriteMessage(this, string.Format("Grazed {0:0.0} kgDM/ha, N content {1:0.0} kgN/ha, ME {2:0.0} MJME/ha", GrazedDM, GrazedN, GrazedME), MessageType.Diagnostic);

            // Reduce plant population if necessary.
            if (MathUtilities.IsGreaterThan(FractionPopulationDecline, 0.0))
                foreach (var zone in zones)
                    zone.ReducePopulation(FractionPopulationDecline);

            // Invoke grazed event.
            Grazed?.Invoke(this, new EventArgs());
        }

        /// <summary>Add urine to the soil.</summary>
        private void DoUrineDungTrampling()
        {
            if (UsePatching)
            {
                if (Parent is Zone)
                    throw new Exception("To use patches for urine/dung return, SimpleGrazing needs to be at the top level of a simulation.");
                urineDungPatches.DoUrineDungReturn(zones.First().GrazedN);  // Assumes all zones are harvested the same.
            }
            else
            {
                foreach (var zone in zones)
                    zone.DoUrineDungTrampling(clock.Today.Month, FractionDefoliatedBiomassToSoil,
                                                FractionDefoliatedNToSoil, FractionExcretedNToDung,
                                                CNRatioDung, DepthUrineIsAdded, TramplingOn,
                                                PastureConsumedAtMaximumRateOfLitterRemoval, MaximumPropLitterMovedToSoil,
                                                SendDungElsewhere, SendUrineElsewhere);
            }
        }

        /// <summary>Calculate whether simple rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool SimpleRotation()
        {
            bool isEndOfMonth = clock.Today.AddDays(1).Day == 1;
            if ((simpleGrazingFrequency == 0 && isEndOfMonth) ||
                (DaysSinceGraze >= simpleGrazingFrequency && simpleGrazingFrequency > 0))
            {
                residualBiomass = SimpleGrazingResidual;
                if (MathUtilities.IsGreaterThan(PreGrazeHarvestableDM, SimpleMinGrazable))
                    return true;
                else
                {
                    summary.WriteMessage(this, "Defoliation will not happen because there is not enough plant material.", MessageType.Diagnostic);
                    DaysSinceGraze = 0;  //VOS why would this be zeroed if it is not grazed?
                }
            }
            return false;
        }

        /// <summary>Calculate whether a target mass rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool TargetMass()
        {
            residualBiomass = GetValueFromMonthlyArray(clock.Today.Month - 1, PostGrazeDMArray);

            // Don't graze if days since last grazing is < minimum
            if (MinimumRotationLengthArray != null && DaysSinceGraze < MinimumRotationLengthArray[clock.Today.Month - 1])
                return false;

            // Do graze if days since last grazing is > maximum
            if (MaximumRotationLengthArray != null && DaysSinceGraze > MaximumRotationLengthArray[clock.Today.Month - 1])
                return true;

            // Do graze if expression is true
            return PreGrazeDM > GetValueFromMonthlyArray(clock.Today.Month - 1, PreGrazeDMArray);
        }

        /// <summary>
        /// Helper function to return a monthly value from an array that may have
        /// number in it or 12 numbers.
        /// </summary>
        /// <param name="monthIndex">The index</param>
        /// <param name="array">The array</param>
        /// <returns></returns>
        private double GetValueFromMonthlyArray(int monthIndex, double[] array)
        {
            return array.Length == 1 ? array[0] : array[monthIndex];
        }

        /// <summary>Calculate whether a target mass and length rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool FlexibleTiming()
        {
            residualBiomass = FlexibleGrazePostDM;

            // Don't graze if days since last grazing is < minimum
            if (MinimumRotationLengthArray != null && DaysSinceGraze < MinimumRotationLengthArray[clock.Today.Month - 1])
                return false;

            // Do graze if days since last grazing is > maximum
            if (MaximumRotationLengthArray != null && DaysSinceGraze > MaximumRotationLengthArray[clock.Today.Month - 1])
                return true;

            // Do graze if expression is true
            else
                return expressionFunction.Value();
        }

        private class ZoneWithForage
        {
            private SurfaceOrganicMatter surfaceOrganicMatter;
            private Solute urea;
            private IPhysical physical;
            private List<ModelWithDigestibleBiomass> forages;
            private double grazedDM;
            private double grazedN;
            private double grazedME;
            private double amountDungNReturned;
            private double amountDungWtReturned;
            private double amountUrineNReturned;
            private double dmRemovedToday;
            private double areaWeighting;
            private List<Forages.MaterialRemoved> grazedForages = new();
            private ISummary summary;

            /// <summary>onstructor</summary>
            /// <param name="zone">Our zone.</param>
            /// <param name="forages">Our forages.</param>
            /// <param name="areaOfAllZones">The area of all zones in the simulation.</param>
            /// <param name="summary">The Summary file.</param>
            public ZoneWithForage(Zone zone, List<ModelWithDigestibleBiomass> forages, double areaOfAllZones, ISummary summary)
            {
                this.Zone = zone;
                this.forages = forages;
                surfaceOrganicMatter = zone.FindInScope<SurfaceOrganicMatter>();
                urea = zone.FindInScope<Solute>("Urea");
                physical = zone.FindInScope<IPhysical>();
                areaWeighting = zone.Area / areaOfAllZones;
                this.summary = summary;
            }

            public Zone Zone { get; private set; }

            /// <summary>The number of forages in our care</summary>
            public int NumForages => forages.Count;

            /// <summary>Dry matter of all forages in zone, weighted for area on zone (kg/ha)</summary>
            public double TotalDM => forages.Sum(f => f.Material.Sum(m => m.Total.Wt) * 10) * areaWeighting;

            /// <summary>Harvestable dry matter of all forages in zone, weighted for area on zone (kg/ha)</summary>
            public double HarvestableDM => forages.Sum(f => f.Material.Sum(m => m.Consumable.Wt) * 10) * areaWeighting;

            /// <summary>Proportions of each species within the zone to the total dm within the zone (0-1).</summary>
            public List<double> ProportionsToTotal => forages.Select(f => f.Material.Sum(m => m.Total.Wt) / TotalDM).ToList();

            /// <summary>Area weighted grazed dry matter (kg/ha)</summary>
            public double GrazedDM => grazedDM * areaWeighting;

            /// <summary>Area weighted grazed nitrogen (kg N/ha)</summary>
            public double GrazedN => grazedN * areaWeighting;

            /// <summary>Area weighted metabolisable energy in grazed dry matter (kg/ha)</summary>
            public double GrazedME => grazedME * areaWeighting;

            /// <summary>Area weighted nitrogen in dung (kg N/ha)</summary>
            public double AmountDungNReturned => amountDungNReturned * areaWeighting;

            /// <summary>Area weighted dry matter in dung (kg/ha)</summary>
            public double AmountDungWtReturned => amountDungWtReturned * areaWeighting;

            /// <summary>Area weighted nitrogen in uring (kg N/ha)</summary>
            public double AmountUrineNReturned => amountUrineNReturned * areaWeighting;

            /// <summary>
            /// Called at start of day.
            /// </summary>
            public void OnStartOfDay()
            {
                grazedDM = 0.0;
                grazedN = 0.0;
                grazedME = 0.0;
                amountDungNReturned = 0;
                amountDungWtReturned = 0;
                amountUrineNReturned = 0;
                grazedForages.Clear();
            }

            /// <summary>
            /// Reduce the forage population,
            /// </summary>
            /// <param name="fractionPopulationDecline">The fraction to reduce to population to.</param>
            public void ReducePopulation(double fractionPopulationDecline)
            {
                foreach (var forage in forages)
                {
                    if ((forage as IModel) is IHasPopulationReducer populationReducer)
                        populationReducer.ReducePopulation(populationReducer.Population * (1.0 - fractionPopulationDecline));
                    else
                        throw new Exception($"Model {forage.Name} is unable to reduce its population due to grazing. Not implemented.");
                }
            }

            /// <summary>Remove biomass from the specified forage.</summary>
            /// <param name="residual">The residual to cut to (kg/ha).</param>
            /// <param name="speciesCutProportions">The proportions to cut each species.</param>
            public void RemoveDMFromPlants(double residual, double[] speciesCutProportions)
            {
                // This is a simple implementation. It proportionally removes biomass from organs.
                // What about non harvestable biomass?
                // What about PreferenceForGreenOverDead and PreferenceForLeafOverStems?
                double preGrazeDM = forages.Sum(f => f.Material.Sum(m => m.Total.Wt * 10));
                double removeAmount = Math.Max(0, preGrazeDM - residual) / 10; // to g/m2

                dmRemovedToday = removeAmount;
                if (MathUtilities.IsGreaterThan(removeAmount, 0.0))
                {
                    // Remove a proportion of required DM from each species
                    double totalHarvestableWt = 0.0;
                    double totalWeightedHarvestableWt = 0.0;
                    for (int i = 0; i < forages.Count; i++)
                    {
                        var harvestableWt = forages[i].Material.Sum(m => m.Consumable.Wt);  // g/m2
                        totalHarvestableWt += harvestableWt;
                        totalWeightedHarvestableWt += speciesCutProportions[i] * harvestableWt;
                    }

                    // If a fraction consumable was specified in the forages component by the user then the above calculated
                    // removeAmount might be > consumable amount. Constrain the removeAmount to the consumable
                    // amount so that we don't get an exception thrown in ModelWithDigestibleBiomass.RemoveBiomass method
                    removeAmount = Math.Min(removeAmount, totalHarvestableWt);

                    for (int i = 0; i < forages.Count; i++)
                    {
                        var harvestableWt = forages[i].Material.Sum(m => m.Consumable.Wt);  // g/m2
                        var proportion = harvestableWt * speciesCutProportions[i] / totalWeightedHarvestableWt;
                        var amountToRemove = removeAmount * proportion;
                        if (MathUtilities.IsGreaterThan(amountToRemove, 0.0))
                        {
                            var grazed = forages[i].RemoveBiomass(amountToRemove: amountToRemove);
                            double grazedDigestibility = grazed.Digestibility;
                            var grazedMetabolisableEnergy = potentialMEOfHerbage * grazedDigestibility;

                            grazedDM += grazed.Wt;  // kg/ha
                            grazedN += grazed.N;    // kg/ha
                            grazedME += grazedMetabolisableEnergy * grazed.Wt;

                            grazedForages.Add(grazed);
                        }
                    }
                }
            }

            /// <summary>
            /// Perform urine and dung return and trampling.
            /// </summary>
            /// <param name="month"></param>
            /// <param name="fractionDefoliatedBiomassToSoil"></param>
            /// <param name="fractionDefoliatedNToSoil"></param>
            /// <param name="fractionExcretedNToDung"></param>
            /// <param name="CNRatioDung"></param>
            /// <param name="depthUrineIsAdded"></param>
            /// <param name="doTrampling"></param>
            /// <param name="pastureConsumedAtMaximumRateOfLitterRemoval"></param>
            /// <param name="maximumPropLitterMovedToSoil"></param>
            /// <param name="sendDungElsewhere"></param>
            /// <param name="sendUrineElsewhere"></param>
            public void DoUrineDungTrampling(int month, double[] fractionDefoliatedBiomassToSoil,
                                    double[] fractionDefoliatedNToSoil,
                                    double[] fractionExcretedNToDung,
                                    double CNRatioDung,
                                    double depthUrineIsAdded,
                                    bool doTrampling,
                                    double pastureConsumedAtMaximumRateOfLitterRemoval,
                                    double maximumPropLitterMovedToSoil,
                                    double sendDungElsewhere,
                                    double sendUrineElsewhere)
            {
                var urineDung = UrineDungReturn.CalculateUrineDungReturn(grazedForages,
                                                                         GetValueFromMonthArray(fractionDefoliatedBiomassToSoil, month),
                                                                         GetValueFromMonthArray(fractionDefoliatedNToSoil, month),
                                                                         GetValueFromMonthArray(fractionExcretedNToDung, month),
                                                                         CNRatioDung,
                                                                         sendUrineElsewhere,
                                                                         sendDungElsewhere);

                if (urineDung != null)
                {
                    amountDungNReturned += urineDung.DungNToSoil;
                    amountDungWtReturned += urineDung.DungWtToSoil;
                    amountUrineNReturned += urineDung.UrineNToSoil;

                    UrineDungReturn.DoUrineReturn(urineDung, physical.Thickness, urea, depthUrineIsAdded);
                    summary.WriteMessage(this.Zone, $"Urine N added to the soil of {urineDung.UrineNToSoil} to a depth of {depthUrineIsAdded} mm", MessageType.Diagnostic);
                    UrineDungReturn.DoDungReturn(urineDung, surfaceOrganicMatter);
                    summary.WriteMessage(this.Zone, $"Dung N and C added to the surface organic matter {urineDung.DungNToSoil}", MessageType.Diagnostic);

                    if (doTrampling)
                    {
                        var proportionLitterMovedToSoil = Math.Min(MathUtilities.Divide(pastureConsumedAtMaximumRateOfLitterRemoval, dmRemovedToday, 0),
                                                                    maximumPropLitterMovedToSoil);
                        surfaceOrganicMatter.Incorporate(proportionLitterMovedToSoil, depth: 100);
                    }
                }
            }

            /// <summary>Return a value from an array that can have either 1 yearly value or 12 monthly values.</summary>
            private static double GetValueFromMonthArray(double[] arr, int month)
            {
                if (arr == null)
                    return double.NaN;
                else if (arr.Length == 1)
                    return arr[0];
                else
                    return arr[month - 1];
            }
        }

        // **************************************************************************************
        // Patching code
        // **************************************************************************************

        /// <summary>
        /// Encapsulates urine patch functionality.
        /// </summary>
        public class UrineDungPatches
        {
            private readonly SimpleGrazing simpleGrazing;
            private readonly bool pseudoPatches;
            private double[] monthlyUrineNAmt;                 // breaks the N balance but useful for testing
            private double[] urineDepthPenetrationArray;
            private Random pseudoRandom;
            private int pseudoRandomSeed;
            private readonly ISummary summary;
            private readonly Clock clock;
            private readonly Physical physical;

            // User properties.

            /// <summary>Number of patches or zones to create.</summary>
            private readonly int zoneCount;

            /// <summary>Urine return type</summary>
            private readonly UrineReturnTypes urineReturnType;

            /// <summary>Urine return pattern.</summary>
            private readonly UrineReturnPatterns urineReturnPattern;

            /// <summary>Depth of urine penetration (mm)</summary>
            private readonly double urineDepthPenetration;

            /// <summary>Maximum effective NO3-N or NH4-N concentration</summary>
            private readonly double maxEffectiveNConcentration;

            // Outputs.

            /// <summary>Zone or patch that urine will be applied to</summary>
            public int ZoneNumForUrine { get; private set; }

            /// <summary>Number of zones for applying urine</summary>
            public int NumZonesForUrine { get; private set; }

            /// <summary>Divisor for reporting</summary>
            public double DivisorForReporting { get; private set; }

            /// <summary>Amount of urine returned to soil for whole paddock (kg/ha)</summary>
            public double AmountUrineNReturned { get; private set; }

            /// <summary>Amount of dung nitrogen returned to soil for whole paddock (kg/ha)</summary>
            public double AmountDungNReturned { get; private set; }

            /// <summary>Amount of dung carbon returned to soil for whole paddock (kg/ha)</summary>
            public double AmountDungCReturned { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="simpleGrazing">Parent SimpleGrazing model</param>
            /// <param name="pseudoPatches">Use pseudo patches?</param>
            /// <param name="zoneCount"></param>
            /// <param name="urineReturnType"></param>
            /// <param name="urineReturnPattern"></param>
            /// <param name="pseudoRandomSeed"></param>
            /// <param name="urineDepthPenetration"></param>
            /// <param name="maxEffectiveNConcentration"></param>
            public UrineDungPatches(SimpleGrazing simpleGrazing, bool pseudoPatches,
                                    int zoneCount,
                                    UrineReturnTypes urineReturnType,
                                    UrineReturnPatterns urineReturnPattern,
                                    int pseudoRandomSeed,
                                    double urineDepthPenetration,
                                    double maxEffectiveNConcentration)
            {
                this.simpleGrazing = simpleGrazing;
                this.pseudoPatches = pseudoPatches;
                this.zoneCount = zoneCount;
                this.urineReturnType = urineReturnType;
                this.urineReturnPattern = urineReturnPattern;
                this.pseudoRandomSeed = pseudoRandomSeed;
                this.urineDepthPenetration = urineDepthPenetration;
                this.maxEffectiveNConcentration = maxEffectiveNConcentration;
                summary = simpleGrazing.FindInScope<ISummary>();
                clock = simpleGrazing.FindInScope<Clock>();
                physical = simpleGrazing.FindInScope<Physical>();
            }

            /// <summary>
            /// Invoked by the infrastructure before the simulation gets created in memory.
            /// Use this to create patches.
            /// </summary>
            public void OnPreLink()
            {
                var simulation = simpleGrazing.FindAncestor<Simulation>() as Simulation;
                var zone = simulation.FindChild<Zone>();

                if (zoneCount == 0)
                    throw new Exception("Number of patches/zones in urine patches is zero.");

                if (pseudoPatches)
                {
                    zone.Area = 1.0;

                    var patchManager = simpleGrazing.FindInScope<NutrientPatchManager>();
                    if (patchManager == null)
                        throw new Exception("Cannot find NutrientPatchManager");
                    var soilPhysical = simpleGrazing.FindInScope<Physical>();
                    if (patchManager == null)
                        throw new Exception("Cannot find Physical");

                    double[] ArrayForMaxEffConc = new double[soilPhysical.Thickness.Length];
                    for (int i = 0; i <= (soilPhysical.Thickness.Length - 1); i++)
                        ArrayForMaxEffConc[i] = maxEffectiveNConcentration;

                    patchManager.MaximumNO3AvailableToPlants = ArrayForMaxEffConc;
                    patchManager.MaximumNH4AvailableToPlants = ArrayForMaxEffConc;

                    patchManager.NPartitionApproach = PartitionApproachEnum.BasedOnConcentrationAndDelta;
                    patchManager.AutoAmalgamationApproach = AutoAmalgamationApproachEnum.None;
                    patchManager.basePatchApproach = BaseApproachEnum.IDBased;
                    patchManager.AllowPatchAmalgamationByAge = false;
                    patchManager.PatchAgeForForcedMerge = 1000000.0;  // ie don't merge

                    int[] PatchToAddTo = new int[1];  //need an array variable for this
                    string[] PatchNmToAddTo = new string[1];
                    int nPatchesAdded = 0;
                    double NewArea = 1.0 / zoneCount;

                    while (nPatchesAdded < zoneCount - 1)
                    {
                        AddSoilCNPatchType NewPatch = new AddSoilCNPatchType();
                        NewPatch.DepositionType = DepositionTypeEnum.ToNewPatch;
                        NewPatch.AreaFraction = NewArea;
                        PatchToAddTo[0] = 0;
                        PatchNmToAddTo[0] = "0";
                        NewPatch.AffectedPatches_id = PatchToAddTo;
                        NewPatch.AffectedPatches_nm = PatchNmToAddTo;
                        NewPatch.SuppressMessages = false;
                        patchManager.Add(NewPatch);
                        nPatchesAdded += 1;
                    }

                }
                else //(!PseudoPatches)  // so now this is zones - possibly multiple zones
                {
                    zone.Area = 1.0 / zoneCount;  // and then this will apply to all the new zones
                    for (int i = 0; i < zoneCount - 1; i++)
                    {
                        var newZone = Apsim.Clone(zone);
                        Structure.Add(newZone, simulation);
                    }
                }
            }

            /// <summary>Invoked at start of simulation.</summary>
            public void OnStartOfSimulation()
            {
                if (!pseudoPatches)
                    summary.WriteMessage(simpleGrazing, "Created " + zoneCount + " identical zones, each of area " + (1.0 / zoneCount) + " ha", MessageType.Diagnostic);

                summary.WriteMessage(simpleGrazing, "Initialising the ZoneManager for grazing, urine return and reporting", MessageType.Diagnostic);

                pseudoRandom = new Random(pseudoRandomSeed);  // sets a constant seed value

                if (pseudoPatches)
                    DivisorForReporting = 1.0;
                else
                    DivisorForReporting = zoneCount;

                monthlyUrineNAmt = new double[] { 24, 19, 17, 12, 8, 5, 5, 10, 16, 19, 23, 25 }; //This is to get a pattern of return that varies with month but removes the variation that might be caused by small changes in herbage growth
                //MonthlyUrineNAmt = new double[] { 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25 }; //This is to get a pattern of return that varies with month but removes the variation that might be caused by small changes in herbage growth
                //MonthlyUrineNAmt = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; //This is to get a pattern of return that varies with month but removes the variation that might be caused by small changes in herbage growth

                if (pseudoPatches)
                {
                    var patchManager = simpleGrazing.FindInScope<NutrientPatchManager>();

                    //var patchManager = FindInScope<NutrientPatchManager>();
                    summary.WriteMessage(simpleGrazing, patchManager.NumPatches.ToString() + " pseudopatches have been created", MessageType.Diagnostic);
                }
                else
                {
                    var simulation = simpleGrazing.FindAncestor<Simulation>();
                    var physical = simpleGrazing.FindInScope<IPhysical>();
                    double[] arrayForMaxEffConc = Enumerable.Repeat(maxEffectiveNConcentration, physical.Thickness.Length).ToArray();
                    foreach (Zone zone in simulation.FindAllInScope<Zone>())
                    {
                        foreach (var patchManager in zone.FindAllInScope<NutrientPatchManager>())
                        {
                            patchManager.MaximumNO3AvailableToPlants = arrayForMaxEffConc;
                            patchManager.MaximumNH4AvailableToPlants = arrayForMaxEffConc;
                        }
                    }
                }

                summary.WriteMessage(simpleGrazing, "Finished initialising the Manager for grazing, urine return and reporting", MessageType.Diagnostic);

                NumZonesForUrine = 1;  // in the future this might be > 1
                ZoneNumForUrine = -1;  // this will be incremented to 0 (first zone) below

                UrinePenetration();
            }

            /// <summary>Invoked at DoManagement.</summary>
            public void DoUrineDungReturn(double harvestedN)
            {
                if (urineReturnType == UrineReturnTypes.FromHarvest)
                {
                    AmountUrineNReturned = harvestedN * 0.50;  //
                    AmountDungNReturned = harvestedN * 0.35;  //
                    AmountDungCReturned = AmountDungNReturned * 20;
                }
                else if (urineReturnType == UrineReturnTypes.SetMonthly)
                {
                    AmountUrineNReturned = monthlyUrineNAmt[clock.Today.Month - 1];   //  hardcoded as an input
                    AmountDungNReturned = AmountUrineNReturned / 0.50 * 0.35;  //
                    AmountDungCReturned = AmountDungNReturned * 20;
                }
                summary.WriteMessage(simpleGrazing, "The amount of urine N to be returned to the whole paddock is " + AmountUrineNReturned, MessageType.Diagnostic);

                DoUrineReturn();

                DoTramplingAndDungReturn();

                summary.WriteMessage(simpleGrazing, "Finished Grazing", MessageType.Diagnostic);
            }

            /// <summary>Invoked to do trampling and dung return.</summary>
            private void DoTramplingAndDungReturn()
            {
                // Note that dung is assumed to be spread uniformly over the paddock (patches or sones).
                // There is no need to bring zone area into the calculations here but zone area must be included for variables reported FROM the zone to the upper level

                int i = -1;  // patch or paddock counter
                foreach (Zone zone in simpleGrazing.FindAllInScope<Zone>())
                {
                    i += 1;
                    SurfaceOrganicMatter surfaceOM = zone.FindInScope<SurfaceOrganicMatter>() as SurfaceOrganicMatter;

                    // do some trampling of litter
                    // accelerate the movement of surface litter into the soil - do this before the dung is added
                    double temp = surfaceOM.Wt * 0.1;

                    surfaceOM.Incorporate(fraction: (double)0.1, depth: (double)100.0, doOutput: true);

                    summary.WriteMessage(simpleGrazing, "For patch " + i + " the amount of litter trampled was " + temp + " and the remaining litter is " + (surfaceOM.Wt), MessageType.Diagnostic);

                    // move the dung to litter
                    AddFaecesType dung = new()
                    {
                        OMWeight = AmountDungCReturned / 0.4,  //assume dung C is 40% of OM
                        OMN = AmountDungNReturned
                    };
                    surfaceOM.AddFaeces(dung);
                    summary.WriteMessage(simpleGrazing, "For patch " + i + " the amount of dung DM added to the litter was " + (AmountDungCReturned / 0.4) + " and the amount of N added in the dung was " + (AmountDungNReturned), MessageType.Diagnostic);

                }
            }

            /// <summary>Invoked to do urine return</summary>
            private void DoUrineReturn()
            {
                GetZoneForUrineReturn();

                summary.WriteMessage(simpleGrazing, "The Zone for urine return is " + ZoneNumForUrine, MessageType.Diagnostic);

                if (!pseudoPatches)
                {
                    Zone zone = simpleGrazing.FindAllInScope<Zone>().ToArray()[ZoneNumForUrine];
                    Fertiliser thisFert = zone.FindInScope<Fertiliser>() as Fertiliser;

                    thisFert.Apply(amount: AmountUrineNReturned * zoneCount,
                            type: Fertiliser.Types.UreaN,
                            depthTop: 0.0,
                            depthBottom: urineDepthPenetration,
                            doOutput: true);

                    summary.WriteMessage(simpleGrazing, AmountUrineNReturned + " urine N added to Zone " + ZoneNumForUrine + ", the local load was " + AmountUrineNReturned / zone.Area + " kg N /ha", MessageType.Diagnostic);
                }
                else // PseudoPatches
                {
                    int[] PatchToAddTo = new int[1];  //because need an array variable for this
                    string[] PatchNmToAddTo = new string[0];  //need an array variable for this
                    double[] UreaToAdd = new double[physical.Thickness.Length];

                    for (int ii = 0; ii <= (physical.Thickness.Length - 1); ii++)
                        UreaToAdd[ii] = urineDepthPenetrationArray[ii] * AmountUrineNReturned * zoneCount;

                    // needed??   UreaReturned += AmountFertNReturned;

                    AddSoilCNPatchType CurrentPatch = new();
                    CurrentPatch.Sender = "manager";
                    CurrentPatch.DepositionType = DepositionTypeEnum.ToSpecificPatch;
                    PatchToAddTo[0] = ZoneNumForUrine;
                    CurrentPatch.AffectedPatches_id = PatchToAddTo;
                    CurrentPatch.AffectedPatches_nm = PatchNmToAddTo;
                    CurrentPatch.Urea = UreaToAdd;

                    var patchManager = simpleGrazing.FindInScope<NutrientPatchManager>();

                    summary.WriteMessage(simpleGrazing, "Patch MinN prior to urine return: " + patchManager.MineralNEachPatch[ZoneNumForUrine], MessageType.Diagnostic);
                    patchManager.Add(CurrentPatch);
                    summary.WriteMessage(simpleGrazing, "Patch MinN after urine return: " + patchManager.MineralNEachPatch[ZoneNumForUrine], MessageType.Diagnostic);
                }
            }

            /// <summary>Determine and return the zone for urine return.</summary>
            private void GetZoneForUrineReturn()
            {
                if (urineReturnPattern == UrineReturnPatterns.RotatingInOrder)
                {
                    ZoneNumForUrine += 1;  //increment the zone number - it was initialised at -1. NOTE, ZoneNumForUrine is used for both zones and patches
                    if (ZoneNumForUrine >= zoneCount)
                        ZoneNumForUrine = 0;  // but reset back to the first patch if needed
                }
                else if (urineReturnPattern == UrineReturnPatterns.Random)
                {
                    Random rnd = new Random();
                    ZoneNumForUrine = rnd.Next(0, zoneCount); // in C# the maximum value (ZoneCount) will not be selected
                }
                else if (urineReturnPattern == UrineReturnPatterns.PseudoRandom)
                {
                    ZoneNumForUrine = pseudoRandom.Next(0, zoneCount); // in C# the maximum value (ZoneCount) will not be selected
                }
                else
                    throw new Exception("UrineResturnPattern not recognised");

                summary.WriteMessage(simpleGrazing, "The next zone/patch for urine return is " + ZoneNumForUrine, MessageType.Diagnostic);
            }

            /// <summary>Calculate the urine penetration array.</summary>
            private void UrinePenetration()
            {
                // note this assumes that all the paddocks are the same
                double tempDepth = 0.0;
                urineDepthPenetrationArray = new double[physical.Thickness.Length];
                for (int i = 0; i <= (physical.Thickness.Length - 1); i++)
                {
                    tempDepth += physical.Thickness[i];
                    if (tempDepth <= urineDepthPenetration)
                    {
                        urineDepthPenetrationArray[i] = physical.Thickness[i] / urineDepthPenetration;
                    }
                    else
                    {
                        urineDepthPenetrationArray[i] = (urineDepthPenetration - (tempDepth - physical.Thickness[i])) / (tempDepth - (tempDepth - physical.Thickness[i])) * physical.Thickness[i] / urineDepthPenetration;
                        urineDepthPenetrationArray[i] = Math.Max(0.0, Math.Min(1.0, urineDepthPenetrationArray[i]));
                    }
                    summary.WriteMessage(simpleGrazing, "The proportion of urine applied to the " + i + "th layer will be " + urineDepthPenetrationArray[i], MessageType.Diagnostic);
                }
            }
        }
    }
}
