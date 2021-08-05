using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Models.Core.Attributes;

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
    [Description("This activity performs sales and purchases of ruminants. It requires activities such as RuminantActivityManage, RuminantActivityTrade and RuminantActivitySellDryBreeders to identify individuals to be bought or sold. It will use a pricing schedule if supplied for the herd and can include additional trucking rules and emissions settings.")]
    [Version(1, 0, 2, "Allows for recording transactions by groups of individuals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantBuySell.htm")]
    public class RuminantActivityBuySell : CLEMRuminantActivityBase
    {
        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        public string BankAccountName { get; set; }

        private FinanceType bankAccount = null;
        private TruckingSettings trucking = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityBuySell()
        {
            TransactionCategory = "Livestock.Manage";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);
            IEnumerable<Ruminant> testherd = this.CurrentHerd(true);

            // check if finance is available and warn if not supplying bank account.
            if (Resources.ResourceItemsExist<Finance>())
            {
                if (BankAccountName == "")
                {
                    Summary.WriteWarning(this, $"No bank account has been specified in [a={this.Name}] while Finances are available in the simulation. No financial transactions will be recorded for the purchase and sale of animals.");
                }
            }
            if (BankAccountName != "")
            {
                bankAccount = Resources.GetResourceItem(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
            }

            // get trucking settings
            trucking = this.FindAllChildren<TruckingSettings>().FirstOrDefault() as TruckingSettings;

            // check if pricing is present
            if (bankAccount != null)
            {
                var breeds = HerdResource.Herd.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).GroupBy(a => a.HerdName);
                foreach (var herd in breeds)
                {
                    if (!herd.FirstOrDefault().BreedParams.PricingAvailable())
                    {
                        Summary.WriteWarning(this, String.Format("No pricing schedule has been provided for herd [r={0}]. No transactions will be recorded for activity [a={1}]", herd.Key, this.Name));
                    }
                }
            }
        }

        /// <summary>An event handler to call for animal purchases</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalBuy")]
        private void OnCLEMAnimalBuy(object sender, EventArgs e)
        {
            if (TimingOK)
            {
                if (trucking == null)
                {
                    BuyWithoutTrucking();
                }
                else
                {
                    BuyWithTrucking();
                }
            }
        }

        /// <summary>An event handler to call for animal sales</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalSell")]
        private void OnCLEMAnimalSell(object sender, EventArgs e)
        {
            Status = ActivityStatus.NoTask;

            int trucks = 0;
            double saleValue = 0;
            double saleWeight = 0;
            int head = 0;
            double aESum = 0;

            // only perform this activity if timing ok, or selling required as a result of forces destock
            List<Ruminant> herd = new List<Ruminant>();
            if(this.TimingOK || this.CurrentHerd(false).Where(a => a.SaleFlag == HerdChangeReason.DestockSale).Any())
            {
                this.Status = ActivityStatus.NotNeeded;
                // get current untrucked list of animals flagged for sale
                herd = this.CurrentHerd(false).Where(a => a.SaleFlag != HerdChangeReason.None).OrderByDescending(a => a.Weight).ToList();
            }

            // no individuals to sell
            if(herd.Count() == 0)
            {
                return;
            }

            List<Ruminant> soldIndividuals = new List<Ruminant>();

            if (trucking == null)
            {
                // no trucking just sell
                SetStatusSuccess();
                foreach (var ind in herd)
                {
                    aESum += ind.AdultEquivalent;
                    var pricing = ind.BreedParams.ValueofIndividual(ind, PurchaseOrSalePricingStyleType.Sale);
                    if (pricing != null)
                    {
                        saleValue += pricing.CalculateValue(ind);
                    }
                    saleWeight += ind.Weight;
                    soldIndividuals.Add(ind);
                    HerdResource.RemoveRuminant(ind, this);
                    head++;
                }
            }
            else
            {
                // if sale herd > min loads before allowing sale
                if (herd.Select(a => a.Weight / 450.0).Sum() / trucking.Number450kgPerTruck >= trucking.MinimumTrucksBeforeSelling)
                {
                    // while truck to fill
                    while (herd.Select(a => a.Weight / 450.0).Sum() / trucking.Number450kgPerTruck > trucking.MinimumLoadBeforeSelling)
                    {
                        bool nonloaded = true;
                        trucks++;
                        double load450kgs = 0;
                        // while truck below carrying capacity load individuals
                        foreach (var ind in herd)
                        {
                            if (load450kgs + (ind.Weight / 450.0) <= trucking.Number450kgPerTruck)
                            {
                                nonloaded = false;
                                head++;
                                aESum += ind.AdultEquivalent;
                                load450kgs += ind.Weight / 450.0;
                                var pricing = ind.BreedParams.ValueofIndividual(ind, PurchaseOrSalePricingStyleType.Sale);
                                if (pricing != null)
                                {
                                    saleValue += pricing.CalculateValue(ind);
                                }
                                saleWeight += ind.Weight;
                                soldIndividuals.Add(ind);
                                HerdResource.RemoveRuminant(ind, this);

                                //TODO: work out what to do with suckling calves still with mothers if mother sold.
                            }
                        }
                        if (nonloaded)
                        {
                            Summary.WriteWarning(this, String.Format("There was a problem loading the sale truck as sale individuals did not meet the loading criteria for breed [r={0}]", this.PredictedHerdBreed));
                            break;
                        }
                        herd = this.CurrentHerd(false).Where(a => a.SaleFlag != HerdChangeReason.None).OrderByDescending(a => a.Weight).ToList();
                    }
                    // create trucking emissions
                    trucking.ReportEmissions(trucks, true);
                    // if sold all
                    Status = (this.CurrentHerd(false).Where(a => a.SaleFlag != HerdChangeReason.None).Count() == 0) ? ActivityStatus.Success : ActivityStatus.Warning;
                }
            }
            
            
            if (bankAccount != null && head > 0) //(trucks > 0 || trucking == null)
            {
                ResourceRequest expenseRequest = new ResourceRequest
                {
                    ActivityModel = this,
                    AllowTransmutation = false
                };

                // calculate transport costs
                if (trucking != null)
                {
                    expenseRequest.Required = trucks * trucking.DistanceToMarket * trucking.CostPerKmTrucking;
                    expenseRequest.Category = trucking.TransactionCategory;
                    bankAccount.Remove(expenseRequest);
                }

                // perform payments by transaction grouping
                // uses a list of individuals that were taken from the herd

                // calculate aEsum and saleValue form the above list for use below
                // currently done above but can be shifted to calc from grouped indiv

                // add and remove from bank
                if (saleValue > 0)
                {
                    //bankAccount.Add(saleValue, this, this.PredictedHerdName, TransactionCategory);
                    var groupedIndividuals = HerdResource.SummarizeIndividualsByGroups(soldIndividuals, PurchaseOrSalePricingStyleType.Sale);
                    foreach (var item in groupedIndividuals)
                    {
                        foreach (var item2 in item.RuminantTypeGroup)
                        {
                            bankAccount.Add(item2.TotalPrice, this, item.RuminantTypeName, $"{TransactionCategory}.{item2.GroupName}");
                        }
                    }
                }

                // perform activity fee payments
                foreach (RuminantActivityFee item in this.FindAllChildren<RuminantActivityFee>())
                {
                    switch (item.PaymentStyle)
                    {
                        case AnimalPaymentStyleType.Fixed:
                            expenseRequest.Required = item.Amount;
                            break;
                        case AnimalPaymentStyleType.perHead:
                            expenseRequest.Required = head * item.Amount;
                            break;
                        case AnimalPaymentStyleType.perAE:
                            expenseRequest.Required = aESum * item.Amount;
                            break;
                        case AnimalPaymentStyleType.ProportionOfTotalSales:
                            expenseRequest.Required = saleValue * item.Amount;
                            break;
                        default:
                            throw new Exception(String.Format("PaymentStyle [{0}] is not supported for [{1}] in [{2}]", item.PaymentStyle, item.Name, this.Name));
                    }
                    expenseRequest.Category = item.TransactionCategory;
                    // uses bank account specified in the RuminantActivityFee
                    item.BankAccount.Remove(expenseRequest);
                }
            }
        }

        private void BuyWithoutTrucking()
        {
            // This activity will purchase animals based on available funds.

            // get current untrucked list of animal purchases
            List<Ruminant> herd = HerdResource.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).ToList();
            if (herd.Count() > 0)
            {
                if(this.Status!= ActivityStatus.Warning)
                {
                    this.Status = ActivityStatus.Success;
                }
            }
            else
            {
                return;
            }

            List<Ruminant> boughtIndividuals = new List<Ruminant>();

            double fundsAvailable = 0;
            if (bankAccount != null)
            {
                fundsAvailable = bankAccount.FundsAvailable;
            }
            double cost = 0;
            double shortfall = 0;
            bool fundsexceeded = false;
            foreach (var newind in herd)
            {
                if (bankAccount != null)  // perform with purchasing
                {
                    double value = 0;
                    AnimalPriceGroup pricing = null;
                    if (newind.SaleFlag == HerdChangeReason.SirePurchase)
                    {
                        pricing = newind.BreedParams.ValueofIndividual(newind, PurchaseOrSalePricingStyleType.Purchase, RuminantFilterParameters.IsSire, "true");
                    }
                    else
                    {
                        pricing = newind.BreedParams.ValueofIndividual(newind, PurchaseOrSalePricingStyleType.Purchase);
                    }
                    if (pricing != null)
                    {
                        value = pricing.CalculateValue(newind);
                    }

                    if (cost + value <= fundsAvailable && fundsexceeded == false)
                    {
                        boughtIndividuals.Add(newind);
                        HerdResource.PurchaseIndividuals.Remove(newind);
                        newind.ID = HerdResource.NextUniqueID;

                        HerdResource.AddRuminant(newind, this);
                        cost += value;
                    }
                    else
                    {
                        fundsexceeded = true;
                        shortfall += value;
                    }
                }
                else // no financial transactions
                {
                    boughtIndividuals.Add(newind);
                    HerdResource.PurchaseIndividuals.Remove(newind);
                    newind.ID = HerdResource.NextUniqueID;
                    HerdResource.AddRuminant(newind, this);
                }
            }

            if (bankAccount != null)
            {
                ResourceRequest purchaseRequest = new ResourceRequest
                {
                    ActivityModel = this,
                    Required = cost,
                    AllowTransmutation = false,
                    Category =  TransactionCategory,
                    RelatesToResource = this.PredictedHerdName
                };

                //bankAccount.Add(saleValue, this, this.PredictedHerdName, TransactionCategory);
                var groupedIndividuals = HerdResource.SummarizeIndividualsByGroups(boughtIndividuals, PurchaseOrSalePricingStyleType.Purchase);
                foreach (var item in groupedIndividuals)
                {
                    foreach (var item2 in item.RuminantTypeGroup)
                    {
                        purchaseRequest.Required = item2.TotalPrice??0;
                        purchaseRequest.Category = $"{TransactionCategory}.{item2.GroupName}";
                        bankAccount.Remove(purchaseRequest);
//                        bankAccount.Add(item2.TotalPrice, this, item.RuminantTypeName, $"{TransactionCategory}.{item2.GroupName}");
                    }
                }

                // report any financial shortfall in purchases
                if (shortfall > 0)
                {
                    purchaseRequest.Available = bankAccount.Amount;
                    purchaseRequest.Required = cost + shortfall;
                    purchaseRequest.Provided = cost;
                    purchaseRequest.ResourceType = typeof(Finance);
                    purchaseRequest.ResourceTypeName = BankAccountName;
                    ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = purchaseRequest };
                    OnShortfallOccurred(rre);
                }
            }
        }

        private void BuyWithTrucking()
        {
            // This activity will purchase animals based on available funds.

            int trucks = 0;
            int head = 0;
            double aESum = 0;
            double fundsAvailable = 0;
            if (bankAccount != null)
            {
                fundsAvailable = bankAccount.FundsAvailable;
            }
            double cost = 0;
            double shortfall = 0;
            bool fundsexceeded = false;

            // get current untrucked list of animal purchases
            List<Ruminant> herd = HerdResource.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).OrderByDescending(a => a.Weight).ToList();
            if (herd.Count() == 0)
            {
                return;
            }

            List<Ruminant> boughtIndividuals = new List<Ruminant>();

            // if purchase herd > min loads before allowing trucking
            if (herd.Select(a => a.Weight / 450.0).Sum() / trucking.Number450kgPerTruck >= trucking.MinimumTrucksBeforeBuying)
            {
                // while truck to fill
                while (herd.Select(a => a.Weight / 450.0).Sum() / trucking.Number450kgPerTruck > trucking.MinimumLoadBeforeBuying)
                {
                    bool nonloaded = true;
                    trucks++;
                    double load450kgs = 0;
                    // while truck below carrying capacity load individuals
                    foreach (var ind in herd)
                    {
                        if (load450kgs + (ind.Weight / 450.0) <= trucking.Number450kgPerTruck)
                        {
                            nonloaded = false;
                            head++;
                            aESum += ind.AdultEquivalent;
                            load450kgs += ind.Weight / 450.0;

                            if (bankAccount != null)  // perform with purchasing
                            {
                                double value = 0;
                                AnimalPriceGroup pricing = null;
                                if (ind.SaleFlag == HerdChangeReason.SirePurchase)
                                {
                                    pricing = ind.BreedParams.ValueofIndividual(ind, PurchaseOrSalePricingStyleType.Purchase, RuminantFilterParameters.IsSire, "true");
                                }
                                else
                                {
                                    pricing = ind.BreedParams.ValueofIndividual(ind, PurchaseOrSalePricingStyleType.Purchase);
                                }
                                if (pricing != null)
                                {
                                    value = pricing.CalculateValue(ind);
                                }

                                if (cost + value <= fundsAvailable && fundsexceeded == false)
                                {
                                    ind.ID = HerdResource.NextUniqueID;
                                    boughtIndividuals.Add(ind);
                                    HerdResource.AddRuminant(ind, this);
                                    HerdResource.PurchaseIndividuals.Remove(ind);
                                    cost += value;
                                }
                                else
                                {
                                    fundsexceeded = true;
                                    shortfall += value;
                                }
                            }
                            else // no financial transactions
                            {
                                ind.ID = HerdResource.NextUniqueID;
                                boughtIndividuals.Add(ind);
                                HerdResource.AddRuminant(ind, this);
                                HerdResource.PurchaseIndividuals.Remove(ind);
                            }

                        }
                    }
                    if (nonloaded)
                    {
                        Summary.WriteWarning(this, String.Format("There was a problem loading the purchase truck as purchase individuals did not meet the loading criteria for breed [r={0}]", this.PredictedHerdBreed));
                        break;
                    }
                    if (shortfall > 0)
                    {
                        break;
                    }

                    herd = HerdResource.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).OrderByDescending(a => a.Weight).ToList();
                }

                if (Status != ActivityStatus.Warning)
                {
                    if(HerdResource.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).Count() == 0)
                    {
                        SetStatusSuccess();
                    }
                    else
                    {
                        Status = ActivityStatus.Partial;
                    }
                }

                // create trucking emissions
                if (trucking != null && trucks > 0 )
                {
                    trucking.ReportEmissions(trucks, false);
                }

                if (bankAccount != null && (trucks > 0 || trucking == null))
                {
                    ResourceRequest purchaseRequest = new ResourceRequest
                    {
                        ActivityModel = this,
                        Required = cost,
                        AllowTransmutation = false,
                        Category = TransactionCategory,
                        RelatesToResource = this.PredictedHerdName
                    };

                    var groupedIndividuals = HerdResource.SummarizeIndividualsByGroups(boughtIndividuals, PurchaseOrSalePricingStyleType.Purchase);
                    foreach (var item in groupedIndividuals)
                    {
                        foreach (var item2 in item.RuminantTypeGroup)
                        {
                            purchaseRequest.Required = item2.TotalPrice??0;
                            purchaseRequest.Category = $"{TransactionCategory}.{item2.GroupName}";
                            bankAccount.Remove(purchaseRequest);
                        }
                    }

                    // report any financial shortfall in purchases
                    if (shortfall > 0)
                    {
                        purchaseRequest.Available = bankAccount.Amount;
                        purchaseRequest.Required = cost + shortfall;
                        purchaseRequest.Provided = cost;
                        purchaseRequest.ResourceType = typeof(Finance);
                        purchaseRequest.ResourceTypeName = BankAccountName;
                        ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = purchaseRequest };
                        OnShortfallOccurred(rre);
                    }

                    ResourceRequest expenseRequest = new ResourceRequest
                    {
                        Available = bankAccount.Amount,
                        ActivityModel = this,
                        AllowTransmutation = false
                    };

                    // calculate transport costs
                    if (trucking != null)
                    {
                        expenseRequest.Required = trucks * trucking.DistanceToMarket * trucking.CostPerKmTrucking;
                        expenseRequest.Category = trucking.TransactionCategory;
                        bankAccount.Remove(expenseRequest);

                        if (expenseRequest.Required > expenseRequest.Available)
                        {
                            expenseRequest.Available = bankAccount.Amount;
                            expenseRequest.ResourceType = typeof(Finance);
                            expenseRequest.ResourceTypeName = BankAccountName;
                            ResourceRequestEventArgs rre = new ResourceRequestEventArgs() { Request = expenseRequest };
                            OnShortfallOccurred(rre);
                        }
                    }
                }
            }
            else
            {
                this.Status = ActivityStatus.Warning;
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<Ruminant> herd = HerdResource.Herd.Where(a => (a.SaleFlag.ToString().Contains("Purchase") || a.SaleFlag.ToString().Contains("Sale")) && a.Breed == this.PredictedHerdBreed).ToList();
            int head = herd.Count();
            double animalEquivalents = herd.Sum(a => a.AdultEquivalent);
            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = animalEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            Status = ActivityStatus.NotNeeded;
            return; 
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        #region transactions

        /// <inheritdoc/>
        public override event EventHandler ResourceShortfallOccurred;

        /// <inheritdoc/>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public override event EventHandler ActivityPerformed;

        /// <inheritdoc/>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n<div class=\"activityentry\">Purchases and sales will use ";
            if (BankAccountName == null || BankAccountName == "")
            {
                html += "<span class=\"errorlink\">[ACCOUNT NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + BankAccountName + "</span>";
            }
            html += "</div>";

            return html;
        } 
        #endregion
    }
}
