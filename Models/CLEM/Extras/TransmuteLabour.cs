using Models.Core;
using Models.CLEM.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Models.CLEM.Interfaces;
using System.Collections.Generic;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System.Linq;
using System.IO;
using APSIM.Shared.Utilities;
using APSIM.Numerics;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation labour cost item
    /// Determines the amount of labour required for the transmutation
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Transmutation))]
    [Description("Identifies how the labour (as resource B) is transmuted into a shortfall resource (A, e.g.food)")]
    [HelpUri(@"Content/Features/Transmutation/TransmutationCostLabour.htm")]
    public class TransmuteLabour : CLEMModel, ITransmute, IValidatableObject
    {
        [Link]
        private ResourcesHolder resources = null;

        private double shortfallPacketSize = 1;
        private bool shortfallWholePackets = false;
        private List<object> groupings;

        /// <inheritdoc/>
        [JsonIgnore]
        [field: NonSerialized]
        public IResourceType TransmuteResourceType { get; set; }

        /// <inheritdoc/>
        [Description("Days labour (B) per shortfall packet (A)")]
        [Required, GreaterThanEqualValue(0)]
        [Core.Display(EnabledCallback = "AmountPerPacketEnabled")]
        public double AmountPerPacket { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public string TransmuteResourceTypeName { get; set; }

        ///<inheritdoc/>
        [JsonIgnore]
        public TransmuteStyle TransmuteStyle { get; set; }

        ///<inheritdoc/>
        [JsonIgnore]
        public ResourceBaseWithTransactions ResourceGroup { get; set; }

        ///<inheritdoc/>
        [JsonIgnore]
        public string FinanceTypeForTransactionsName { get; set; }

        /// <summary>
        /// Method to determine if direct transmute style will enable the amount property
        /// </summary>
        public bool AmountPerPacketEnabled() { return TransmuteStyle != TransmuteStyle.Direct; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TransmuteLabour()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ResourceGroup = resources.FindResourceGroup<Labour>();
            shortfallPacketSize = (Parent as Transmutation).TransmutationPacketSize;
            shortfallWholePackets = (Parent as Transmutation).UseWholePackets;
            groupings = this.FindAllChildren<RuminantGroup>().ToList<object>();
        }

        ///<inheritdoc/>
        public bool DoTransmute(ResourceRequest request, double shortfall, double requiredByActivities, ResourcesHolder holder, bool queryOnly)
        {
            request.Required = shortfall / shortfallPacketSize * AmountPerPacket;

            if (MathUtilities.IsPositive(request.Required))
            {
                request.FilterDetails = groupings;
                CLEMActivityBase.TakeLabour(request, !queryOnly, request.ActivityModel, resources, (request.ActivityModel is CLEMActivityBase)?(request.ActivityModel as CLEMActivityBase).AllowsPartialResourcesAvailable:false);
            }
            return (request.Provided >= request.Required);
        }

        ///<inheritdoc/>
        public double ShortfallPackets(double amount)
        {
            double unitsNeeded = amount / shortfallPacketSize;
            if (shortfallWholePackets)
                unitsNeeded = Math.Ceiling(unitsNeeded);
            return unitsNeeded;
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
            IResourceType parentResource = null;
            if (ResourceGroup is null)
            {
                parentResource = FindAncestor<CLEMResourceTypeBase>() as IResourceType;
                string[] memberNames = new string[] { "Labour resource" };
                results.Add(new ValidationResult($"No [r=Labour] resource was found for a labour-based transmutation [{this.Name}] of [{parentResource.Name}]", memberNames));
            }

            if (TransmuteStyle == TransmuteStyle.UsePricing)
            {
                if(parentResource is null )
                    parentResource = FindAncestor<CLEMResourceTypeBase>() as IResourceType;
                string[] memberNames = new string[] { "Transmte pricing" };
                results.Add(new ValidationResult($"The UsePricing Transmute style is not supported in the [{this.Name}] of [{parentResource.Name}]", memberNames));

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

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                if (TransmuteStyle == TransmuteStyle.Direct)
                    htmlWriter.Write($"<span class=\"setvalue\">{AmountPerPacket:#,##0.##}</span> days labour ");

                htmlWriter.Write(" ruminants (B) are taken from the following groups to supply shortfall resource (A) ");

                if (TransmuteStyle == TransmuteStyle.UsePricing)
                {
                    htmlWriter.Write($" using the herd pricing details");
                    if (FinanceTypeForTransactionsName != null && FinanceTypeForTransactionsName != "")
                        htmlWriter.Write($" with all financial Transactions of sales and purchases using <span class=\"resourcelink\">{TransmuteResourceTypeName}</span>");
                }
                htmlWriter.WriteLine("</div>");
                return htmlWriter.ToString();
            }
        }

        #endregion

    }

}
