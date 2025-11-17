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

            if (Structure.FindChildren<LabourPriceGroup>(relativeTo: this).Count() == 0)
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult("No [LabourPriceGroups] have been provided for [r=" + this.Name + "].\r\nAdd [LabourPriceGroups] to include labour pricing.", memberNames));
            }
            else if (Structure.FindChildren<LabourPriceGroup>(relativeTo: this).Cast<LabourPriceGroup>().Where(a => a.Value == 0).Count() > 0)
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
            string html = "";
            if (this.Children.OfType<LabourPriceGroup>().Count() == 0)
            {
                html += "\r\n<div class=\"errorlink\">";
                html += "No labour price groups has been defined";
                html += "</div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            string html = "";
            html += "</table>";
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            if (this.Children.OfType<LabourPriceGroup>().Count() > 0)
                html += "<table><tr><th>Name</th><th>Filter</th><th>Rate per day</th></tr>";
            return html;
        }

        #endregion
    }
}
