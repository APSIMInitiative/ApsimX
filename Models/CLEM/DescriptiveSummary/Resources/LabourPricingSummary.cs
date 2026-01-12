using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for AnimalPricing
    /// </summary>
    public class LabourPricingSummary : DescriptiveSummaryProviderBase<LabourPricing>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public LabourPricingSummary()
        {
            SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>();

            return new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (model.Structure.FindChildren<LabourPriceGroup>().Cast<IModel>(), true, "", "The following Labour Price Groups are applied in the order provided to determine the pay rate of any individual.", $"No {CLEMModel.DisplaySummaryValueSnippet("LabourPriceGroup", entryStyle: HTMLSummaryStyle.Filter)} provided!")
            };
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocks()
        {
            var cm = CLEMModel;
            if (cm is null) return;

            if (cm.Structure.FindChildren<AnimalPriceGroup>().Any())
            {
                Generator.CreateTable(new string[] { "Name", "Filter", "Rate per day" });
            }
            //else
            //{
            //    generator.AddBlockWithText("errorbanner", "No Labour Price Groups defined!");
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
