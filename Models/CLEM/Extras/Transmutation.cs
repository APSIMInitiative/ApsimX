using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation
    /// Will convert one resource into another (e.g. $ => labour)
    /// These transmutations are defined under each ResourceType in the Resources section of the UI tree
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IResourceType))]
    [Description("Define a Transmutation to automatically convert the current resource (A) from any other resource (B) when in deficit")]
    [Version(2, 0, 1, "Full reworking of transmute resource (B) to shortfall resource (A)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Transmutation/Transmutation.htm")]
    public class Transmutation: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Resource in shortfall (A)
        /// </summary>
        [Description("Resource in shortfall (A)")]
        [Core.Display(Type = DisplayType.FieldName)]
        public string ResourceInShortfall { get { return (Parent as CLEMModel).NameWithParent; } private set {; } }

        /// <summary>
        /// Amount of resource in shortfall per transmutation packet
        /// </summary>
        [Description("Transmutation packet size (amount of A)")]
        [Required, GreaterThanValue(0)]
        public double TransmutationPacketSize { get; set; }

        /// <summary>
        /// Enforce transmutation in whole packets
        /// </summary>
        [Description("Use whole packets")]
        public bool UseWholePackets { get; set; }

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public Transmutation()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
            TransactionCategory = "Transmutation";
        }

        #region validation

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Structure.FindChildren<ITransmute>().Where(a => (a as IModel).Enabled).Any())
            {
                string[] memberNames = new string[] { "Transmutes" };
                yield return new ValidationResult("No transmute components provided under this transmutation", memberNames);
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");

                var pricing = Structure.FindChildren<ITransmute>().Where(a => a.TransmuteStyle == TransmuteStyle.UsePricing);
                var direct = Structure.FindChildren<ITransmute>().Where(a => a.TransmuteStyle == TransmuteStyle.Direct);

                htmlWriter.Write($"The following resources (B) will transmute ");
                if (pricing.Any())
                {
                    htmlWriter.Write($"using the resource purchase price ");
                    var transmuteResourcePrice = ((Structure.FindParent<ResourcesHolder>(recurse: true)).FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceInShortfall, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore))?.Price(PurchaseOrSalePricingStyleType.Purchase);
                    if (transmuteResourcePrice != null)
                        htmlWriter.Write("found");
                    else
                        htmlWriter.Write($"<span class=\"errorlink\">not found</span>");
                }
                htmlWriter.WriteLine(" to provide this shortfall resource (A)");


                if (direct.Any())
                    htmlWriter.Write($" in {(UseWholePackets ? " whole" : "")} packets of <span class=\"setvalue\">{TransmutationPacketSize:#,##0.##}</span>");

                if (pricing.Count() + direct.Count() > 1)
                    htmlWriter.Write($" (or the largest packet size needed the individual transmutes)");

                htmlWriter.WriteLine("</div>");

                if (!Structure.FindChildren<ITransmute>().Any())
                {
                    htmlWriter.Write("<div class=\"errorbanner\">");
                    htmlWriter.Write("No Transmute components provided");
                    htmlWriter.WriteLine("</div>");
                }
                return htmlWriter.ToString();
            }
        }

        #endregion
    }

}
