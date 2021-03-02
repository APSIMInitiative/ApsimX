using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// An individual labour availability item with monthly days available
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourAvailabilityList))]
    [Description("An individual labour availability item with monthly days available")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailabilityItemMonthly.htm")]
    public class LabourAvailabilityItemMonthly : LabourSpecificationItem, IFilterGroup
    {
        /// <summary>
        /// Monthly values. 
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(new double[] { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 })]
        [Description("Availability each month of the year")]
        [Required, ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [JsonIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Proportion of group to use
        /// </summary>
        [JsonIgnore]
        public double Proportion { get; set; }

        /// <summary>
        /// Provide the monthly labour availability
        /// </summary>
        /// <param name="month">Month for labour</param>
        /// <returns></returns>
        public override double GetAvailability(int month)
        {
            if(month<=12 && month>0 && month<=MonthlyValues.Count())
            {
                return MonthlyValues[month - 1];
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityItemMonthly()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (!formatForParentControl)
                {
                    if (MonthlyValues == null)
                    {
                        return "\r\n<div class=\"activityentry\">No availability provided</div>";
                    }
                    double min = MonthlyValues.Min();
                    double max = MonthlyValues.Max();
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Monthly availability ");
                    if (min != max)
                    {
                        htmlWriter.Write("ranges from ");
                    }
                    else
                    {
                        htmlWriter.Write("is ");
                    }
                    if (min <= 0)
                    {
                        htmlWriter.Write("<span class=\"errorlink\">" + min.ToString() + "</span>");
                    }
                    else
                    {
                        htmlWriter.Write("<span class=\"setvalue\">" + min.ToString() + "</span>");
                    }
                    if (min != max)
                    {
                        htmlWriter.Write("to <span class=\"setvalue\">" + max.ToString() + "</span>");
                    }
                    htmlWriter.Write(" days each month</div>");
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (formatForParentControl)
                {
                    htmlWriter.Write("</td>");
                    for (int i = 0; i < 12; i++)
                    {
                        if (MonthlyValues == null)
                        {
                            htmlWriter.Write("<td><span class=\"errorlink\">?</span></td>");
                        }
                        else if (i > MonthlyValues.Count() - 1)
                        {
                            htmlWriter.Write("<td><span class=\"errorlink\">?</span></td>");
                        }
                        else
                        {
                            htmlWriter.Write("<td><span class=\"setvalue\">" + ((this.MonthlyValues.Length - 1 >= i) ? this.MonthlyValues[i].ToString() : "") + "</span></td>");
                        }
                    }
                    htmlWriter.Write("</tr>");
                }
                else
                {
                    htmlWriter.Write("\r\n</div>");
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (formatForParentControl)
                {
                    htmlWriter.Write("<tr><td>");
                    if ((this.FindAllChildren<LabourFilter>().Count() == 0))
                    {
                        htmlWriter.Write("<div class=\"filter\">Any labour</div>");
                    }
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
                    if (!(this.FindAllChildren<LabourFilter>().Count() >= 1))
                    {
                        htmlWriter.Write("<div class=\"filter\">Any labour</div>");
                    }
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return !formatForParentControl ? base.ModelSummaryClosingTags(true) : "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return !formatForParentControl ? base.ModelSummaryOpeningTags(true) : "";
        }


        #endregion
    }
}
