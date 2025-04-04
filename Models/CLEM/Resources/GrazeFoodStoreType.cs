using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    public class GrazeFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFeed, IValidatableObject
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private IPastureManager manager;
        private GrazeFoodStoreFertilityLimiter grazeFoodStoreFertilityLimiter;
        private double biomassAddedThisYear;
        private double biomassConsumed;

        /// <inheritdoc/>
        [Description("Units (nominal)")]
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
        public FeedType TypeOfFeed { get; set; } = FeedType.PastureTropical;

        /// <inheritdoc/>
        [Description("Gross energy content")]
        [Category("Pasture", "Quality")]
        [Units("MJ/kg digestible DM")]
        [Required]
        public double GrossEnergyContent { get; set; } = 18.4;

        /// <inheritdoc/>
        [Required, GreaterThanEqualValue(0)]
        [Category("Pasture", "Quality")]
        [Description("Metabolisable energy content")]
        [Units("MJ/kg DM")]
        public double MetabolisableEnergyContent { get; set; }

        private double rumenDegradableProteinPercent;

        /// <inheritdoc/>
        [Required, Percentage, GreaterThanEqualValue(0)]
        [Category("Pasture", "Quality")]
        [Description("Degradable protein percent (%, g/g CP * 100)")]
        public double RumenDegradableProteinPercent
        {
            get
            {
                return rumenDegradableProteinPercent;
            }
            set
            {
                rumenDegradableProteinPercent = value;
                AcidDetergentInsoluableProtein = FoodResourcePacket.CalculateAcidDetergentInsoluableProtein(rumenDegradableProteinPercent, TypeOfFeed);
            }
        }

        /// <inheritdoc/>
        public double AcidDetergentInsoluableProtein { get; set; }

        /// <inheritdoc/>
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double NitrogenPercent { get; set; }

        /// <inheritdoc/>
        public double CrudeProteinPercent { get; set; }

        /// <inheritdoc/>
        public double FatPercent { get; set; } = 0;

        /// <inheritdoc/>
        [JsonIgnore]
        public double OverallPastureBiomass { get; set; }

        //ToDo: NOT SURE THIS IS USED!
        /// <inheritdoc/>
        [Category("Pasture", "Quality")]
        [Description("Crude protein degradability")]
        public double CPDegradability { get; set; } 

        /// <summary>
        /// Coefficient to convert initial N% to DMD%
        /// </summary>
        [Category("Pasture", "Quality")]
        [Description("Coefficient to convert initial N% to DMD%")]
        [Required]
        public double NToDMDCoefficient { get; set; }

        /// <summary>
        /// Intercept to convert initial N% to DMD%
        /// </summary>
        [Category("Pasture", "Quality")]
        [Description("Intercept to convert initial N% to DMD%")]
        [Required]
        public double NToDMDIntercept { get; set; }

        /// <summary>
        /// Nitrogen of new growth (%)
        /// </summary>
        [Category("Farm", "Quality")]
        [Description("Percent nitrogen of new growth")]
        [Units("%")]
        [Required, Percentage]
        public double GreenNitrogenPercent { get; set; }

        /// <summary>
        /// Proportion Nitrogen loss each month from pools
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Loss of Nitrogen percent from pools (note: amount as %N not proportion)")]
        [Required, GreaterThanEqualValue(0), Percentage]
        [Units("%")]
        public double DecayNitrogen { get; set; }

        /// <summary>
        /// Minimum Nitrogen %
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Minimum nitrogen")]
        [Required, Percentage]
        [Units("%")]
        public double MinimumNitrogen { get; set; }

        /// <summary>
        /// Proportion Dry Matter Digestibility loss each month from pools
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Proportion DMD loss each month from pools")]
        [Required, Proportion]
        public double DecayDMD { get; set; }

        /// <summary>
        /// Minimum Dry Matter Digestibility (%)
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Minimum Dry Matter Digestibility")]
        [Required, Percentage]
        [Units("%")]
        public double MinimumDMD { get; set; }

        /// <summary>
        /// Monthly detachment rate
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Detachment rate")]
        [Required, Proportion]
        public double DetachRate { get; set; }

        /// <summary>
        /// Detachment rate of 12 month or older plants
        /// </summary>
        [Category("Farm", "Decay")]
        [Description("Carryover detachment rate")]
        [Required, Proportion]
        public double CarryoverDetachRate { get; set; }

        /// <summary>
        /// Coefficient to adjust intake for tropical herbage quality
        /// </summary>
        [Category("Advanced", "Quality")]
        [Description("Coefficient to adjust intake for tropical herbage quality")]
        [Required]
        public double IntakeTropicalQualityCoefficient { get; set; }

        /// <summary>
        /// Coefficient to adjust intake for herbage quality
        /// </summary>
        [Category("Advanced", "Quality")]
        [Description("Coefficient to adjust intake for herbage quality")]
        [Required]
        public double IntakeQualityCoefficient { get; set; }

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
        [System.ComponentModel.DefaultValueAttribute(3)]
        [Required, Month]
        public MonthsOfYear LastMonthOfGrowSeason { get; set; }

        /// <summary>
        /// Number of months for initial biomass
        /// </summary>
        [Category("Farm", "Initial biomass")]
        [Description("Number of months for initial biomass")]
        [System.ComponentModel.DefaultValueAttribute(5)]
        public int NumberMonthsForInitialBiomass { get; set; }

        /// <summary>
        /// List of pools available
        /// </summary>
        [JsonIgnore]
        public List<GrazeFoodStorePool> Pools = new();

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
                        Summary.WriteMessage(this, $"Each [r=GrazeStoreType] can only be managed by a single activity.{Environment.NewLine}Two managing activities (a=[{(manager as CLEMModel).NameWithParent}] and [a={(value as CLEMModel).NameWithParent}]) are trying to manage [r={this.NameWithParent}]. Ensure the [CropActivityManageProduct] children have timers that prevent them running in the same time-step", MessageType.Warning);
                    else
                        throw new ApsimXException(this, $"Each [r=GrazeStoreType] can only be managed by a single activity.{Environment.NewLine}Two managing activities (a=[{(manager as CLEMModel).NameWithParent}] and [a={(value as CLEMModel).NameWithParent}]) are trying to manage [r={this.NameWithParent}]. Ensure they hvae timers");
                }
                manager = value;
            }
        }

        /// <summary>
        /// Return the specified pool
        /// </summary>
        /// <param name="index">index to use</param>
        /// <param name="getByAge">return where index is age</param>
        /// <returns>GraxeFoodStore pool</returns>
        public IEnumerable<GrazeFoodStorePool> Pool(int index, bool getByAge)
        {
            if (getByAge)
            {
                return Pools.Where(a => (index < 12) ? a.Age == index : a.Age >= 12);
            }
            else
            {
                if (index < Pools.Count)
                    return new List<GrazeFoodStorePool> { Pools.ElementAt(index) };
            }
            return null;
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
            }
        }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        [JsonIgnore]
        public double Amount
        {
            get
            {
                return Pools.Sum(a => a.Amount);
            }
        }

        /// <summary>
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa
        {
            get
            {
                if (Manager != null)
                    return Amount / Manager.Area;
                return 0;
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
                if (Manager != null)
                    return KilogramsPerHa / 1000.0;
                else
                    return 0;
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
                    return (biomassConsumed > 0) ? 100 : 0;

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
                if (this.Amount > 0)
                    dmd = Pools.Sum(a => a.Amount * a.DryMatterDigestibility) / this.Amount;

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
                if (this.Amount > 0)
                    n = Pools.Sum(a => a.Amount * a.NitrogenPercent) / this.Amount;

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
                return 0;

            double convert = (tonnes ? 1000 : 1) * (hectares ? Manager.Area : 1);
            double valueToUse = 0;
            switch (grazeProperty)
            {
                case "Amount":
                    if (age < 0)
                        valueToUse = Pools.Sum(a => a.Amount);
                    else
                        valueToUse = Pool(age, true).Sum(a => a.Amount);
                    break;
                case "Growth":
                    valueToUse = Pool(0, true).Sum(a => a.Growth);
                    break;
                case "Consumed":
                    if (age < 0)
                        valueToUse = Pools.Sum(a => a.Consumed);
                    else
                        valueToUse = Pool(age, true).Sum(a => a.Consumed);
                    break;
                case "Detached":
                    if (age < 0)
                        valueToUse = Pools.Sum(a => a.Detached);
                    else
                        valueToUse = Pool(age, true).Sum(a => a.Detached);
                    break;
                case "Nitrogen":
                    if (age < 0)
                        return SwardNitrogenPercent;
                    else
                    {
                        IEnumerable<GrazeFoodStorePool> pools = Pool(age, true);
                        if(pools.Count() == 1)
                            valueToUse = pools.FirstOrDefault().NitrogenPercent;
                        else
                            valueToUse = pools.Sum(a => a.NitrogenPercent * a.Amount) / pools.Sum(a => a.Amount);
                    }
                    return valueToUse;
                case "DMD":
                    if (age < 0)
                        return SwardDryMatterDigestibility;
                    else
                    {
                        IEnumerable<GrazeFoodStorePool> pools = Pool(age, true);
                        if (pools.Count() == 1)
                            valueToUse = pools.FirstOrDefault().DryMatterDigestibility;
                        else
                            valueToUse = pools.Sum(a => a.DryMatterDigestibility * a.Amount) / pools.Sum(a => a.Amount);
                    }
                    return valueToUse;
                case "Age":
                    if (age < 0)
                        return Pools.Sum(a => a.Amount * a.Age) / this.Amount;
                    return valueToUse;
                default:
                    throw new ApsimXException(this, $"Property [{grazeProperty}] not available for reporting pools");
            }
            // convert biomass to units specified kg,tonnes & farm,per/hectare
            return valueToUse / convert;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GrazeFoodStoreType()
        {
            SetDefaults();
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
            AcidDetergentInsoluableProtein = FoodResourcePacket.CalculateAcidDetergentInsoluableProtein(RumenDegradableProteinPercent, TypeOfFeed);
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
            grazeFoodStoreFertilityLimiter = FindAllChildren<GrazeFoodStoreFertilityLimiter>().FirstOrDefault();
        }

        /// <summary>An event handler to allow us to make checks after resources and activities initialised.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("FinalInitialise")]
        private void OnFinalInitialise(object sender, EventArgs e)
        {
            if (Manager == null)
                Summary.WriteMessage(this, $"There is no activity managing [r={Name}]. This resource cannot be used and will have no growth.{Environment.NewLine}To manage [r={Name}] include a [a=CropActivityManage]+[a=CropActivityManageProduct] or a [a=PastureActivityManage] depending on your external data type.", MessageType.Warning);
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
                pool.Reset();
        }

        /// <summary>
        /// Function to detach pasture before reporting
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMDetachPasture")]
        private void OnCLEMDetachPasture(object sender, EventArgs e)
        {
            if (DetachRate < 1 | CarryoverDetachRate < 1)
            {
                foreach (var pool in Pools)
                {
                    double detach = CarryoverDetachRate;
                    if (pool.Age < 12)
                        detach = DetachRate;
                    double amountRemaining = pool.Amount * (1 - detach);
                    pool.Detached = pool.Amount * detach;
                    pool.Set(amountRemaining);
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
            if (DecayNitrogen != 0 | DecayDMD > 0)
            {
                // decay N and DMD of pools and age by 1 month
                foreach (var pool in Pools)
                {
                    // N is a loss of N% (x = x -loss)
                    pool.NitrogenPercent = Math.Max(pool.NitrogenPercent - DecayNitrogen, MinimumNitrogen);
                    // DMD is a proportional loss (x = x*(1-proploss))
                    pool.DryMatterDigestibility = Math.Max(pool.DryMatterDigestibility * (1 - DecayDMD), MinimumDMD);

                    if (pool.Age < 12)
                        pool.Age++;
                }
                // remove all pools with less than 10g of food
                Pools.RemoveAll(a => a.Amount < 0.01);
            }

            if (events.IsEcologicalIndicatorsCalculationMonth())
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
        /// A method to initialise initial pasture  biomass across pools
        /// </summary>
        /// <param name="area">Area of pasture (ha)</param>
        /// <param name="firstMonthsGrowth">The growth (kg per ha) expected in the first month for accuracy</param>
        public void SetupStartingPasturePools(double area, double firstMonthsGrowth)
        {

            if (area <= 0) return;
            if (NumberMonthsForInitialBiomass <= 0) return;

            // Initial biomass
            double amountToAdd = area * StartingAmount;
            if (amountToAdd <= 0)
                return;

            // Set up pasture pools to start run based on month and user defined pasture properties
            // Locates the previous five months where growth occurred (Nov-Mar) and applies decomposition to current month
            // This months growth will not be included.

            int month = events.Clock.Today.Month;
            int monthCount = 0;
            int includedMonthCount = 0;
            double propBiomass = 1.0;
            double currentN = GreenNitrogenPercent;
            // NABSA changes N by 0.8 for particular months. Not needed here as decay included.
            double currentDMD = currentN * NToDMDCoefficient + NToDMDIntercept;
            currentDMD = Math.Max(MinimumDMD, currentDMD);
            Pools.Clear();

            List<GrazeFoodStorePool> newPools = new List<GrazeFoodStorePool>();

            // number of previous growth months to consider. default should be 5
            int growMonthHistory = NumberMonthsForInitialBiomass;

            while (includedMonthCount < growMonthHistory)
            {
                // start month before start of simulation.
                monthCount++;
                month--;
                currentN -= DecayNitrogen;
                currentN = Math.Max(currentN, MinimumNitrogen);
                currentDMD *= 1 - DecayDMD;
                currentDMD = Math.Max(currentDMD, MinimumDMD);

                if (month == 0)
                    month = 12;

                bool insideGrowthWindow = false;
                int first = (int)FirstMonthOfGrowSeason;
                int last = (int)LastMonthOfGrowSeason;

                if (first < last)
                    insideGrowthWindow = (month >= first & month <= last);
                else
                    insideGrowthWindow = (month >= first | month <= last);

                if (insideGrowthWindow) // (month <= 3 | month >= 11)
                {
                    // add new pool
                    newPools.Add(new GrazeFoodStorePool()
                    {
                        Age = monthCount,
                        NitrogenPercent = currentN,
                        DryMatterDigestibility = currentDMD,
                        StartingAmount = propBiomass
                    });
                    includedMonthCount++;
                }
                propBiomass *= 1 - DetachRate;
            }

            // assign pasture biomass to pools based on proportion of total
            double total = newPools.Sum(a => a.StartingAmount);
            foreach (var pool in newPools)
                pool.Set(amountToAdd * (pool.StartingAmount / total));

            // Previously: remove this months growth from pool age 0 to keep biomass at approximately setup.
            // But as updates happen at the end of the month, the first month's biomass is never added so stay with 0 or delete following section
            // Get this months growth
            // Get this months pasture data from the pasture data list
            if (firstMonthsGrowth > 0)
            {
                double thisMonthsGrowth = firstMonthsGrowth * area;
                if (thisMonthsGrowth > 0)
                    if (newPools.Where(a => a.Age == 0).FirstOrDefault() is GrazeFoodStorePool thisMonth)
                        thisMonth.Set(Math.Max(0, thisMonth.Amount - thisMonthsGrowth));
            }

            // Add to pasture. This will add pool to pasture available store.
            foreach (var pool in newPools)
            {
                string reason = "Initialise";
                if (newPools.Any())
                    reason = "Initialise pool " + pool.Age.ToString();

                Add(pool, null, null, reason);
            }
        }

        #region transactions

        /// <summary>
        /// Graze food add method.
        /// This style is not supported in GrazeFoodStoreType
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            GrazeFoodStorePool pool = new()
            {
                MetabolisableEnergyContent = MetabolisableEnergyContent,
                CPDegradability = CPDegradability,
                FatPercent = FatPercent,
                NitrogenPercent = 0,
                DryMatterDigestibility = 0
            };

            switch (resourceAmount)
            {
                case GrazeFoodStorePool _:
                    // coming from the advanced PastureActivityManage
                    GrazeFoodStorePool incomingPool = resourceAmount as GrazeFoodStorePool;
                    // adjust N content only if new growth (age = 0) based on yield limits and month range defined in GrazeFoodStoreFertilityLimiter if present
                    if (incomingPool.Age == 0 && !(grazeFoodStoreFertilityLimiter is null))
                        pool.NitrogenPercent = Math.Max(MinimumNitrogen, incomingPool.NitrogenPercent * grazeFoodStoreFertilityLimiter.GetProportionNitrogenLimited(incomingPool.Amount / Manager.Area));
                    break;
                case FoodResourcePacket _:
                    // coming from the CropActivityManage
                    FoodResourcePacket packet = resourceAmount as FoodResourcePacket;
                    pool.Set(packet.Amount);
                    pool.NitrogenPercent = packet.NitrogenPercent;
                    pool.DryMatterDigestibility = packet.DryMatterDigestibility;
                    break;
                case double _:
                    // add amount at current rates
                    pool.Set((double)resourceAmount);
                    pool.NitrogenPercent = this.SwardNitrogenPercent;
                    pool.DryMatterDigestibility = SwardDryMatterDigestibility; //this.EstimateDMD(this.Nitrogen);
                    break;
                default:
                    throw new Exception($"ResourceAmount object of type [{resourceAmount.GetType().Name}] is not supported in [r={Name}]");
            }

            if (pool.Amount > 0)
            {
                if (pool.Age == 0)
                    pool.Growth = pool.Amount;

                // allow decaying or no pools currently available
                if (PastureDecays || Pools.Count == 0)
                    Pools.Insert(0, pool);
                else
                    Pools[0].Add(pool);

                // update biomass available
                if (!category.StartsWith("Initialise"))
                    // do not update if this is ian initialisation pool
                    biomassAddedThisYear += pool.Amount;

                ReportTransaction(TransactionType.Gain, pool.Amount, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="removeAmount"></param>
        /// <param name="activityName"></param>
        /// <param name="reason"></param>
        public double Remove(double removeAmount, string activityName, string reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        public new void Remove(ResourceRequest request)
        {
            // handles grazing by breed from this pasture pools based on breed pool limits

            if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(RuminantActivityGrazePastureHerd))
            {
                RuminantActivityGrazePastureHerd thisBreed = request.AdditionalDetails as RuminantActivityGrazePastureHerd;

                // take from pools as specified for the breed
                double amountRequired = request.Required;
                thisBreed.DMD = 0;
                thisBreed.N = 0;

                // first take from pools
                foreach (GrazeBreedPoolLimit pool in thisBreed.PoolFeedLimits)
                {
                    // take min of amount in pool, intake*limiter, remaining intake needed
                    double amountToRemove = Math.Min(request.Required * pool.Limit, Math.Min(pool.Pool.Amount, amountRequired));
                    // update DMD and N based on pool utilised
                    thisBreed.DMD += pool.Pool.DryMatterDigestibility * amountToRemove;
                    thisBreed.N += pool.Pool.NitrogenPercent * amountToRemove;

                    amountRequired -= amountToRemove;

                    // remove resource from pool
                    pool.Pool.Remove(amountToRemove, thisBreed, "Graze");

                    if (amountRequired <= 0)
                        break;
                }

                // if forage still limiting and second take allowed (enforce strict limits is false)
                if (amountRequired > 0 & !thisBreed.RuminantTypeModel.Parameters.Grazing.StrictFeedingLimits)
                {
                    // allow second take for the limited pools
                    double forage = thisBreed.PoolFeedLimits.Sum(a => a.Pool.Amount);

                    // this will only be the previously limited pools
                    double amountTakenDuringSecondTake = 0;
                    foreach (GrazeBreedPoolLimit pool in thisBreed.PoolFeedLimits.Where(a => a.Limit < 1))
                    {
                        //if still not enough take all
                        double amountToRemove = 0;
                        if (amountRequired >= forage)
                            // take as a proportion of the pool to total forage remaining
                            amountToRemove = pool.Pool.Amount / forage * amountRequired;
                        else
                            amountToRemove = pool.Pool.Amount;

                        // update DMD and N based on pool utilised
                        thisBreed.DMD += pool.Pool.DryMatterDigestibility * amountToRemove;
                        thisBreed.N += pool.Pool.NitrogenPercent * amountToRemove;
                        amountTakenDuringSecondTake += amountToRemove;
                        // remove resource from pool
                        pool.Pool.Remove(amountToRemove, thisBreed, "Graze");
                    }
                    amountRequired -= amountTakenDuringSecondTake;
                }

                request.Provided = request.Required - amountRequired;

                // adjust DMD and N of biomass consumed
                thisBreed.DMD /= request.Provided;
                thisBreed.N /= request.Provided;

                //if graze activity
                biomassConsumed += request.Provided;

                // report

                ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
            }
            else if (request.AdditionalDetails != null && request.AdditionalDetails.GetType() == typeof(PastureActivityCutAndCarry))
            {
                // take from pools by cut and carry
                double amountRequired = request.Required;
                double amountCollected = 0;
                double dryMatterDigestibility = 0;
                double nitrogen = 0;

                // take proportionally from all pools.
                double useproportion = Math.Min(1.0, amountRequired / Pools.Sum(a => a.Amount));
                // if less than pools then take required as proportion of pools
                foreach (GrazeFoodStorePool pool in Pools)
                {
                    double amountToRemove = pool.Amount * useproportion;
                    amountCollected += amountToRemove;
                    dryMatterDigestibility += pool.DryMatterDigestibility * amountToRemove;
                    nitrogen += pool.NitrogenPercent * amountToRemove;
                    pool.Remove(amountToRemove, this, "Cut and Carry");
                }
                request.Provided = amountCollected;

                // adjust DMD and N of biomass consumed
                dryMatterDigestibility /= request.Provided;
                nitrogen /= request.Provided;

                // report
                ReportTransaction(TransactionType.Loss, request.Provided, request.ActivityModel, request.RelatesToResource, request.Category, this);
            }
            else
            {
                // Need to add new section here to allow non grazing activity to remove resources from pasture.
                throw new Exception("Removing resources from native food store can only be performed by a grazing and cut and carry activities at this stage");
            }
        }

        /// <summary>
        ///
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
                noGrowSeason = (last - first <= 1);
            else
                noGrowSeason = ((12 - first) + last <= 1);

            if (StartingAmount > 0 & noGrowSeason)
            {
                yield return new ValidationResult($"There must be at least one month differnece between the first month [{FirstMonthOfGrowSeason}] and the last month [{LastMonthOfGrowSeason}] of the growth season specified to calculate the initial biomass in [r={NameWithParent}]", new string[] { "Invalid initial biomass growth season" });
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new StringWriter();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("This pasture has an initial green nitrogen content of ");
            if (this.GreenNitrogenPercent == 0)
                htmlWriter.Write("<span class=\"errorlink\">Not set</span>%");
            else
                htmlWriter.Write($"<span class=\"setvalue\">{GreenNitrogenPercent:0.###}%</span>");

            if (DecayNitrogen > 0)
                htmlWriter.Write($" and will decline by <span class=\"setvalue\">{DecayNitrogen:0.###}%</span> per month to a minimum nitrogen of <span class=\"setvalue\">{MinimumNitrogen:0.###}%</span>");

            htmlWriter.Write("\r\n</div>");
            if (DecayDMD > 0)
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Dry Matter Digestibility will decay at a rate of <span class=\"setvalue\">{DecayDMD:0.###}</span> per month to a minimum DMD of <span class=\"setvalue\">{MinimumDMD:0.###}%</span>");
                htmlWriter.Write("\r\n</div>");
            }
            if (DetachRate > 0)
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Pasture is lost through detachment at a rate of <span class=\"setvalue\">{DetachRate:0.###}</span> per month");
                if (CarryoverDetachRate > 0)
                    htmlWriter.Write($" and <span class=\"setvalue\">{CarryoverDetachRate:0.###}</span> per month after 12 months");

                htmlWriter.Write("\r\n</div>");
            }
            else
            {
                if (CarryoverDetachRate > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"Pasture is lost through detachement at a rate of <span class=\"setvalue\">{CarryoverDetachRate:0.###}</span> per month after 12 months");
                    htmlWriter.Write("\r\n</div>");
                }
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }
        #endregion

    }

}