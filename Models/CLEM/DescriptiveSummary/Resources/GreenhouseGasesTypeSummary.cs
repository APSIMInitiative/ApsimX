using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

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
            Generator.AddBlockWithText($"This store will automatically receive {generator.DisplaySummaryValueSnippet(model.AutoCollectType)} from activities");
        }

        Generator.AddBlockWithText($"There is a starting amount of {generator.DisplaySummaryValueSnippet(model.StartingAmount)}");
    }
}
