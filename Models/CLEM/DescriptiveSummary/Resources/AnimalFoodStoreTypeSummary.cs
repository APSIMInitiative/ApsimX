using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
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

            Generator.AddBlockWithText("activityentry", $"This food type is {CLEMModel.DisplaySummaryValueSnippet(model.TypeOfFeed, "Not specified")} with the following properties:");
            Generator.AddBlockWithText("activityentry", $"Gross Energy Content: {CLEMModel.DisplaySummaryValueSnippet(model.GrossEnergyContent, warnZero:true)}");
            Generator.AddBlockWithText("activityentry", $"Metabolisable Energy Content: {CLEMModel.DisplaySummaryValueSnippet(model.MetabolisableEnergyContent, warnZero: true)}");
            Generator.AddBlockWithText("activityentry", $"Percent fat (ether extract): {CLEMModel.DisplaySummaryValueSnippet(model.FatPercent, warnZero: true)} %");
            Generator.AddBlockWithText("activityentry", $"Dry Matter Digestibility (DMD): {CLEMModel.DisplaySummaryValueSnippet(model.DryMatterDigestibility, warnZero: true)} %");
            switch (model.CPContentStyle)
            {
                case CrudeProteinContentStyle.SpecifyCrudeProteinContent:
                    Generator.AddBlockWithText("activityentry", $"Crude protein percent: {CLEMModel.DisplaySummaryValueSnippet(model.UserCrudeProteinPercent, warnZero: true)}  %");
                    Generator.AddBlockWithText("activityentry", $"Rumen degradable crude protein percent: {CLEMModel.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}  %");
                    break;
                case CrudeProteinContentStyle.EstimateFromNitrogenContent:
                    Generator.AddBlockWithText("activityentry", $"Crude protein estimated from nitrogen percent of: {CLEMModel.DisplaySummaryValueSnippet(model.UserNitrogenPercent, warnZero: true)}  %");
                    Generator.AddBlockWithText("activityentry", $"Rumen degradable crude protein percent: {CLEMModel.DisplaySummaryValueSnippet(model.RumenDegradableProteinPercent, warnZero: true)}   %");
                    break;
                case CrudeProteinContentStyle.NoCrudeProtein:
                    Generator.AddBlockWithText("activityentry", $"This feed contains no crude protein");
                    break;
                default:
                    break;
            }

            if (model.StartingAmount > 0)
            {
                Generator.AddBlockWithText("activityentry", $"Simulation starts with {CLEMModel.DisplaySummaryValueSnippet(model.StartingAmount)}");
            }
        }
    }
}
