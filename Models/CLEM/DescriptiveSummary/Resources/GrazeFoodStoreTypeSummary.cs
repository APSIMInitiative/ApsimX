using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for the GrazeFoodStore Resource
/// </summary>
public class GrazeFoodStoreTypeSummary : DescriptiveSummaryProviderBase<GrazeFoodStoreType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        Generator.AddBlockWithText($"This store is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
        Generator.AddBlockWithText($"Gross Energy Content: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero: true)}");
        Generator.AddBlockWithText($"Metabolisable Energy Content: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
        Generator.AddBlockWithText($"Percent fat (ether extract): {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
        Generator.AddBlockWithText($"Crude protein is determined from Percent Nitrogen");

        if (model.IsDMDProvided())
            Generator.AddBlockWithText($"Dry Matter Digestibility (DMD) of new growth is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.GreenDMD)}%");
        else
        {
            Generator.AddBlockWithText($"Dry Matter Digestibility (DMD) of new growth is calculated: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.DMDStyle)}");
            Generator.AddBlockWithText($"N% to Dry Matter Digestibility (DMD) coefficient: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.NToDMDCoefficient)}");
            Generator.AddBlockWithText($"N% to Dry Matter Digestibility (DMD) coefficient: {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.NToDMDIntercept)}");
        }


        Generator.AddBlockWithText($"This pasture has an initial green nitrogen content of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.GreenNitrogenPercent, warnZero: true)}%");
        Generator.AddBlockWithText($"Percent nitrogen declines by {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.DecayNitrogen, warnZero: true)}% per month to a minimum of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MinimumNitrogen)}%");
        if (model.IsDMDProvided())
            Generator.AddBlockWithText($"DMD (%) will decay at a rate of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.DecayDMD, warnZero: true)} per month to a minimum of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MinimumDMD)}");

        Generator.AddBlockWithText($"Pasture is lost through detachment at a rate of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.DetachRate, warnZero: true)} per month and {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.CarryoverDetachRate, warnZero: true)} per month after 12 months");

        if (model.StartingAmount > 0)
        {
            Generator.AddBlockWithText($"Simulation starts with {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.StartingAmount)}");
            Generator.AddBlockWithText($"First month of growing season is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.FirstMonthOfGrowSeason)}");
            Generator.AddBlockWithText($"Last month of growing season is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.LastMonthOfGrowSeason)}");
            Generator.AddBlockWithText($"Number of months prior to simulation to consider for initial biomass {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.NumberMonthsForInitialBiomass)}");
        }



    }
}
