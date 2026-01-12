using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Greenhouse gases type
    /// </summary>
    public class GreenhouseGasesTypeSummary : DescriptiveSummaryProviderBase<GreenhouseGasesType>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            if (model.AutoCollectType != GreenhouseGasTypes.None)
            {
                Generator.AddBlockWithText("activityentry", $"This store will automatically receive {CLEMModel.DisplaySummaryValueSnippet(model.AutoCollectType)} from activities");
            }

            Generator.AddBlockWithText("activityentry", $"There is a starting amount of {CLEMModel.DisplaySummaryValueSnippet(model.StartingAmount)}");
        }
    }
}
