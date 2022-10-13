using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
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
    [Version(1, 1, 1, "Allows improved trucking settings component")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 2, "Allows for recording transactions by groups of individuals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantBuySell.htm")]
    public class RuminantActivityBuySell : CLEMRuminantActivityBase, IHandlesActivityCompanionModels
    {
        private FinanceType bankAccount = null;
        private IEnumerable<RuminantTrucking> truckingBuy;
        private IEnumerable<RuminantTrucking> truckingSell;
        private int numberToDo;
        private int numberToSkip;
        private double numberTrucks;
        private double numberTrucksToSkip;
        private int numberTrucksToSkipIndividuals;
        private int fundsNeededPurchaseSkipIndividuals;
        private double herdValue = 0;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroupsSell;
        private IEnumerable<RuminantGroup> filterGroupsBuy;
        private IEnumerable<RuminantGroup> filterGroups;
        private string task = "";

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
                        identifiers: new List<string>() {
                            "Purchases",
                            "Sales"
                        },
                        measures: new List<string>()
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Purchases",
                            "Sales"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "Value of individuals",
                        }
                        );
                case "RuminantTrucking":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Purchases",
                            "Sales"
                        },
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
            filterGroupsBuy = GetCompanionModelsByIdentifier<RuminantGroup>( false, true, "Purchases");
            filterGroupsSell = GetCompanionModelsByIdentifier<RuminantGroup>(false, true, "Sales");

            IEnumerable<Ruminant> testherd = this.CurrentHerd(true);

            // check if finance is available and warn if not supplying bank account.
            if (Resources.ResourceItemsExist<Finance>())
            {
                if (BankAccountName == "")
                    Summary.WriteMessage(this, $"No bank account has been specified in [a={this.Name}] while Finances are available in the simulation. No financial transactions will be recorded for the purchase and sale of animals.", MessageType.Warning);
            }
            if (BankAccountName != "")
                bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);

            // get trucking settings
            truckingBuy = GetCompanionModelsByIdentifier<RuminantTrucking>(false, false, "Purchases");
            truckingSell = GetCompanionModelsByIdentifier<RuminantTrucking>(false, false, "Sales");

            // check if pricing is present
            if (bankAccount != null)
            {
                var breeds = HerdResource.Herd.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).GroupBy(a => a.HerdName);
                foreach (var herd in breeds)
                    if (!herd.FirstOrDefault().BreedParams.PricingAvailable())
                    {
                        string warn = $"No pricing schedule has been provided for herd [r={herd.Key}]. No financial transactions will be recorded for activity [a={this.Name}]";
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                    }
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalBuy")]
        private void OnCLEMAnimalBuyPerformActivity(object sender, EventArgs e)
        {
            task = "Buy";
            ResourceRequestList.Clear();
            ManageActivityResourcesAndTasks("Purchases");
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalSell")]
        private void OnCLEMAnimalSellPerformActivity(object sender, EventArgs e)
        {
            Status = ActivityStatus.NotNeeded;
            task = "Sell";
            ResourceRequestList.Clear();
            ManageActivityResourcesAndTasks("Sales");
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            numberToDo = 0;
            numberToSkip = 0;
            numberTrucks = 0;
            numberTrucksToSkip = 0;
            numberTrucksToSkipIndividuals = 0;
            fundsNeededPurchaseSkipIndividuals = 0;

            IEnumerable<Ruminant> herd;
            switch (task)
            {
                case "Buy":
                    herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.ForPurchase);
                    filterGroups = filterGroupsBuy;
                    uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
                    IndividualsToBeTrucked = uniqueIndividuals;
                    numberToDo = uniqueIndividuals?.Count() ?? 0;

                    if(truckingBuy != null)
                        foreach (var trucking in truckingBuy)
                            trucking.ManuallyGetResourcesPerformActivity();

                    break;
                case "Sell":
                    herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.MarkedForSale);
                    filterGroups = filterGroupsSell;
                    uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
                    IndividualsToBeTrucked = uniqueIndividuals;
                    numberToDo = uniqueIndividuals?.Count() ?? 0;

                    if (truckingSell != null)
                        foreach (var trucking in truckingSell)
                            trucking.ManuallyGetResourcesPerformActivity();

                    break;
                default:
                    break;
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> requestResources = new List<ResourceRequest>();
            int numberTrucked = 0;
            double herdValue = 0;
            string identifier = "";
            switch (task)
            {
                case "Buy":
                    identifier = "Purchases";

                    if (truckingBuy is null)
                    {
                        // no trucking found
                        herdValue = IndividualsToBeTrucked.Sum(a => a.BreedParams?.GetPriceGroupOfIndividual(a, PurchaseOrSalePricingStyleType.Purchase)?.CalculateValue(a)??0);
                        numberTrucked = numberToDo;
                    }
                    else
                    {
                        // all trucking has been allocated and each trucking component knows its individuals
                        foreach (var trucking in truckingBuy)
                        {
                            herdValue += trucking.IndividualsToBeTrucked.Sum(a => a.BreedParams?.GetPriceGroupOfIndividual(a, PurchaseOrSalePricingStyleType.Purchase)?.CalculateValue(a)??0);
                            numberTrucked += trucking.IndividualsToBeTrucked.Count();
                        }
                    }

                    // add payment request so we can manage by shortfall, place at top of all requests for first access
                    if (bankAccount != null && MathUtilities.IsGreaterThan(herdValue, 0))
                    {
                        // request a single transaction.
                        // this will be deleted and replaced with class based transactions in the adjust section
                        requestResources.Add(new ResourceRequest
                        {
                            ActivityModel = this,
                            Required = herdValue,
                            AllowTransmutation = false,
                            Category = TransactionCategory,
                            RelatesToResource = this.PredictedHerdNameToDisplay,
                            AdditionalDetails = "Purchases",
                            ResourceType = typeof(Finance),
                            ResourceTypeName = BankAccountName,
                        });
                    }
                    // provide updated measure for companion models
                    foreach (var valueToSupply in valuesForCompanionModels.Where(a => a.Key.identifier == identifier).ToList())
                    {
                        int number = numberToDo;
                        switch (valueToSupply.Key.identifier)
                        {
                            case "Purchases":
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
                                break;
                            case "Sales":
                                break;
                            default:
                                throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                        }
                    }
                    break;
                case "Sell":
                    identifier = "Sales";

                    if (truckingSell is null)
                    {
                        // no trucking found
                        herdValue = IndividualsToBeTrucked.Sum(a => a.BreedParams?.GetPriceGroupOfIndividual(a, PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(a)??0);
                        numberTrucked = numberToDo;
                    }
                    else
                    {
                        // all trucking has been allocated and each trucking component knows its individuals
                        foreach (var trucking in truckingSell)
                        {
                            herdValue += trucking.IndividualsToBeTrucked.Sum(a => a.BreedParams?.GetPriceGroupOfIndividual(a, PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(a)??0);
                            numberTrucked += trucking.IndividualsToBeTrucked.Count();
                        }
                    }

                    // provide updated measure for companion models
                    foreach (var valueToSupply in valuesForCompanionModels.Where(a => a.Key.identifier == identifier).ToList())
                    {
                        int number = numberToDo;
                        switch (valueToSupply.Key.identifier)
                        {
                            case "Sales":
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
                                break;
                            case "Purchases":
                                break;
                            default:
                                throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                        }
                    }
                    break;
                default:
                    break;
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
                var buySellShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == task && a.CompanionModelDetails.unit == "per head").FirstOrDefault();
                if (buySellShort != null)
                {
                    numberToSkip = Convert.ToInt32(numberToDo * buySellShort.Required / buySellShort.Provided);
                    this.Status = ActivityStatus.Partial;
                }

                // now for remaining limiters
                buySellShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == task && a.CompanionModelDetails.unit == "per truck").FirstOrDefault();
                if (buySellShort != null)
                {
                    numberTrucksToSkip = Convert.ToInt32(numberTrucks * buySellShort.Required / buySellShort.Provided);
                    this.Status = ActivityStatus.Partial;
                }

                buySellShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == task && a.CompanionModelDetails.unit == "per km trucked").FirstOrDefault();
                if (buySellShort != null)
                    throw new Exception($"Unable to limit [{task}] by units [per km trucked] in [a={NameWithParent}]{Environment.NewLine}This resource cost does not support [ShortfallAffectsActivity] in [a=RuminantHerdBuySell]");

                buySellShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == task && a.CompanionModelDetails.unit == "per $ value").FirstOrDefault();
                if (buySellShort != null)
                    throw new Exception($"Unable to limit [{task}] by units [per $ value] in [a={NameWithParent}]{Environment.NewLine}This resource cost does not support [ShortfallAffectsActivity] in [a=RuminantHerdBuySell] as costs are already accounted in ruminant purchases.");
            }

            // remove any additional individuals from end based on trucks to skip

            //TODO: need to decide whether to re-adjust to new min load and truck rules after reduction.

            // further limit buy purchase shortfalls buy reducing the herd (unique individuals) to new level
            if (task == "Buy")
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
                            valueOfSkipped = skipped.Sum(a => a.BreedParams.GetPriceGroupOfIndividual(a, PurchaseOrSalePricingStyleType.Sale).CalculateValue(a));
                        }
                        double shortfall = request.Required - request.Provided - valueOfSkipped;
                        if(MathUtilities.IsGreaterThan(shortfall, 0))
                        {
                            // need to further reduce the herd to account for finance shortfall
                            // count from back while value less than shortfall
                            foreach (var ind in uniqueIndividuals.SkipLast(numberToSkip + numberTrucksToSkipIndividuals).Reverse())
                            {
                                fundsNeededPurchaseSkipIndividuals++;
                                shortfall -= ind.BreedParams.GetPriceGroupOfIndividual(ind, PurchaseOrSalePricingStyleType.Sale).CalculateValue(ind);
                                if (MathUtilities.IsLessThanOrEqual(shortfall, 0))
                                    break;
                            }
                            // report any financial shortfall in purchases when trying to purchase the animals
                            if (MathUtilities.IsPositive(shortfall))
                            {
                                ResourceRequest purchaseRequest = new ResourceRequest
                                {
                                    ActivityModel = this,
                                    AllowTransmutation = false,
                                    Category = TransactionCategory,
                                    RelatesToResource = this.PredictedHerdNameToDisplay
                                };
                                purchaseRequest.Available = bankAccount.Amount;
                                purchaseRequest.Required = request.Required-valueOfSkipped;
                                purchaseRequest.Provided = request.Provided-valueOfSkipped;
                                purchaseRequest.ResourceType = typeof(Finance);
                                purchaseRequest.ResourceTypeName = BankAccountName;
                                ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = purchaseRequest };
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
            List<Ruminant> taskIndividuals = new List<Ruminant>();

            if (task == "Sell")  // sales
            {
                double saleValue = 0;
                foreach (var ind in uniqueIndividuals.SkipLast(numberToSkip+numberTrucksToSkipIndividuals).ToList())
                {
                    var pricing = ind.BreedParams.GetPriceGroupOfIndividual(ind, PurchaseOrSalePricingStyleType.Sale);
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

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Purchases and sales will use ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(BankAccountName, "Not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        } 
        #endregion
    }
}
