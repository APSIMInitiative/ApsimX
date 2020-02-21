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
        [XmlIgnore]
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
        /// Constructor
        /// </summary>
        public LabourAvailabilityItem()
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
                html += "\n<div class=\"activityentry\">";
                if (Value <= 0)
                {
                    html += "<span class=\"errorlink\">" + Value.ToString() + "</span>";
                }
                else if (Value > 0)
                {
                    html += "<span class=\"setvalue\">" + Value.ToString() + "</span> x ";
                }
                html += " days available each month</div>";
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
                string classstr = "setvalue";
                if(Value==0)
                {
                    classstr = "errorlink";
                }
                html += "</td>";
                html += "<td><span class=\""+classstr+"\">" + this.Value.ToString() + "</span></td>";
                for (int i = 1; i < 12; i++)
                {
                    html += "<td><span class=\"disabled\">" + this.Value.ToString() + "</span></td>";
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
