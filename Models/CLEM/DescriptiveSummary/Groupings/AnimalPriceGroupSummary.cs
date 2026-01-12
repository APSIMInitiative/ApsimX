using DocumentFormat.OpenXml.EMMA;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Groupings
{
    /// <summary>
    /// Descriptive summary provider for AnimalPriceGroup
    /// </summary>
    public class AnimalPriceGroupSummary : DescriptiveSummaryProviderBase<AnimalPriceGroup>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalPriceGroupSummary()
        {
            //SummaryStyle = HTMLSummaryStyle.Filter;
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            if (!FormatForParentControl)
            {
                //ToDo: format value for currency
                generator.AddBlockWithText("activityentry", $"{PricingStyleToFullString(model.PurchaseOrSale)} {CLEMModel.DisplaySummaryValueSnippet(model.Value, warnZero:true)} {CLEMModel.DisplaySummaryValueSnippet(model.PricingStyle)} ");
            }
        }

        /// <summary>
        /// Convert pricing style into introductory string
        /// </summary>
        /// <param name="pricingStyle">pricing style to use</param>
        /// <returns>Description of style</returns>
        private string PricingStyleToFullString(PurchaseOrSalePricingStyleType pricingStyle)
        {
            switch (pricingStyle)
            {
                case PurchaseOrSalePricingStyleType.Both:
                    return "Buy and sell for ";
                case PurchaseOrSalePricingStyleType.Purchase:
                    return "Buy for ";
                case PurchaseOrSalePricingStyleType.Sale:
                    return "Sell for ";
                default:
                    return "Unknown pricing style";
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerClosingBlocks()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.CloseMostRecentBlock("animalPriceGroup_filters");
            if (FormatForParentControl)
            {
                generator.AddBlockWithText("", CLEMModel.DisplaySummaryValueSnippet(model.Value, warnZero: true), tag: "td");
                generator.AddBlockWithText("", CLEMModel.DisplaySummaryValueSnippet(model.PricingStyle), tag: "td");
                generator.AddBlockWithText("", CLEMModel.DisplaySummaryValueSnippet(PricingStyleToFullString(model.PurchaseOrSale)), tag: "td");
                generator.CloseMostRecentBlock("animalPriceGroup_row");
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocks()
        {

            var cm = CLEMModel;
            if (cm is null) return;

            if (FormatForParentControl)
            {
                generator.OpenBlock("", "", tag: "tr", id: "animalPriceGroup_row");
                generator.AddBlockWithText("", cm.Name, tag: "td");
                generator.OpenBlock("", "", tag: "td", id: "animalPriceGroup_filters");
            }
            else
            {
                generator.OpenBlock("filterborder clearfix", "", id: "animalPriceGroup_filters");
            }
            if (cm.Structure.FindChildren<Filter>().Any() == false)
            {
                generator.AddBlockWithText("filter", "All individuals");
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryOpeningBlocks()
        {
            if (!FormatForParentControl)
                base.CreateSummaryOpeningBlocks();
        }

        /// <inheritdoc/>
        public override void CreateSummaryClosingBlocks()
        {
            if (!FormatForParentControl)
                base.CreateSummaryClosingBlocks();
        }
    }
}
