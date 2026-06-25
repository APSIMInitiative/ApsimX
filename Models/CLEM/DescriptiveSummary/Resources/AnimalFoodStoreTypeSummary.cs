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

        generator.AddBlockWithText($"This food type is {generator.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
        generator.AddBlockWithText($"Gross Energy Content: {generator.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero:true)}");
        generator.AddBlockWithText($"Metabolisable Energy Content: {generator.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
        generator.AddBlockWithText($"Percent fat (ether extract): {generator.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
        generator.AddBlockWithText($"Dry Matter Digestibility (DMD): {generator.DisplaySummaryValueSnippet(model.DryMatterDigestibility, warnZero: true)} %");
        generator.AddBlockWithText($"Gut fill: {generator.DisplaySummaryValueSnippet(model.GutFill, warnZero: true)}");
        switch (model.CPContentStyle)
        {
            case CrudeProteinContentStyle.SpecifyCrudeProteinContent:
                generator.AddBlockWithText($"Crude protein percent: {generator.DisplaySummaryValueSnippet(model.UserCrudeProteinPercent, warnZero: true)}  %");
                generator.AddBlockWithText($"Rumen degradable crude protein percent: {generator.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}  %");
                break;
            case CrudeProteinContentStyle.EstimateFromNitrogenContent:
                generator.AddBlockWithText($"Crude protein estimated from nitrogen percent of: {generator.DisplaySummaryValueSnippet(model.UserNitrogenPercent, warnZero: true)}  %");
                generator.AddBlockWithText($"Rumen degradable crude protein percent: {generator.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}   %");
                break;
            case CrudeProteinContentStyle.NoCrudeProtein:
                generator.AddBlockWithText($"This feed contains no crude protein");
                break;
            default:
                break;
        }

        if (model.StartingAmount > 0)
        {
            generator.AddBlockWithText($"Simulation starts with {generator.DisplaySummaryValueSnippet(model.StartingAmount)}");
        }
    }
}
