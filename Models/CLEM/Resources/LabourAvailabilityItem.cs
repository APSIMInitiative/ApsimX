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
    /// An individual labour availability item with the same days available every month
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourAvailabilityList))]
    [Description("An individual labour availability with the same days available every month")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Labour/LabourAvailabilityItem.htm")]
    public class LabourAvailabilityItem : LabourSpecificationItem, IFilterGroup
    {
        /// <summary>
        /// Single values 
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(20)]
        [Description("Availability")]
        [Required, GreaterThanValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [JsonIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Provide the labour availability
        /// </summary>
        /// <param name="month">Month for labour</param>
        /// <returns></returns>
        public override double GetAvailability(int month)
        {
            return Value;
        }

        /// <summary>
        /// Proportion of group to use
        /// </summary>
        [JsonIgnore]
        public double Proportion { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourAvailabilityItem()
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
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    if (Value <= 0)
                    {
                        htmlWriter.Write("<span class=\"errorlink\">" + Value.ToString() + "</span>");
                    }
                    else if (Value > 0)
                    {
                        htmlWriter.Write("<span class=\"setvalue\">" + Value.ToString() + "</span> x ");
                    }
                    htmlWriter.Write(" days available each month</div>");
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
                    string classstr = "setvalue";
                    if (Value == 0)
                    {
                        classstr = "errorlink";
                    }
                    htmlWriter.Write("</td>");
                    htmlWriter.Write("<td><span class=\"" + classstr + "\">" + this.Value.ToString() + "</span></td>");
                    for (int i = 1; i < 12; i++)
                    {
                        htmlWriter.Write("<td><span class=\"disabled\">" + this.Value.ToString() + "</span></td>");
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
