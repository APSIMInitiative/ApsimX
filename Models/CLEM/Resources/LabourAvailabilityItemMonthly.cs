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
    /// An individual labour availability item with monthly days available
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourAvailabilityList))]
    [Description("Set the labour availability of specified individuals with monthly days available")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailabilityItemMonthly.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(LabourAvailabilityList) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class LabourAvailabilityItemMonthly : FilterGroup<LabourType>, ILabourSpecificationItem
    {
        /// <summary>
        /// Monthly values. 
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(new double[] { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 })]
        [Description("Availability each month of the year")]
        [Required, ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <summary>
        /// Provide the monthly labour availability
        /// </summary>
        /// <param name="month">Month for labour</param>
        /// <returns></returns>
        public double GetAvailability(int month)
        {
            if (month <= 12 && month > 0 && month <= MonthlyValues.Count())
                return MonthlyValues[month - 1];
            else
                return 0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityItemMonthly()
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
                    if (MonthlyValues == null)
                        return "\r\n<div class=\"activityentry\">No availability provided</div>";

                    double min = MonthlyValues.Min();
                    double max = MonthlyValues.Max();
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Monthly availability ");
                    if (min != max)
                        htmlWriter.Write("ranges from ");
                    else
                        htmlWriter.Write("is ");

                    if (min <= 0)
                        htmlWriter.Write("<span class=\"errorlink\">" + min.ToString() + "</span>");
                    else
                        htmlWriter.Write("<span class=\"setvalue\">" + min.ToString() + "</span>");

                    if (min != max)
                        htmlWriter.Write("to <span class=\"setvalue\">" + max.ToString() + "</span>");

                    htmlWriter.Write(" days each month</div>");
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
                    htmlWriter.Write("</td>");
                    for (int i = 0; i < 12; i++)
                    {
                        if (MonthlyValues == null)
                            htmlWriter.Write("<td><span class=\"errorlink\">?</span></td>");
                        else
                        {
                            if (i > MonthlyValues.Count() - 1)
                                htmlWriter.Write("<td><span class=\"errorlink\">?</span></td>");
                            else
                                htmlWriter.Write("<td><span class=\"setvalue\">" + ((this.MonthlyValues.Length - 1 >= i) ? this.MonthlyValues[i].ToString() : "") + "</span></td>");
                        }
                    }
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
            string html = FormatForParentControl
                ? "<tr><td>"
                : "\r\n<div class=\"filterborder clearfix\">";

            if (FindAllChildren<Filter>().Count() < 1)
                html += "<div class=\"filter\">Any labour</div>";

            return html;
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
