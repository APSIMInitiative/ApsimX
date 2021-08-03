using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.CLEM.Interfaces;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation cost using defined finance pricing
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("This Transmutation cost uses the pricing defined for the given resource.")]
    [HelpUri(@"Content/Features/Transmutation/TransmutationCostUsePricing.htm")]
    public class TransmutationCostUsePricing : CLEMModel, IValidatableObject, ITransmutationCost
    {
        private ResourcePricing pricing;

        /// <summary>
        /// Type of resource to use
        /// </summary>
        [JsonIgnore]
        [field: NonSerialized]
        public Type ResourceType { get; set; }

        /// <summary>
        /// Name of resource type to use
        /// </summary>
        [Description("Name of Resource Type to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        [Required]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Cost per unit taken from pricing component if available.
        /// </summary>
        [JsonIgnore]
        public double CostPerUnit
        {
            get
            {
                if(pricing != null)
                {
                    return pricing.PricePerPacket;
                }
                else
                {
                    return 0;
                }
            }
            set
            {

            }
        }

        /// <summary>
        /// Get the price object for this transmutation cost
        /// </summary>
        [JsonIgnore]
        public ResourcePricing Pricing { get {return pricing; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmutationCostUsePricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ResourceType = typeof(Finance);
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

            // get pricing if available
            IResourceType parentResource = FindAncestor<CLEMResourceTypeBase>() as IResourceType;
            if (parentResource != null)
            {
                pricing = parentResource.Price(PurchaseOrSalePricingStyleType.Purchase);
            }
            if (pricing is null)
            {
                string[] memberNames = new string[] { "Resource pricing" };
                results.Add(new ValidationResult($"No resource pricing was found for [r={(parentResource as IModel).Name}] required for a price based transmutation [{this.Name}]", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";

            // get the pricing 
            var w = FindAncestor<CLEMResourceTypeBase>() as IResourceType;
            bool multiPrice = (w as IModel).FindAllChildren<ResourcePricing>().Count() > 1;
            ResourcePricing price = w.Price(PurchaseOrSalePricingStyleType.Purchase);
            if (price != null)
            {
                html += "<div class=\"activityentry\">Use ";
                html += (ResourceTypeName != null && ResourceTypeName != "") ? "<span class=\"resourcelink\">" + ResourceTypeName + "</span>" : "<span class=\"errorlink\">Account not set</span>";
                html += " based upon the " + (multiPrice ? "most suitable" : "<span class=\"resourcelink\"> " + price.Name + "</span>") + " packet size and price for this resource</div>";
            }
            else
            {
                html += "<div class=\"errorlink\">";
                html += "Invalid transmutation cost. Cannot find a [r=ResourcePricing] for this resource.";
                html += "</div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        } 
        #endregion

    }

}
