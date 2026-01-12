using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for AnimalPricing
    /// </summary>
    public class AnimalPricingSummary : DescriptiveSummaryProviderBase<AnimalPricing>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalPricingSummary()
        {
            SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<AnimalPriceGroup>().Cast<IModel>(), true, "", "The following Animal Price Groups are applied in the order provided to determine the purchase and sale price of any individual.", $"No {CLEMModel.DisplaySummaryValueSnippet("AnimalFoodStoreType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }

        ///// <inheritdoc/>
        //public override void BuildSummary()
        //{
        //    var model = ModelTyped;
        //    if (model is null) return;

        //    generator.AddBlockWithText("detailsnote", $"The following Animal Price Groups are applied in the order provided to determine the purchase and sale price of any individual.");
        //}

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocks()
        {
            var cm = CLEMModel;
            if (cm is null) return;

            if (cm.Structure.FindChildren<AnimalPriceGroup>().Any())
            {
                Generator.CreateTable(new string[] { "Name", "Filter", "Value", "Style", "Type" });
            }
            //else
            //{
            //    generator.AddBlockWithText("errorbanner", "No Animal Price Groups defined!");
            //}
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerClosingBlocks()
        {
            var cm = CLEMModel;
            if (cm is null) return;

            if (cm.Structure.FindChildren<AnimalPriceGroup>().Any())
            {
                Generator.CloseTable();
            }
        }
    }
}