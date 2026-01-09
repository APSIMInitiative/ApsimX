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
            // Keep minimal for now - expand with type-specific properties if required.
        }
    }
}