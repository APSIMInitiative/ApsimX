using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// User entry of Animal prices
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyMultiModelView")]
    [PresenterName("UserInterface.Presenters.PropertyMultiModelPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [ValidParent(ParentType = typeof(OtherAnimalsType))]
    [Description("Holds all animal price entries defining the value of individual ruminants")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Custom grouping with filtering")]
    [Version(1, 0, 3, "Purchase and sales identifier used")]
    [HelpUri(@"Content/Features/Resources/Ruminants/AnimalPricing.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class AnimalPricing : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalPricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {

            if (Structure.FindChildren<AnimalPriceGroup>().Count() == 0)
            {
                yield return new ValidationResult("No [AnimalPriceGroups] have been provided for [r=" + this.Name + "].\r\nAdd [AnimalPriceGroups] to include animal pricing.", new string[] { "Animal pricing" });
            }
            else if (Structure.FindChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().Where(a => a.Value == 0).Count() > 0)
            {
                yield return new ValidationResult("No price [Value] has been set for some of the [AnimalPriceGroup] in [r=" + this.Name + "]\r\nThese will not result in price calculations and can be deleted.", new string[] { "Animal pricing" });
            }
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            string html = "";
            if (Structure.FindChildren<AnimalPriceGroup>().Count() >= 1)
                html += "</table></div>";

            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            if (Structure.FindChildren<AnimalPriceGroup>().Count() >= 1)
                html += "<div class=\"topspacing\"><table><tr><th>Name</th><th>Filter</th><th>Value</th><th>Style</th><th>Type</th></tr>";
            else
                html += "<span class=\"errorlink\">No Animal Price Groups defined!</span>";

            return html;
        }
        #endregion

    }
}
