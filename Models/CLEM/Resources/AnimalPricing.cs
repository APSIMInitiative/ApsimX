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
    [Description("Holds all animal price entries defining the value of individual ruminants")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Custom grouping with filtering")]
    [Version(1, 0, 3, "Purchase and sales identifier used")]
    [HelpUri(@"Content/Features/Resources/Ruminants/AnimalPricing.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantType) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
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
            var results = new List<ValidationResult>();

            if (!FindAllChildren<AnimalPriceGroup>().Any())
            {
                string[] memberNames = new string[] { "Animal pricing" };
                results.Add(new ValidationResult("No [AnimalPriceGroups] have been provided for [r=" + this.Name + "].\r\nAdd [AnimalPriceGroups] to include animal pricing.", memberNames));
            }
            else if (FindAllChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().Any(a => a.Value == 0))
            {
                string[] memberNames = new string[] { "Animal pricing" };
                results.Add(new ValidationResult("No price [Value] has been set for some of the [AnimalPriceGroup] in [r=" + this.Name + "]\r\nThese will not result in price calculations and can be deleted.", memberNames));
            }
            return results;
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
            if (FindAllChildren<AnimalPriceGroup>().Count() >= 1)
                html += "</table></div>";

            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            if (FindAllChildren<AnimalPriceGroup>().Count() >= 1)
                html += "<div class=\"topspacing\"><table><tr><th>Name</th><th>Filter</th><th>Value</th><th>Style</th><th>Type</th></tr>";
            else
                html += "<span class=\"errorlink\">No Animal Price Groups defined!</span>";

            return html;
        }
        #endregion

    }
}
