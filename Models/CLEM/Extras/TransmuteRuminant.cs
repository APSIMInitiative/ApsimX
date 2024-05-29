using APSIM.Shared.Utilities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM
{
    ///<summary>
    /// Determines the individual ruminans required for the transmutation
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("Identifies how rumiants (as resource B) are transmuted into a shortfall resource (A, e.g.food)")]
    [HelpUri(@"Content/Features/Transmutation/TransmuteRuminant.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class TransmuteRuminant : CLEMModel, ITransmute, IValidatableObject
    {
        [Link(IsOptional = true)]
        private ResourcesHolder resources = null;

        private double shortfallPacketSize = 1;
        private bool shortfallWholePackets = false;
        private FinanceType financeType = null;
        private ResourcePricing shortfallPricing = null;
        private IEnumerable<RuminantGroup> groupings;

        /// <inheritdoc/>
        [JsonIgnore]
        [field: NonSerialized]
        public IResourceType TransmuteResourceType { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public string TransmuteResourceTypeName { get; set; }

        /// <inheritdoc/>
        [Description("Transmute style")]
        public TransmuteStyle TransmuteStyle { get ; set; }

        /// <summary>
        /// Style for direct exchange 
        /// </summary>
        [Description("Measure of ruminant (for direct transmute)")]
        public PricingStyleType DirectExhangeStyle { get; set; }

        /// <inheritdoc/>
        [Description("Amount (B) per packet (A)")]
        [Required, GreaterThanEqualValue(0)]
        [Core.Display(EnabledCallback = "AmountPerPacketEnabled")]
        public double AmountPerPacket { get; set; }

        ///<inheritdoc/>
        [JsonIgnore]
        public ResourceBaseWithTransactions ResourceGroup { get; set; }

        ///<inheritdoc/>
        [Description("Resource for price-based transactions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "No transactions", typeof(Finance) } })]
        [System.ComponentModel.DefaultValueAttribute("No transactions")]
        public string FinanceTypeForTransactionsName { get; set; }

        /// <summary>
        /// Method to determine if direct transmute style will enable the amount property
        /// </summary>
        public bool AmountPerPacketEnabled() { return TransmuteStyle == TransmuteStyle.Direct; }

        ///<inheritdoc/>
        public bool DoTransmute(ResourceRequest request, double shortfall, double requiredByActivities, ResourcesHolder holder, bool queryOnly)
        {
            double needed = 0;
            switch (TransmuteStyle)
            {
                case TransmuteStyle.Direct:
                    needed = shortfall / shortfallPacketSize * AmountPerPacket;
                    break;
                case TransmuteStyle.UsePricing:
                    needed = shortfall / shortfallPacketSize * shortfallPricing.CurrentPrice;
                    break;
                default:
                    break;
            }

            // walk through herd based on filters and take those needed to achieve exchange

            double available = 0;
            foreach (var group in groupings)
            {
                foreach (var ind in group.Filter((ResourceGroup as RuminantHerd).Herd.Where(a => !a.IsReadyForSale)))
                {
                    switch (TransmuteStyle)
                    {
                        case TransmuteStyle.Direct:
                            switch (DirectExhangeStyle)
                            {
                                case PricingStyleType.perHead:
                                    available += 1;
                                    break;
                                case PricingStyleType.perKg:
                                    available += ind.Weight.Live;
                                    break;
                                case PricingStyleType.perAE:
                                    available += ind.Weight.AdultEquivalent;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case TransmuteStyle.UsePricing:
                            available += ind.BreedDetails.GetPriceGroupOfIndividual(ind, PurchaseOrSalePricingStyleType.Sale)?.CurrentPrice ?? 0;
                            break;
                        default:
                            break;
                    }

                    if (!queryOnly)
                        // remove individual from herd immediately
                        (ResourceGroup as RuminantHerd).Herd.Remove(ind);
                    
                    if (MathUtilities.IsGreaterThanOrEqual(available, needed))
                    {
                        if (queryOnly)
                            return true;
                        break;
                    }
                }
            }

            if (!queryOnly)
            {
                if (TransmuteStyle == TransmuteStyle.UsePricing && financeType != null)
                {
                    // finance transaction from sale of animals
                    financeType.Add(available, request.ActivityModel, TransmuteResourceTypeName, request.Category);

                    // finance transaction from purchase of shortfall
                    ResourceRequest financeRequest = new ResourceRequest()
                    {
                        Resource = financeType,
                        Required = shortfall / shortfallPacketSize * shortfallPricing.CurrentPrice,
                        RelatesToResource = request.ResourceTypeName,
                        ResourceType = typeof(Finance),
                        ActivityModel = request.ActivityModel,
                        Category = request.Category,
                    };
                    financeType.Remove(financeRequest);
                }
            }
            return true;
        }

        ///<inheritdoc/>
        public double ShortfallPackets(double amount)
        {
            double unitsNeeded = amount / shortfallPacketSize;
            if (shortfallWholePackets)
                unitsNeeded = Math.Ceiling(unitsNeeded);
            return unitsNeeded;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmuteRuminant()
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
            // get herd for transmutating
            ResourceGroup = resources.FindResourceGroup<RuminantHerd>();
            TransmuteResourceTypeName = ResourceGroup.Name;
            shortfallPacketSize = (Parent as Transmutation).TransmutationPacketSize;
            shortfallWholePackets = (Parent as Transmutation).UseWholePackets;
            groupings = ResourceGroup.FindAllChildren<RuminantGroup>();

            var shortfallResourceType = this.FindAncestor<IResourceType>();
            if (shortfallResourceType != null && TransmuteStyle == TransmuteStyle.UsePricing)
            {
                shortfallPricing = shortfallResourceType.Price(PurchaseOrSalePricingStyleType.Purchase);
                if (FinanceTypeForTransactionsName != "No transactions")
                    // link to first bank account
                    financeType = resources.FindResourceType<Finance, FinanceType>(this, FinanceTypeForTransactionsName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportWarning);
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ResourceGroup is null)
            {
                IResourceType parentResource = FindAncestor<CLEMResourceTypeBase>() as IResourceType;
                yield return new ValidationResult($"No [r=Ruminant] resource was found for a herd-based transmute [r={Name}] for [r={parentResource.Name}]", new string[] { "Ruminant herd resource" });
            }
        }
        #endregion

        #region descriptive summary

        ///<inheritdoc/>
        public override string ModelSummaryNameTypeHeaderText()
        {
            return Transmute.AddTransmuteStyleText(this);
        }

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("<div class=\"activityentry\">");
            if (TransmuteStyle == TransmuteStyle.Direct)
            {
                string directexchangeStyleText = "";
                switch (DirectExhangeStyle)
                {
                    case PricingStyleType.perHead:
                        directexchangeStyleText = "head of ";
                        break;
                    case PricingStyleType.perKg:
                        directexchangeStyleText = "kg live weight head of ";
                        break;
                    case PricingStyleType.perAE:
                        directexchangeStyleText = "animal equivalents of ";
                        break;
                    default:
                        break;
                }
                if (AmountPerPacket > 0)
                    htmlWriter.Write($"<span class=\"setvalue\">{AmountPerPacket:#,##0.##}</span> {directexchangeStyleText} ");
                else
                    htmlWriter.Write($"<span class=\"errorlink\">Not set</span> {directexchangeStyleText} ");
            }

            IModel ruminants = this.FindAncestor<ResourcesHolder>().FindResourceGroup<RuminantHerd>();
            if (ruminants is null)
                htmlWriter.Write("<span class=\"errorlink\">Herd not found</span>");
            else
                htmlWriter.Write($"<span class=\"resourcelink\">{ruminants.Name}</span>");

            htmlWriter.Write($" (B) are taken from the following groups to supply shortfall resource (A) ");

            if (TransmuteStyle == TransmuteStyle.UsePricing)
            {
                htmlWriter.Write($" using the herd pricing details");
                if (FinanceTypeForTransactionsName != null && FinanceTypeForTransactionsName != "")
                    htmlWriter.Write($" with all financial Transactions of sales and purchases using <span class=\"resourcelink\">{TransmuteResourceTypeName}</span>");
            }
            htmlWriter.WriteLine("</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }
}
