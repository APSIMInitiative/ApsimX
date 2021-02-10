using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.CLEM.Groupings;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// User entry of Animal prices
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyTablePresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This component holds all Animal Price Entries that define the value of individuals in the breed/herd.")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Custom grouping with filtering")]
    [Version(1, 0, 3, "Purchase and sales identifier used")]
    [HelpUri(@"Content/Features/Resources/Ruminants/AnimalPricing.htm")]
    public class AnimalPricing: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalPricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
            this.SetDefaults();
        }

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (this.FindAllChildren<AnimalPriceGroup>().Count() == 0)
            {
                string[] memberNames = new string[] { "Animal pricing" };
                results.Add(new ValidationResult("No [AnimalPriceGroups] have been provided for [r=" + this.Name + "].\r\nAdd [AnimalPriceGroups] to include animal pricing.", memberNames));
            }
            else if (this.FindAllChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().Where(a => a.Value == 0).Count() > 0)
            {
                string[] memberNames = new string[] { "Animal pricing" };
                results.Add(new ValidationResult("No price [Value] has been set for some of the [AnimalPriceGroup] in [r=" + this.Name + "]\r\nThese will not result in price calculations and can be deleted.", memberNames));
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            if (this.FindAllChildren<AnimalPriceGroup>().Count() >= 1)
            {
                html += "</table></div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            if (this.FindAllChildren<AnimalPriceGroup>().Count() >= 1)
            {
                html += "<div class=\"topspacing\"><table><tr><th>Name</th><th>Filter</th><th>Value</th><th>Style</th><th>Type</th></tr>";
            }
            else
            {
                html += "<span class=\"errorlink\">No Animal Price Groups defined!</span>";
            }
            return html;
        } 
        #endregion

    }
}
