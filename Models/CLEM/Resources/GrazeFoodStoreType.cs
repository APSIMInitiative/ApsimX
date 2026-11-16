using BruTile;
using DeepCloner.Core;
using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Bibliography;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a GrazeFoodType and holds values in the store
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [Description("This resource represents a graze food store of native pasture (e.g. a specific paddock)")]
    [Version(1, 0, 3, "Fully automated version with user properties")]
    [Version(1, 0, 2, "Grazing from pasture pools is fixed to reflect NABSA approach.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreType.htm")]
    [ModelAssociations(associatedModels: [typeof(RuminantParametersGrazing)], associationStyles: [ModelAssociationStyle.DescendentOfRuminantType])]
    public class GrazeFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFeed, IValidatableObject, IGrazeFoodStoreType
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private IPastureManager manager;
        private GrazeFoodStoreFertilityLimiter grazeFoodStoreFertilityLimiter;
        private double biomassAddedThisYear;
        private double biomassConsumed;

        // Cache over total biomass for sward quality reporting.
        private double weightedSumDMD; // sum(pool.Amount * pool.DMD)
        private double weightedSumN;   // sum(pool.Amount * pool.N)

        /// <summary>
        /// Smallest amount of pool mass or pending permitted.
        /// </summary>
        public const double PoolMassEpsilon = 1e-6;

        /// <inheritdoc/>
        [Description("Units (nominal)")]
        [Category("Simulation", "Details")]
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
        public FeedType TypeOfFeed { get; set; } = FeedType.PastureTropical;

        /// <inheritdoc/>
        [Description("Gross energy content (MJ/kg DM)")]
        [Category("Farm", "Quality")]
        [Units("MJ/kg digestible DM")]
        [Required, GreaterThanValue(0)]
        public double GrossEnergyContent { get; set; } = 18.4;

        /// <inheritdoc/>
        [Required, GreaterThanValue(0)]
        [Description("Metabolisable energy content")]
        [Category("Farm", "Quality")]
        [Units("MJ/kg DM")]
        public double MetabolisableEnergyContent { get; set; } = 8.0;

        private double nitrogenPercent = 0;

        /// <inheritdoc/>
        public double NitrogenPercent
        {
            get
            {
                return nitrogenPercent;
            }
            set
            {
                nitrogenPercent = value;
                CrudeProteinPercent = nitrogenPercent * 6.25;
                if (DMDStyle == DryMatterDigestibilityStyle.EstimateFromNitrogenContent)
                {
                    DryMatterDigestibility = EstimateDMD(nitrogenPercent);
                }
            }
        }

        /// <summary>
        /// Nitrogen of new growth (%)
        /// </summary>
        [Category("Farm", "Nitrogen")]
        [Description("Percent nitrogen of new growth")]
        [Units("%")]
        [Required, Percentage, GreaterThanValue(0)]
        public double GreenNitrogenPercent { get; set; } = 2.0;

        /// <summary>
        /// Proportion Nitrogen loss each month from pools
        /// </summary>
        [Category("Farm", "Nitrogen")]
        [Description("Monthly loss of Nitrogen percent (note: amount as %N not proportion)")]
        [Required, GreaterThanEqualValue(0), Percentage]
        [Units("%")]
        public double DecayNitrogen { get; set; } = 0.4;

        /// <summary>
        /// Minimum Nitrogen %
        /// </summary>
        [Category("Farm", "Nitrogen")]
        [Description("Minimum nitrogen")]
        [Required, Percentage]
        [Units("%")]
        public double MinimumNitrogen { get; set; } = 0.4;

        private double rumenDegradableProteinPercent = 58;

        /// <inheritdoc/>
        [Required, Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Rumen degradable protein percent (%, g/g CP * 100)")]
        public double RumenDegradableProteinPercent
        {
            get
            {
                return rumenDegradableProteinPercent;
            }
            set
            {
                rumenDegradableProteinPercent = value;
                AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(rumenDegradableProteinPercent, TypeOfFeed);
            }
        } 

        /// <summary>
        /// Style of providing the dry matter digestibility of pasture
        /// </summary>
        [Category("Farm", "DMD")]
        [Description("Style of providing DMD")]
        [Required]
        public DryMatterDigestibilityStyle DMDStyle { get; set; } = DryMatterDigestibilityStyle.EstimateFromNitrogenContent;

        /// <summary>
        /// Method to determine if DMD is calculated from N%
        /// </summary>
        /// <returns>True if user has selected Estimate from N</returns>
        public bool IsDMDFromN() { return DMDStyle == DryMatterDigestibilityStyle.EstimateFromNitrogenContent; }

        /// <summary>
        /// Method to determine if DMD of new growth is provided with decay rates and minimum
        /// </summary>
        /// <returns>True if user has selected Specify DMD</returns>
        public bool IsDMDProvided() { return DMDStyle == DryMatterDigestibilityStyle.SpecifyNewGrowthDMD; }

        /// <inheritdoc/>
        public double DryMatterDigestibility { get; set; }

        /// <summary>
        /// DMD of new growth (%)
        /// </summary>
        [Category("Farm", "DMD")]
        [Description("Dry Matter Digestibility of new growth")]
        [Units("%")]
        [Required, Percentage, GreaterThanValue(0)]
        public double GreenDMD { get; set; } = 58;

        /// <summary>
        /// Coefficient to convert initial N% to DMD%
        /// </summary>
        [Category("Farm", "DMD")]
        [Description("Coefficient to convert initial N% to DMD%")]
        [Core.Display(VisibleCallback = "IsDMDFromN")]
        [Required, GreaterThanValue(0)]
        public double NToDMDCoefficient { get; set; } = 11.03;

        /// <summary>
        /// Intercept to convert initial N% to DMD%
        /// </summary>
        [Category("Farm", "DMD")]
        [Description("Intercept to convert initial N% to DMD%")]
        [Core.Display(VisibleCallback = "IsDMDFromN")]
        [Required, GreaterThanValue(0)]
        public double NToDMDIntercept { get; set; } = 41.4;

        /// <summary>
        /// Proportion Dry Matter Digestibility loss each month from pools
        /// </summary>
        [Category("Farm", "DMD")]
        [Description("Proportion DMD loss each month from pools")]
        [Core.Display(VisibleCallback = "IsDMDProvided")]
        [Required, Proportion]
        public double DecayDMD { get; set; } = 0.12;

        /// <summary>
        /// Minimum Dry Matter Digestibility (%)
        /// </summary>
        [Category("Farm", "DMD")]
        [Description("Minimum Dry Matter Digestibility")]
        [Core.Display(VisibleCallback = "IsDMDProvided")]
        [Required, Percentage]
        [Units("%")]
        public double MinimumDMD { get; set; } = 42;

        /// <summary>
        /// Monthly detachment rate
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Detachment rate (monthly)")]
        [Required, Proportion]
        public double DetachRate { get; set; } = 0.03;

        /// <summary>
        /// Detachment rate of 12 month or older plants
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Carryover detachment rate (monthly)")]
        [Required, Proportion]
        public double CarryoverDetachRate { get; set; } = 0.12;

        /// <inheritdoc/>
        public double AcidDetergentInsolubleProtein { get; set; }

        /// <inheritdoc/>
        public double CrudeProteinPercent { get; set; }

        /// <inheritdoc/>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Fat percent (ether extract) (%)")]
        public double FatPercent { get; set; } = 1.9;

        /// <summary>
        /// Value of gut fill for highest quality green pasture
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill high quality (Green DMD)")]
        public double GutFillHighQuality { get; set; } = 0.08;

        /// <summary>
        /// Value of gut fill for lowest quality cured pasture at min DMD
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill low quality (min DMD)")]
        public double GutFillLowQuality { get; set; } = 0.2;

        /// <inheritdoc/>
        [JsonIgnore]
        public double GutFill
        {
            get
            {
                return CalculateGutFill(DryMatterDigestibility);
            }
            set
            {
                throw new NotImplementedException("Setting GutFill is not possible in GrazeFoodStoreType as this value is calculated.");
            }
        }

        /// <summary>
        /// Calculate gut fill based on the pasture gutfill quality values and a specified dry matter digestibility.
        /// </summary>
        /// <param name="dmd">The dry matter digesibility with which to calculate gut fill</param>
        /// <returns></returns>
        public double CalculateGutFill(double dmd)
        {
            if (GreenDMD == MinimumDMD)
                return GutFillLowQuality;
            return GutFillLowQuality + ((dmd - MinimumDMD) / (GreenDMD - MinimumDMD)) * (GutFillHighQuality - GutFillLowQuality);
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public double OverallPastureBiomass { get; private set; }

        ///// <summary>
        ///// Coefficient to adjust intake for tropical herbage quality
        ///// </summary>
        //[Category("Advanced", "Intake")]
        //[Description("Coefficient to adjust intake for tropical herbage quality")]
        //[Required]
        //public double IntakeTropicalQualityCoefficient { get; set; } = 0.16;

        ///// <summary>
        ///// Coefficient to adjust intake for herbage quality
        ///// </summary>
        //[Category("Advanced", "Intake")]
        //[Description("Coefficient to adjust intake for herbage quality")]
        //[Required]
        //public double IntakeQualityCoefficient { get; set; } = 1.7;

        /// <summary>
        /// Initial pasture biomass
        /// </summary>
        [Category("Farm", "Initial biomass")]
        [Description("Initial biomass (kg/ha)")]
        [Units("kg/ha")]
        public double StartingAmount { get; set; }

        /// <summary>
        /// First month of seasonal growth
        /// </summary>
        [Category("Farm", "Initial biomass")]
        [Description("First month of seasonal growth")]
        [System.ComponentModel.DefaultValueAttribute(11)]
        [Required, Month]
        public MonthsOfYear FirstMonthOfGrowSeason { get; set; }

        /// <summary>
        /// Last month of seasonal growth
        /// </summary>
        [Category("Farm", "Initial biomass")]
        [Description("Last month of seasonal growth")]
        [Required, Month]
        public MonthsOfYear LastMonthOfGrowSeason { get; set; } = MonthsOfYear.March;

        /// <summary>
        /// Number of months for initial biomass
        /// </summary>
        [Category("Farm", "Initial biomass")]
        [Description("Number of months for initial biomass")]
        public int NumberMonthsForInitialBiomass { get; set; } = 5;

        /// <summary>
        /// List of pools available
        /// </summary>
        [JsonIgnore]
        public List<GrazeFoodStorePool> Pools = [];

        /// <summary>
        /// A link to the Activity managing this Graze Food Store
        /// </summary>
        [JsonIgnore]
        public IPastureManager Manager
        {
            get
            {
                return manager;
            }
            set
            {
                if (manager != null && manager != value)
                {
                    if (manager is CropActivityManageCrop)
                    {
                        Summary.WriteMessage(this, $"Each [r=GrazeStoreType] can only be managed by a single activity.{Environment.NewLine}Two managing activities (a=[{(manager as CLEMModel).NameWithParent}] and [a={(value as CLEMModel).NameWithParent}]) are trying to manage [r={this.NameWithParent}]. Ensure the [CropActivityManageProduct] children have timers that prevent them running in the same time-step", MessageType.Warning);
                    }
                    else
                    {
                        throw new ApsimXException(this, $"Each [r=GrazeStoreType] can only be managed by a single activity.{Environment.NewLine}Two managing activities (a=[{(manager as CLEMModel).NameWithParent}] and [a={(value as CLEMModel).NameWithParent}]) are trying to manage [r={this.NameWithParent}]. Ensure they hvae timers");
                    }
                }
                manager = value;
            }
        }

        /// <summary>
        /// Return the specified pool
        /// </summary>
        /// <param name="index">index to use</param>
        /// <param name="getByAge">return where index is age</param>
        /// <returns>GrazeFoodStore pool</returns>
        public IEnumerable<GrazeFoodStorePool> Pool(int index, bool getByAge)
        {
            if (getByAge)
            {
                return Pools.Where(a => (index < 12) ? (a.AgeInMonths == index) : (a.AgeInMonths >= 12));
            }

            if (index < Pools.Count)
            {
                return [Pools.ElementAt(index)];
            }
            return null;
        }

        /// <summary>
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa
        {
            get
            {
                if (Manager is null)
                {
                    return 0;
                }
                return AmountAvailable / Manager.Area;
            }
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectare
        {
            get
            {
                if (Manager is null)
                {
                    return 0;
                }
                return KilogramsPerHa / 1000.0;
            }
        }

        /// <summary>
        /// Set the current pasture biomass for analysis
        /// </summary>
        public void SetCurrentBiomass()
        {
            OverallPastureBiomass = KilogramsPerHa;
        }

        /// <summary>
        /// Percent utilisation
        /// </summary>
        public double PercentUtilisation
        {
            get
            {
                if (biomassAddedThisYear == 0)
                {
                    return (biomassConsumed > 0) ? 100 : 0;
                }

                return biomassConsumed == 0 ? 0 : Math.Min(biomassConsumed / biomassAddedThisYear * 100, 100);
            }
        }

        // Single full sweep
        private void RecalculateWeightedSumsFromPools()
        {
            weightedSumDMD = 0;
            weightedSumN = 0;

            foreach (var p in Pools)
            {
                var amount = Math.Max(0, p.Amount);
                if (amount <= PoolMassEpsilon) continue;

                weightedSumDMD += amount * p.DryMatterDigestibility;
                weightedSumN += amount * p.NitrogenPercent;
            }
        }

        // Delta helper for total biomass changes
        private void ApplyAvailableDelta(double deltaKg, double dmd, double n)
        {
            if (Math.Abs(deltaKg) <= PoolMassEpsilon) return;
            weightedSumDMD += deltaKg * dmd;
            weightedSumN += deltaKg * n;
        }

        /// <summary>
        /// Calculated total pasture (all pools) Dry Matter Digestibility (%)
        /// </summary>
        public double SwardDryMatterDigestibility
        {
            get
            {
                if (AmountTotal == 0)
                {
                    return 0;
                }

                double dmd = weightedSumDMD / AmountTotal;

                //double dmd = 0;
                //double amount = AmountAvailable;
                //if (amount > 0)
                //{
                //    dmd = Pools.Sum(a => a.AmountAvailable * a.DryMatterDigestibility) / amount;
                //}

                return Math.Max(MinimumDMD, dmd);
            }
        }

        /// <summary>
        /// Calculated total pasture (all pools) percent nitrogen (%)
        /// </summary>
        public double SwardNitrogenPercent
        {
            get
            {
                if (AmountTotal == 0)
                {
                    return 0;
                }

                double n = weightedSumN / AmountTotal;

                //double n = 0;
                //double amount = AmountAvailable;
                //if (amount > 0)
                //{
                //    n = Pools.Sum(a => a.AmountAvailable * a.NitrogenPercent) / amount;
                //}

                return Math.Max(MinimumNitrogen, n);
            }
        }

        /// <summary>
        /// DecayOfPasture
        /// </summary>
        [JsonIgnore]
        public bool PastureDecays
        {
            get
            {
                return (DetachRate + CarryoverDetachRate + DecayDMD + DecayNitrogen != 0);
            }
        }

        /// <summary>
        /// Method to provide conversion factor to tonnes and/or hectares
        /// </summary>
        public double Report(string grazeProperty, bool tonnes = false, bool hectares = false, int age = -1)
        {
            if ((hectares && Manager is null) | (age > 11))
            {
                return 0;
            }

            double convert = (tonnes ? 1000 : 1) * (hectares ? Manager.Area : 1);
            double valueToUse = 0;
            switch (grazeProperty)
            {
                case "Amount":
                    if (age < 0)
                    {
                        valueToUse = AmountAvailable;
                    }
                    else
                    {
                        var poolsByAge = Pool(age, true);
                        if (poolsByAge is not null)
                        {
                            foreach (var pool in poolsByAge)
                            {
                                valueToUse += pool.AmountAvailable;
                            }
                        }
                    }

                    break;
                case "Growth":
                    {
                        var poolsByAge = Pool(0, true);
                        if (poolsByAge is not null)
                        {
                            foreach (var pool in poolsByAge)
                            {
                                valueToUse += pool.Growth;
                            }
                        }
                    }
                    break;
                case "Consumed":
                    if (age < 0)
                    {
                        foreach (var pool in Pools)
                        {
                            valueToUse += pool.Consumed;
                        }
                    }
                    else
                    {
                        var poolsByAge = Pool(age, true);
                        if (poolsByAge is not null)
                        {
                            foreach (var pool in poolsByAge)
                            {
                                valueToUse += pool.Consumed;
                            }
                        }
                    }

                    break;
                case "Detached":
                    if (age < 0)
                    {
                        foreach (var pool in Pools)
                        {
                            valueToUse += pool.Detached;
                        }
                    }
                    else
                    {
                        var poolsByAge = Pool(age, true);
                        if (poolsByAge is not null)
                        {
                            foreach (var pool in poolsByAge)
                            {
                                valueToUse += pool.Detached;
                            }
                        }
                    }

                    break;
                case "Nitrogen":
                    if (age < 0)
                    {
                        return SwardNitrogenPercent;
                    }
                    else
                    {
                        var pools = Pool(age, true);
                        if (pools is null)
                        {
                            return 0;
                        }

                        double amount = 0;
                        double weightedN = 0;
                        bool seenPool = false;
                        GrazeFoodStorePool firstPool = null;
                        foreach (var pool in pools)
                        {
                            seenPool = true;
                            firstPool ??= pool;
                            amount += pool.AmountAvailable;
                            weightedN += pool.NitrogenPercent * pool.AmountAvailable;
                        }

                        if (!seenPool)
                        {
                            return 0;
                        }

                        if (Math.Abs(amount) <= PoolMassEpsilon)
                        {
                            valueToUse = firstPool.NitrogenPercent;
                        }
                        else
                        {
                            valueToUse = weightedN / amount;
                        }
                    }
                    return valueToUse;
                case "DMD":
                    if (age < 0)
                    {
                        return SwardDryMatterDigestibility;
                    }
                    else
                    {
                        var pools = Pool(age, true);
                        if (pools is null)
                        {
                            return 0;
                        }

                        double amount = 0;
                        double weightedDmd = 0;
                        bool seenPool = false;
                        GrazeFoodStorePool firstPool = null;
                        foreach (var pool in pools)
                        {
                            seenPool = true;
                            firstPool ??= pool;
                            amount += pool.AmountAvailable;
                            weightedDmd += pool.DryMatterDigestibility * pool.AmountAvailable;
                        }

                        if (!seenPool)
                        {
                            return 0;
                        }

                        if (Math.Abs(amount) <= PoolMassEpsilon)
                        {
                            valueToUse = firstPool.DryMatterDigestibility;
                        }
                        else
                        {
                            valueToUse = weightedDmd / amount;
                        }
                    }
                    return valueToUse;
                case "Age":
                    if (age < 0)
                    {
                        if (AmountAvailable <= PoolMassEpsilon)
                        {
                            return 0;
                        }

                        double weightedAge = 0;
                        foreach (var pool in Pools)
                        {
                            weightedAge += pool.AmountAvailable * pool.AgeInMonths;
                        }

                        return weightedAge / AmountAvailable;
                    }

                    return valueToUse;
                default:
                    throw new ApsimXException(this, $"Property [{grazeProperty}] not available for reporting pools");
            }
            // convert biomass to units specified kg,tonnes & farm,per/hectare
            return valueToUse / convert;
        }

        /// <summary>
        /// Method to estimate DMD from N%
        /// </summary>
        /// <returns></returns>
        public double EstimateDMD(double nitrogenPercent)
        {
            return Math.Max(MinimumDMD, nitrogenPercent * NToDMDCoefficient + NToDMDIntercept);
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectareStartOfTimeStep { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            AcidDetergentInsolubleProtein = FoodResourcePacket.CalculateAcidDetergentInsolubleProtein(RumenDegradableProteinPercent, TypeOfFeed);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CurrentEcologicalIndicators = new EcologicalIndicators
            {
                ResourceType = Name
            };
            grazeFoodStoreFertilityLimiter = Structure.FindChildren<GrazeFoodStoreFertilityLimiter>().FirstOrDefault();
        }

        /// <summary>An event handler to allow us to make checks after resources and activities initialised.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("FinalInitialise")]
        private void OnFinalInitialise(object sender, EventArgs e)
        {
            if (Manager == null)
            {
                Summary.WriteMessage(this, $"There is no activity managing [r={NameWithParent}]. This resource will have no growth.{Environment.NewLine}To manage [r={Name}] include a [a=CropActivityManage]+[a=CropActivityManageProduct] or a [a=PastureActivityManage] depending on your external data type.", MessageType.Warning);
            }
        }

        /// <summary>
        /// Cleans up pools
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            Pools?.Clear();
            Pools = null;
            weightedSumDMD = 0;
            weightedSumN = 0;
        }

        /// <summary>An event handler to allow us to clear pools.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // reset pool counters
            foreach (var pool in Pools)
            {
                pool.Reset();
            }
        }

        /// <summary>
        /// Function to detach pasture before reporting
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDetachPasture")]
        private void OnCLEMDetachPasture(object sender, EventArgs e)
        {
            DetachPasture(events.Interval, 30.4);
        }

        /// <summary>
        /// Detach pasture based on specified detachment rate for pools less than and greater than or equal to 12 months
        /// old
        /// </summary>
        /// <param name="daysInTimeStep">Number of days in the time step</param>
        /// <param name="daysInMonth">Number of days in a month for conversion from monthly to daily rates</param>
        /// <exception cref="ApsimXException"></exception>
        public void DetachPasture(int daysInTimeStep, double daysInMonth)
        {
            if (daysInMonth == 0)
                return;

            if (daysInMonth <= 0)
                throw new ApsimXException(this, $"Core logic error: Invalid days in month provided [{daysInMonth}] to detach pasture by [r={this.NameWithParent}]");

            if (DetachRate < 0)
                throw new ApsimXException(this, $"Core logic error: Negative detachment rate applied by [r={this.NameWithParent}]");

            if (CarryoverDetachRate < 0)
                throw new ApsimXException(this, $"Core logic error: Negative carryover detachment rate applied by [r={this.NameWithParent}]");

            double detached = 0;
            foreach (var pool in Pools)
            {
                if (pool.AmountPending > PoolMassEpsilon)
                {
                    throw new ApsimXException(this, "Core logic error: Cannot detach pasture as there is pending growth or grazing. Check timers of managing activities to ensure they run after detachment or pending resources are handled before detachment");
                }

                if (pool.AmountPending > 0)
                {
                    // clear numerical dust from pending before detachment
                    pool.ReducePending(pool.AmountPending);
                }

                double rate = (pool.AgeInMonths >= 12) ? CarryoverDetachRate : DetachRate;
                double detach = Math.Min(1.0, rate / daysInMonth * daysInTimeStep);

                double detachedPool = pool.Detach(detach);
                detached += detachedPool;
                ApplyAvailableDelta(-detachedPool, pool.DryMatterDigestibility, pool.NitrogenPercent);
            }

            if (detached > 0)
            {
                base.RemoveFromResource(detached, null);
                ReportTransaction(TransactionType.Loss, detached, null, null, "Detached", this);
            }
        }

        /// <summary>
        /// Function to age resource pools
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAgeResources")]
        private void OnCLEMAgeResources(object sender, EventArgs e)
        {
            AgePasture(events.Interval, 30.4);
        }

        /// <summary>
        /// Age pasture by days in the time step
        /// </summary>
        /// <param name="daysInTimeStep">Number of days in the time step</param>
        /// <param name="daysInMonth">Number of days in a month for conversion from monthly to daily rates</param>
        /// <exception cref="ApsimXException"></exception>
        public void AgePasture(int daysInTimeStep, double daysInMonth)
        {
            foreach (var pool in Pools)
            {
                // N is a loss of N% (x = x -loss)
                if (DecayNitrogen > 0)
                {
                    pool.NitrogenPercent = Math.Max(pool.NitrogenPercent - (DecayNitrogen / daysInMonth * daysInTimeStep), MinimumNitrogen);
                }

                if (DecayDMD > 0 && DMDStyle == DryMatterDigestibilityStyle.SpecifyNewGrowthDMD)
                {
                    // DMD is a proportional loss (x = x*(1-proploss))
                    pool.DryMatterDigestibility = Math.Max(pool.DryMatterDigestibility * (1 - (DecayDMD / daysInMonth * daysInTimeStep)), MinimumDMD);
                }

                pool.UpdateAge(pool.GrowthDate, events.TimeStepStart.AddDays(events.Interval));
            }
            // remove all pools with less than 1g of food
            Pools.RemoveAll(a => a.Amount < 0.001);
            RecalculateWeightedSumsFromPools();

            if (events.IsEcologicalIndicatorsCalculationDue())
            {
                OnEcologicalIndicatorsCalculated(new EcolIndicatorsEventArgs() { Indicators = CurrentEcologicalIndicators });
                // reset so available is sum of years growth
                biomassAddedThisYear = 0;
                biomassConsumed = 0;
            }
        }

        /// <summary>Store amount of pasture available for everyone at the start of the step (kg per hectare)</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPastureReady")]
        private void OnCLEMPastureReady(object sender, EventArgs e)
        {
            // do not return zero as there is always something there and zero affects calculations.
            base.Set(Pools.Sum(a => a.Amount));
            RecalculateWeightedSumsFromPools();

            TonnesPerHectareStartOfTimeStep = Math.Max(TonnesPerHectare, 0.01);
        }

        /// <summary>
        /// Ecological indicators have been calculated
        /// </summary>
        public event EventHandler EcologicalIndicatorsCalculated;

        /// <summary>
        /// Ecological indicators calculated
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnEcologicalIndicatorsCalculated(EventArgs e)
        {
            EcologicalIndicatorsCalculated?.Invoke(this, e);
            CurrentEcologicalIndicators.Reset();
        }

        /// <summary>
        /// Ecological indicators of this pasture
        /// </summary>
        [JsonIgnore]
        public EcologicalIndicators CurrentEcologicalIndicators { get; set; }

        /// <summary>
        /// A method to initialise initial pasture biomass across pools
        /// </summary>
        /// <param name="area">Area of pasture (ha)</param>
        /// <param name="firstMonthsGrowth">The growth (kg per ha) expected in the first month for accuracy</param>
        public void SetupStartingPasturePools(double area, double firstMonthsGrowth)
        {
            if (area <= 0) return;
            if (NumberMonthsForInitialBiomass <= 0) return;

            // Initial biomass
            double amountToAdd = area * StartingAmount;
            if (amountToAdd <= 0) return;

            // Set up pasture pools to start run based on month and user defined pasture properties
            // Locates the previous five months where growth occurred (Nov-Mar) and applies decomposition to current month
            // This months growth will not be included.

            int month = events.Clock.Today.Month;
            int monthCount = 0;
            int includedMonthCount = 0;
            double propBiomass = 1.0;
            double currentN = GreenNitrogenPercent;
            DateTime growDate = new(events.Clock.Today.Year, month, 1);

            double currentDMD = 0;
            switch (DMDStyle)
            {
                case DryMatterDigestibilityStyle.SpecifyNewGrowthDMD:
                    currentDMD = GreenDMD;
                    break;
                case DryMatterDigestibilityStyle.EstimateFromNitrogenContent:
                    currentDMD = EstimateDMD(currentN);
                    break;
                default:
                    break;
            }
            Pools.Clear();
            weightedSumDMD = 0;
            weightedSumN = 0;

            List<GrazeFoodStorePool> newPools = [];

            // number of previous growth months to consider. default should be 5
            int growMonthHistory = NumberMonthsForInitialBiomass;

            while (includedMonthCount < growMonthHistory)
            {
                // start month before start of simulation.
                monthCount++;
                month--;
                growDate = growDate.AddMonths(-1);
                currentN -= DecayNitrogen;
                currentN = Math.Max(currentN, MinimumNitrogen);
                currentDMD *= 1 - DecayDMD;
                currentDMD = Math.Max(currentDMD, MinimumDMD);

                if (month == 0)
                {
                    month = 12;
                }

                bool insideGrowthWindow = false;
                int first = (int)FirstMonthOfGrowSeason;
                int last = (int)LastMonthOfGrowSeason;

                if (first < last)
                {
                    insideGrowthWindow = (month >= first & month <= last);
                }
                else
                {
                    insideGrowthWindow = (month >= first | month <= last);
                }

                if (insideGrowthWindow) // (month <= 3 | month >= 11)
                {
                    GrazeFoodStorePool newPool = new(0, this, growDate, events.Clock.Today)
                    {
                        GrossEnergyContent = this.GrossEnergyContent,
                        MetabolisableEnergyContent = this.MetabolisableEnergyContent,
                        FatPercent = this.FatPercent,
                        StartingAmount = propBiomass,
                        RumenDegradableProteinPercent = this.RumenDegradableProteinPercent,
                        NitrogenPercent = currentN
                    };
                    if (DMDStyle == DryMatterDigestibilityStyle.SpecifyNewGrowthDMD)
                    {
                        newPool.DryMatterDigestibility = currentDMD;
                    }
                    else
                    {
                        newPool.DryMatterDigestibility = EstimateDMD(currentN);
                    }

                    // add new pool
                    newPools.Add(newPool);
                    includedMonthCount++;
                }
                propBiomass *= 1 - DetachRate;
            }

            // assign pasture biomass to pools based on proportion of total
            double total = newPools.Sum(a => a.StartingAmount);
            foreach (var pool in newPools)
            {
                pool.InitialBiomassSet(amountToAdd * (pool.StartingAmount / total));
            }

            // Previously: remove this months growth from pool age 0 to keep biomass at approximately setup.
            // But as updates happen at the end of the month, the first month's biomass is never added so stay with 0 or delete following section
            // Get this months growth
            // Get this months pasture data from the pasture data list
            if (firstMonthsGrowth > 0)
            {
                double thisMonthsGrowth = firstMonthsGrowth * area;
                if (thisMonthsGrowth > 0)
                {
                    if (newPools.Where(a => a.AgeInDays == 0).FirstOrDefault() is GrazeFoodStorePool thisMonth)
                    {
                        thisMonth.InitialBiomassSet(Math.Max(0, thisMonth.AmountAvailable - thisMonthsGrowth));
                    }
                }
            }

            // Add to pasture. This will add pool to pasture available store.
            foreach (var pool in newPools)
            {
                string reason = "Initialise";
                if (newPools.Count > 0)
                {
                    reason = "Initialise pool " + pool.AgeInMonths.ToString();
                }

                AddToResource(pool, null, null, reason);
            }
        }

        /// <inheritdoc/>
        public List<FoodResourceStore> GenerateIntakeGroups(int numberOfTimesteps, int greenAge = -1, int dmdStep = 10)
        {
            IEnumerable<GrazeFoodStorePool> pasturePools;
            pasturePools = Pools;

            // think about different approaches
            // 1. whole avearge pasture pool (DMD step = 100)
            // 2. select by DMD - current DMD step (e.g. 10)
            // 3. proportional with weighting toward green
            // 4. CLEM green biomass limit - implemented
            // 5. CLEM low biomass intake limited - implemented

            // individual selective ability proceedures can be actioned in GeneratePoolGroups and thus the list and order of pools the animals feed from.

            var nestedGroups = pasturePools
                .GroupBy(s => Convert.ToInt32(s.DryMatterDigestibility / dmdStep) * dmdStep)
                .Select(groups => new FoodResourceStore(
                    [.. groups],
                    greenAge,
                    numberOfTimesteps
                    )
                ).OrderByDescending(a => a.Details.DryMatterDigestibility);

            return nestedGroups.ToList();
        }

        /// <summary>
        /// Finalise pending transactions and reconcile pool totals once at end of timestep.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("CLEMManagePendingTransactions")]
        public override void ManagePendingTransactions(object sender, EventArgs e)
        {
            base.ManagePendingTransactions(sender, e);

            bool needsPoolCleanup = false;
            foreach (var pool in Pools)
            {
                if (pool.AmountPending > 0 && pool.AmountPending <= PoolMassEpsilon)
                {
                    pool.ReducePending(pool.AmountPending);
                }

                if (pool.Amount <= PoolMassEpsilon)
                {
                    needsPoolCleanup = true;
                }
            }

            if (needsPoolCleanup)
            {
                Pools.RemoveAll(a => a.Amount <= PoolMassEpsilon && a.AmountPending <= PoolMassEpsilon);
            }

            // single exact sweep to align base amount and quality aggregates with pool state
            RecalculateWeightedSumsFromPools();
            double poolTotal = 0;
            foreach (var pool in Pools)
            {
                poolTotal += pool.Amount;
            }

            base.Set(poolTotal);
        }

        #region transactions

        /// <summary>
        /// Graze food add method. This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="resourceAmount">
        /// Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being
        /// added
        /// </param>
        /// <param name="activity">Reference to the activity adding resource</param>
        /// <param name="relatesToResource">Optional relates to resource as string for reporting</param>
        /// <param name="category">Transaction category</param>
        public new void AddToResource(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            if (events is null)
                throw new ApsimXException(this, $"Core logic error: Cannot add to [r={this.NameWithParent}] as the [Clock.CLEMEvents] is not available. Check that the [Clock.CLEMEvents] is present in the simulation.");

            GrazeFoodStorePool pool = new(0, this)
            {
                GrossEnergyContent = GrossEnergyContent,
                MetabolisableEnergyContent = MetabolisableEnergyContent,
                FatPercent = FatPercent,
                NitrogenPercent = 0,
                DryMatterDigestibility = 0,
                RumenDegradableProteinPercent = RumenDegradableProteinPercent
            };

            switch (resourceAmount)
            {
                case GrazeFoodStorePool incomingPool:
                    // coming from the advanced PastureActivityManage
                    // adjust N content only if new growth (age = 0) based on yield limits and month range defined in GrazeFoodStoreFertilityLimiter if present
                    if (incomingPool.IsGrowthThisTimeStep && grazeFoodStoreFertilityLimiter is not null)
                    {
                        pool.NitrogenPercent = Math.Max(MinimumNitrogen, incomingPool.NitrogenPercent * grazeFoodStoreFertilityLimiter.GetProportionNitrogenLimited(incomingPool.AmountAvailable / Manager.Area));
                        pool.DryMatterDigestibility = Math.Min(100, Math.Max(MinimumDMD, pool.NitrogenPercent * NToDMDCoefficient + NToDMDIntercept));
                    }
                    else
                    {
                        pool.NitrogenPercent = incomingPool.NitrogenPercent;
                        pool.DryMatterDigestibility = incomingPool.DryMatterDigestibility;
                    }
                    pool.GutFill = CalculateGutFill(pool.DryMatterDigestibility);
                    pool.InitialBiomassSet(incomingPool.Amount);
                    pool.UpdateAge(incomingPool.GrowthDate, events.TimeStepStart);
                    break;
                case FoodResourcePacket packet:
                    // coming from the CropActivityManage
                    // TODO: does this need to track age (growthdate etc)?
                    pool.InitialBiomassSet(packet.Amount);
                    pool.NitrogenPercent = packet.NitrogenPercent;
                    pool.DryMatterDigestibility = packet.DryMatterDigestibility;
                    pool.UpdateAge(events.TimeStepStart, events.TimeStepStart);
                    break;
                case double amount:
                    // add amount at current rates
                    pool.InitialBiomassSet(amount);
                    pool.NitrogenPercent = this.SwardNitrogenPercent;
                    pool.DryMatterDigestibility = SwardDryMatterDigestibility; 
                    pool.UpdateAge(events.TimeStepStart, events.TimeStepStart);
                    break;
                default:
                    throw new Exception($"ResourceAmount object of type [{resourceAmount.GetType().Name}] is not supported in [r={Name}]");
            }

            if (pool.Amount > 0)
            {
                // allow decaying or no pools currently available
                if (PastureDecays || Pools.Count == 0)
                    Pools.Insert(0, pool);
                else
                    Pools[0].Add(pool);

                ApplyAvailableDelta(pool.Amount, pool.DryMatterDigestibility, pool.NitrogenPercent);

                // update biomass available
                if (!category.StartsWith("Initialise"))
                    // do not update if this is an initialisation pool
                    biomassAddedThisYear += pool.Amount;

                base.Add(pool.Amount);
                ReportTransaction(TransactionType.Gain, pool.Amount, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        /// Remove a specified amount from the resource.
        /// </summary>
        /// <param name="amountToRemove">Amount to remove from resource store</param>
        /// <param name="pendingRequest">
        /// Provides a the request if this is a pending transaction that has not yet been completed. This will not
        /// reduce the amount total available until the transaction is completed.
        /// </param>
        /// <returns>Amount removed</returns>
        protected double Remove(double amountToRemove, ResourceRequest pendingRequest)
        {
            amountToRemove = base.RemoveFromResource(amountToRemove, pendingRequest);

            // add pending amount to each pool
            if (pendingRequest.AdditionalDetails is IEnumerable<FoodResourceStore> foodStores)
            {
                double scaleToProvided = (pendingRequest.Required > 0)
                    ? Math.Min(1.0, amountToRemove / pendingRequest.Required)
                    : 0;

                foreach (var foodStore in foodStores)
                {
                    for (int i = 0; i < foodStore.Pools.Count; i++)
                    {
                        double pendingAmount = foodStore.Details.Amount * scaleToProvided * foodStore.PoolProportions[i];
                        foodStore.Pools[i].SetPending(pendingAmount);
                    }
                }
            }
            return amountToRemove;
        }

        /// <summary>
        /// Decrease pending for specified food resource store
        /// </summary>
        /// <param name="request"></param>
        /// <param name="store">Food store to modify</param>
        /// <param name="amount">Amount to decrease (kg/day)</param>
        public void DecreasePendingByStore(ResourceRequest request, FoodResourceStore store, double amount)
        {
            double amountForTimeStep = amount * store.NumberOfDaysInTimestep;
            for (int i = 0; i < store.Pools.Count; i++)
            {
                store.Pools[i].ReducePending(amountForTimeStep * store.PoolProportions[i]);
            }

            // do removal from pending
            base.DecreasePending(request, amountForTimeStep);
        }

        /// <summary>
        /// Remove resource based on a ResourceRequest
        /// </summary>
        /// <param name="request">Resource request specifying removal details</param>
        public new void RemoveFromResource(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            if (request.AdditionalDetails is null)
            {
                throw new Exception("A ResourceRequest to remove from GrazeFoodStoreType must contain a value in the AdditionalDetails property");
            }

            switch (request.AdditionalDetails)
            {
                case IEnumerable<FoodResourceStore> foodStores:
                    // A food store will be provided for grazing activities representing the pool group consumed. 
                    // nothing is needed here. 
                    // the base remove below will set the pending requests in the resource type which will then be filled in the selective feeding process and adjusted in Ruminant.Intake
                    Remove(request.Required, request);
                    break;
                case PastureActivityCutAndCarry:
                case PastureActivityBurn:
                    RemoveFromPools(request);
                    // use generic removal to handle pending and reporting transaction if needed 
                    base.RemoveFromResource(request);
                    break;
                case CropActivityManageProduct:
                    // this occurs when the pasture is being replaced by the provided biomass and clears the stores
                    if (request.Category == "StoreCleared")
                    {
                        double amountCleared = Pools.Sum(a => a.AmountAvailable);
                        if (amountCleared == 0)
                        {
                            return;
                        }
                        Pools.Clear();
                        weightedSumDMD = 0;
                        weightedSumN = 0;
                        request.Provided = amountCleared;
                        // use generic removal to handle pending and reporting transaction if needed 
                        base.RemoveFromResource(request);
                    }
                    break;
                default:
                    // Need to add new section here to allow non grazing activity to remove resources from pasture.
                    throw new Exception("Removing resources from GrazeFoodStore can only be performed by a grazing, burning and cut and carry activities at this stage");
            }
        }

        /// <summary>
        /// Performs a transaction by specified amount.
        /// </summary>
        /// <param name="request">The amount of the transaction.</param>
        /// <param name="handlePendingTransaction">
        /// This transaction should handle any pending amount rather than the amount provided.
        /// </param>
        public override void PerformTransaction(ResourceRequest request, bool handlePendingTransaction = false)
        {
            double provided = 0;
            // remove all pending and take from pools 
            // set provided to peding pool amounts
            if (request.AdditionalDetails is IEnumerable<FoodResourceStore> foodStores)
            {
                foreach (var foodStore in foodStores)
                {
                    for (int i = 0; i < foodStore.Pools.Count; i++)
                    {
                        double pendingToConsume = foodStore.Pools[i].AmountPending;
                        provided += pendingToConsume;
                        biomassConsumed += pendingToConsume;
                        ApplyAvailableDelta(-pendingToConsume, foodStore.Pools[i].DryMatterDigestibility, foodStore.Pools[i].NitrogenPercent);
                        foodStore.Pools[i].ConsumePending();
                    }
                }
            }
            request.Provided = provided;

            base.PerformTransaction(request, handlePendingTransaction);
        }

        /// <summary>
        /// Method to undertake the removal of the amount required from the pasture pools
        /// </summary>
        /// <param name="request"></param>
        private void RemoveFromPools(ResourceRequest request)
        {
            // take from pools by cut and carry
            double amountRequired = request.Required;
            double amountCollected = 0;
            double dryMatterDigestibility = 0;
            double nitrogen = 0;

            // take proportionally from all pools.
            double useproportion = Math.Min(1.0, amountRequired / Pools.Sum(a => a.AmountAvailable));
            // if less than pools then take required as proportion of pools
            foreach (GrazeFoodStorePool pool in Pools)
            {
                double amountRemoveed = pool.AmountAvailable * useproportion;
                amountCollected += amountRemoveed;
                dryMatterDigestibility += pool.DryMatterDigestibility * amountRemoveed;
                nitrogen += pool.NitrogenPercent * amountRemoveed;
                pool.Remove(amountRemoveed); // "Cut and carry"
                ApplyAvailableDelta(-amountRemoveed, pool.DryMatterDigestibility, pool.NitrogenPercent);
            }
            request.Provided = amountCollected;

            // adjust DMD and N of biomass consumed
            dryMatterDigestibility /= request.Provided;
            nitrogen /= request.Provided;

            base.RemoveFromResource(amountCollected, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool noGrowSeason;
            int first = (int)FirstMonthOfGrowSeason;
            int last = (int)LastMonthOfGrowSeason;
            if (first < last)
            {
                noGrowSeason = (last - first <= 1);
            }
            else
            {
                noGrowSeason = ((12 - first) + last <= 1);
            }

            if (StartingAmount > 0 & noGrowSeason)
            {
                yield return new ValidationResult($"There must be at least one month differnece between the first month [{FirstMonthOfGrowSeason}] and the last month [{LastMonthOfGrowSeason}] of the growth season specified to calculate the initial biomass in [r={NameWithParent}]", ["Invalid initial biomass growth season"]);
            }
        }
        #endregion
    }

}