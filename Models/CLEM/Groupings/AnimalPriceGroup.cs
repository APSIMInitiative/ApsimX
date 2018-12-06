using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "")]
    public class AnimalPriceGroup: CLEMModel
    {
        /// <summary>
        /// Purchase value of individual
        /// </summary>
        [Description("Purchase value")]
        [Required, GreaterThanEqualValue(0)]
        public double PurchaseValue { get; set; }

        /// <summary>
        /// Sell value of individual
        /// </summary>
        [Description("Sale value")]
        [Required, GreaterThanEqualValue(0)]
        public double SellValue { get; set; }

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
                PurchaseValue = this.PurchaseValue,
                SellValue = this.SellValue
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
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            if (!FormatForParentControl)
            {
                html += "\n<div class=\"activityentry\">";
                if (PurchaseValue == SellValue)
                {
                    html += "Buy and sell for ";
                }
                else
                {
                    html += "Buy for ";
                    html += "<span class=\"setvalue\">";
                    html += PurchaseValue.ToString("#,0.##");
                    html += "</span> and sell for ";
                }
                html += "<span class=\"setvalue\">";
                html += SellValue.ToString("#,0.##");
                html += "</span></div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool FormatForParentControl)
        {
            string html = "";
            if (FormatForParentControl)
            {
                html += "</td><td><span class=\"setvalue\">" + this.PurchaseValue.ToString("#,0.##") + "</span></td><td><span class=\"setvalue\">" + this.SellValue.ToString("#,0.##") + "</span></td></tr>";
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
        public override string ModelSummaryInnerOpeningTags(bool FormatForParentControl)
        {
            string html = "";
            if (FormatForParentControl)
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
        public override string ModelSummaryClosingTags(bool FormatForParentControl)
        {
            if (!FormatForParentControl)
                return base.ModelSummaryClosingTags(true);
            else
                return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool FormatForParentControl)
        {
            if (!FormatForParentControl)
                return base.ModelSummaryOpeningTags(true);
            else
                return "";
        }


    }
}
