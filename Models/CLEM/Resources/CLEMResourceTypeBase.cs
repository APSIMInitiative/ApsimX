using APSIM.Core;
using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.PMF;
using NetTopologySuite.Precision;
using Newtonsoft.Json;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// CLEM Resource Type base model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Resource Type Base Class and should not be used directly.")]
    [Version(1, 0, 1, "")]
    public class CLEMResourceTypeBase : CLEMModel, IResourceWithTransactionType
    {
        [Link]
        private readonly IClock clock = null;
        private ResourceBaseWithTransactions parent;
        private double amount = 0;
        private Dictionary<ResourceRequest, double> pending = new ();
        private bool marketStoreChecked = false;

        private const double TOLERANCE = 0.0000001;

        /// <summary>
        /// The amount available accounting for unavailable and pending transactions.
        /// </summary>
        [JsonIgnore]
        public double AmountAvailable { get { return AmountTotal - AmountPending - AmountUnavailable; } }

        /// <summary>
        /// Total amount present
        /// </summary>
        [JsonIgnore]
        public double AmountTotal { get { return amount; } }

        /// <summary>
        /// Amount in pending transactions
        /// </summary>
        [JsonIgnore]
        public double AmountPending { get { return pending.Sum(a => a.Value); } }

        /// <summary>
        /// Amount unavailable
        /// </summary>
        [JsonIgnore]
        public double AmountUnavailable { get; private set; }

        /// <summary>
        /// A link to the equivalent market store for trading.
        /// </summary>
        [JsonIgnore]
        public CLEMResourceTypeBase EquivalentMarketStore { get; set; }

        /// <summary>
        /// Has a market store been found
        /// </summary>
        [JsonIgnore]
        public bool MarketStoreExists
        {
            get
            {
                FindEquivalentMarketStore();
                return EquivalentMarketStore is not null;
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSetupTypeBase(object sender, EventArgs e)
        {
            parent = Structure.FindParent<ResourceBaseWithTransactions>(recurse: true);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        protected void OnStartOfTimeStep(object sender, EventArgs e)
        {
            foreach (var item in pending)
            {
                if (item.Value > 0)
                {
                    string warnMessage = $"Pending transaction for [r={Name}] from [a={item.Key.ActivityModel.Name}] has not been completed at the start of the time step. Amount pending of [a={item.Value}] was cleared";
                    Warnings.CheckAndWrite(warnMessage, Summary, this, MessageType.Warning);
                }
            }
            pending.Clear();
        }

        /// <summary>
        /// Set the resource base with transaction parent of the resource type
        /// </summary>
        /// <param name="resourceBaseParent"></param>
        public void SetParentResourceBaseWithTransactions(ResourceBaseWithTransactions resourceBaseParent)
        {
            parent = resourceBaseParent;
        }

        /// <summary>
        /// Does pricing exist for this type
        /// </summary>
        public bool PricingExists(PurchaseOrSalePricingStyleType priceType)
        {
            // find pricing that is ok;
            return Structure.FindChildren<ResourcePricing>().Where(a => a.Enabled & ((a as ResourcePricing).PurchaseOrSale == PurchaseOrSalePricingStyleType.Both | (a as ResourcePricing).PurchaseOrSale == priceType) && (a as ResourcePricing).TimingOK).FirstOrDefault() != null;
        }

        /// <summary>
        /// Resource price
        /// </summary>
        public ResourcePricing Price(PurchaseOrSalePricingStyleType priceType)
        {
            // find pricing that is ok;
            ResourcePricing price = null;

            // if market exists look for market pricing to override local pricing as all transactions will be through the market
            if ((Parent.Parent as ResourcesHolder).FoundMarket is not null && MarketStoreExists)
            {
                price = Structure.FindChildren<ResourcePricing>(relativeTo: EquivalentMarketStore).FirstOrDefault(a => a.Enabled && (a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both || a.PurchaseOrSale == priceType) && a.TimingOK);
            }
            else
            {
                price = Structure.FindChildren<ResourcePricing>().FirstOrDefault(a => (a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both | a.PurchaseOrSale == priceType) && a.TimingOK);
            }

            if (price == null)
            {
                // does simulation have finance
                if (Structure.FindParent<ResourcesHolder>(recurse: true).FindResourceGroup<Finance>() != null)
                {
                    string market = "";
                    if((Parent.Parent as ResourcesHolder).MarketPresent)
                    {
                        if (!(EquivalentMarketStore is null))
                        {
                            market = EquivalentMarketStore.CLEMParentName + ".";
                        }
                        else
                        {
                            market = CLEMParentName + ".";
                        }
                    }
                    string warn = $"No pricing is available for [r={market}{Parent.Name}.{Name}]";
                    if (clock != null && Structure.FindChildren<ResourcePricing>().Any())
                    {
                        warn += " in month [" + clock.Today.ToString("MM yyyy") + "]";
                    }

                    warn += "\r\nAdd [r=ResourcePricing] component to [r=" + market + Parent.Name + "." + Name + "] to include financial transactions for purchases and sales.";

                    if (Summary != null)
                    {
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                    }
                }
                return new ResourcePricing() { PricePerPacket = 0, PacketSize = 1, UseWholePackets = true };
            }
            return price;
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(AmountAvailable);
            }
        }

        /// <summary>
        /// Convert specified amount of this resource to another value using ResourceType supplied converter
        /// </summary>
        /// <param name="converterName">Name of converter to use</param>
        /// <param name="amount">Amount to convert</param>
        /// <returns>Value to report</returns>
        public object ConvertTo(string converterName, double amount)
        {
            // get converted value
            if (converterName.StartsWith("$"))
            {
                // calculate price as special case using pricing structure if present.
                ResourcePricing price;
                PurchaseOrSalePricingStyleType style;
                switch (converterName)
                {
                    case "$gain":
                        style = PurchaseOrSalePricingStyleType.Purchase;
                        break;
                    case "$loss":
                        style = PurchaseOrSalePricingStyleType.Sale;
                        break;
                    default:
                        style = PurchaseOrSalePricingStyleType.Both;
                        break;
                }

                if (PricingExists(style))
                {
                    price = Price(style);
                    if (price.PricePerPacket > 0)
                    {
                        double packets = amount / price.PacketSize;
                        // this does not include whole packet restriction as needs to report full value
                        return packets * price.PricePerPacket;
                    }
                }
                else
                {
                    if (Structure.FindParent<ResourcesHolder>(recurse: true).FindResourceGroup<Finance>() != null && amount != 0)
                    {
                        string market = "";
                        if ((Parent.Parent as ResourcesHolder).MarketPresent)
                        {
                            if (!(EquivalentMarketStore is null))
                            {
                                market = EquivalentMarketStore.CLEMParentName + ".";
                            }
                            else
                            {
                                market = CLEMParentName + ".";
                            }
                        }

                        string warn = $"Cannot report the value of {((converterName.Contains("gain"))?"gains":"losses")} for [r={market}{Parent.Name}.{Name}]";
                        warn += $" in [o=ResourceLedger] as no [{((converterName.Contains("gain")) ? "purchase" : "sale")}] pricing has been provided.";
                        warn += $"\r\nInclude [r=ResourcePricing] component with [{((converterName.Contains("gain")) ? "purchases" : "sales")}] to resource to include all finance conversions";
                        if (Summary != null)
                        {
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                        }
                    }
                }
                return null;
            }
            else
            {
                ResourceUnitsConverter converter = Structure.FindChildren<ResourceUnitsConverter>().Where(a => string.Compare(a.Name, converterName, true) == 0).FirstOrDefault() as ResourceUnitsConverter;
                if (converter != null)
                {
                    double result = amount;
                    // convert to edible proportion for all HumanFoodStore converters
                    // this assumes these are all nutritional. Price will be handled above.
                    if (GetType() == typeof(HumanFoodStoreType))
                    {
                        result *= (this as HumanFoodStoreType).EdibleProportion;
                    }

                    return result * converter.Factor;
                }
                else
                {
                    string warning = "Unable to find the required unit converter [r=" + converterName + "] in resource [r=" + Name + "]";
                    Warnings.Add(warning);
                    Summary.WriteMessage(this, warning, MessageType.Warning);
                    return null;
                }
            }
        }

        /// <summary>
        /// Convert the current amount of this resource to another value using ResourceType supplied converter
        /// </summary>
        /// <param name="converterName">Name of converter to use</param>
        /// <returns>Value to report</returns>
        public object ConvertTo(string converterName)
        {
            return ConvertTo(converterName, (this as IResourceType).AmountAvailable);
        }

        /// <summary>
        /// Convert the current amount of this resource to another value using ResourceType supplied converter
        /// </summary>
        /// <param name="converterName">Name of converter to use</param>
        /// <returns>Value to report</returns>
        public double ConversionFactor(string converterName)
        {
            ResourceUnitsConverter converter = Structure.FindChildren<ResourceUnitsConverter>().Where(a => a.Name.ToLower() == converterName.ToLower()).FirstOrDefault() as ResourceUnitsConverter;
            if (converter is null)
            {
                return 0;
            }
            else
            {
                return converter.Factor;
            }
        }

        /// <summary>
        /// Locate the equivalent store in a market if available
        /// </summary>
        protected void FindEquivalentMarketStore()
        {
            if (marketStoreChecked)
                return;

            // determine what resource types allow market transactions
            switch (this)
            {
                case FinanceType _:
                case HumanFoodStoreType _:
                case AnimalFoodStoreType _:
                //ToDo: add WaterType AnimalFoodType EquipmentType GreenhousGasesType _: as needed
                case ProductStoreType _:
                    break;
                default:
                    throw new NotImplementedException($"[r={Parent.GetType().Name}] resource does not currently support transactions to and from a [m=Market]\r\nThis problem has arisen because a resource transaction in the code is flagged to exchange resources [r={this.Name}] with the [m=Market]\r\nPlease contact developers for assistance.");
            }

            ResourcesHolder holder = Structure.FindParent<ResourcesHolder>(recurse: true);
            // is there a market
            if (holder is not null && holder.FoundMarket is not null)
            {
                IResourceWithTransactionType store = holder.FoundMarket.Resources.LinkToMarketResourceType(this);
                if (store is not null)
                {
                    EquivalentMarketStore = store as CLEMResourceTypeBase;
                }
            }

            marketStoreChecked = true;
        }

        /// <summary>
        /// Determines if any transmutation is defined for this resource type
        /// </summary>
        public bool TransmutationDefined
        {
            get
            {
                return Structure.FindChildren<Transmutation>().Where(a => a.Enabled).Any();
            }
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [JsonIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Bank account transaction occurred
        /// </summary>
        public virtual event EventHandler TransactionOccurred;

        /// <summary>
        /// Amount of last gain transaction
        /// </summary>
        [JsonIgnore]
        public double LastGain { get; set; }

        /// <summary>
        /// Report a transaction with details for reporting
        /// </summary>
        /// <param name="type"></param>
        /// <param name="amount"></param>
        /// <param name="activity"></param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        /// <param name="resource"></param>
        /// <param name="extraInformation"></param>
        public void ReportTransaction(TransactionType type, double amount, CLEMModel activity, string relatesToResource, string category, CLEMResourceTypeBase resource, object extraInformation = null)
        {
            if (parent != null)
            {
                // update the last transaction object of parent
                parent.LastTransaction.TransactionType = type;
                parent.LastTransaction.Amount = amount;
                parent.LastTransaction.Activity = activity;
                parent.LastTransaction.RelatesToResource = relatesToResource;
                parent.LastTransaction.Category = category;
                parent.LastTransaction.ResourceType = resource;

                if (type == TransactionType.Gain)
                {
                    LastGain = amount;
                }

                LastTransaction = parent.LastTransaction;
                TransactionOccurred?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Handles reporting of transactions
        /// </summary>
        public void PerformTransactionOccurred()
        {
            TransactionOccurred?.Invoke(this, null);
        }

        /// <summary>
        /// Add an amount to the resource.
        /// </summary>
        /// <param name="amountToAdd">Amount to add to resource store</param>
        /// <returns></returns>
        protected void Add(double amountToAdd)
        {
            amount += amountToAdd;
            if (amount < TOLERANCE) amount = 0;
        }

        /// <summary>
        /// Add resource to store from various sources with transaction handling and reporting.
        /// </summary>
        /// <param name="resourceAmount">Object containing amount to be applied</param>
        /// <param name="activity">Activity performing this transaction</param>
        /// <param name="relatesToResource">Resource this transaction relates to</param>
        /// <param name="category">Category of this resource transaction</param>
        public void AddToResource(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            // overridden methods will handle other types of resourceAmount object, this base method only handles double amounts and is used by most resource types. If other types are needed (e.g. food with nutritional information) then the resource type can override this method and handle the additional information as needed.
            if (resourceAmount.GetType().ToString() != "System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }

            double amountAdded = (double)resourceAmount;
            if (amountAdded > 0)
            {
                Add(amountAdded);
                ReportTransaction(TransactionType.Gain, amountAdded, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        /// Remove amount based on a ResourceRequest object
        /// </summary>
        /// <param name="request">Object containing amount required</param>
        public void RemoveFromResource(ResourceRequest request)
        {
            if (request.Required == 0)
                return;

            double amountRemoved = RemoveFromResource(request.Required, request.TransactionPending ? request : null);
            request.Provided = amountRemoved;

            if (!request.TransactionPending)
            {
                PerformTransaction(request, request.TransactionPending);
            }
        }

        /// <summary>
        /// Remove a specified amount from the resource with additional pending details.
        /// </summary>
        /// <param name="amountToRemove">Amount to remove from resource store</param>
        /// <param name="pendingRequest">
        /// Provides a the request if this is a pending transaction that has not yet been completed. This will not
        /// reduce the amount total until available until the transaction is completed.
        /// </param>
        /// <returns>Amount removed</returns>
        protected double RemoveFromResource(double amountToRemove, ResourceRequest pendingRequest)
        {
            amountToRemove = Math.Min(amountToRemove, AmountAvailable);
            if (pendingRequest is not null)
            {
                if (pending.ContainsKey(pendingRequest))
                {
                    pending[pendingRequest] += amountToRemove;
                }
                else
                {
                    pending.Add(pendingRequest, amountToRemove);
                }
            }
            else
            {
                amount -= amountToRemove;
                if (amount < TOLERANCE) amount = 0;
            }
            return amountToRemove;
        }

        /// <inheritdoc/>
        public void DecreasePending(ResourceRequest request, double amount)
        {
            if (pending.Count == 0 || !request.TransactionPending || !pending.ContainsKey(request))
            {
                string warnMessage = $"Attempted to reduce a pending transaction for [r={Name}] that does not exist or is not pending.";
                Warnings.CheckAndWrite(warnMessage, Summary, this, MessageType.Warning);
                return;
            }
            amount = Math.Min(amount, pending[request]);
            pending[request] -= amount;
        }

        ///// <inheritdoc/>
        //public void DecreasePendingByProportion(ResourceRequest request, double proportion)
        //{
        //    if (pending.Count == 0 || !request.TransactionPending || !pending.ContainsKey(request))
        //    {
        //        string warnMessage = $"Attempted to reduce a pending transaction for [r={Name}] that does not exist or is not pending.";
        //        Warnings.CheckAndWrite(warnMessage, Summary, this, MessageType.Warning);
        //        return;
        //    }
        //    pending[request] *= (1 - proportion);
        //}

        /// <summary>
        /// Performs a transaction by specified amount.
        /// </summary>
        /// <param name="request">The amount of the transaction.</param>
        /// <param name="handlePendingTransaction">
        /// This transaction should handle any pending amount rather than the amount provided.
        /// </param>
        public virtual void PerformTransaction(ResourceRequest request, bool handlePendingTransaction = false)
        {
            double amountToRemove = request.Provided;
            if (handlePendingTransaction)
            {
                if (pending.ContainsKey(request))
                {
                    amountToRemove = pending[request];
                    pending.Remove(request);
                }
                else
                {
                    amountToRemove = 0;
                }
            }

            if (amountToRemove == 0)
                return;

            // if this request aims to trade with a market see if we need to set up details for the first time
            if (request.MarketTransactionMultiplier > 0)
            {
                FindEquivalentMarketStore();
                // send to market if needed
                if (MarketStoreExists)
                {
                    EquivalentMarketStore.AddToResource(amountToRemove * request.MarketTransactionMultiplier, request.ActivityModel, this.NameWithParent, "Farm sales");
                }
            }
            ReportTransaction(TransactionType.Loss, amountToRemove, request.ActivityModel, request.RelatesToResource, request.Category, this);
        }

        /// <summary>A method to arrange clearing the activity status on CLEMStartOfTimeStep event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMManagePendingTransactions")]
        public virtual void ManagePendingTransactions(object sender, EventArgs e)
        {
            if (pending.Count == 0)
                return;

            foreach (var item in pending)
            {
                if (item.Value > 0)
                {
                    item.Key.Provided = item.Value;
                    amount -= item.Key.Provided;
                    PerformTransaction(item.Key, true);
                }
            }
            pending.Clear();
        }

        /// <summary>
        /// Set the amount of the resource. Use with caution as resources should be changed by add and remove methods.
        /// </summary>
        /// <param name="total">The total amount</param>
        public void Set(double total)
        {
            amount = total;
            if (pending.Count > 0) 
            {
                string warnMessage = $"Pending transactions for [r={Name}] have not been completed at the time of a Set operation. Amount pending of [a={AmountPending}] was not reported";
                Warnings.CheckAndWrite(warnMessage, Summary, this, MessageType.Warning);
            }
            pending.Clear();
        }

        /// <summary>
        /// Set the amount of the resource that is unavailable. This is influence TotalAvailable
        /// </summary>
        /// <param name="amount">The amount not available</param>
        public void SetUnavailable(double amount)
        {
            this.AmountUnavailable = amount;
            if (pending.Count > 0)
            {
                string warnMessage = $"Pending transactions for [r={Name}] have not been completed at the time of a SetUnavailable operation. Amount pending of [a={AmountPending}] may no be correct for new unavailable details";
                Warnings.CheckAndWrite(warnMessage, Summary, this, MessageType.Warning);
            }
        }

    }
}
