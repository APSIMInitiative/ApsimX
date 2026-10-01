using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

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

        Generator.AddBlockWithText($"Manure will decay at a rate of {generator.DisplaySummaryValueSnippet(model.DecayRate, warnZero: true)} each month and will only last for {generator.DisplaySummaryValueSnippet(model.MaximumAge.InDays, warnZero: true)} days.");
        Generator.AddBlockWithText($"Fresh manure is {generator.DisplaySummaryValueSnippet(model.ProportionMoistureFresh, warnZero: true)} moisture and declines by {generator.DisplaySummaryValueSnippet(model.MoistureDecayRate, warnZero: true)} each month.");
    }
}