using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for ProductStoreTypeManure (sub-resource)
/// </summary>
public class ProductStoreTypeManureSummary : DescriptiveSummaryProviderBase<ProductStoreTypeManure>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        Generator.AddBlockWithText($"Manure will decay at a rate of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.DecayRate, warnZero: true)} each month and will only last for {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MaximumAge.InDays, warnZero: true)} days.");
        Generator.AddBlockWithText($"Fresh manure is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.ProportionMoistureFresh, warnZero: true)} moisture and declines by {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MoistureDecayRate, warnZero: true)} each month.");
    }
}