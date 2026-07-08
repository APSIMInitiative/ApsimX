using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Enteric CH4 (Charmley)
    /// </summary>
    public class RuminantActivityEntericCH4CharmleySummary : RuminantActivitySummaryBase<RuminantActivityEntericCH4Charmley>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityEntericCH4CharmleySummary()
        {
            SummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText("Produces enteric methane emissions using the Charmley et al. method and places emissions into the greenhouse gas store(s) with AutoAllocate on.");
            generator.AddBlockWithText($"Methane emissions will be calculated from individuals' intake and grouped by {generator.DisplaySummaryValueSnippet(ModelTyped.GroupingStyle, "Style not set")} for reporting.");
        }
    }
}