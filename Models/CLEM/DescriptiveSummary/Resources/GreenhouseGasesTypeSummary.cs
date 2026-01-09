using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Greenhouse gases type
    /// </summary>
    public class GreenhouseGasesTypeSummary : DescriptiveSummaryProviderBase<GreenhouseGasesType>
    {
        /// <inheritdoc/>
        public override void BuildSummary(GreenhouseGasesType model)
        {
            Generator.AddBlockWithText("activityentry", $"There is a starting amount of {CLEMModel.DisplaySummaryValueSnippet(model.StartingAmount)}");
        }
    }
}
