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
    /// User entry of Labour prices
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyMultiModelView")]
    [PresenterName("UserInterface.Presenters.PropertyMultiModelPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("Holds all labour price entries that define the pay rate of individuals")]
    [Version(1, 0, 1, "Initial release")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourPricing.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(Labour) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class LabourPricing : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public LabourPricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
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

            if (!FindAllChildren<LabourPriceGroup>().Any())
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult("No [LabourPriceGroups] have been provided for [r=" + this.Name + "].\r\nAdd [LabourPriceGroups] to include labour pricing.", memberNames));
            }
            else if (FindAllChildren<LabourPriceGroup>().Cast<LabourPriceGroup>().Any(a => a.Value == 0))
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult("No price [Value] has been set for some of the [LabourPriceGroup] in [r=" + this.Name + "]\r\nThese will not result in price calculations and can be deleted.", memberNames));
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            if (!Children.OfType<LabourPriceGroup>().Any())
            {
                return "\r\n<div class=\"errorlink\">No labour price groups has been defined</div>";
            }
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "</table>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            if (Children.OfType<LabourPriceGroup>().Any())
                return "<table><tr><th>Name</th><th>Filter</th><th>Rate per day</th></tr>";
            return "";
        }

        #endregion
    }
}
