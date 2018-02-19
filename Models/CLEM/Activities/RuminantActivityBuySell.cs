using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant sales activity</summary>
    /// <summary>This activity undertakes the sale and transport of any individuals flagged for sale.</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs sales and purchases of ruminants. It requires activities such as RuminantActivityManage, RuminantActivityTrade and RuminantActivitySellDryBreeders to identify individuals to be bought or sold. It will use a pricing schedule if supplied for the herd and can include additional trucking rules and emissions settings.")]
    public class RuminantActivityBuySell : CLEMRuminantActivityBase
    {
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// name of account to use
        /// </summary>
        [Description("Name of bank account to use")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string BankAccountName { get; set; }

        private FinanceType bankAccount = null;
        private List<LabourFilterGroupSpecified> labour = null;
        private TruckingSettings trucking = null;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, true);

            bankAccount = Resources.GetResourceItem(this, typeof(Finance), BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();

            // get trucking settings
            trucking = Apsim.Children(this, typeof(TruckingSettings)).FirstOrDefault() as TruckingSettings;

            // check if pricing is present
            if (bankAccount != null)
            {
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                var breeds = ruminantHerd.Herd.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).GroupBy(a => a.HerdName);
                foreach (var herd in breeds)
                {
                    if (!herd.FirstOrDefault().BreedParams.PricingAvailable())
                    {
                        Summary.WriteWarning(this, String.Format("No pricing schedule has been provided for herd ({0}). No transactions will be recorded for activity ({1}).", herd.Key, this.Name));
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
                // report that this activity was performed as it does not use base GetResourcesRequired
                // only triggered on buy not sell.
                this.TriggerOnActivityPerformed();
            }
        }

        /// <summary>An event handler to call for animal sales</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalSell")]
        private void OnCLEMAnimalSell(object sender, EventArgs e)
        {
            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            int trucks = 0;
            double saleValue = 0;
            double saleWeight = 0;
            int head = 0;
            double AESum = 0;

            // get current untrucked list of animals flagged for sale
            List<Ruminant> herd = this.CurrentHerd(true).Where(a => a.SaleFlag != HerdChangeReason.None).OrderByDescending(a => a.Weight).ToList();

            if (trucking == null)
            {
                // no trucking just sell
                head = herd.Count();
                foreach (var ind in herd)
                {
                    AESum += ind.AdultEquivalent;
                    saleValue += ind.BreedParams.ValueofIndividual(ind, false);
                    saleWeight += ind.Weight;
                    ruminantHerd.RemoveRuminant(ind);
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
                                AESum += ind.AdultEquivalent;
                                load450kgs += ind.Weight / 450.0;
                                saleValue += ind.BreedParams.ValueofIndividual(ind, false);
                                saleWeight += ind.Weight;
                                ruminantHerd.RemoveRuminant(ind);

                                //TODO: work out what to do with suckling calves still with mothers if mother sold.
                            }
                        }
                        if (nonloaded)
                        {
                            Summary.WriteWarning(this, String.Format("There was a problem loading the sale truck as sale individuals did not meet the loading criteria for breed {0}", this.PredictedHerdBreed));
                            break;
                        }
                        herd = this.CurrentHerd(false).Where(a => a.SaleFlag != HerdChangeReason.None).OrderByDescending(a => a.Weight).ToList();
                    }
                    // create trucking emissions
                    trucking.ReportEmissions(trucks, true);
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
                    expenseRequest.Reason = "Transport sales";
                    bankAccount.Remove(expenseRequest);
                }

                foreach (RuminantActivityFee item in Apsim.Children(this, typeof(RuminantActivityFee)))
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
                            expenseRequest.Required = AESum * item.Amount;
                            break;
                        case AnimalPaymentStyleType.ProportionOfTotalSales:
                            expenseRequest.Required = saleValue * item.Amount;
                            break;
                        default:
                            throw new Exception(String.Format("PaymentStyle ({0}) is not supported for ({1}) in ({2})", item.PaymentStyle, item.Name, this.Name));
                    }
                    expenseRequest.Reason = item.Name;
                    bankAccount.Remove(expenseRequest);
                }

                // add and remove from bank
                if(saleValue > 0)
                {
                    bankAccount.Add(saleValue, this.Name, this.PredictedHerdName+" sales");
                }
            }

        }

        private void BuyWithoutTrucking()
        {
            // This activity will purchase animals based on available funds.
            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            // get current untrucked list of animal purchases
            List<Ruminant> herd = ruminantHerd.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).ToList();

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
                    if (newind.SaleFlag == HerdChangeReason.SirePurchase)
                    {
                        value = newind.BreedParams.SirePrice;
                    }
                    else
                    {
                        value = newind.BreedParams.ValueofIndividual(newind, true);
                    }
                    if (cost + value <= fundsAvailable & fundsexceeded == false)
                    {
                        ruminantHerd.PurchaseIndividuals.Remove(newind);
                        newind.ID = ruminantHerd.NextUniqueID;
                        ruminantHerd.AddRuminant(newind);
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
                    ruminantHerd.PurchaseIndividuals.Remove(newind);
                    newind.ID = ruminantHerd.NextUniqueID;
                    ruminantHerd.AddRuminant(newind);
                }
            }

            if (bankAccount != null)
            {
                ResourceRequest purchaseRequest = new ResourceRequest
                {
                    ActivityModel = this,
                    Required = cost,
                    AllowTransmutation = false,
                    Reason = this.PredictedHerdName + " purchases"
                };
                bankAccount.Remove(purchaseRequest);

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
            RuminantHerd ruminantHerd = Resources.RuminantHerd();

            int trucks = 0;
            int head = 0;
            double AESum = 0;
            double fundsAvailable = 0;
            if (bankAccount != null)
            {
                fundsAvailable = bankAccount.FundsAvailable;
            }
            double cost = 0;
            double shortfall = 0;
            bool fundsexceeded = false;

            // get current untrucked list of animal purchases
            List<Ruminant> herd = ruminantHerd.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).OrderByDescending(a => a.Weight).ToList();
            if (herd.Count() == 0) return;

            // if purchase herd > min loads before allowing trucking
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
                            AESum += ind.AdultEquivalent;
                            load450kgs += ind.Weight / 450.0;

                            if (bankAccount != null)  // perform with purchasing
                            {
                                double value = 0;
                                if (ind.SaleFlag == HerdChangeReason.SirePurchase)
                                {
                                    value = ind.BreedParams.SirePrice;
                                }
                                else
                                {
                                    value = ind.BreedParams.ValueofIndividual(ind, true);
                                }
                                if (cost + value <= fundsAvailable & fundsexceeded == false)
                                {
                                    ind.ID = ruminantHerd.NextUniqueID;
                                    ruminantHerd.AddRuminant(ind);
                                    ruminantHerd.PurchaseIndividuals.Remove(ind);
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
                                ind.ID = ruminantHerd.NextUniqueID;
                                ruminantHerd.AddRuminant(ind);
                                ruminantHerd.PurchaseIndividuals.Remove(ind);
                            }

                        }
                    }
                    if (nonloaded)
                    {
                        Summary.WriteWarning(this, String.Format("There was a problem loading the purchase truck as purchase individuals did not meet the loading criteria for breed {0}", this.PredictedHerdBreed));
                        break;
                    }
                    if (shortfall > 0) break;
                    herd = ruminantHerd.PurchaseIndividuals.Where(a => a.BreedParams.Breed == this.PredictedHerdBreed).OrderByDescending(a => a.Weight).ToList();
                }

                // create trucking emissions
                if(trucking != null & trucks > 0 )
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
                        Reason = this.PredictedHerdName + " purchases"
                    };
                    bankAccount.Remove(purchaseRequest);

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
                        ActivityModel = this,
                        AllowTransmutation = false
                    };

                    // calculate transport costs
                    if (trucking != null)
                    {
                        expenseRequest.Required = trucks * trucking.DistanceToMarket * trucking.CostPerKmTrucking;
                        expenseRequest.Reason = "Transport purchases";
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
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;

            for (int i = 0; i < 2; i++)
            {
                string BuySellString = (i == 0) ? "Purchase" : "Sell";

                List<Ruminant> herd = Resources.RuminantHerd().Herd.Where(a => a.SaleFlag.ToString().Contains(BuySellString) & a.Breed == this.PredictedHerdBreed).ToList();
                int head = herd.Count();
                double AE = herd.Sum(a => a.AdultEquivalent);

                if (head > 0)
                {
                    // for each labour item specified
                    foreach (var item in labour)
                    {
                        double daysNeeded = 0;
                        switch (item.UnitType)
                        {
                            case LabourUnitType.Fixed:
                                daysNeeded = item.LabourPerUnit;
                                break;
                            case LabourUnitType.perHead:
                                daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
                                break;
                            case LabourUnitType.perAE:
                                daysNeeded = Math.Ceiling(AE / item.UnitSize) * item.LabourPerUnit;
                                break;
                            default:
                                throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                        }
                        if (daysNeeded > 0)
                        {
                            if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                            ResourceRequestList.Add(new ResourceRequest()
                            {
                                AllowTransmutation = false,
                                Required = daysNeeded,
                                ResourceType = typeof(Labour),
                                ResourceTypeName = "",
                                ActivityModel = this,
                                Reason = BuySellString,
                                FilterDetails = new List<object>() { item }
                            }
                            );
                        }
                    }
                }

            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return; 
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }


    }
}