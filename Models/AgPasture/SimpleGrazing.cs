namespace Models.AgPasture
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Models.Core;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using APSIM.Shared.Utilities;
    using Models.Interfaces;
    using Models.Soils.Nutrients;
    using Models.Surface;
    using Models.Functions;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class SimpleGrazing : Model
    {
        [Link] Clock clock = null;
        [Link] Fertiliser fertiliser = null;
        [Link] ISummary summary = null;
        [Link] List<IPlantDamage> forages = null;

        private double residualBiomass;
        private CSharpExpressionFunction expressionFunction;

        /// <summary>Average potential ME concentration in herbage material (MJ/kg)</summary>
        private const double PotentialMEOfHerbage = 16.0;

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

        /// <summary>Invoked when a grazing occurs.</summary>
        public event EventHandler Grazed;

        /// <summary>Occurs when [biomass removed].</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        ////////////// GUI parameters shown to user //////////////

        /// <summary></summary>
        [Description("Verbose mode - write many informational statements to the Summary file")]
        public bool Verbose { get; set; }

        /// <summary>Use a strict rotation, a target pasture mass, or both?</summary>
        [Separator("Grazing parameters")]
        [Description("Use a simple rotation, a target pasture mass, or both?")]
        [Units("-")]
        public GrazingRotationTypeEnum GrazingRotationType { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Simple Rotation'")]
        [Description("Frequency of grazing - 0 will be interpreted as the end of each month (days)")]
        [Units("days")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public int SimpleGrazingFrequency { get; set; }

        /// <summary></summary>
        [Description("Minimum grazeable dry matter to trigger grazing (kgDM/ha). Set to zero to turn off.")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleMinGrazable { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleGrazingResidual { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Target Mass' - all values by month from January")]
        [Description("Target mass of pasture to trigger grazing event, monthly values (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn")]
        public double[] PreGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Residual mass of pasture post grazing, monthly values (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn")]
        public double[] PostGrazeDMArray { get; set; }

        /// <summary></summary>
        [Separator("Optional rules for rotation length")]
        [Description("Monthly maximum rotation length (days)")]
        [Units("days")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn,IsFlexibleGrazingTurnedOn")]
        public double[] MaximumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Description("Monthly minimum rotation length (days)")]
        [Units("days")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn,IsFlexibleGrazingTurnedOn")]
        public double[] MinimumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Separator("Settings for flexible grazing")]
        [Description("Expression for timing of grazing (e.g. AGPRyegrass.CoverTotal > 0.95)")]
        [Display(EnabledCallback = "IsFlexibleGrazingTurnedOn")]
        public string FlexibleExpressionForTimingOfGrazing { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsFlexibleGrazingTurnedOn")]
        public double FlexibleGrazePostDM { get; set; }

        /// <summary></summary>
        [Separator("Optional no-grazing window")]
        [Description("Start of the no-grazing window (dd-mmm)")]
        [Display(EnabledCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingStartString { get; set; }

        /// <summary></summary>
        [Description("End of the no-grazing window (dd-mmm)")]
        [Display(EnabledCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingEndString { get; set; }

        /// <summary></summary>

        [Separator("Urine and Dung - The remainder will be removed from the simulation.")]

        [Description("Proportion of defoliated N going to urine (0-1). Yearly or 12 monthly values.")]
        public double[] FractionOfBiomassToUrine { get; set; }

        /// <summary></summary>
        [Description("Depth that urine is added (mm)")]
        [Units("mm")]
        public double DepthUrineIsAdded { get; set; }

        /// <summary></summary>
        [Description("Proportion of defoliated N going to dung (0-1). Yearly or 12 monthly values.")]
        public double[] FractionOfBiomassToDung { get; set; }

        /// <summary></summary>
        [Description("C:N ratio of biomass for dung. If set to zero it will calculate the C:N using digestibility. ")]
        public double CNRatioDung { get; set; }


        ////////////// Callbacks to enable/disable GUI parameters //////////////

        /// <summary></summary>
        public bool IsSimpleGrazingTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation;
            }
        }

        /// <summary></summary>
        public bool IsTargetMassTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.TargetMass;
            }
        }

        /// <summary></summary>
        public bool IsNotTimingControlledElsewhere
        {
            get
            {
                return GrazingRotationType != GrazingRotationTypeEnum.TimingControlledElsewhere;
            }
        }

        /// <summary></summary>
        public bool IsFlexibleGrazingTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.Flexible;
            }
        }

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
        public double GrazedDM { get; private set; }

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double GrazedN { get; private set; }

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("MJME/ha")]
        public double GrazedME { get; private set; }

        /// <summary>N in urine returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountUrineNReturned { get; private set; }

        /// <summary>C in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double AmountDungCReturned { get; private set; }

        /// <summary>N in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountDungNReturned { get; private set; }

        /// <summary>Mass of herbage just before grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PreGrazeDM { get; private set; }

        /// <summary>Mass of herbage just after grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PostGrazeDM { get; private set; }

        /// <summary>Proportion of each species biomass to the total biomass.</summary>
        [JsonIgnore]
        [Units("0-1")]
        public double[] ProportionOfTotalDM { get; private set; }

        ////////////// Methods //////////////

        /// <summary>This method is invoked at the beginning of the simulation.</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ProportionOfTotalDM = new double[forages.Count];

            if (Verbose)
                summary.WriteMessage(this, "Initialising the Manager for grazing, urine return and reporting");

            if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
            {
                if (PreGrazeDMArray == null || PreGrazeDMArray.Length != 12)
                    throw new Exception("There must be 12 values input for the pre-grazing DM");
                if (PostGrazeDMArray == null || PostGrazeDMArray.Length != 12)
                    throw new Exception("There must be 12 values input for the post-grazing DM");
            }
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
            {
                if (string.IsNullOrEmpty(FlexibleExpressionForTimingOfGrazing))
                    throw new Exception("You must specify an expression for timing of grazing.");
                expressionFunction = new CSharpExpressionFunction();
                expressionFunction.Parent = this;
                expressionFunction.Expression = "Convert.ToDouble(" + FlexibleExpressionForTimingOfGrazing + ")";
                expressionFunction.CompileExpression();
            }

            if (FractionOfBiomassToDung.Length != 1 && FractionOfBiomassToDung.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of biomass going to dung' or 12 monthly values.");

            if (FractionOfBiomassToUrine.Length != 1 && FractionOfBiomassToUrine.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of biomass going to urine' or 12 monthly values.");

            if (Verbose)
                summary.WriteMessage(this, "Finished initialising the Manager for grazing, urine return and reporting");
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DaysSinceGraze += 1;
            PostGrazeDM = 0.0;
            GrazedDM = 0.0;
            GrazedN = 0.0;
            GrazedME = 0.0;
            AmountDungNReturned = 0;
            AmountDungCReturned = 0;
            AmountUrineNReturned = 0;
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            // Calculate pre-grazed dry matter.
            PreGrazeDM = 0.0;
            foreach (var forage in forages)
                PreGrazeDM += forage.AboveGround.Wt;

            // Convert to kg/ha
            PreGrazeDM *= 10;

            // Determine if we can graze today.
            var grazeNow = false;
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                grazeNow = SimpleRotation();
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
                grazeNow = TargetMass();
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
                grazeNow = FlexibleTiming();

            if (NoGrazingStartString != null &&
                NoGrazingEndString != null &&
                DateUtilities.WithinDates(NoGrazingStartString, clock.Today, NoGrazingEndString))
                grazeNow = false;

            // Perform grazing if necessary.
            if (grazeNow)
                GrazeToResidual(residualBiomass);
        }

        /// <summary>Perform grazing.</summary>
        /// <param name="residual">The residual biomass to graze to (kg/ha).</param>
        public void GrazeToResidual(double residual)
        {
            var amountDMToRemove = Math.Max(0, PreGrazeDM - residual);
            Graze(amountDMToRemove);
        }

        /// <summary>Perform grazing</summary>
        /// <param name="amountDMToRemove">The amount of biomas to remove (kg/ha).</param>
        public void Graze(double amountDMToRemove)
        {
            GrazingInterval = DaysSinceGraze;  // i.e. yesterday's value
            DaysSinceGraze = 0;

            RemoveDMFromPlants(amountDMToRemove);

            AddUrineToSoil();

            AddDungToSurface();

            // Calculate post-grazed dry matter.
            PostGrazeDM = forages.Sum(forage => forage.AboveGround.Wt);

            // Calculate proportions of each species to the total biomass.
            for (int i = 0; i < forages.Count; i++)
            {
                var proportionToTotalDM = MathUtilities.Divide(forages[i].AboveGround.Wt, PostGrazeDM, 0);
                ProportionOfTotalDM[i] = proportionToTotalDM;
            }

            if (Verbose)
                summary.WriteMessage(this, string.Format("Grazed {0:0.0} kgDM/ha, N content {1:0.0} kgN/ha, ME {2:0.0} MJME/ha", GrazedDM, GrazedN, GrazedME));

            // Invoke grazed event.
            Grazed?.Invoke(this, new EventArgs());
        }

        /// <summary>Add dung to the soil surface.</summary>
        private void AddDungToSurface()
        {
            var SOMData = new BiomassRemovedType();
            SOMData.crop_type = "RuminantDung_PastureFed";
            SOMData.dm_type = new string[] { SOMData.crop_type };
            SOMData.dlt_crop_dm = new float[] { (float)AmountDungCReturned };
            SOMData.dlt_dm_n = new float[] { (float)AmountDungNReturned };
            SOMData.dlt_dm_p = new float[] { 0.0F };
            SOMData.fraction_to_residue = new float[] { 1.0F };
            BiomassRemoved.Invoke(SOMData);
        }

        /// <summary>Add urine to the soil.</summary>
        private void AddUrineToSoil()
        {
            AmountUrineNReturned = GetValueFromMonthArray(FractionOfBiomassToUrine) * GrazedN;
            fertiliser.Apply(AmountUrineNReturned, Fertiliser.Types.UreaN, DepthUrineIsAdded);
        }

        /// <summary>Return a value from an array that can have either 1 yearly value or 12 monthly values.</summary>
        private double GetValueFromMonthArray(double[] arr)
        {
            if (arr.Length == 1)
                return arr[0];
            else
                return arr[clock.Today.Month - 1];
        }

        /// <summary>Calculate whether simple rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool SimpleRotation()
        {
            bool isEndOfMonth = clock.Today.AddDays(1).Day == 1;
            if ((SimpleGrazingFrequency == 0 && isEndOfMonth) ||
                (DaysSinceGraze >= SimpleGrazingFrequency && SimpleGrazingFrequency > 0))
            {
                residualBiomass = SimpleGrazingResidual;
                return PreGrazeDM > SimpleMinGrazable;
            }
            return false;
        }

        /// <summary>Calculate whether a target mass rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool TargetMass()
        {
            residualBiomass = PostGrazeDMArray[clock.Today.Month - 1];

            // Don't graze if days since last grazing is < minimum
            if (MinimumRotationLengthArray != null && DaysSinceGraze < MinimumRotationLengthArray[clock.Today.Month - 1])
                return false;

            // Do graze if days since last grazing is > maximum
            if (MaximumRotationLengthArray != null && DaysSinceGraze > MaximumRotationLengthArray[clock.Today.Month - 1])
                return true;

            // Do graze if expression is true
            else
                return PreGrazeDM > PreGrazeDMArray[clock.Today.Month - 1];
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
                return expressionFunction.Value() == 1;
        }

        /// <summary>Remove biomass from the specified forage.</summary>
        /// <param name="removeAmount">The total amount to remove from all forages.</param>
        private void RemoveDMFromPlants(double removeAmount)
        {
            // This is a simple implementation. It proportionally removes biomass from organs.
            // What about non harvestable biomass?
            // What about PreferenceForGreenOverDead and PreferenceForLeafOverStems?

            if (removeAmount > 0)
            {
                // Remove a proportion of required DM from each species
                double totalHarvestableWt = 0.0;
                foreach (var forage in forages)
                    totalHarvestableWt += forage.Organs.Sum(organ => organ.Live.Wt + organ.Dead.Wt);

                foreach (var forage in forages)
                {
                    var harvestableWt = forage.Organs.Sum(organ => organ.Live.Wt + organ.Dead.Wt);
                    var amountToRemove = removeAmount * harvestableWt / totalHarvestableWt;
                    var grazed = forage.RemoveBiomass(amountToRemove);

                    const double CToDMRatio = 0.4; // 0.4 is C:DM ratio.

                    double dungCReturned;
                    var dungNReturned = GetValueFromMonthArray(FractionOfBiomassToDung) * grazed.N;
                    if (CNRatioDung == 0)
                        dungCReturned = (1 - grazed.DMDOfStructural) * grazed.Wt * CToDMRatio;
                    else
                        dungCReturned = dungNReturned * CNRatioDung * CToDMRatio;

                    AmountDungNReturned += dungNReturned;
                    AmountDungCReturned += dungCReturned;
                }
            }
        }
    }
}