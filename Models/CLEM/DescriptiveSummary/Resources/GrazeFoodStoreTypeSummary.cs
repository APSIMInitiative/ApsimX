using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
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

            Generator.AddBlockWithText("activityentry", $"This store is {CLEMModel.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
            Generator.AddBlockWithText("activityentry", $"Gross Energy Content: {CLEMModel.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero: true)}");
            Generator.AddBlockWithText("activityentry", $"Metabolisable Energy Content: {CLEMModel.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
            Generator.AddBlockWithText("activityentry", $"Percent fat (ether extract): {CLEMModel.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
            Generator.AddBlockWithText("activityentry", $"Crude protein is determined from Percent Nitrogen");

            if (model.IsDMDProvided())
                Generator.AddBlockWithText("activityentry", $"Dry Matter Digestibility (DMD) of new growth is {CLEMModel.DisplaySummaryValueSnippet(model.GreenDMD)}%");
            else
            {
                Generator.AddBlockWithText("activityentry", $"Dry Matter Digestibility (DMD) of new growth is calculated: {CLEMModel.DisplaySummaryValueSnippet(model.DMDStyle)}");
                Generator.AddBlockWithText("activityentry", $"N% to Dry Matter Digestibility (DMD) coefficient: {CLEMModel.DisplaySummaryValueSnippet(model.NToDMDCoefficient)}");
                Generator.AddBlockWithText("activityentry", $"N% to Dry Matter Digestibility (DMD) coefficient: {CLEMModel.DisplaySummaryValueSnippet(model.NToDMDIntercept)}");
            }


            Generator.AddBlockWithText("activityentry", $"This pasture has an initial green nitrogen content of {CLEMModel.DisplaySummaryValueSnippet(model.GreenNitrogenPercent, warnZero: true)}%");
            Generator.AddBlockWithText("activityentry", $"Percent nitrogen declines by {CLEMModel.DisplaySummaryValueSnippet(model.DecayNitrogen, warnZero: true)}% per month to a minimum of {CLEMModel.DisplaySummaryValueSnippet(model.MinimumNitrogen)}%");
            if (model.IsDMDProvided())
                Generator.AddBlockWithText("activityentry", $"DMD (%) will decay at a rate of {CLEMModel.DisplaySummaryValueSnippet(model.DecayDMD, warnZero: true)} per month to a minimum of {CLEMModel.DisplaySummaryValueSnippet(model.MinimumDMD)}");

            string detachText = $"Pasture is lost through detachment at a rate of {CLEMModel.DisplaySummaryValueSnippet(model.DetachRate, warnZero: true)} per month and  and {CLEMModel.DisplaySummaryValueSnippet(model.CarryoverDetachRate, warnZero: true)} per month after 12 months";

            if (model.StartingAmount > 0)
            {
                Generator.AddBlockWithText("activityentry", $"Simulation starts with {CLEMModel.DisplaySummaryValueSnippet(model.StartingAmount)}");
                Generator.AddBlockWithText("activityentry", $"First month of growing season is {CLEMModel.DisplaySummaryValueSnippet(model.FirstMonthOfGrowSeason)}");
                Generator.AddBlockWithText("activityentry", $"Last month of growing season is {CLEMModel.DisplaySummaryValueSnippet(model.LastMonthOfGrowSeason)}");
                Generator.AddBlockWithText("activityentry", $"Number of months prior to simulation to consider for initial biomass {CLEMModel.DisplaySummaryValueSnippet(model.NumberMonthsForInitialBiomass)}");
            }



        }
    }
}
