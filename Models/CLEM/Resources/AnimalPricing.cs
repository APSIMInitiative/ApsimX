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
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "Beta build")]
    [Version(1, 0, 2, "Adam Liedloff", "CSIRO", "Custom grouping with filtering")]
    public class AnimalPricing: CLEMModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalPricing()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Style of pricing animals
        /// </summary>
        [Description("Style of pricing animals")]
        [Required]
        public PricingStyleType PricingStyle { get; set; }

        /// <summary>
        /// Price of individual breeding sire
        /// </summary>
        [Description("Price of individual breeding sire")]
        [Required, GreaterThanEqualValue(0)]
        public double BreedingSirePrice { get; set; }

        /// <summary>
        /// Create a copy of the current instance
        /// </summary>
        /// <returns></returns>
        public AnimalPricing Clone()
        {
            AnimalPricing clone = new AnimalPricing()
            {
                PricingStyle = this.PricingStyle,
                BreedingSirePrice = this.BreedingSirePrice
            };

            foreach (AnimalPriceGroup item in this.Children.OfType<AnimalPriceGroup>())
            {
                clone.Children.Add(item.Clone());
            }
            return clone;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">";
            html += "Pricing is provided <span class=\"setvalue\">" + this.PricingStyle.ToString() + "</span></div>";
            html += "\n<div class=\"activityentry\">";
            html += "Male breeder purchase price is <span class=\"setvalue\">" + this.BreedingSirePrice.ToString("0.00") + "</span></div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool FormatForParentControl)
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
        public override string ModelSummaryInnerOpeningTags(bool FormatForParentControl)
        {
            string html = "";
            if(Apsim.Children(this, typeof(AnimalPriceGroup)).Count() >= 1)
            {
                html += "<div class=\"topspacing\"><table><tr><th>Name</th><th>Filter</th><th>Purchase</th><th>Sell</th></tr>";
            }
            else
            {
                html += "<span class=\"errorlink\">No Animal Price Groups defined!</span>";
            }
            return html;
        }

    }
}
