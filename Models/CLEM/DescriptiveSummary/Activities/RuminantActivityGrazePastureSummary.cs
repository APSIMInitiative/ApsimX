using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity GrazePasture
    /// </summary>
    public class RuminantActivityGrazePastureSummary : RuminantActivitySummaryBase<RuminantActivityGrazePasture>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText($"All individuals in {generator.DisplaySummaryValueSnippet(ModelTyped.GrazeFoodStoreTypeName, "Pasture not set", HTMLSummaryStyle.Resource)}" +
                $" will graze for {generator.DisplaySummaryValueSnippet(ModelTyped.HoursGrazed, errorNotSet: true, warnZero: true)} hours of the maximum 8 hours per day.");

            generator.AddBlockWithText("This activity arbitrates pasture allocation between breed-specific grazing sub-activities.", "componentBanner");
        }
    }
}