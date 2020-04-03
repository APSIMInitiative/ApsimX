using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (!formatForParentControl)
            {
                if(MonthlyValues == null)
                {
                    return "\n<div class=\"activityentry\">No availability provided</div>";
                }
                double min = MonthlyValues.Min();
                double max = MonthlyValues.Max();
                html += "\n<div class=\"activityentry\">Monthly availability ";
                if (min != max)
                {
                    html += "ranges from ";
                }
                else
                {
                    html += "is ";
                }
                if (min <= 0)
                {
                    html += "<span class=\"errorlink\">" + min.ToString() + "</span>";
                }
                else 
                {
                    html += "<span class=\"setvalue\">" + min.ToString() + "</span>";
                }
                if (min != max)
                {
                    html += "to <span class=\"setvalue\">" + max.ToString() + "</span>";
                }
                html += " days each month</div>";
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
            if (formatForParentControl)
            {
                html += "</td>";
                for (int i = 0; i < 12; i++)
                {
                    if (MonthlyValues == null)
                    {
                        html += "<td><span class=\"errorlink\">?</span></td>";
                    }
                    else if (i > MonthlyValues.Count() - 1)
                    {
                        html += "<td><span class=\"errorlink\">?</span></td>";
                    }
                    else
                    {
                        html += "<td><span class=\"setvalue\">" + ((this.MonthlyValues.Length - 1 >= i) ? this.MonthlyValues[i].ToString() : "") + "</span></td>";
                    }
                }
                html += "</tr>";
            }
            else
            {
                html += "\n</div>";
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
            if (formatForParentControl)
            {
                html += "<tr><td>";
                if ((Apsim.Children(this, typeof(LabourFilter)).Count() == 0))
                {
                    html += "<div class=\"filter\">Any labour</div>";
                }
            }
            else
            {
                html += "\n<div class=\"filterborder clearfix\">";
                if (!(Apsim.Children(this, typeof(LabourFilter)).Count() >= 1))
                {
                    html += "<div class=\"filter\">Any labour</div>";
                }
            }
            return html;
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

    }
}
