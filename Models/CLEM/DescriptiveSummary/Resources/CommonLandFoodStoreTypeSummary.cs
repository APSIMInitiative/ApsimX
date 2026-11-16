using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for the CommonLandFoodStoreType Resource
/// </summary>
public class CommonLandFoodStoreTypeSummary : DescriptiveSummaryProviderBase<CommonLandFoodStoreType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        if (model.Parent.GetType() == typeof(AnimalFoodStore))
        {
            Generator.AddBlockWithText($"This common land can be used by animal feed activities only");
        }
        else
        {
            Generator.AddBlockWithText($"This common land can be used by grazing and cut and carry activities");
        }

        if (model.PastureLink != null)
        {
            Generator.AddBlockWithText($"The quality of this common land is based on {DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet(model.PastureLink)} with a further nitrogen reduction of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.NitrogenReductionFromPasture, warnZero:true)} percent.");
        }
        else
        {
            Generator.AddBlockWithText($"The percent nitrogen of new pasture is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Nitrogen, warnZero:true)} and can be reduced to {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MinimumNitrogen)}");
            Generator.AddBlockWithText($"The minimum Dry Matter Digestaibility (%) is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MinimumDMD, warnZero: true)} and is estimated from N%");
        }
    }
}
