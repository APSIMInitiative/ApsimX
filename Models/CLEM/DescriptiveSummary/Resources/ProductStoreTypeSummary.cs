using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for ProductStoreType (sub-resource)
/// </summary>
public class ProductStoreTypeSummary : DescriptiveSummaryProviderBase<ProductStoreType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        Generator.AddBlockWithText($"There is a starting amount of {generator.DisplaySummaryValueSnippet(model.StartingAmount)}");
    }
}