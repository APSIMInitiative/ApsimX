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
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrazing) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class GrazeFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFeed, IValidatableObject, IGrazeFoodStoreType
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private IPastureManager manager;
        private GrazeFoodStoreFertilityLimiter grazeFoodStoreFertilityLimiter;
        private double biomassAddedThisYear;
        private double biomassConsumed;

        /// <inheritdoc/>
        [JsonIgnore]
        public ResourceRequest CurrentGrazingRequest { get; set; } = null;

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
            return GutFillLowQuality + ((dmd - MinimumDMD) / (GreenDMD - MinimumDMD)) * (GutFillHighQuality - GutFillLowQuality);
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public double OverallPastureBiomass { get; private set; }

        /// <summary>
        /// Coefficient to adjust intake for tropical herbage quality
        /// </summary>
        [Category("Advanced", "Intake")]
        [Description("Coefficient to adjust intake for tropical herbage quality")]
        [Required]
        public double IntakeTropicalQualityCoefficient { get; set; } = 0.16;

        /// <summary>
        /// Coefficient to adjust intake for herbage quality
        /// </summary>
        [Category("Advanced", "Intake")]
        [Description("Coefficient to adjust intake for herbage quality")]
        [Required]
        public double IntakeQualityCoefficient { get; set; } = 1.7;

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
                return Pools.Where(a => (index < 12) ? a.Age == index : a.Age >= 12);
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

        /// <summary>
        /// Calculated total pasture (all pools) Dry Matter Digestibility (%)
        /// </summary>
        public double SwardDryMatterDigestibility
        {
            get
            {
                double dmd = 0;
                double amount = AmountAvailable;
                if (amount > 0)
                {
                    dmd = Pools.Sum(a => a.AmountAvailable * a.DryMatterDigestibility) / amount;
                }

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
                double n = 0;
                double amount = AmountAvailable;
                if (amount > 0)
                {
                    n = Pools.Sum(a => a.AmountAvailable * a.NitrogenPercent) / amount;
                }

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
                        valueToUse = Pools.Sum(a => a.AmountAvailable);
                    }
                    else
                    {
                        valueToUse = Pool(age, true).Sum(a => a.AmountAvailable);
                    }

                    break;
                case "Growth":
                    valueToUse = Pool(0, true).Sum(a => a.Growth);
                    break;
                case "Consumed":
                    if (age < 0)
                    {
                        valueToUse = Pools.Sum(a => a.Consumed);
                    }
                    else
                    {
                        valueToUse = Pool(age, true).Sum(a => a.Consumed);
                    }

                    break;
                case "Detached":
                    if (age < 0)
                    {
                        valueToUse = Pools.Sum(a => a.Detached);
                    }
                    else
                    {
                        valueToUse = Pool(age, true).Sum(a => a.Detached);
                    }

                    break;
                case "Nitrogen":
                    if (age < 0)
                    {
                        return SwardNitrogenPercent;
                    }
                    else
                    {
                        IEnumerable<GrazeFoodStorePool> pools = Pool(age, true);
                        if (pools.Count() == 1)
                        {
                            valueToUse = pools.FirstOrDefault().NitrogenPercent;
                        }
                        else
                        {
                            valueToUse = pools.Sum(a => a.NitrogenPercent * a.AmountAvailable) / pools.Sum(a => a.AmountAvailable);
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
                        IEnumerable<GrazeFoodStorePool> pools = Pool(age, true);
                        if (pools.Count() == 1)
                        {
                            valueToUse = pools.FirstOrDefault().DryMatterDigestibility;
                        }
                        else
                        {
                            valueToUse = pools.Sum(a => a.DryMatterDigestibility * a.AmountAvailable) / pools.Sum(a => a.AmountAvailable);
                        }
                    }
                    return valueToUse;
                case "Age":
                    if (age < 0)
                    {
                        return Pools.Sum(a => a.AmountAvailable * a.Age) / this.AmountAvailable;
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
            // detach and carryover detach are monthly so divide by 30.4 to daily and apply for time-step
            if (DetachRate <= 1 | CarryoverDetachRate <= 1)
            {
                double detached = 0;
                foreach (var pool in Pools)
                {
                    if (pool.AmountPending > 0)
                    {
                        throw new ApsimXException(this, "Core logic error: Cannot detach pasture as there is pending growth or grazing. Check timers of managing activities to ensure they run after detachment");
                    }

                    double detach = Math.Min(1.0, DetachRate / 30.4 * events.Interval);
                    if (pool.Age >= 12)
                    {
                        detach = CarryoverDetachRate / 30.4 * events.Interval;
                    }
                    detached += pool.Detach(detach);
                }


                if (detached > 0)
                {
                    base.RemoveFromResource(detached, null);
                    ReportTransaction(TransactionType.Loss, detached, null, null, "Detached", this);
                }
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
            // Nitrogen and DMD are monthly so divide by 30.4 to daily and apply for time-step
            if (DecayNitrogen != 0 | (DecayDMD > 0 && DMDStyle == DryMatterDigestibilityStyle.SpecifyNewGrowthDMD))
            {
                // decay N and DMD of pools and age by 1 month
                foreach (var pool in Pools)
                {
                    // N is a loss of N% (x = x -loss)
                    pool.NitrogenPercent = Math.Max(pool.NitrogenPercent - (DecayNitrogen / 30.4 * events.Interval), MinimumNitrogen);

                    if (DMDStyle == DryMatterDigestibilityStyle.SpecifyNewGrowthDMD)
                    {
                        // DMD is a proportional loss (x = x*(1-proploss))
                        pool.DryMatterDigestibility = Math.Max(pool.DryMatterDigestibility * (1 - (DecayDMD / 30.4 * events.Interval)), MinimumDMD);
                    }

                    int age = Convert.ToInt32((events.Clock.Today - pool.GrowthDate).TotalDays / 30.4);
                    pool.Age = age;
                }
                // remove all pools with less than 1g of food
                Pools.RemoveAll(a => a.Amount < 0.001);
            }

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
        private void ONCLEMPastureReady(object sender, EventArgs e)
        {
            // do not return zero as there is always something there and zero affects calculations.
            base.Set(Pools.Sum(a => a.Amount));

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
                    GrazeFoodStorePool newPool = new (0, this)
                    {
                        GrossEnergyContent = this.GrossEnergyContent,
                        MetabolisableEnergyContent = this.MetabolisableEnergyContent,
                        FatPercent = this.FatPercent,
                        GrowthDate = growDate,
                        Age = monthCount,
                        StartingAmount = propBiomass
                    };
                    newPool.RumenDegradableProteinPercent = this.RumenDegradableProteinPercent;
                    newPool.NitrogenPercent = currentN;
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
                    if (newPools.Where(a => a.Age == 0).FirstOrDefault() is GrazeFoodStorePool thisMonth)
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
                    reason = "Initialise pool " + pool.Age.ToString();
                }

                AddToResource(pool, null, null, reason);
            }
        }

        ///// <inheritdoc/>
        //public void ApplyDailyIntakeReduction(double fractionReduced)
        //{
        //}

        #region transactions

        /// <summary>
        /// Graze food add method. This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="resourceAmount">
        /// Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being
        /// added
        /// </param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void AddToResource(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
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
                case GrazeFoodStorePool _:
                    // coming from the advanced PastureActivityManage
                    GrazeFoodStorePool incomingPool = resourceAmount as GrazeFoodStorePool;
                    // adjust N content only if new growth (age = 0) based on yield limits and month range defined in GrazeFoodStoreFertilityLimiter if present
                    if (incomingPool.Age == 0 && !(grazeFoodStoreFertilityLimiter is null))
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
                    pool.Age = incomingPool.Age;
                    pool.InitialBiomassSet(incomingPool.Amount);
                    pool.GrowthDate = incomingPool.GrowthDate;
                    break;
                case FoodResourcePacket _:
                    // coming from the CropActivityManage
                    FoodResourcePacket packet = resourceAmount as FoodResourcePacket;
                    pool.InitialBiomassSet(packet.Amount);
                    pool.NitrogenPercent = packet.NitrogenPercent;
                    pool.DryMatterDigestibility = packet.DryMatterDigestibility;
                    break;
                case double _:
                    // add amount at current rates
                    pool.InitialBiomassSet((double)resourceAmount);
                    pool.NitrogenPercent = this.SwardNitrogenPercent;
                    pool.DryMatterDigestibility = SwardDryMatterDigestibility; //this.EstimateDMD(this.Nitrogen);
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
        /// reduce the amount total until available until the transaction is completed.
        /// </param>
        /// <returns>Amount removed</returns>
        protected double Remove(double amountToRemove, ResourceRequest pendingRequest)
        {
            amountToRemove = base.RemoveFromResource(amountToRemove, pendingRequest);

            // add pending amount to each pool
            if (pendingRequest.AdditionalDetails is IEnumerable<FoodResourceStore> foodStores)
            {
                foreach (var foodStore in foodStores)
                {
                    for (int i = 0; i < foodStore.Pools.Count; i++)
                    {
                        foodStore.Pools[i].AmountPending += foodStore.Details.Amount * foodStore.PoolProportions[i];
                    }
                }
            }
            return amountToRemove;
        }

        /// <inheritdoc/>
        public new void DecreasePending(ResourceRequest request, double amount)
        {
            if (request.AdditionalDetails is FoodResourceStore foodStore)
            {
                amount = foodStore.NumberOfDaysInTimestep;
                for (int i = 0; i < foodStore.Pools.Count; i++)
                {
                    Pools[i].ReducePending(amount * foodStore.PoolProportions[i]);
                }
            }
            // do removal from pending
            base.DecreasePending(request, amount);
        }

        ///// <summary>
        ///// Simple remove resource by specified amount
        ///// </summary>
        ///// <param name="removeAmount">Amount to remove</param>
        ///// <param name="activityName">Activity requesting resource</param>
        ///// <param name="reason">Label representing reason for removal</param>
        //public double Remove(double removeAmount, string activityName, string reason)
        //{
        //    throw new NotImplementedException();
        //}

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
                        request.Provided = amountCleared;
                        // use generic removal to handle pending and reporting transaction if needed 
                        base.RemoveFromResource(request);
                        //ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
                    }

                    break;
                default:
                    // Need to add new section here to allow non grazing activity to remove resources from pasture.
                    throw new Exception("Removing resources from GrazeFoodStore can only be performed by a grazing, burning and cut and carry activities at this stage");
            }




            //// handles grazing by breed from this pasture pools based on breed pool limits

            //if (request.AdditionalDetails != null && request.AdditionalDetails is RuminantActivityGrazePastureHerd grazingActivity)
            //{
            //    // All pasture selection and quality adjustments should have been made in the grazing activity and the request should be for the final amount to take from the pasture.
            //    // all Pasture Pools should have had the consumed pasture taken.

            //    // handles pending and reporting transaction if needed 
            //    base.Remove(request);




            //    if (request.TransactionPending)
            //        return;

            //    //if graze activity
            //    biomassConsumed += request.Provided;

            //    // report
            //    ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
            //}
            //else if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(PastureActivityCutAndCarry))
            //{
            //    // take from pools by cut and carry
            //    double amountRequired = request.Required;
            //    double amountCollected = 0;
            //    double dryMatterDigestibility = 0;
            //    double nitrogen = 0;

            //    // take proportionally from all pools.
            //    double useproportion = Math.Min(1.0, amountRequired / Pools.Sum(a => a.AmountAvailable));
            //    // if less than pools then take required as proportion of pools
            //    foreach (GrazeFoodStorePool pool in Pools)
            //    {
            //        double amountToRemove = pool.AmountAvailable * useproportion;
            //        amountCollected += amountToRemove;
            //        dryMatterDigestibility += pool.DryMatterDigestibility * amountToRemove;
            //        nitrogen += pool.NitrogenPercent * amountToRemove;
            //        pool.Remove(amountToRemove); // "Cut and carry"
            //    }
            //    request.Provided = amountCollected;

            //    // adjust DMD and N of biomass consumed
            //    dryMatterDigestibility /= request.Provided;
            //    nitrogen /= request.Provided;

            //    // report
            //    ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
            //}
            //else if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(PastureActivityBurn))
            //{
            //    // take from pools by burning
            //    double amountRequired = request.Required;
            //    double amountBurned = 0;
            //    double dryMatterDigestibility = 0;
            //    double nitrogen = 0;

            //    // take proportionally from all pools.
            //    double useproportion = Math.Min(1.0, amountRequired / Pools.Sum(a => a.AmountAvailable));
            //    // if less than pools then take required as proportion of pools
            //    foreach (GrazeFoodStorePool pool in Pools)
            //    {
            //        double amountToRemove = pool.AmountAvailable * useproportion;
            //        amountBurned += amountToRemove;
            //        dryMatterDigestibility += pool.DryMatterDigestibility * amountToRemove;
            //        nitrogen += pool.NitrogenPercent * amountToRemove;
            //        pool.Remove(amountToRemove); // "Burned"
            //    }
            //    request.Provided = amountBurned;

            //    // adjust DMD and N of biomass consumed
            //    dryMatterDigestibility /= request.Provided;
            //    nitrogen /= request.Provided;

            //    // report
            //    ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
            //}
            //else if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(CropActivityManageProduct))
            //{
            //    // this occurs when the pasture is being replaced by the provided biomass and clears the stores
            //    if (request.Category == "StoreCleared")
            //    {
            //        double amountCleared = Pools.Sum(a => a.AmountAvailable);
            //        if (amountCleared == 0)
            //        {
            //            return;
            //        }
            //        Pools.Clear();
            //        request.Provided = amountCleared;
            //        // report
            //        ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
            //    }
            //}
            //else
            //{
            //    // Need to add new section here to allow non grazing activity to remove resources from pasture.
            //    throw new Exception("Removing resources from GrazeFoodStore can only be performed by a grazing, burning and cut and carry activities at this stage");
            //}
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
                        provided += foodStore.Pools[i].AmountPending;
                        biomassConsumed += foodStore.Pools[i].AmountPending;
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
                yield return new ValidationResult($"There must be at least one month differnece between the first month [{FirstMonthOfGrowSeason}] and the last month [{LastMonthOfGrowSeason}] of the growth season specified to calculate the initial biomass in [r={NameWithParent}]", new string[] { "Invalid initial biomass growth season" });
            }
        }
        #endregion
    }

}