namespace Models
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

        private DateTime NoGrazingStart;
        private DateTime NoGrazingEnd;
        private double residualBiomass;

        /// <summary>Average potential ME concentration in herbage material (MJ/kg)</summary>
        private const double PotentialMEOfHerbage = 16.0;

        /// <summary>Grazing rotation type enum for drop down.</summary>
        public enum GrazingRotationTypeEnum
        {
            /// <summary>A simple rotation.</summary>
            SimpleRotation,

            /// <summary>A rotation based on a target mass.</summary>
            TargetMass,

            /// <summary>A rotation based on a target mass and length.</summary>
            TargetMassAndLength,

            /// <summary>Timing of grazing is controlled elsewhere.</summary>
            TimingControlledElsewhere
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
        [Description("Frequency of grazing (0 will be interpreted as the end of each month)")]
        [Units("days")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public int SimpleGrazingFrequency { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleGrazingResidual { get; set; }

        /// <summary></summary>
        [Description("Minimum grazeable dry matter to trigger grazing")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleMinGrazable { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Target Mass' and 'Maximum Rotation Length' - all values by month from January")]
        [Description("Monthly target mass of pasture to trigger grazing event")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn,IsTargetMassAndLengthTurnedOn")]
        public double[] PreGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Monthly mass of pasture post grazing")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn,IsTargetMassAndLengthTurnedOn")]
        public double[] PostGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Monthly maximum rotation length")]
        [Units("days")]
        [Display(EnabledCallback = "IsTargetMassAndLengthTurnedOn")]
        public double[] RotationLengthArray { get; set; }


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
        [Description("Depth that urine is added.")]
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
        public bool IsTargetMassAndLengthTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.TargetMassAndLength;
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

        ////////////// Outputs //////////////

        /// <summary>Number of days since grazing.</summary>
        public int DaysSinceGraze { get; set; }

        /// <summary></summary>
        public int GrazingInterval { get; set; }

        /// <summary>DM grazed</summary>
        [Units("kgDM/ha")]
        public double GrazedDM { get; set; }

        /// <summary>N in the DM grazed.</summary>
        [Units("kgN/ha")]
        public double GrazedN { get; set; } 

        /// <summary>N in the DM grazed.</summary>
        [Units("MJME/ha")]
        public double GrazedME { get; set; }

        /// <summary>N in urine returned to the paddock.</summary>
        [Units("kgN/ha")]
        public double AmountUrineNReturned { get; set; }

        /// <summary>C in dung returned to the paddock.</summary>
        [Units("kgDM/ha")]
        public double AmountDungCReturned { get; set; }

        /// <summary>N in dung returned to the paddock.</summary>
        [Units("kgN/ha")]
        public double AmountDungNReturned { get; set; }

        /// <summary>Mass of herbage just before grazing.</summary>
        [Units("kgDM/ha")]
        public double PreGrazeDM { get; private set; }

        /// <summary>Mass of herbage just after grazing.</summary>
        [Units("kgDM/ha")]
        public double PostGrazeDM { get; private set; }


        ////////////// Methods //////////////

        /// <summary>This method is invoked at the beginning of the simulation.</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (Verbose)
                summary.WriteMessage(this, "Initialising the Manager for grazing, urine return and reporting");

            if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass ||
                GrazingRotationType == GrazingRotationTypeEnum.TargetMassAndLength)
            {
                if (PreGrazeDMArray == null || PreGrazeDMArray.Length != 12)
                    throw new Exception("There must be 12 values input for the pre-grazing DM");
                if (PostGrazeDMArray == null || PostGrazeDMArray.Length != 12)
                    throw new Exception("There must be 12 values input for the post-grazing DM");
            }
            if (GrazingRotationType == GrazingRotationTypeEnum.TargetMassAndLength)
            { 
                if (RotationLengthArray == null || RotationLengthArray.Length != 12)
                    throw new Exception("There must be 12 values input for rotation length");
            }

            if (FractionOfBiomassToDung.Length != 1 && FractionOfBiomassToDung.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of biomass going to dung' or 12 monthly values.");

            if (FractionOfBiomassToUrine.Length != 1 && FractionOfBiomassToUrine.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of biomass going to urine' or 12 monthly values.");

            if (Verbose)
                summary.WriteMessage(this, "Finished initialising the Manager for grazing, urine return and reporting");

            if (NoGrazingStartString != null)
                NoGrazingStart = DateUtilities.GetDate(NoGrazingStartString);
            if (NoGrazingEndString != null)
                NoGrazingEnd = DateUtilities.GetDate(NoGrazingEndString);
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            DaysSinceGraze += 1;
            PostGrazeDM = 0.0;
            GrazedDM = 0.0;
            GrazedN = 0.0;
            GrazedME = 0.0;
            AmountDungNReturned = 0;
            AmountDungCReturned = 0;
            AmountUrineNReturned = 0;

            // Calculate herbage mass.
            PreGrazeDM = 0.0;
            foreach (var forage in forages)
                foreach (var forageOrgan in forage.Organs)
                    PreGrazeDM += forageOrgan.Live.Wt + forageOrgan.Dead.Wt;

            // Convert to kg/ha
            PreGrazeDM *= 10;

            // Determine if we can graze today.
            var grazeNow = false;
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                grazeNow = SimpleRotation();
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
                grazeNow = TargetMass();
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMassAndLength)
                grazeNow = TargetMassAndLength();

            if (NoGrazingStart != null && 
                NoGrazingEnd != null &&
                clock.Today.DayOfYear >= NoGrazingStart.DayOfYear && 
                clock.Today.DayOfYear <= NoGrazingEnd.DayOfYear)
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
                return PreGrazeDM > SimpleGrazingResidual + SimpleMinGrazable;
            }
            return false;
        }

        /// <summary>Calculate whether a target mass rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool TargetMass()
        {
            residualBiomass = PostGrazeDMArray[clock.Today.Month - 1];
            return PreGrazeDM > PreGrazeDMArray[clock.Today.Month - 1];
        }

        /// <summary>Calculate whether a target mass and length rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool TargetMassAndLength()
        {
            residualBiomass = PostGrazeDMArray[clock.Today.Month - 1];
            return ((PreGrazeDM > PreGrazeDMArray[clock.Today.Month - 1] || DaysSinceGraze > RotationLengthArray[clock.Today.Month - 1])
                    && PreGrazeDM > PostGrazeDMArray[clock.Today.Month - 1] && DaysSinceGraze > 14);  // does have to be grazable!
        }

        /// <summary>Remove biomass from the specified forage.</summary>
        /// <param name="amountToRemove">The total amount to remove from all forages.</param>
        private void RemoveDMFromPlants(double amountToRemove)
        {
            // This is a simple implementation. It proportionally removes biomass from organs.
            // What about non harvestable biomass?
            // What about PreferenceForGreenOverDead and PreferenceForLeafOverStems?

            if (amountToRemove > 0)
            {
                foreach (var forage in forages)
                    foreach (var organ in forage.Organs)
                    {
                        // These calculations convert organ live weight from g/m2 to kg/ha
                        var amountLiveToRemove = organ.Live.Wt * 10 / PreGrazeDM * amountToRemove;
                        var amountDeadToRemove = organ.Dead.Wt * 10 / PreGrazeDM * amountToRemove;
                        var fractionLiveToRemove = MathUtilities.Divide(amountLiveToRemove, (organ.Live.Wt * 10), 0);
                        var fractionDeadToRemove = MathUtilities.Divide(amountDeadToRemove, (organ.Dead.Wt * 10), 0);
                        var grazedDigestibility = organ.Live.DMDOfStructural * fractionLiveToRemove
                                                    + organ.Dead.DMDOfStructural * fractionDeadToRemove;
                        var grazedMetabolisableEnergy = PotentialMEOfHerbage * grazedDigestibility;
                        var grazedDM = amountLiveToRemove + amountDeadToRemove;
                        var grazedN = organ.Live.N * 10 * fractionLiveToRemove + organ.Dead.N * 10 * fractionDeadToRemove;
                        if (grazedDM > 0)
                        {
                            GrazedDM += grazedDM;
                            GrazedN += grazedN;
                            GrazedME += grazedMetabolisableEnergy * grazedDM;

                            forage.RemoveBiomass(organ.Name, "Graze",
                                                 new OrganBiomassRemovalType()
                                                 {
                                                     FractionLiveToRemove = fractionLiveToRemove / 10,  // g/m2
                                                 FractionDeadToRemove = fractionDeadToRemove / 10   // g/m2
                                             });

                            PostGrazeDM += (organ.Live.Wt + organ.Dead.Wt) * 10;

                            const double CToDMRatio = 0.4; // 0.4 is C:DM ratio.

                            double dungCReturned;
                            var dungNReturned = GetValueFromMonthArray(FractionOfBiomassToDung) * grazedN;
                            if (CNRatioDung == 0)
                                dungCReturned = (1 - grazedDigestibility) * grazedDM * CToDMRatio;
                            else
                                dungCReturned = dungNReturned * CNRatioDung * CToDMRatio;

                            AmountDungNReturned += dungNReturned;
                            AmountDungCReturned += dungCReturned;
                        }
                    }
            }
        }
    }
}