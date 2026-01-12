using Models.CLEM.Resources;
using System;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for HumanFoodStoreType (sub-resource)
    /// </summary>
    public class HumanFoodStoreTypeSummary : DescriptiveSummaryProviderBase<HumanFoodStoreType>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            if (!(model.Units ?? "").Equals("KG", StringComparison.CurrentCultureIgnoreCase))
            {
                generator.AddBlockWithText("activityentry", $"Each unit of this resource is equivalent to {CLEMModel.DisplaySummaryValueSnippet(model.ConvertToKg, warnZero:true)} kg");
            }
            else
            {
                if (model.ConvertToKg != 1)
                {
                    generator.AddBlockWithText("errorbanner", $"SET UnitsToKg to <span class=\"setvalue\">1</span> as this Food Type is measured in kg");
                }
            }

            if (model.StartingAmount > 0)
            {
                string start = $"The simulation starts with {CLEMModel.DisplaySummaryValueSnippet(model.StartingAmount)}";
                if (model.StartingAge > 0)
                    start += $" with an age of {CLEMModel.DisplaySummaryValueSnippet(model.StartingAge)} months";
                generator.AddBlockWithText("activityentry", start);
            }

            if (model.UseByAge == 0)
            {
                generator.AddBlockWithText("activityentry", "This food does not spoil");
            }
            else
            {
                generator.AddBlockWithText("activityentry", $"This food must be consumed before {CLEMModel.DisplaySummaryValueSnippet(model.UseByAge)} month{((model.UseByAge > 1) ? "s" : "")} old");
            }
            generator.AddBlockWithText("activityentry", $" {CLEMModel.DisplaySummaryValueSnippet(((model.EdibleProportion == 1) ? "All" : model.EdibleProportion.ToString("#0%")))} of this raw food is edible.");
        }
    }
}