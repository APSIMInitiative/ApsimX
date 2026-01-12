using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for EquipmentType (sub-resource)
    /// </summary>
    public class EquipmentTypeSummary : DescriptiveSummaryProviderBase<EquipmentType>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            Generator.AddBlockWithText("activityentry", $"There is a starting amount of {CLEMModel.DisplaySummaryValueSnippet(model.StartingAmount)}");
        }

    }
}