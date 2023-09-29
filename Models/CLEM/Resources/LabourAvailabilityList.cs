using Models.Core;
using Models.Core.Attributes;
using System;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Holds a list of labour availability items
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyMultiModelView")]
    [PresenterName("UserInterface.Presenters.PropertyMultiModelPresenter")]
    [ValidParent(ParentType = typeof(Labour))]
    [Description("This represents a list of labour availability settings")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailability.htm")]
    public class LabourAvailabilityList : LabourSpecifications
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityList()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            if (this.Children.OfType<LabourAvailabilityItem>().Count() + this.Children.OfType<LabourAvailabilityItemMonthly>().Count() == 0)
            {
                html += "\r\n<div class=\"errorlink\">";
                html += "No labour availability has been defined";
                html += "</div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "</table>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            if (this.Children.OfType<LabourAvailabilityItem>().Count() + this.Children.OfType<LabourAvailabilityItemMonthly>().Count() > 0)
                html += "<table><tr><th>Filter</th><th>J</th><th>F</th><th>M</th><th>A</th><th>M</th><th>J</th><th>J</th><th>A</th><th>S</th><th>O</th><th>N</th><th>D</th></tr>";
            return html;
        }

        #endregion
    }
}
