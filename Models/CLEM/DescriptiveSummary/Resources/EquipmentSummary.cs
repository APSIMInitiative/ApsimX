using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for Equipment resource
    /// </summary>
    public class EquipmentSummary : DescriptiveSummaryProviderBase<Equipment>
    {
        /// <inheritdoc/>
        public override void BuildSummary(Equipment model)
        {
            // Keep minimal for now - expand with equipment-specific properties if required.
        }
    }
}