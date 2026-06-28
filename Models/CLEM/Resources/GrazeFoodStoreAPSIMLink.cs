using APSIM.Shared.Extensions.Collections;
using Models.AgPasture;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.ForageDigestibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a GrazeFoodType that links directly to an APSIM paddock containing forages for
    /// grazing
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(GrazeFoodStore))]
    [Description("This resource represents a link to an APSIM paddock with forages")]
    [HelpUri(@"Content/Features/Resources/Graze food store/GrazeFoodStoreAPSIMLink.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrazing) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class GrazeFoodStoreAPSIMLink : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IFeed, IGrazeFoodStoreType, IValidatableObject
    {
        private const double gm2TokgHa = 10.0;
        private double biomassAddedThisYear;
        private double biomassConsumed;
        private List<ModelWithDigestibleBiomass> forageModels = [];
        private Forages forages;
        private Zone paddock;
        private SimpleGrazing simpleGrazing;
        private List<FoodResourceStore> intakeStoreRequests = [];
        private double consumableWt = 0;
        private double totalWt = 0;

        /// <inheritdoc/>
        [Description("Type of pasture or forage")]
        [Category("Farm", "Paddock")]
        [Required]
        public FeedType TypeOfFeed { get; set; } = FeedType.PastureTemperate;

        /// <inheritdoc/>
        [Description("Name of APSIM paddock")]
        [Category("Farm", "Paddock")]
        [Required]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetModelsAvailableByType", ValuesArgs = new object[] { new Type[] { typeof(Zone) } })]
        public string PaddockName { get; set; }

        /// <inheritdoc/>
        [Description("Gross energy content (MJ/kg DM)")]
        [Category("Farm", "Quality")]
        [Units("MJ/kg digestible DM")]
        [Required, GreaterThanValue(0)]
        public double GrossEnergyContent { get; set; } = 18.4;

        private double rumenDegradableProteinPercent = 58;

        /// <summary>
        /// Highest expected sward Dry Matter Digestibility (%)
        /// </summary>
        [Category("Farm", "Gut fill")]
        [Description("Highest Dry Matter Digestibility expected")]
        [Units("%")]
        [Required, Percentage, GreaterThanValue(0)]
        public double HighestDMD { get; set; } = 58;

        /// <summary>
        /// Lowest expected sward Dry Matter Digestibility (%)
        /// </summary>
        [Category("Farm", "Gut fill")]
        [Description("Minimum Dry Matter Digestibility expected")]
        [Required, Percentage]
        [Units("%")]
        public double LowestDMD { get; set; } = 42;

        /// <summary>
        /// Value of gut fill for highest quality green pasture
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill high quality (Highest DMD)")]
        public double GutFillHighQuality { get; set; } = 0.08;

        /// <summary>
        /// Value of gut fill for lowest quality cured pasture at min DMD
        /// </summary>
        [Percentage, GreaterThanEqualValue(0)]
        [Category("Farm", "Quality")]
        [Description("Gut fill low quality (Lowest DMD)")]
        public double GutFillLowQuality { get; set; } = 0.2;

        /// <inheritdoc/>
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
        [Units("MJ/kg DM")]
        public double MetabolisableEnergyContent { get; set; } = 0.0;

        /// <inheritdoc/>
        [JsonIgnore]
        public double NitrogenPercent { get; set; }

        /// <summary>
        /// Style of providing the dry matter digestibility of pasture
        /// </summary>
        [JsonIgnore]
        public DryMatterDigestibilityStyle DMDStyle { get; set; } = DryMatterDigestibilityStyle.EstimateFromNitrogenContent;

        /// <inheritdoc/>
        [JsonIgnore]
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public double AcidDetergentInsolubleProtein { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public double CrudeProteinPercent { get; set; }

        /// <inheritdoc/>
        public double FatPercent { get; set; } = 1.9;

        /// <inheritdoc/>
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
        /// The Simple Grazing model used to manage pasture and urine.
        /// </summary>
        public SimpleGrazing SimpleGrazingModel => simpleGrazing;

        /// <summary>
        /// The number of intake stores
        /// </summary>
        public int NumberOfItakeStores => intakeStoreRequests.Count;

        /// <summary>
        /// Get the name of a specified food store
        /// </summary>
        /// <param name="index">0 based index of store in list</param>
        /// <returns></returns>
        public string GetStoreName(int index)
        {
            if (index < 0 || !intakeStoreRequests.Any() || index > intakeStoreRequests.Count - 1)
                return "Invalid";
            return intakeStoreRequests.ElementAt(index).Name;
        }

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
            }
        }

        /// <inheritdoc/>
        public double CalculateGutFill(double dmd)
        {
            if (dmd <= LowestDMD)
            {
                return GutFillLowQuality;
            }
            if (dmd >= HighestDMD)
            {
                return GutFillHighQuality;
            }
            return GutFillLowQuality + ((dmd - LowestDMD) / (HighestDMD - LowestDMD)) * (GutFillHighQuality - GutFillLowQuality);
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public double OverallPastureBiomass { get; set; }

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
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa
        {
            get
            {
                if (paddock is null)
                {
                    return 0;
                }
                return AmountAvailable / paddock.Area;
            }
        }

        /// <summary>
        /// Converts an amount to units per hectare per day
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="daysInTimeStep">Number of days in current time step</param>
        public double ConvertToPerHaPerDay(double amount, int daysInTimeStep)
        {
            return amount / paddock.Area / daysInTimeStep;
        }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectareStartOfTimeStep { get; set; }

        /// <summary>
        /// Amount (tonnes per ha)
        /// </summary>
        [JsonIgnore]
        public double TonnesPerHectare
        {
            get
            {
                if (paddock is null)
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
        /// Get the new growth from the pasture model
        /// </summary>
        public void GetNewGrowth()
        {
            // todo: this will need to come from the pasture models... need to work out how
            biomassAddedThisYear = 0; 
        }

        /// <summary>
        /// Get the biomass consumed from the pasture model
        /// </summary>
        public double GetConsumed() => biomassConsumed;

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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            forages = Node.Find<Forages>();
            paddock = Node.Find<Zone>(name: PaddockName);
            if (forages is null || paddock is null)
                return;

            simpleGrazing = paddock.Node.Find<SimpleGrazing>();
            if (simpleGrazing is null)
                return;

            // do not include surface organic matter in feed pools
            forageModels = [.. forages.ModelsWithDigestibleBiomass.Where(m => m.Zone == paddock && !m.Name.Contains("SurfaceOrganic"))];
        }

        /// <summary>
        /// Store amount of consumable pasture available for everyone at the start of the step (kg per hectare)
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMPastureReady")]
        private void OnCLEMPastureReady(object sender, EventArgs e)
        {
            // this is CLEM getting the details of the pasture available for reporting and checks
            // we might be able to grab this from simple grazing

            // units provided from Material (PMF.Biomass) are g per m2, so need to be converted to kg/ha here (x10)
            // this is performed in SimpleGrazing.ZoneWithForage where amount to remove in kh/ha is converted to g m-2 before removal

            totalWt = 0;
            consumableWt = 0;
            biomassConsumed = 0;
            foreach (var biomassModel in forageModels)
            {
                totalWt += biomassModel.Material.Where(m => m.IsLive)
                                        .Sum(m => m.Total.Wt) * gm2TokgHa;
                totalWt += biomassModel.Material.Where(m => !m.IsLive)
                                        .Sum(m => m.Total.Wt) * gm2TokgHa; 
                consumableWt += biomassModel.Material.Where(m => !m.IsLive)
                                        .Sum(m => m.Consumable.Wt) * gm2TokgHa;
                consumableWt += biomassModel.Material.Where(m => m.IsLive)
                                        .Sum(m => m.Consumable.Wt) * gm2TokgHa;
            }

            // ToDo: Uncheck units. Consumable (PMF.Biomass) says it is kg/ha not g/m^2 as in Stock code.
            // ToDo: the PMF.Biomass object being used states g/m^2 which means we need *10 to kg/ha then * area (ha) to get to paddock but this value is already 10x more than APSIM report saying paddock biomass in kg (maybe this is kg/ha not total paddock biomass)
            // ToDo: simple grazing converts all Material.Sum() from g/m2 to kg/ha with * 10.0

            Set(totalWt * paddock.Area);
            SetUnavailable(AmountTotal - (consumableWt * paddock.Area)); // non-consumable set aside and available for reporting

            // do not return zero as there is always something there and zero affects calculations.
            TonnesPerHectareStartOfTimeStep = Math.Max(TonnesPerHectare, 0.01);
        }

        /// <inheritdoc/>
        public List<FoodResourceStore> GenerateIntakeGroups(int numberOfTimesteps, int greenAge = -1, int dmdStep = 10)
        {
            //string devStyle = "ByDMD";
            //string devStyle = "BySpecies";
            string devStyle = "ByForageVSurfaceOM";

            if (forageModels == null || !forageModels.Any())
                return [];

            var pools = forageModels
                .Where(model =>
                {
                    return (model.Material?.Sum(m => m.Consumable.Wt) ?? 0.0) > 0.0;
                })
                .Select(provider => new GrazeAPSIMForagePool(this, provider, forages, paddock.Area));

            switch (devStyle)
            {
                case "ByDMD":
                    // pools ordered by DMD steps (e.g. 0-10, 10-20, etc.) and grouped into FoodResourceStore for feeding to animals            
                    return pools
                    .GroupBy(s => Convert.ToInt32(s.DryMatterDigestibility / dmdStep) * dmdStep) // now group into DMD steps and place in ResourceFoodStore to feed to animals
                    .Select(groups => new FoodResourceStore(
                        [.. groups],
                        greenAge,
                        numberOfTimesteps,
                        groups.Key.ToString()
                        )
                    ).OrderByDescending(a => a.Details.DryMatterDigestibility).ToList();
                case "BySpecies":
                    // pools ordered by species and grouped into FoodResourceStore for feeding to animals            
                    return pools
                    .GroupBy(s => s.Name.Split('.').First())
                    .Select(groups => new FoodResourceStore(
                        [.. groups],
                        greenAge,
                        numberOfTimesteps,
                        groups.Key
                        )
                    ).OrderByDescending(a => a.Details.DryMatterDigestibility).ToList();
                case "ByForageVSurfaceOM":
                    // all pools combined and excluding surface organic matter
                    return pools
                    .GroupBy(s => s.Name.Contains("Surface") == false)
                    .Select(groups => new FoodResourceStore(
                        [.. groups],
                        greenAge,
                        numberOfTimesteps, 
                        (groups.Key ? "SurfaceOrganicMatter" : "MixedSward")
                        )
                    ).OrderByDescending(a => a.Details.DryMatterDigestibility).ToList();
                default:
                    return [];
            }

            // think about different approaches
            // 1. whole avearge pasture pool (DMD step = 100)
            // 2. select by DMD - current DMD step (e.g. 10)
            // 3. proportional with weighting toward green
            // 4. CLEM green biomass limit - implemented
            // 5. CLEM low biomass intake limited - implemented

            // individual selective ability proceedures can be actioned in GeneratePoolGroups and thus the list and order of pools the animals feed from.
        }

        /// <summary>
        /// Method to provide conversion factor to tonnes and/or hectares
        /// </summary>
        public double Report(string grazeProperty, bool tonnes = false, bool hectares = false, int age = -1)
        {
            double convertToKg = 10.0 * paddock.Area;
            double convert = (tonnes ? 1000 : 1) * (hectares ? paddock.Area : 1);
            double valueToUse = 0;
            switch (grazeProperty)
            {
                case "Amount":
                    if (age < 0)
                    {
                        valueToUse = intakeStoreRequests.SelectMany(a => a.Pools).Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Total.Wt)) * convertToKg / convert;
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Pools.Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Total.Wt)) * convertToKg / convert;
                    }
                    break;
                case "AmountConsumable":
                    if (age < 0)
                    {
                        valueToUse = intakeStoreRequests.SelectMany(a => a.Pools).Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Consumable.Wt)) * convertToKg / convert;
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Pools.Cast<GrazeAPSIMForagePool>().Sum(p => p.BiomassModel.Material.Sum(b => b.Consumable.Wt)) * convertToKg / convert;
                    }
                    break;
                case "Growth":
                    valueToUse = double.NaN;
                    break;
                case "Consumed":
                    if (age < 0)
                    {
                        valueToUse = GetConsumed() / convert;
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Pools.Cast<GrazeAPSIMForagePool>().Sum(p => p.Consumed) / convert;
                    }
                    break;
                case "Detached":
                    valueToUse = double.NaN;
                    break;
                case "Nitrogen":
                    if (age < 0)
                    {
                        return intakeStoreRequests.Sum(a => a.Details.NitrogenPercent * a.Details.Amount) / intakeStoreRequests.Sum(a => a.Details.Amount);
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Details.NitrogenPercent;
                    }
                    return valueToUse;
                case "DMD":
                    if (age < 0)
                    {
                        return intakeStoreRequests.Sum(a => a.Details.DryMatterDigestibility * a.Details.Amount)/intakeStoreRequests.Sum(a => a.Details.Amount);
                    }
                    else
                    {
                        valueToUse = intakeStoreRequests.ElementAt(age).Details.DryMatterDigestibility;
                    }
                    return valueToUse;
                case "Age":
                    return double.NaN;
                default:
                    throw new ApsimXException(this, $"Property [{grazeProperty}] not available for reporting pools");
            }
            // convert biomass to units specified kg,tonnes & farm,per/hectare
            return valueToUse;
        }

        #region transactions

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
            // receives the entire amount to remove from the resource holder that needs to be proportioned to pools
            if (request.AdditionalDetails is FoodResourceStore foodStore)
            {
                for (int i = 0; i < foodStore.Pools.Count; i++)
                {
                    foodStore.Pools[i].ReducePending(amount * foodStore.PoolProportions[i]);
                }
            }
            // do removal from pending
            base.DecreasePending(request, amount);
        }

        /// <inheritdoc/>
        public new void DecreasePendingByProportion(ResourceRequest request, double proportion)
        {
            double totalAmount = 0;
            if (request.AdditionalDetails is FoodResourceStore foodStore)
            {
                for (int i = 0; i < foodStore.Pools.Count; i++)
                {
                    double amountToRemove = foodStore.Pools[i].AmountPending * proportion;
                    foodStore.Pools[i].ReducePending(amountToRemove);
                    totalAmount += amountToRemove;
                }
            }
            // do removal from pending
            base.DecreasePending(request, totalAmount);
        }

        /// <inheritdoc/>
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
                    intakeStoreRequests.AddRange(foodStores);
                    Remove(request.Required, request);
                    break;
                case PastureActivityCutAndCarry:
                case PastureActivityBurn:
                    //RemoveFromPools(request);
                    // use generic removal to handle pending and reporting transaction if needed 
                    base.RemoveFromResource(request);
                    break;
                default:
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
                        provided += foodStore.Pools[i].AmountPending;
                        biomassConsumed += foodStore.Pools[i].AmountPending;
                        foodStore.Pools[i].ConsumePending();
                    }
                }
            }
            request.Provided = provided;

            base.PerformTransaction(request, handlePendingTransaction);
        }

        /// <inheritdoc/>
        public new void AddToResource(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            throw new NotImplementedException("Biomass cannot be added to a linked APSIM paddock");
        }

        /// <inheritdoc/>
        public double Remove(double removeAmount, string activityName, string reason)
        {
            throw new NotImplementedException("Biomass cannot be removed from a linked APSIM paddock");
        }

        #endregion

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (forages is null)
            {
                yield return new ValidationResult($"Could not find a Forages component in scope.", ["APSIM Forages component not found"]);
            }
            if (paddock is null)
            {
                yield return new ValidationResult($"Could not find a Paddock (Zone) component named [{PaddockName}] in scope.", ["APSIM Paddock component not found"]);
            }
            if (simpleGrazing is null)
            {
                yield return new ValidationResult($"Could not find a SimpleGrazing in paddock named [{PaddockName}] in scope.", ["APSIM SimpleGrazing component not found"]);
            }
        }
    }
}