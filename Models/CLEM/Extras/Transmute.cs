using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using Models.CLEM.Interfaces;
using System.IO;
using APSIM.Shared.Utilities;

namespace Models.CLEM
{
    ///<summary>
    /// A resource transmute component used as a child of a Transmutation component
    /// Determines the amount of a specified resource (B) required for the transmutation of shortfall resource (A)
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("Identifies how a resource (B, e.g. money) is transmuted into a shortfall resource (A, e.g.food)")]
    [Version(1, 0, 1, "")]
    [Version(2, 0, 0, "Refactor from TransmutationCost with generic functionality and include TransmutationCostUsePrice")]
    [HelpUri(@"Content/Features/Transmutation/Transmute.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class Transmute : CLEMModel, IValidatableObject, ITransmute
    {
        [Link(IsOptional = true)]
        private ResourcesHolder resources = null;
        private ResourcePricing transmutePricing;
        private ResourcePricing shortfallPricing;
        private double shortfallPacketSize = 1;
        private bool shortfallWholePackets = false;
        private double shortfallPricePacketMultiplier;
        private FinanceType financeType; 

        /// <inheritdoc/>
        [JsonIgnore]
        [field: NonSerialized]
        public IResourceType TransmuteResourceType { get; set; }

        /// <inheritdoc/>
        [Description("Resource to transmute (B)")]
        [Category("Direct style:By pricing style", "All:All")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(Finance), typeof(HumanFoodStore), typeof(GreenhouseGases), typeof(Labour), typeof(ProductStore), typeof(WaterStore) } })]
        [Required]
        public string TransmuteResourceTypeName { get; set; }

        /// <inheritdoc/>
        [Description("Amount (B) per packet (A)")]
        [Category("Direct style", "All")]
        [Core.Display(EnabledCallback = "AmountPerPacketEnabled")]
        [Required, GreaterThanEqualValue(0)]
        public double AmountPerPacket { get; set; }

        /// <inheritdoc/>
        [Description("Transmute style")]
        [Category("Direct style:By pricing style", "All:All")]
        public TransmuteStyle TransmuteStyle { get; set; }

        ///<inheritdoc/>
        public ResourceBaseWithTransactions ResourceGroup { get; set; }

        ///<inheritdoc/>
        [Description("Resource for price-based transactions")]
        [Category("By pricing style", "All")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "No transactions", typeof(Finance) } })]
        [System.ComponentModel.DefaultValueAttribute("No transactions")]
        public string FinanceTypeForTransactionsName { get; set; }

        /// <summary>
        /// Method to determine if direct transmute style will enable the amount property
        /// </summary>
        public bool AmountPerPacketEnabled() { return TransmuteStyle == TransmuteStyle.Direct; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Transmute()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
            base.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // determine resource type from name
            TransmuteResourceType = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, TransmuteResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);

            if (TransmuteResourceType != null)
            {
                ResourceGroup = (TransmuteResourceType as IModel).Parent as ResourceBaseWithTransactions;

                var shortfallResourceType = (this as IModel).FindAncestor<IResourceType>();
                shortfallPacketSize = (Parent as Transmutation).TransmutationPacketSize;
                shortfallWholePackets = (Parent as Transmutation).UseWholePackets;

                // get pricing of shortfall resource
                if (shortfallResourceType != null && TransmuteStyle == TransmuteStyle.UsePricing)
                {
                    // get pricing
                    if ((shortfallResourceType as CLEMResourceTypeBase).MarketStoreExists)
                        if ((shortfallResourceType as CLEMResourceTypeBase).EquivalentMarketStore.PricingExists(PurchaseOrSalePricingStyleType.Purchase))
                            shortfallPricing = (shortfallResourceType as CLEMResourceTypeBase).EquivalentMarketStore.Price(PurchaseOrSalePricingStyleType.Purchase);

                    if(shortfallPricing is null)                    
                        shortfallPricing = shortfallResourceType.Price(PurchaseOrSalePricingStyleType.Purchase);

                    shortfallPacketSize = shortfallPricing.PacketSize;
                    shortfallWholePackets = shortfallPricing.UseWholePackets;

                    // get pricing of transmute resource
                    if (!(TransmuteResourceType is FinanceType))
                    {
                        transmutePricing = TransmuteResourceType.Price(PurchaseOrSalePricingStyleType.Sale);
                        if (FinanceTypeForTransactionsName != "No transactions")
                            // link to first bank account
                            financeType = resources.FindResourceType<Finance, FinanceType>(this, FinanceTypeForTransactionsName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportWarning);
                    }
                    else
                        transmutePricing = shortfallPricing;
                }

                shortfallPricePacketMultiplier = (Parent as Transmutation).TransmutationPacketSize / shortfallPacketSize;

            }
        }

        ///<inheritdoc/>
        public bool DoTransmute(ResourceRequest request, double shortfall, double requiredByActivities, ResourcesHolder holder, bool queryOnly)
        {
            double transPackets = 0;
            double shortfallPackets = shortfall / shortfallPacketSize;
            switch (TransmuteStyle)
            {
                case TransmuteStyle.Direct:
                    if ((Parent as Transmutation).UseWholePackets)
                        shortfallPackets = Math.Ceiling(shortfallPackets);
                    request.Required = shortfallPackets * AmountPerPacket;
                    break;
                case TransmuteStyle.UsePricing:
                    if (MathUtilities.IsPositive(shortfallPricing.CurrentPrice))
                    {
                        if (MathUtilities.FloatsAreEqual(transmutePricing.CurrentPrice, 0))
                            // no value of transmute resource
                            request.Required = 0;
                        else
                        {
                            if (shortfallWholePackets)
                                shortfallPackets = Math.Ceiling(shortfallPackets);
                            request.Required = shortfallPackets * shortfallPricing.CurrentPrice;

                            if(transmutePricing != shortfallPricing && transmutePricing.UseWholePackets)
                            {
                                transPackets = shortfall / transmutePricing.PacketSize;
                                if (transmutePricing.UseWholePackets)
                                    transPackets = Math.Ceiling(transPackets);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }

            if (queryOnly)
            {
                return request.Required + requiredByActivities <= TransmuteResourceType.Amount;
            }
            else
            {
                if (TransmuteStyle == TransmuteStyle.UsePricing && !(TransmuteResourceType is FinanceType))
                {
                    // add finance transaction to sell transmute
                    financeType.Add(transPackets * transmutePricing.CurrentPrice, request.ActivityModel, TransmuteResourceTypeName, request.Category);

                    // add finance transaction to buy shortfall
                    ResourceRequest financeRequest = new ResourceRequest()
                    {
                        Resource = financeType,
                        Required = shortfallPackets * shortfallPricing.CurrentPrice,
                        RelatesToResource = request.ResourceTypeName,
                        ResourceType = typeof(Finance),
                        ActivityModel = request.ActivityModel,
                        Category = request.Category,
                    };
                    financeType.Remove(financeRequest);
                }
                else
                {
                    TransmuteResourceType.Remove(request);
                }
            }
            return true;
        }

        ///<inheritdoc/>
        public double ShortfallPackets(double amount)
        {
            // if shortfall price packet multiplier != 1
            // the packet size being used (pricing) is different to the shotfall resource packet size specified

            double unitsNeeded = amount / (shortfallPacketSize==0?1: shortfallPacketSize);
            if (shortfallWholePackets)
                unitsNeeded = Math.Ceiling(unitsNeeded);

            return unitsNeeded / shortfallPricePacketMultiplier;
        }

        #region validation

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (TransmuteResourceTypeName != null && TransmuteResourceTypeName != "")
            {
                if (!TransmuteResourceTypeName.Contains("."))
                {
                    string[] memberNames = new string[] { "ResourceTypeName" };
                    results.Add(new ValidationResult("Invalid resource type entry. Please select resource type from the drop down list provided or ensure the value is formatted as ResourceGroup.ResourceType", memberNames));
                }
                else
                {
                    object result = resources.FindResource<ResourceBaseWithTransactions>(TransmuteResourceTypeName.Split('.').First());
                    if (result == null)
                    {
                        Summary.WriteMessage(this, $"Could not find resource group [r={TransmuteResourceTypeName.Split('.').First()}] in transmute [{this.Name}]{Environment.NewLine}The parent transmutation [{(this.Parent as CLEMModel).NameWithParent}] will not suceed without this resource and will not be performed", MessageType.Warning);
                    }
                    else
                    {
                        object resultType = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, TransmuteResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
                        if (resultType is null)
                        {
                            string[] memberNames = new string[] { "ResourceType" };
                            results.Add(new ValidationResult($"Could not find resource [r={TransmuteResourceTypeName.Split('.').First()}][r={TransmuteResourceTypeName.Split('.').Last()}] for [{this.Name}]{Environment.NewLine}The parent transmutation [{(this.Parent as CLEMModel).NameWithParent}] will not suceed without this resource and will not be performed", memberNames));
                        }
                    }
                }
            }

            // get pricing if available
            IResourceType parentResource = FindAncestor<CLEMResourceTypeBase>() as IResourceType;
            if (TransmuteStyle == TransmuteStyle.UsePricing)
            {
                if (shortfallPricing is null)
                {
                    string[] memberNames = new string[] { "Shortfall resource pricing" };
                    results.Add(new ValidationResult($"No resource pricing was found for [r={(parentResource as CLEMModel).NameWithParent}] required for a price based transmute [{this.Name}]{Environment.NewLine}Provide a pricing for the shortfall resource or use Direct transmute style", memberNames));
                }

                if (!(TransmuteResourceType is FinanceType))
                {
                    if (transmutePricing is null)
                    {
                        string[] memberNames = new string[] { "Transmute resource pricing" };
                        results.Add(new ValidationResult($"No resource pricing was found for [r={(TransmuteResourceType as CLEMModel).NameWithParent}] required for a price based transmute [{this.Name}]{Environment.NewLine}Provide a pricing for the transmute resource or use Direct transmute style", memberNames));
                    }
                }
            }

            return results;
        }
        #endregion

        #region descriptive summary

        ///<inheritdoc/>
        public override string ModelSummaryNameTypeHeaderText()
        {
            return Transmute.AddTransmuteStyleText(this);
        }

        /// <summary>
        /// Create additional text for transmute headers
        /// </summary>
        /// <param name="transmute"></param>
        /// <returns></returns>
        public static string AddTransmuteStyleText(ITransmute transmute)
        {
            using StringWriter htmlWriter = new();
            htmlWriter.WriteLine((transmute as IModel).Name);
            if (transmute.TransmuteStyle == TransmuteStyle.Direct)
                htmlWriter.WriteLine(": B&#8594;A");
            else
            {
                if (transmute.FinanceTypeForTransactionsName != null && transmute.FinanceTypeForTransactionsName != "")
                    htmlWriter.WriteLine(": B&#8594;$ $&#8594;A");
                else
                    htmlWriter.WriteLine(": B&#8594;$&#8594;A");
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("<div class=\"activityentry\">");
            if (TransmuteStyle == TransmuteStyle.Direct && AmountPerPacket > 0)
            {
                htmlWriter.Write($"<span class=\"setvalue\">{AmountPerPacket:#,##0.##}</span> x ");
            }

            if (TransmuteResourceTypeName != null && TransmuteResourceTypeName != "")
                htmlWriter.Write($"<span class=\"resourcelink\">{TransmuteResourceTypeName}</span>");
            else
                htmlWriter.Write("<span class=\"errorlink\">No Transmute resource (B) set</span>");

            if (TransmuteStyle == TransmuteStyle.UsePricing)
            {
                htmlWriter.Write($" using the resource pricing details");
                if (FinanceTypeForTransactionsName != null && FinanceTypeForTransactionsName != "")
                    htmlWriter.Write($" and all financial Transactions of sales and purchases using <span class=\"resourcelink\">{TransmuteResourceTypeName}</span>");
            }
            htmlWriter.WriteLine("</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }

}
