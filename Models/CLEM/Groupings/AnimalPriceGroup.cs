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
    /// Contains a group of filters to identify individual ruminants in a set price group
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalPricing))]
    [Description("This ruminant price group sets the sale and purchase price for a set group of individuals.")]
    [Version(1, 0, 1, "")]
    [Version(1, 0, 2, "Purchase and sales identifier used")]
    [HelpUri(@"Content/Features/Filters/AnimalPriceGroup.htm")]
    public class AnimalPriceGroup: CLEMModel, IFilterGroup
    {
        /// <summary>
        /// Style of pricing animals
        /// </summary>
        [Description("Style of pricing animals")]
        [Required]
        public PricingStyleType PricingStyle { get; set; }

        /// <summary>
        /// Value of individual
        /// </summary>
        [Description("Value")]
        [Required, GreaterThanEqualValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Determine whether this is a purchase or sale price, or both
        /// </summary>
        [Description("Purchase or sale price")]
        [System.ComponentModel.DefaultValueAttribute(PurchaseOrSalePricingStyleType.Both)]
        public PurchaseOrSalePricingStyleType PurchaseOrSale { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        protected AnimalPriceGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <summary>
        /// Create a copy of the current instance
        /// </summary>
        /// <returns></returns>
        public AnimalPriceGroup Clone()
        {
            AnimalPriceGroup clone = new AnimalPriceGroup()
            {
                PricingStyle = this.PricingStyle,
                PurchaseOrSale = this.PurchaseOrSale,
                Value = this.Value
            };

            foreach (RuminantFilter item in this.Children.OfType<RuminantFilter>())
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
                switch (PurchaseOrSale)
                {
                    case PurchaseOrSalePricingStyleType.Both:
                        html += "Buy and sell for ";
                        break;
                    case PurchaseOrSalePricingStyleType.Purchase:
                        html += "Buy for ";
                        break;
                    case PurchaseOrSalePricingStyleType.Sale:
                        html += "Sell for ";
                        break;
                }
                if (Value.ToString() == "0")
                {
                    html += "<span class=\"errorlink\">NOT SET";
                }
                else
                {
                    html += "<span class=\"setvalue\">";
                    html += Value.ToString("#,0.##");
                }
                html += "</span> ";
                html += "<span class=\"setvalue\">";
                html += PricingStyle.ToString();
                html += "</span>";
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
                html += "<td><span class=\"setvalue\">" + this.PricingStyle.ToString() + "</span></td>";
                string buySellString = "";
                switch (PurchaseOrSale)
                {
                    case PurchaseOrSalePricingStyleType.Both:
                        buySellString = "Buy and sell";
                        break;
                    case PurchaseOrSalePricingStyleType.Purchase:
                        buySellString = "Buy";
                        break;
                    case PurchaseOrSalePricingStyleType.Sale:
                        buySellString = "Sell";
                        break;
                }
                html += "<td><span class=\"setvalue\">" + buySellString + "</span></td>";
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
                if (!(Apsim.Children(this, typeof(RuminantFilter)).Count() >= 1))
                {
                    html += "<div class=\"filter\">All individuals</div>";
                }
            }
            else
            {
                html += "\n<div class=\"filterborder clearfix\">";
                if (!(Apsim.Children(this, typeof(RuminantFilter)).Count() >= 1))
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
