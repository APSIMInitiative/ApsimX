using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant sales activity</summary>
    /// <summary>This activity undertakes the sale and transport of any individuals flagged for sale.</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs all sales and purchases of ruminants. This requires other herd management activities to identify individuals to be bought or sold. It uses any pricing schedule supplied and can include additional trucking rules and emissions settings")]
    [Version(1, 2, 1, "Activity style to control separate purchases and sales instances")]
    [Version(1, 1, 1, "Allows improved trucking settings component")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 2, "Allows for recording transactions by groups of individuals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantBuySell.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantActivityBuySell : CLEMRuminantActivityBase, IHandlesActivityCompanionModels, IValidatableObject
    {
        private FinanceType bankAccount = null;
        private IEnumerable<RuminantTrucking> truckingOptions;
        private int numberToDo;
        private int numberToSkip;
        private int numberTrucksToSkipIndividuals;
        private int fundsNeededPurchaseSkipIndividuals;
        private double herdValue = 0;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;
        private bool truckingWithImplications = false;

        /// <summary>
        /// Activity style
        /// </summary>
        [Description("Activity style")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ActivityStyleList")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "The style (arrange purchases or sales) is required")]
        public string ActivityStyle { get; set; }

        /// <summary>
        /// Get the styles available for this activity
        /// </summary>
        /// <returns>An Ienumerable of strings</returns>
        public static IEnumerable<string> ActivityStyleList()
        {
            return new string[] { "Arrange sales", "Arrange purchases" };
        }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        public string BankAccountName { get; set; }

        /// <summary>
        /// The list of individuals remaining to be trucked in the current timestep and task (buy or sell)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Ruminant> IndividualsToBeTrucked { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityBuySell()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>()
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "Value of individuals",
                        }
                        );
                case "RuminantTrucking":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>());
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);
            filterGroups = GetCompanionModelsByIdentifier<RuminantGroup>( false, true);

            IEnumerable<Ruminant> testherd = this.CurrentHerd(true);

            // check if finance is available and warn if not supplying bank account.
            if (Resources.ResourceItemsExist<Finance>())
            {
                if (BankAccountName == "")
                    Summary.WriteMessage(this, $"No bank account has been specified in [a={Name}] while Finances are available in the simulation. No financial transactions will be recorded for the purchase and sale of animals.", MessageType.Warning);
            }
            if (BankAccountName != "")
                bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);

            // get trucking settings
            truckingOptions = GetCompanionModelsByIdentifier<RuminantTrucking>(false, false);
            truckingWithImplications = truckingOptions?.Where(a => a.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseAvailableWithImplications).Any()??false;
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalBuy")]
        private void OnCLEMAnimalBuyPerformActivity(object sender, EventArgs e)
        {
            if (ActivityStyle == "Arrange purchases")
            {
                Status = ActivityStatus.NotNeeded;
                ResourceRequestList.Clear();
                ManageActivityResourcesAndTasks();
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalSell")]
        private void OnCLEMAnimalSellPerformActivity(object sender, EventArgs e)
        {
            if (ActivityStyle == "Arrange sales")
            {
                Status = ActivityStatus.NotNeeded;
                ResourceRequestList.Clear();
                ManageActivityResourcesAndTasks();
            }
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            numberToDo = 0;
            numberToSkip = 0;
            numberTrucksToSkipIndividuals = 0;
            fundsNeededPurchaseSkipIndividuals = 0;

            IEnumerable<Ruminant> herd;
            if(ActivityStyle == "Arrange purchases")
                herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.ForPurchase);
            else
                herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.MarkedForSale);

            uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
            IndividualsToBeTrucked = uniqueIndividuals;
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            if (truckingOptions != null)
            {
                foreach (var trucking in truckingOptions)
                    trucking.ManuallyGetResourcesPerformActivity();

                if (!truckingWithImplications)
                {
                    uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
                    IndividualsToBeTrucked = uniqueIndividuals;
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> requestResources = new();
            int numberTrucked = 0;
            double herdValue = 0;

            PurchaseOrSalePricingStyleType priceStyle = (ActivityStyle == "Arrange sales") ? PurchaseOrSalePricingStyleType.Sale : PurchaseOrSalePricingStyleType.Purchase;

            if (truckingOptions is null)
            {
                // no trucking found
                herdValue = IndividualsToBeTrucked.Sum(a => a.BreedDetails?.GetPriceGroupOfIndividual(a, priceStyle)?.CalculateValue(a)??0);
                numberTrucked = numberToDo;
            }
            else
            {
                // all trucking has been allocated and each trucking component knows its individuals
                foreach (var trucking in truckingOptions)
                {
                    herdValue += trucking.IndividualsToBeTrucked.Sum(a => a.BreedDetails?.GetPriceGroupOfIndividual(a, priceStyle)?.CalculateValue(a)??0);
                    numberTrucked += trucking.IndividualsToBeTrucked.Count();
                }
            }

            // add payment request so we can manage by shortfall, place at top of all requests for first access
            if (ActivityStyle == "Arrange purchases" && bankAccount != null && MathUtilities.IsGreaterThan(herdValue, 0))
            {
                // request a single transaction.
                // this will be deleted and replaced with class based transactions in the adjust section
                requestResources.Add(new ResourceRequest
                {
                    ActivityModel = this,
                    Required = herdValue,
                    AllowTransmutation = false,
                    Category = TransactionCategory,
                    RelatesToResource = PredictedHerdNameToDisplay,
                    AdditionalDetails = "Purchases",
                    ResourceType = typeof(Finance),
                    ResourceTypeName = BankAccountName,
                });
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                int number = numberToDo;
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per head":
                        valuesForCompanionModels[valueToSupply.Key] = numberTrucked;
                        break;
                    case "Value of individuals":
                        valuesForCompanionModels[valueToSupply.Key] = herdValue;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }

            if (numberTrucked < numberToDo)
            {
                // Report trucks shortfall for task
                ResourceRequestEventArgs rrEventArgs = new()
                {
                    Request = new ResourceRequest()
                    {
                        Resource = null,
                        ResourceType = null,
                        ResourceTypeName = "Head trucked",
                        AllowTransmutation = false,
                        Required = numberToDo,
                        Provided = numberTrucked,
                        Category = ActivityStyle,
                        AdditionalDetails = null,
                        RelatesToResource = null,
                        ActivityModel = this,
                    }
                };


                if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    string warn = $"Insufficient [r=Trucks] for [{ActivityStyle}] in [a={NameWithParent}]{Environment.NewLine}[Report error and stop] is selected as action when shortfall of resources. Ensure sufficient resources are available or change OnPartialResourcesAvailableAction setting";
                    Status = ActivityStatus.Critical;
                    ActivitiesHolder.ReportActivityShortfall(rrEventArgs);
                    throw new ApsimXException(this, warn);
                }
                else if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.SkipActivity)
                {
                    Status = ActivityStatus.Skipped;
                }
                else
                {
                    Status = ActivityStatus.Partial;
                    if(OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseAvailableResources)
                        rrEventArgs.Request.ShortfallStatus = "No implication";
                    if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseAvailableWithImplications)
                        rrEventArgs.Request.ShortfallStatus = "Affected outcome";

                    if(numberTrucked == 0)
                    {
                        Status= ActivityStatus.Warning;
                        AddStatusMessage($"{ActivityStyle} could not be performed");
                    }
                    else
                    {
                        AddStatusMessage($"{ActivityStyle} were restricted");
                    }
                }
                ActivitiesHolder.ReportActivityShortfall(rrEventArgs);


            }
            return requestResources;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var buySellShort = shortfalls.Where(a => a.CompanionModelDetails.unit == "per head").FirstOrDefault();
                if (buySellShort != null)
                {
                    numberToSkip = Convert.ToInt32(numberToDo * buySellShort.Required / buySellShort.Provided);
                    this.Status = ActivityStatus.Partial;
                }

                // now for remaining limiters
                buySellShort = shortfalls.Where(a => a.CompanionModelDetails.unit == "per truck").FirstOrDefault();
                if (buySellShort != null)
                {
                    throw new Exception($"Unable to limit [{ActivityStyle}] by units [per km trucked] in [a={NameWithParent}]{Environment.NewLine}This resource cost does not support [ShortfallAffectsActivity] in [a=RuminantHerdBuySell]");
                }

                buySellShort = shortfalls.Where(a => a.CompanionModelDetails.unit == "per km trucked").FirstOrDefault();
                if (buySellShort != null)
                    throw new Exception($"Unable to limit [{ActivityStyle}] by units [per km trucked] in [a={NameWithParent}]{Environment.NewLine}This resource cost does not support [ShortfallAffectsActivity] in [a=RuminantHerdBuySell]");

                buySellShort = shortfalls.Where(a => a.CompanionModelDetails.unit == "per $ value").FirstOrDefault();
                if (buySellShort != null)
                    throw new Exception($"Unable to limit [{ActivityStyle}] by units [per $ value] in [a={NameWithParent}]{Environment.NewLine}This resource cost does not support [ShortfallAffectsActivity] in [a=RuminantHerdBuySell] as costs are already accounted in ruminant purchases.");
            }

            // remove any additional individuals from end based on trucks to skip

            //TODO: need to decide whether to re-adjust to new min load and truck rules after reduction.

            // further limit buy purchase shortfalls buy reducing the herd (unique individuals) to new level
            if (ActivityStyle == "Arrange purchases")
            {
                var request = ResourceRequestList.Where(a => a.AdditionalDetails.ToString() == "Purchases").FirstOrDefault();
                if(request != null)
                {
                    if (MathUtilities.IsLessThan(request.Required, request.Available))
                    {
                        double valueOfSkipped = 0;
                        if (MathUtilities.IsGreaterThan(numberToSkip + numberTrucksToSkipIndividuals, 0))
                        {
                            // adjust to take care of skipped individuals
                            var skipped = uniqueIndividuals.TakeLast(numberToSkip+numberTrucksToSkipIndividuals);
                            valueOfSkipped = skipped.Sum(a => a.BreedDetails.GetPriceGroupOfIndividual(a, PurchaseOrSalePricingStyleType.Sale).CalculateValue(a));
                        }
                        double shortfall = request.Required - request.Provided - valueOfSkipped;
                        if(MathUtilities.IsGreaterThan(shortfall, 0))
                        {
                            // need to further reduce the herd to account for finance shortfall
                            // count from back while value less than shortfall
                            foreach (var ind in uniqueIndividuals.SkipLast(numberToSkip + numberTrucksToSkipIndividuals).Reverse())
                            {
                                fundsNeededPurchaseSkipIndividuals++;
                                shortfall -= ind.BreedDetails.GetPriceGroupOfIndividual(ind, PurchaseOrSalePricingStyleType.Sale).CalculateValue(ind);
                                if (MathUtilities.IsLessThanOrEqual(shortfall, 0))
                                    break;
                            }
                            // report any financial shortfall in purchases when trying to purchase the animals
                            if (MathUtilities.IsPositive(shortfall))
                            {
                                ResourceRequest purchaseRequest = new()
                                {
                                    ActivityModel = this,
                                    AllowTransmutation = false,
                                    Category = TransactionCategory,
                                    RelatesToResource = this.PredictedHerdNameToDisplay,
                                    Available = bankAccount.Amount,
                                    Required = request.Required - valueOfSkipped,
                                    Provided = request.Provided - valueOfSkipped,
                                    ResourceType = typeof(Finance),
                                    ResourceTypeName = BankAccountName
                                };
                                ResourceRequestEventArgs rre = new() { Request = purchaseRequest };
                                ActivitiesHolder.ReportActivityShortfall(rre);
                            }
                        }

                        // create pricing-based purchase requests
                        if (MathUtilities.IsGreaterThanOrEqual(herdValue - shortfall, request.Provided))
                            throw new Exception("Invalid reduction of herd in Buy sell activity");
                    }

                    var groupedIndividuals = HerdResource.SummarizeIndividualsByGroups(uniqueIndividuals.SkipLast(numberToSkip+ numberTrucksToSkipIndividuals + fundsNeededPurchaseSkipIndividuals), PurchaseOrSalePricingStyleType.Purchase);
                    foreach (var item in groupedIndividuals)
                    {
                        foreach (var item2 in item.RuminantTypeGroup)
                        {
                            ResourceRequestList.Add(new ResourceRequest
                            {
                                Resource = request.Resource,
                                ResourceType = request.ResourceType,
                                ResourceTypeName = request.ResourceTypeName,
                                ActivityModel = this,
                                Required = item2.TotalPrice ?? 0,
                                Available = item2.TotalPrice ?? 0,
                                AllowTransmutation = false,
                                Category = TransactionCategory,
                                RelatesToResource = $"{PredictedHerdNameToDisplay}.{item2.GroupName}".TrimStart('.')
                            });
                        }
                    }
                    ResourceRequestList.Remove(request);
                }
            }
        }

        private void ProcessAnimals()
        {
            int head = 0;
            List<Ruminant> taskIndividuals = new();

            if (ActivityStyle == "Arrange sales")
            {
                double saleValue = 0;
                foreach (var ind in uniqueIndividuals.SkipLast(numberToSkip+numberTrucksToSkipIndividuals).ToList())
                {
                    var pricing = ind.BreedDetails.GetPriceGroupOfIndividual(ind, PurchaseOrSalePricingStyleType.Sale);
                    if (pricing != null)
                        saleValue += pricing.CalculateValue(ind);
                   
                    taskIndividuals.Add(ind);
                    HerdResource.RemoveRuminant(ind, this);
                    head++;
                }

                // earn money from sales
                if (bankAccount != null && MathUtilities.IsGreaterThan(saleValue, 0))
                {
                    var groupedIndividuals = HerdResource.SummarizeIndividualsByGroups(taskIndividuals, PurchaseOrSalePricingStyleType.Sale);
                    foreach (var item in groupedIndividuals)
                        foreach (var item2 in item.RuminantTypeGroup)
                            bankAccount.Add(item2.TotalPrice, this, $"{item.RuminantTypeNameToDisplay}.{item2.GroupName}".TrimStart('.'), TransactionCategory);
                }
            }
            else // purchases
            {
                foreach (var ind in uniqueIndividuals.SkipLast(numberToSkip + numberTrucksToSkipIndividuals).ToList())
                {
                    head++;
                    taskIndividuals.Add(ind);
                    HerdResource.PurchaseIndividuals.Remove(ind);
                    ind.ID = HerdResource.NextUniqueID;
                    HerdResource.AddRuminant(ind, this);
                }
            }
            SetStatusSuccessOrPartial(head < numberToDo);
        }
        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (numberToDo - numberToSkip > 0)
                ProcessAnimals();
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // check that all or none of children are ShortfallsWithImplications
            var truckingComponents = FindAllChildren<RuminantTrucking>().Where(a => a.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseAvailableWithImplications).ToList();
            if (truckingComponents.Any() && (truckingComponents.Count != FindAllChildren<RuminantTrucking>().Count()))
            {
                string[] memberNames = new string[] { "RuminantTrucking" };
                results.Add(new ValidationResult($"All [r=RuminantTrucking] components for [{ActivityStyle}] must be set to [UseAvailableWithImplications] if any are defined for this partial resources available action", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">{ActivityStyle} will use ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(BankAccountName, "Not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        } 
        #endregion
    }
}
