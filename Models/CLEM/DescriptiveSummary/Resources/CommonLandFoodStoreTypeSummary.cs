using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

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
            Generator.AddBlockWithText("activityentry", $"This common land can be used by animal feed activities only");
        }
        else
        {
            Generator.AddBlockWithText("activityentry", $"This common land can be used by grazing and cut and carry activities");
        }

        if (model.PastureLink != null)
        {
            Generator.AddBlockWithText("activityentry", $"The quality of this common land is based on {generator.DisplaySummaryResourceTypeSnippet(model.PastureLink)} with a further nitrogen reduction of {generator.DisplaySummaryValueSnippet(model.NitrogenReductionFromPasture, warnZero:true)} percent.");
        }
        else
        {
            Generator.AddBlockWithText("activityentry", $"The percent nitrogen of new pasture is {generator.DisplaySummaryValueSnippet(model.Nitrogen, warnZero:true)} and can be reduced to {generator.DisplaySummaryValueSnippet(model.MinimumNitrogen)}");
            Generator.AddBlockWithText("activityentry", $"The minimum Dry Matter Digestaibility (%) is {generator.DisplaySummaryValueSnippet(model.MinimumDMD, warnZero: true)} and is estimated from N%");
        }
    }
}
