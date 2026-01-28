using Models.CLEM.Resources;
using System;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

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
            generator.AddBlockWithText($"Each unit of this resource is equivalent to {generator.DisplaySummaryValueSnippet(model.ConvertToKg, warnZero:true)} kg");
        }
        else
        {
            if (model.ConvertToKg != 1)
            {
                generator.AddBlockWithText($"SET UnitsToKg to {generator.DisplaySummaryValueSnippet(1)} as this Food Type is measured in kg", "infoBanner error");
            }
        }

        if (model.StartingAmount > 0)
        {
            string start = $"The simulation starts with {generator.DisplaySummaryValueSnippet(model.StartingAmount)}";
            if (model.StartingAge > 0)
                start += $" with an age of {generator.DisplaySummaryValueSnippet(model.StartingAge)} months";
            generator.AddBlockWithText(start);
        }

        if (model.UseByAge == 0)
        {
            generator.AddBlockWithText("This food does not spoil");
        }
        else
        {
            generator.AddBlockWithText($"This food must be consumed before {generator.DisplaySummaryValueSnippet(model.UseByAge)} month{((model.UseByAge > 1) ? "s" : "")} old");
        }
        generator.AddBlockWithText($" {generator.DisplaySummaryValueSnippet(((model.EdibleProportion == 1) ? "All" : model.EdibleProportion.ToString("#0%")))} of this raw food is edible.");
    }
}