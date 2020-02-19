using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual labour in a set price group
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourPricing))]
    [Description("This labour price group sets the pay rate for a set group of individuals.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/LabourPriceGroup.htm")]
    public class LabourPriceGroup : CLEMModel, IFilterGroup
    {
        /// <summary>
        /// Pay rate
        /// </summary>
        [Description("Daily pay rate")]
        [Required, GreaterThanEqualValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        protected LabourPriceGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Create a copy of the current instance
        /// </summary>
        /// <returns></returns>
        public LabourPriceGroup Clone()
        {
            LabourPriceGroup clone = new LabourPriceGroup()
            {
                Value = this.Value
            };

            foreach (LabourFilter item in this.Children.OfType<LabourFilter>())
            {
                clone.Children.Add(item.Clone());
            }

            return clone;
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
                html += "Pay ";
                if (Value.ToString() == "0")
                {
                    html += "<span class=\"errorlink\">NOT SET";
                }
                else
                {
                    html += "<span class=\"setvalue\">";
                    html += Value.ToString("#,0.##");
                }
                html += "</span> for a days work";
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
            if (formatForParentControl)
            {
                if (Value.ToString() == "0")
                {
                    html += "</td><td><span class=\"errorlink\">NOT SET";
                }
                else
                {
                    html += "</td><td><span class=\"setvalue\">";
                    html += this.Value.ToString("#,0.##");
                }
                html += "</span></td>";
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
                html += "<tr><td>" + this.Name + "</td><td>";
                if (!(Apsim.Children(this, typeof(LabourFilter)).Count() >= 1))
                {
                    html += "<div class=\"filter\">All individuals</div>";
                }
            }
            else
            {
                html += "\n<div class=\"filterborder clearfix\">";
                if (!(Apsim.Children(this, typeof(LabourFilter)).Count() >= 1))
                {
                    html += "<div class=\"filter\">All individuals</div>";
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
