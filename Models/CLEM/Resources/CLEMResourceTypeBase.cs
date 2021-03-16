using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// CLEM Resource Type base model
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("This is the CLEM Resource Type Base Class and should not be used directly.")]
    [Version(1, 0, 1, "")]
    public class CLEMResourceTypeBase : CLEMModel
    {
        [Link]
        [NonSerialized]
        Clock Clock = null;

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
                {
                    FindEquivalentMarketStore();
                }
                return !(EquivalentMarketStore is null); 
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
                return this.FindAllChildren<Transmutation>().Where(a => a.Enabled).Count() > 0;
            }
        }

        /// <summary>
        /// Does pricing exist for this type
        /// </summary>
        public bool PricingExists(PurchaseOrSalePricingStyleType priceType)
        {
            // find pricing that is ok;
            return this.FindAllChildren<ResourcePricing>().Where(a => a.Enabled & ((a as ResourcePricing).PurchaseOrSale == PurchaseOrSalePricingStyleType.Both | (a as ResourcePricing).PurchaseOrSale == priceType) && (a as ResourcePricing).TimingOK).FirstOrDefault() != null;
        }

        /// <summary>
        /// Resource price
        /// </summary>
        public ResourcePricing Price(PurchaseOrSalePricingStyleType priceType)
        {
            // find pricing that is ok;
            ResourcePricing price = null;

            // if market exists look for market pricing to override local pricing as all transactions will be through the market
            if (!((this.Parent.Parent as ResourcesHolder).FoundMarket is null) && this.MarketStoreExists)
            {
                price = EquivalentMarketStore.FindAllChildren<ResourcePricing>().FirstOrDefault(a => a.Enabled && ((a as ResourcePricing).PurchaseOrSale == PurchaseOrSalePricingStyleType.Both || (a as ResourcePricing).PurchaseOrSale == priceType) && (a as ResourcePricing).TimingOK);
            }
            else
            {
                price = FindAllChildren<ResourcePricing>().FirstOrDefault(a => ((a as ResourcePricing).PurchaseOrSale == PurchaseOrSalePricingStyleType.Both | (a as ResourcePricing).PurchaseOrSale == priceType) && (a as ResourcePricing).TimingOK);
            }

            if (price == null)
            {
                // does simulation have finance
                if (FindAncestor<ResourcesHolder>().FinanceResource() != null)
                {
                    string market = "";
                    if((this.Parent.Parent as ResourcesHolder).MarketPresent)
                    {
                        if(!(this.EquivalentMarketStore is null))
                        {
                            market = this.EquivalentMarketStore.CLEMParentName + ".";
                        }
                        else
                        {
                            market = this.CLEMParentName + ".";
                        }
                    }
                    string warn = $"No pricing is available for [r={market}{this.Parent.Name}.{this.Name}]";
                    if (Clock != null && FindAllChildren<ResourcePricing>().Any())
                    {
                        warn += " in month [" + Clock.Today.ToString("MM yyyy") + "]";
                    }
                    warn += "\r\nAdd [r=ResourcePricing] component to [r=" + market + this.Parent.Name + "." + this.Name + "] to include financial transactions for purchases and sales.";

                    if (!Warnings.Exists(warn) & Summary != null)
                    {
                        Summary.WriteWarning(this, warn);
                        Warnings.Add(warn);
                    }
                }
                return new ResourcePricing() { PricePerPacket=0, PacketSize=1, UseWholePackets=true };
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
            if(converterName.StartsWith("$"))
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
                    if(FindAncestor<ResourcesHolder>().FinanceResource() != null && amount != 0)
                    {
                        string market = "";
                        if ((this.Parent.Parent as ResourcesHolder).MarketPresent)
                        {
                            if (!(this.EquivalentMarketStore is null))
                            {
                                market = this.EquivalentMarketStore.CLEMParentName + ".";
                            }
                            else
                            {
                                market = this.CLEMParentName + ".";
                            }
                        }
                        string warn = $"Cannot report the value of {((converterName.Contains("gain"))?"gains":"losses")} for [r={market}{this.Parent.Name}.{this.Name}]";
                        warn += $" in [o=ResourceLedger] as no [{((converterName.Contains("gain")) ? "purchase" : "sale")}] pricing has been provided.";
                        warn += $"\r\nInclude [r=ResourcePricing] component with [{((converterName.Contains("gain")) ? "purchases" : "sales")}] to resource to include all finance conversions";
                        if (!Warnings.Exists(warn) & Summary != null)
                        {
                            Summary.WriteWarning(this, warn);
                            Warnings.Add(warn);
                        }
                    }
                }
                return null;
            }
            else
            {
                ResourceUnitsConverter converter = this.FindAllChildren<ResourceUnitsConverter>().Where(a => string.Compare(a.Name, converterName, true) == 0).FirstOrDefault() as ResourceUnitsConverter;
                if (converter != null)
                {
                    double result = amount;
                    // convert to edible proportion for all HumanFoodStore converters
                    // this assumes these are all nutritional. Price will be handled above.
                    if(this.GetType() == typeof(HumanFoodStoreType))
                    {
                        result *= (this as HumanFoodStoreType).EdibleProportion;
                    }
                    return result * converter.Factor;
                }
                else
                {
                    string warning = "Unable to find the required unit converter [r=" + converterName + "] in resource [r=" + this.Name + "]";
                    Warnings.Add(warning);
                    Summary.WriteWarning(this, warning);
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
            ResourceUnitsConverter converter = this.FindAllChildren<ResourceUnitsConverter>().Where(a => a.Name.ToLower() == converterName.ToLower()).FirstOrDefault() as ResourceUnitsConverter;
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
            // determine what resource types allow market transactions
            switch (this.GetType().Name)
            {
                case "FinanceType":
                case "HumanFoodStoreType":
                //case "WaterType":
                //case "AnimalFoodType":
                //case "EquipmentType":
                //case "GreenhousGasesType":
                case "ProductStoreType":
                    break;
                default:
                    throw new NotImplementedException($"\r\n[r={this.Parent.GetType().Name}] resource does not currently support transactions to and from a [m=Market]\r\nThis problem has arisen because a resource transaction in the code is flagged to exchange resources with the [m=Market]\r\nPlease contact developers for assistance.");
            }

            // if not already checked
            if(!EquivalentMarketStoreDetermined)
            {
                // haven't already found a market store
                if(EquivalentMarketStore is null)
                {
                    ResourcesHolder holder = FindAncestor<ResourcesHolder>();
                    // is there a market
                    if (holder != null && holder.FoundMarket != null)
                    {
                        IResourceWithTransactionType store = holder.FoundMarket.Resources.LinkToMarketResourceType(this);
                        if (store != null)
                        {
                            EquivalentMarketStore = store as CLEMResourceTypeBase;
                        }
                    }
                }
                EquivalentMarketStoreDetermined = true;
            }
        }

        /// <summary>
        /// Amount of last gain transaction
        /// </summary>
        public double LastGain { get; set; }

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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

    }
}
