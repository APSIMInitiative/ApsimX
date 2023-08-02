using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// An individual labour availability item with the same days available every month
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourAvailabilityList))]
    [Description("Set the labour availability of specified individuals with the same days available every month")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailabilityItem.htm")]
    public class LabourAvailabilityItem : FilterGroup<LabourType>, ILabourSpecificationItem
    {
        /// <summary>
        /// Single values 
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(20)]
        [Description("Availability")]
        [Required, GreaterThanValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Provide the labour availability
        /// </summary>
        /// <param name="month">Month for labour</param>
        /// <returns></returns>
        public double GetAvailability(int month)
        {
            return Value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityItem()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (!FormatForParentControl)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    if (Value <= 0)
                        htmlWriter.Write("<span class=\"errorlink\">" + Value.ToString() + "</span>");
                    else
                    {
                        if (Value > 0)
                            htmlWriter.Write("<span class=\"setvalue\">" + Value.ToString() + "</span> x ");
                    }

                    htmlWriter.Write(" days available each month</div>");
                }
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (FormatForParentControl)
                {
                    string classstr = "setvalue";
                    if (Value == 0)
                        classstr = "errorlink";

                    htmlWriter.Write("</td>");
                    htmlWriter.Write("<td><span class=\"" + classstr + "\">" + this.Value.ToString() + "</span></td>");
                    for (int i = 1; i < 12; i++)
                        htmlWriter.Write("<td><span class=\"disabled\">" + this.Value.ToString() + "</span></td>");

                    htmlWriter.Write("</tr>");
                }
                else
                    htmlWriter.Write("\r\n</div>");

                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (FormatForParentControl)
                    htmlWriter.Write("<tr><td>");
                else
                    htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");

                if (FindAllChildren<Filter>().Count() < 1)
                    htmlWriter.Write("<div class=\"filter\">Any labour</div>");

                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return !FormatForParentControl ? base.ModelSummaryClosingTags() : "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return !FormatForParentControl ? base.ModelSummaryOpeningTags() : "";
        }

        #endregion

    }
}
