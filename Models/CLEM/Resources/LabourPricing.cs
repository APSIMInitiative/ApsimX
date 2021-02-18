using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// User entry of Labour prices
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyTablePresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("This component holds all Labour Price Entries that define the value of individuals.")]
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

            if (this.FindAllChildren<LabourPriceGroup>().Count() == 0)
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult("No [LabourPriceGroups] have been provided for [r=" + this.Name + "].\r\nAdd [LabourPriceGroups] to include labour pricing.", memberNames));
            }
            else if (this.FindAllChildren<LabourPriceGroup>().Cast<LabourPriceGroup>().Where(a => a.Value == 0).Count() > 0)
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult("No price [Value] has been set for some of the [LabourPriceGroup] in [r=" + this.Name + "]\r\nThese will not result in price calculations and can be deleted.", memberNames));
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
            string html = "";
            if (this.Children.OfType<LabourPriceGroup>().Count() == 0)
            {
                html += "\r\n<div class=\"errorlink\">";
                html += "No labour price groups has been defined";
                html += "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "</table>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            if (this.Children.OfType<LabourPriceGroup>().Count() > 0)
            {
                html += "<table><tr><th>Name</th><th>Filter</th><th>Rate per day</th></tr>";
            }
            return html;
        }

        #endregion
    }
}
