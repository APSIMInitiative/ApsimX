using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

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

        Generator.AddBlockWithText("activityentry", $"This store is {generator.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
        Generator.AddBlockWithText("activityentry", $"Gross Energy Content: {generator.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero: true)}");
        Generator.AddBlockWithText("activityentry", $"Metabolisable Energy Content: {generator.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
        Generator.AddBlockWithText("activityentry", $"Percent fat (ether extract): {generator.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
        Generator.AddBlockWithText("activityentry", $"Crude protein is determined from Percent Nitrogen");

        if (model.IsDMDProvided())
            Generator.AddBlockWithText("activityentry", $"Dry Matter Digestibility (DMD) of new growth is {generator.DisplaySummaryValueSnippet(model.GreenDMD)}%");
        else
        {
            Generator.AddBlockWithText("activityentry", $"Dry Matter Digestibility (DMD) of new growth is calculated: {generator.DisplaySummaryValueSnippet(model.DMDStyle)}");
            Generator.AddBlockWithText("activityentry", $"N% to Dry Matter Digestibility (DMD) coefficient: {generator.DisplaySummaryValueSnippet(model.NToDMDCoefficient)}");
            Generator.AddBlockWithText("activityentry", $"N% to Dry Matter Digestibility (DMD) coefficient: {generator.DisplaySummaryValueSnippet(model.NToDMDIntercept)}");
        }


        Generator.AddBlockWithText("activityentry", $"This pasture has an initial green nitrogen content of {generator.DisplaySummaryValueSnippet(model.GreenNitrogenPercent, warnZero: true)}%");
        Generator.AddBlockWithText("activityentry", $"Percent nitrogen declines by {generator.DisplaySummaryValueSnippet(model.DecayNitrogen, warnZero: true)}% per month to a minimum of {generator.DisplaySummaryValueSnippet(model.MinimumNitrogen)}%");
        if (model.IsDMDProvided())
            Generator.AddBlockWithText("activityentry", $"DMD (%) will decay at a rate of {generator.DisplaySummaryValueSnippet(model.DecayDMD, warnZero: true)} per month to a minimum of {generator.DisplaySummaryValueSnippet(model.MinimumDMD)}");

        string detachText = $"Pasture is lost through detachment at a rate of {generator.DisplaySummaryValueSnippet(model.DetachRate, warnZero: true)} per month and  and {generator.DisplaySummaryValueSnippet(model.CarryoverDetachRate, warnZero: true)} per month after 12 months";

        if (model.StartingAmount > 0)
        {
            Generator.AddBlockWithText("activityentry", $"Simulation starts with {generator.DisplaySummaryValueSnippet(model.StartingAmount)}");
            Generator.AddBlockWithText("activityentry", $"First month of growing season is {generator.DisplaySummaryValueSnippet(model.FirstMonthOfGrowSeason)}");
            Generator.AddBlockWithText("activityentry", $"Last month of growing season is {generator.DisplaySummaryValueSnippet(model.LastMonthOfGrowSeason)}");
            Generator.AddBlockWithText("activityentry", $"Number of months prior to simulation to consider for initial biomass {generator.DisplaySummaryValueSnippet(model.NumberMonthsForInitialBiomass)}");
        }



    }
}
