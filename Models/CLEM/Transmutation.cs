using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM
{
    ///<summary>
    /// Resource transmutation
    /// Will convert one resource into another (e.g. $ => labour) 
    /// These re defined under each ResourceType in the Resources section of the UI tree
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IResourceType))]
    [Description("This Transmutation will convert any other resource into the current resource where there is a shortfall. This is placed under any resource type where you need to provide a transmutation. For example to convert Finance Type (money) into a Animal Food Store Type (Lucerne) or effectively purchase fodder when low.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Transmutation/Transmutation.htm")]
    public class Transmutation: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Transmutation()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
            TransactionCategory = "Transmutation";
        }

        /// <summary>
        /// Amount of this resource per unit purchased
        /// </summary>
        [Description("Amount of this resource per unit purchased")]
        [Required, GreaterThanEqualValue(0)]
        public double AmountPerUnitPurchase { get; set; }

        /// <summary>
        /// Allow purchases to be in partial units (e.g. transmutate exactly what is needed
        /// </summary>
        [Description("Only work in whole units")]
        public bool WorkInWholeUnits { get; set; }

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Category for transactions required")]
        public string TransactionCategory { get; set; }

        #region validation

        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (this.Children.Where(a => a.GetType().Name.Contains("TransmutationCost")).Count() == 0) //   Apsim.Children (this, typeof(TransmutationCost)).Count() == 0)
            {
                string[] memberNames = new string[] { "TransmutationCosts" };
                results.Add(new ValidationResult("No costs provided under this transmutation", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "<div class=\"resourcebannerlight\">";
            if (AmountPerUnitPurchase > 0)
            {
                html += "When needed <span class=\"setvalue\">" + AmountPerUnitPurchase.ToString("#,##0.##") + "</span> of this resource will be converted from";
            }
            else
            {
                html += "<span class=\"errorlink\">Invalid transmutation provided. No amout to purchase set</span>";
            }
            html += "</div>";

            //if (this.Children.OfType<TransmutationCost>().Count() + this.Children.OfType<TransmutationCostLabour>().Count() == 0)
            if (this.Children.Where(a => a.GetType().Name.Contains("TransmutationCost")).Count() == 0)
            {
                html += "<div class=\"resourcebannerlight\">";
                html += "Invalid transmutation provided. No transmutation costs provided";
                html += "</div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n<div class=\"resourcecontentlight clearfix\">";
            if (!(this.Children.Where(a => a.GetType().Name.Contains("TransmutationCost")).Count() >= 1))
            {
                html += "<div class=\"errorlink\">No transmutation costs provided</div>";
            }
            return html;
        } 

        #endregion
    }

}
