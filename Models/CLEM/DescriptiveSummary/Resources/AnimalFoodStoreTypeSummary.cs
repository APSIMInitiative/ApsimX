using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Finances Resource
/// </summary>
public class AnimalFoodStoreTypeSummary : DescriptiveSummaryProviderBase<AnimalFoodStoreType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        Generator.AddBlockWithText("activityentry", $"This food type is {generator.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
        Generator.AddBlockWithText("activityentry", $"Gross Energy Content: {generator.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero:true)}");
        Generator.AddBlockWithText("activityentry", $"Metabolisable Energy Content: {generator.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
        Generator.AddBlockWithText("activityentry", $"Percent fat (ether extract): {generator.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
        Generator.AddBlockWithText("activityentry", $"Dry Matter Digestibility (DMD): {generator.DisplaySummaryValueSnippet(model.DryMatterDigestibility, warnZero: true)} %");
        switch (model.CPContentStyle)
        {
            case CrudeProteinContentStyle.SpecifyCrudeProteinContent:
                Generator.AddBlockWithText("activityentry", $"Crude protein percent: {generator.DisplaySummaryValueSnippet(model.UserCrudeProteinPercent, warnZero: true)}  %");
                Generator.AddBlockWithText("activityentry", $"Rumen degradable crude protein percent: {generator.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}  %");
                break;
            case CrudeProteinContentStyle.EstimateFromNitrogenContent:
                Generator.AddBlockWithText("activityentry", $"Crude protein estimated from nitrogen percent of: {generator.DisplaySummaryValueSnippet(model.UserNitrogenPercent, warnZero: true)}  %");
                Generator.AddBlockWithText("activityentry", $"Rumen degradable crude protein percent: {generator.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}   %");
                break;
            case CrudeProteinContentStyle.NoCrudeProtein:
                Generator.AddBlockWithText("activityentry", $"This feed contains no crude protein");
                break;
            default:
                break;
        }

        if (model.StartingAmount > 0)
        {
            Generator.AddBlockWithText("activityentry", $"Simulation starts with {generator.DisplaySummaryValueSnippet(model.StartingAmount)}");
        }
    }
}
