using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

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

        generator.AddBlockWithText($"This food type is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
        generator.AddBlockWithText($"Gross Energy Content: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero:true)}");
        generator.AddBlockWithText($"Metabolisable Energy Content: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
        generator.AddBlockWithText($"Percent fat (ether extract): {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
        generator.AddBlockWithText($"Dry Matter Digestibility (DMD): {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.DryMatterDigestibility, warnZero: true)} %");
        generator.AddBlockWithText($"Gut fill: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.GutFill, warnZero: true)}");
        switch (model.CPContentStyle)
        {
            case CrudeProteinContentStyle.SpecifyCrudeProteinContent:
                generator.AddBlockWithText($"Crude protein percent: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.UserCrudeProteinPercent, warnZero: true)}  %");
                generator.AddBlockWithText($"Rumen degradable crude protein percent: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}  %");
                break;
            case CrudeProteinContentStyle.EstimateFromNitrogenContent:
                generator.AddBlockWithText($"Crude protein estimated from nitrogen percent of: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.UserNitrogenPercent, warnZero: true)}  %");
                generator.AddBlockWithText($"Rumen degradable crude protein percent: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}   %");
                break;
            case CrudeProteinContentStyle.NoCrudeProtein:
                generator.AddBlockWithText($"This feed contains no crude protein");
                break;
            default:
                break;
        }

        if (model.StartingAmount > 0)
        {
            generator.AddBlockWithText($"Simulation starts with {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.StartingAmount)}");
        }
    }
}
