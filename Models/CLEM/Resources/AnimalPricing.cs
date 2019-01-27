using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.CLEM.Groupings;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// User entry of Animal prices
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This is the parent model component holing all Animal Price Entries that define the value of individuals in the breed/herd.")]
    [Version(1, 0, 1, "Beta build")]
    [Version(1, 0, 2, "Custom grouping with filtering")]
    [Version(1, 0, 3, "Purchase and sales identifier used")]
    public class AnimalPricing: CLEMModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalPricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
            this.SetDefaults();
        }

        /// <summary>
        /// Create a copy of the current instance
        /// </summary>
        /// <returns></returns>
        public AnimalPricing Clone()
        {
            AnimalPricing clone = new AnimalPricing();

            foreach (AnimalPriceGroup item in this.Children.OfType<AnimalPriceGroup>())
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
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            if (Apsim.Children(this, typeof(AnimalPriceGroup)).Count() >= 1)
            {
                html += "</table></div>";
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
            if(Apsim.Children(this, typeof(AnimalPriceGroup)).Count() >= 1)
            {
                html += "<div class=\"topspacing\"><table><tr><th>Name</th><th>Filter</th><th>Value</th><th>Style</th><th>Type</th></tr>";
            }
            else
            {
                html += "<span class=\"errorlink\">No Animal Price Groups defined!</span>";
            }
            return html;
        }

    }
}
