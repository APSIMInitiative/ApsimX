using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// CLEM Resource Type base model
    ///</summary>
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
                if(!EquivalentMarketStoreDetermined)
                    FindEquivalentMarketStore();

                return EquivalentMarketStore is not null;
            }
        }

        /// <summary>
        /// Detemrines if an equivalent resource has been found in the market
        /// </summary>
        protected bool EquivalentMarketStoreDetermined { get; set; }

        /// <summary>
        /// Determine whether transmutation has been defined for this foodtype
        /// </summary>
        [JsonIgnore]
        public bool TransmutationDefined
        {
            get
            {
                return Structure.FindChildren<Transmutation>().Where(a => a.Enabled).Any();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        protected void OnSetupTypeBase(object sender, EventArgs e)
        {
            parent = FindAncestor<ResourceBaseWithTransactions>();
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
                price = Structure.FindChildren<ResourcePricing>(relativeTo: EquivalentMarketStore).FirstOrDefault(a => a.Enabled && (a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both || a.PurchaseOrSale == priceType) && a.TimingOK);
            else
                price = Structure.FindChildren<ResourcePricing>().FirstOrDefault(a => (a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both | a.PurchaseOrSale == priceType) && a.TimingOK);

            if (price == null)
            {
                // does simulation have finance
                if (FindAncestor<ResourcesHolder>().FindResourceGroup<Finance>() != null)
                {
                    string market = "";
                    if((Parent.Parent as ResourcesHolder).MarketPresent)
                    {
                        if(!(EquivalentMarketStore is null))
                            market = EquivalentMarketStore.CLEMParentName + ".";
                        else
                            market = CLEMParentName + ".";
                    }
                    string warn = $"No pricing is available for [r={market}{Parent.Name}.{Name}]";
                    if (clock != null && Structure.FindChildren<ResourcePricing>().Any())
                        warn += " in month [" + clock.Today.ToString("MM yyyy") + "]";
                    warn += "\r\nAdd [r=ResourcePricing] component to [r=" + market + Parent.Name + "." + Name + "] to include financial transactions for purchases and sales.";

                    if (Summary != null)
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                }
                return new ResourcePricing() { PricePerPacket = 0, PacketSize = 1, UseWholePackets = true };
            }
            return price;
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
                    if (FindAncestor<ResourcesHolder>().FindResourceGroup<Finance>() != null && amount != 0)
                    {
                        string market = "";
                        if ((Parent.Parent as ResourcesHolder).MarketPresent)
                        {
                            if (!(EquivalentMarketStore is null))
                                market = EquivalentMarketStore.CLEMParentName + ".";
                            else
                                market = CLEMParentName + ".";
                        }

                        string warn = $"Cannot report the value of {((converterName.Contains("gain"))?"gains":"losses")} for [r={market}{Parent.Name}.{Name}]";
                        warn += $" in [o=ResourceLedger] as no [{((converterName.Contains("gain")) ? "purchase" : "sale")}] pricing has been provided.";
                        warn += $"\r\nInclude [r=ResourcePricing] component with [{((converterName.Contains("gain")) ? "purchases" : "sales")}] to resource to include all finance conversions";
                        if (Summary != null)
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
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
                    if(GetType() == typeof(HumanFoodStoreType))
                        result *= (this as HumanFoodStoreType).EdibleProportion;

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
            return ConvertTo(converterName, (this as IResourceType).Amount);
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
                return 0;
            else
                return converter.Factor;
        }

        /// <summary>
        /// Locate the equivalent store in a market if available
        /// </summary>
        protected void FindEquivalentMarketStore()
        {
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

            // if not already checked
            if (!EquivalentMarketStoreDetermined)
            {
                // haven't already found a market store
                if (EquivalentMarketStore is null)
                {
                    ResourcesHolder holder = FindAncestor<ResourcesHolder>();
                    // is there a market
                    if (holder != null && holder.FoundMarket != null)
                    {
                        IResourceWithTransactionType store = holder.FoundMarket.Resources.LinkToMarketResourceType(this);
                        if (store != null)
                            EquivalentMarketStore = store as CLEMResourceTypeBase;
                    }
                }
                EquivalentMarketStoreDetermined = true;
            }
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Bank account transaction occured
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
            //ResourceBaseWithTransactions parent = FindAncestor<ResourceBaseWithTransactions>();
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
                    LastGain = amount;

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
        /// Add resources from various objects
        /// </summary>
        /// <param name="resourceAmount">Amount to be applied</param>
        /// <param name="activity">Activity performing this transaction</param>
        /// <param name="relatesToResource">Resource this transaction relates to</param>
        /// <param name="category">Category of this resource transaction</param>
        public void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove amount based on a ResourceRequest object
        /// </summary>
        /// <param name="request"></param>
        public void Remove(ResourceRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the amount of the resource. Use with caution as resources should be changed by add and remove methods.
        /// </summary>
        /// <param name="newAmount"></param>
        public void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            return html;
        }

    }
}
