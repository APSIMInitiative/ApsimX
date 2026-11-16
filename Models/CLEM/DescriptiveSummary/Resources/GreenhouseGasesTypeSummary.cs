using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for the Greenhouse gases type
/// </summary>
public class GreenhouseGasesTypeSummary : DescriptiveSummaryProviderBase<GreenhouseGasesType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        if (model.AutoCollectType != GreenhouseGasTypes.None)
        {
            Generator.AddBlockWithText($"This store will automatically receive {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.AutoCollectType)} from activities");
        }

        Generator.AddBlockWithText($"There is a starting amount of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.StartingAmount)}");
    }
}
