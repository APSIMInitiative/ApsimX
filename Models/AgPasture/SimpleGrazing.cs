using System;
using System.Linq;
using System.Collections.Generic;
using Models.Core;
using Models.Soils;
using Models.Surface;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.ForageDigestibility;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;
using APSIM.Numerics;
using APSIM.Core;


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
    public class SimpleGrazing : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        [Link] IClock clock = null;
        [Link] ISummary summary = null;
        [Link] Forages forages = null;
        [Link(IsOptional = true)] SimpleCow simpleCow = null;

        /// <summary>Gets today's minimum rotation length (days)</summary>
        private double MinimumRotationLengthForToday =>
            GetValueFromMonthlyArray(clock.Today.Month - 1, MinimumRotationLengthArray);

        /// <summary>Gets today's maximum rotation length (days)</summary>
        private double MaximumRotationLengthForToday =>
            GetValueFromMonthlyArray(clock.Today.Month - 1, MaximumRotationLengthArray);

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
        [Display(VisibleCallback = nameof(IsSimpleGrazingTurnedOn))]
        public double SimpleMinGrazable { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = nameof(IsSimpleGrazingTurnedOn))]
        public double SimpleGrazingResidual { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Target Mass' - all values by month from January")]
        [Description("Target mass of pasture to trigger grazing event (single value or 12 monthly values) (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = nameof(IsTargetMassTurnedOn))]
        public double[] PreGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Residual mass of pasture post grazing (single value or 12 monthly values) (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = nameof(IsTargetMassTurnedOn))]
        public double[] PostGrazeDMArray { get; set; }

        /// <summary></summary>
        [Separator("Settings for flexible grazing")]
        [Description("Expression for timing of grazing (e.g. AGPRyegrass.CoverTotal > 0.95)")]
        [Display(VisibleCallback = nameof(IsFlexibleGrazingTurnedOn))]
        public string FlexibleExpressionForTimingOfGrazing { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = nameof(IsFlexibleGrazingTurnedOn))]
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
        [Display(VisibleCallback = nameof(IsNotTimingControlledElsewhere))]
        public string NoGrazingStartString { get; set; }

        /// <summary></summary>
        [Description("End of the no-grazing window (dd-mmm)")]
        [Display(VisibleCallback = nameof(IsNotTimingControlledElsewhere))]
        public string NoGrazingEndString { get; set; }

        /// <summary></summary>
        [Separator("Cut and carry")]
        [Description("Is this a cut and carry system with clippings returned?")]
        public bool IsCutAndCarry { get; set; }

        /// <summary></summary>
        [Description("Fraction of clippings returned")]
        [Display(VisibleCallback = nameof(IsCutAndCarry))]
        public double FractionClippingsReturned { get; set; }

        /// <summary></summary>
        [Separator("Urine and Dung - if SimpleCow is in the simulation the first two parameters will be ignored")]
        [Description("Fraction of intake N exported as animal product (0-1). Single value or montly values.")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public double[] FractionIntakeNToAnimal { get; set; } = new double[] { 0 };

        /// <summary></summary>
        [Description("N concentration in the dung (g N / 100 g DM)")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public double DungNConc { get; set; } = 2.6;

        /// <summary>Fraction of defoliated Biomass going to soil</summary>
        [Description("Fraction of dung/urine exported off paddock e.g. to lanes/camps (0-1). Single value or montly values.")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public double[] FractionOfDungUrineOffPaddock { get; set; } = new double[] { 0 };

        /// <summary></summary>
        [Description("Depth that urine is added (mm)")]
        [Units("mm")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public double DepthUrineIsAdded { get; set; }

        /// <summary></summary>
        [Description("Advanced excreta options")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public bool ShowAdvancedExcretaOptions { get; set; }

        /// <summary></summary>
        [Description("Send some fraction of the calculated dung off-paddock - usually this should be zero (0-1)")]
        [Units("0-1")]
        [Display(VisibleCallback = nameof(IsDungUrineAdvancedOn))]
        public double SendDungElsewhere { get; set; }

        /// <summary></summary>
        [Description("Send some fraction of the  calculated urine off-paddock - usually this should be zero (0-1)")]
        [Units("0-1")]
        [Display(VisibleCallback = nameof(IsDungUrineAdvancedOn))]
        public double SendUrineElsewhere { get; set; }

        // Patching variables.
        /// <summary>
        /// Use patching for nutrient returns?
        /// </summary>
        [Separator("Patching options and parameters")]
        [Description("Use patching to return excreta to the soil?")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public bool UsePatching { get; set; }

        /// <summary>Create pseudo patches?</summary>
        [Description("Should this simulation create pseudo patches? If not then explict zones (slow!) will be created")]
        [Display(VisibleCallback = nameof(UsePatching))]
        public bool PseudoPatches { get; set; } = true;

        /// <summary>Number of patches or zones to create.</summary>
        [Description("How many patches or zones should be created?")]
        [Display(VisibleCallback = nameof(UsePatching))]
        public int ZoneCount { get; set; } = 25;

        /// <summary>Urine return pattern.</summary>
        [Description("Pattern (spatial) of nutrient return")]
        [Display(VisibleCallback = nameof(UsePatching))]
        public UrineReturnPatterns UrineReturnPattern { get; set; } = UrineReturnPatterns.PseudoRandom;

        /// <summary>Seed to use for pseudo random number generator.</summary>
        [Description("Seed to use for pseudo random number generator")]
        [Display(VisibleCallback = nameof(UsePatching))]
        public int PseudoRandomSeed { get; set; } = 666;

        // End patching variables.

        /// <summary></summary>
        [Separator("Plant population modifier")]
        [Description("Enter the fraction of population decline due to defoliation (0-1):")]
        public double FractionPopulationDecline { get; set; }

        /// <summary> </summary>
        [Separator("Trampling")]
        [Description("Turn trampling on?")]
        [Display(VisibleCallback = nameof(IsDungUrineReturnOn))]
        public bool TramplingOn { get; set; }

        /// <summary> </summary>
        [Description("Maximum proportion of litter moved to the soil")]
        [Display(VisibleCallback = nameof(TramplingOn))]
        public double MaximumPropLitterMovedToSoil { get; set; } = 0.1;

        /// <summary> </summary>
        [Description("Pasture removed at the maximum rate (e.g. 900 for heavy cattle, 1200 for ewes)")]
        [Display(VisibleCallback = nameof(TramplingOn))]
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

        /// <summary>
        /// Is maximum rotation length input array enabled in the GUI?
        /// </summary>
        public bool IsMaximumRotationLengthArrayTurnedOn => IsTargetMassTurnedOn || IsFlexibleGrazingTurnedOn;

        /// <summary>
        /// Is minimum rotation length array enabled in the GUI?
        /// </summary>
        public bool IsMinimumRotationLengthArrayTurnedOn => IsTargetMassTurnedOn || IsFlexibleGrazingTurnedOn;

        /// <summary>Show dung and urine return parameters?</summary>
        public bool IsDungUrineReturnOn => !IsCutAndCarry;

        /// <summary>Show dung and urine return advanced parameters?</summary>
        public bool IsDungUrineAdvancedOn => IsDungUrineReturnOn && ShowAdvancedExcretaOptions;

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
        public double AmountUrineNReturned { get; private set; }

        /// <summary>C in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double AmountDungWtReturned => zones.Sum(z => z.AmountDungWtReturned);

        /// <summary>N in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountDungNReturned { get; private set; }

        /// <summary>Mass of clippings returned to soil surface (kg/ha).</summary>
        public double ClippingsWtReturned { get; private set; }

        /// <summary>N in clippings returned to soil surface )(kg N/ha).</summary>
        public double ClippingsNReturned { get; private set; }

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

        ////////////// Methods //////////////

        /// <summary>
        /// Invoked by the infrastructure before the simulation gets created in memory.
        /// Use this to create patches.
        /// </summary>
        public override void OnPreLink()
        {
            if (UsePatching)
            {
                urineDungPatches = new UrineDungPatches(this, Structure, PseudoPatches, ZoneCount, urineReturnType,
                                                        UrineReturnPattern, PseudoRandomSeed, DepthUrineIsAdded, maxEffectiveNConcentration);
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
            else if (UsePatching && !PseudoPatches)
                throw new Exception("To use the explicit patching mechanism, SimpleGrazing must be at the top level of the simulation.");

            double areaOfAllZones = forages.ModelsWithDigestibleBiomass.Select(f => f.Zone)
                                                                       .Distinct()
                                                                       .Sum(z => z.Area);

            // For normal simulation there can be 1 or more zones, set up by user.
            // For explicit patches there is 1 zone set up by user and cloned by patching mechanism n times.
            // For pseudo patches only 1 zone.
            zones = forages.ModelsWithDigestibleBiomass.GroupBy(f => f.Zone,
                                                                f => f,
                                                                (z, f) => new ZoneWithForage(this, z, Structure, f.ToList(), areaOfAllZones, summary, urineDungPatches, simpleCow))
                                                       .ToList();

            if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
            {
                if (PreGrazeDMArray == null || (PreGrazeDMArray.Length != 1 && PreGrazeDMArray.Length != 12))
                    throw new Exception("There must be either a single value or monthly values specified for 'target mass of pasture to trigger grazing'");
                if (PostGrazeDMArray == null || (PostGrazeDMArray.Length != 1 && PostGrazeDMArray.Length != 12))
                    throw new Exception("There must be either a single value or monthly values specified for 'residual mass of pasture post grazing'");
            }
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
            {
                if (string.IsNullOrEmpty(FlexibleExpressionForTimingOfGrazing))
                    throw new Exception("You must specify an expression for timing of grazing.");
                if (CSharpExpressionFunction.Compile(FlexibleExpressionForTimingOfGrazing, Node, out IBooleanFunction f, out string errors))
                    expressionFunction = f;
                else
                    throw new Exception(errors);
            }

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

            if (FractionIntakeNToAnimal == null || FractionIntakeNToAnimal.Length == 0)
                FractionIntakeNToAnimal = new double[] { 1 };

            if (FractionOfDungUrineOffPaddock == null || FractionOfDungUrineOffPaddock.Length == 0)
                FractionOfDungUrineOffPaddock = new double[] { 0 };

            // Initialise the days since grazing.
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
            {
                DaysSinceGraze = simpleGrazingFrequency;
            }
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass
                  || GrazingRotationType == GrazingRotationTypeEnum.Flexible)
            {
                if (MinimumRotationLengthArray == null || MinimumRotationLengthArray.Length == 0)
                    MinimumRotationLengthArray = new double[] { 0 };  // or your default

                if (MaximumRotationLengthArray == null || MaximumRotationLengthArray.Length == 0)
                    MaximumRotationLengthArray = new double[] { double.MaxValue };


                DaysSinceGraze = Convert.ToInt32(
                GetValueFromMonthlyArray(clock.Today.Month - 1, MinimumRotationLengthArray)
              );
            }

            urineDungPatches?.OnStartOfSimulation(Structure);
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DaysSinceGraze += 1;
            ProportionOfTotalDM = new double[zones.First().NumForages];
            PostGrazeDM = 0;
            ClippingsWtReturned = 0;
            ClippingsNReturned = 0;
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

            if (IsDungUrineReturnOn)
                DoUrineDungTrampling();
            else
            {
                // cut and carry system.
                ClippingsWtReturned = GrazedDM * FractionClippingsReturned;
                ClippingsNReturned = GrazedN * FractionClippingsReturned;
                foreach (var zone in zones)
                    zone.AddResidueToSoilSurface(ClippingsWtReturned, ClippingsNReturned, "grass");
                summary.WriteMessage(this, $"The amount of plant DM added to the soil surface was {ClippingsWtReturned} and the amount of N added was {ClippingsNReturned}", MessageType.Diagnostic);
            }

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
                zones.First().DoUrineDungTrampling(clock.Today.Month);  // Assumes all zones are harvested the same.

                AmountUrineNReturned = zones.First().AmountUrineNReturned;
                AmountDungNReturned = zones.First().AmountDungNReturned;
            }
            else
            {
                foreach (var zone in zones)
                    zone.DoUrineDungTrampling(clock.Today.Month);
                AmountUrineNReturned = zones.Sum(z => z.AmountUrineNReturned);
                AmountDungNReturned = zones.Sum(z => z.AmountDungNReturned);
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

            if (DaysSinceGraze < MinimumRotationLengthForToday)
                return false;

            if (DaysSinceGraze > MaximumRotationLengthForToday)
                return true;

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
            if (array == null || array.Length == 0)
                throw new ArgumentException("Monthly array is null or empty.");

            if (array.Length == 1)
                return array[0];

            if (array.Length != 12)
                throw new ArgumentException("Monthly array must have either 1 or 12 elements.");

            if (monthIndex < 0 || monthIndex > 11)
                throw new ArgumentOutOfRangeException(nameof(monthIndex), "Month index must be between 0 (Jan) and 11 (Dec).");

            return array[monthIndex];
        }

        /// <summary>Calculate whether a target mass and length rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool FlexibleTiming()
        {
            residualBiomass = FlexibleGrazePostDM;

            // if the user left the min/max boxes blank,
            // treat them as 0 and infinity respectively:
            double min = (MinimumRotationLengthArray?.Length > 0)
                         ? MinimumRotationLengthForToday
                         : 0.0;
            double max = (MaximumRotationLengthArray?.Length > 0)
                         ? MaximumRotationLengthForToday
                         : double.MaxValue;

            // don’t graze if days since last grazing is < minimum
            if (DaysSinceGraze < min)
                return false;

            // do graze if days since last grazing is > maximum
            if (DaysSinceGraze > max)
                return true;

            // otherwise defer to your expression
            return expressionFunction.Value();
        }

        private class ZoneWithForage
        {
            private SimpleGrazing simpleGrazing;
            private IEnumerable<SurfaceOrganicMatter> surfaceOrganicMatters;
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
            UrineDungPatches urineDungPatches;
            SimpleCow simpleCow;

            /// <summary>onstructor</summary>
            /// <param name="simpleGrazing">Parent simplegrazing model.</param>
            /// <param name="zone">Our zone.</param>
            /// <param name="structure">Scope instance</param>
            /// <param name="forages">Our forages.</param>
            /// <param name="areaOfAllZones">The area of all zones in the simulation.</param>
            /// <param name="summary">The Summary file.</param>
            /// <param name="urineDungPatches">An instance for urine / dung return for patching. Can be null.</param>
            /// <param name="simpleCow">Optional simpleCow instance</param>
            public ZoneWithForage(SimpleGrazing simpleGrazing, Zone zone, IStructure structure, List<ModelWithDigestibleBiomass> forages, double areaOfAllZones,
                                  ISummary summary, UrineDungPatches urineDungPatches,
                                  SimpleCow simpleCow)
            {
                this.simpleGrazing = simpleGrazing;
                this.Zone = zone;
                this.forages = forages;
                this.urineDungPatches = urineDungPatches;
                this.simpleCow = simpleCow;
                urea = structure.Find<Solute>("Urea", relativeTo: zone);
                physical = structure.Find<IPhysical>(relativeTo: zone);
                areaWeighting = zone.Area / areaOfAllZones;
                this.summary = summary;

                if (urineDungPatches == null)
                {
                    // No patching - dung/trampling only goes to one surface organic matter model
                    surfaceOrganicMatters = [structure.Find<SurfaceOrganicMatter>(relativeTo: zone)];
                }
                else
                {
                    // Patching - dung/tranpling goes to all surface organic matter models in scope.
                    surfaceOrganicMatters = structure.FindAll<SurfaceOrganicMatter>(relativeTo: simpleGrazing);
                }
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
            public void DoUrineDungTrampling(int month)
            {
                // Calculate the dung wt.
                double dungWt = 0;
                double urineN = 0;
                double intakeN = 0;
                foreach (var grazedForage in grazedForages)
                {
                    intakeN += grazedForage.N;
                    dungWt += (1 - grazedForage.Digestibility) * grazedForage.Wt;
                }

                // DungNConc defaults to 2.6 g N / 100 g DM
                double dungN = dungWt * simpleGrazing.DungNConc / 100; // conversion from g N / 100 g DM to fraction
                dungN = Math.Min(dungN, 0.9 * intakeN);
                urineN = (intakeN - dungN) * (1 - MonthLookup(simpleGrazing.FractionIntakeNToAnimal, month));

                // Calculate the urine and dung N deposition to soil.
                // If SimpleCow is in the simulation then call it to get urine and dung N return
                if (simpleCow != null)
                {
                    // SIMPLECOW is in simulation. It calculates urine and dung N.
                    var (urineNSimpleCow, dungNSimpleCow) = simpleCow.OnGrazed(GrazedDM, GrazedME, GrazedN);
                    urineN = urineNSimpleCow;
                    dungN = dungNSimpleCow;
                }

                // Apply fraction of dung and urine to lanes, gateways etc.
                double fractionOfDungUrineOffPaddock = 1 - MonthLookup(simpleGrazing.FractionOfDungUrineOffPaddock, month);
                dungN *= fractionOfDungUrineOffPaddock;
                urineN *= fractionOfDungUrineOffPaddock;

                // Apply the fractions urine/dung leaving the system.
                dungWt *= 1.0 - simpleGrazing.SendDungElsewhere;
                dungN *= 1.0 - simpleGrazing.SendDungElsewhere;
                urineN *= 1.0 - simpleGrazing.SendUrineElsewhere;

                // Update reporting variables.
                amountDungNReturned += dungN;
                amountDungWtReturned += dungWt;
                amountUrineNReturned += urineN;

                // Perform urine deposition
                if (urineN > 0)
                {
                    if (urineDungPatches == null)
                    {
                        // NO PATCHING
                        // Add urine to the urea solute.
                        if (urineN > 0)
                        {
                            double[] ProportionOfCumThickness = SoilUtilities.ProportionOfCumThickness(physical.Thickness, simpleGrazing.DepthUrineIsAdded);
                            var ureaDelta = new double[physical.Thickness.Length];
                            for (int i = 0; i < physical.Thickness.Length; i++)
                                ureaDelta[i] = urineN * ProportionOfCumThickness[i];
                            urea.AddKgHaDelta(SoluteSetterType.Fertiliser, ureaDelta);
                        }
                    }
                    else
                    {
                        // PATCHING.
                        urineDungPatches.DoUrineReturn(amountUrineNReturned);
                    }
                }

                // Perform trampling.
                if (simpleGrazing.TramplingOn)
                {
                    // If urine patches is turned on, a fraction of residues will happen on all surface organic models in scope,
                    // otherwise it will only go a single zone.
                    var proportionLitterMovedToSoil = Math.Min(MathUtilities.Divide(simpleGrazing.PastureConsumedAtMaximumRateOfLitterRemoval, dmRemovedToday, 0),
                                                               simpleGrazing.MaximumPropLitterMovedToSoil);
                    foreach (var surfaceOrganicMatter in surfaceOrganicMatters)
                    {
                        surfaceOrganicMatter.Incorporate(proportionLitterMovedToSoil, depth: 100);
                        summary.WriteMessage(simpleGrazing, $"For {simpleGrazing.Parent.Name}, the amount of litter trampled was {proportionLitterMovedToSoil} and the remaining litter is {surfaceOrganicMatter.Wt}", MessageType.Diagnostic);
                    }
                }

                // Perform dung deposition.
                if (dungWt > 0)
                {
                    AddResidueToSoilSurface(dungWt, dungN, "RuminantDung_PastureFed");
                    summary.WriteMessage(simpleGrazing, $"For {simpleGrazing.Parent.Name}, the amount of dung DM added to the litter was {dungWt} and the amount of N added in the dung was {dungN}", MessageType.Diagnostic);
                }

                summary.WriteMessage(this.Zone, $"Urine N added to the soil of {urineN} to a depth of {simpleGrazing.DepthUrineIsAdded} mm", MessageType.Diagnostic);
                summary.WriteMessage(this.Zone, $"Dung N and C added to the surface organic matter {dungN}", MessageType.Diagnostic);
            }

            public void AddResidueToSoilSurface(double mass, double n, string residueType)
            {
                // If urine patches is turned on, dung will go to all surface organic models in scope,
                // otherwise dung will only go a single zone.
                foreach (var surfaceOrganicMatter in surfaceOrganicMatters)
                    surfaceOrganicMatter.Add(mass, n, 0, residueType, null);
            }
        }


        /// <summary>Return a value from an array that can have either 1 yearly value or 12 monthly values.</summary>
        private static double MonthLookup(double[] arr, int month)
        {
            if (arr == null)
                return double.NaN;
            else if (arr.Length == 1)
                return arr[0];
            else
                return arr[month - 1];
        }
    }
}
