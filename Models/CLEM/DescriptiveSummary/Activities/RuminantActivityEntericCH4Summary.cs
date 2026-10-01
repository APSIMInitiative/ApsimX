using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Enteric CH4
    /// </summary>
    public class RuminantActivityEntericCH4Summary : RuminantActivitySummaryBase<RuminantActivityEntericCH4>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityEntericCH4Summary()
        {
            SummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText($"Produces enteric methane emissions using {generator.DisplaySummaryValueSnippet(ModelTyped.EquationToUse, errorNotSet: true, errorString: "Equation not specified")}");
            generator.AddBlockWithText($"Methane emissions will be calculated from individuals' intake and grouped by {generator.DisplaySummaryValueSnippet(ModelTyped.GroupingStyle, "Style not set")} for reporting.");
        }
    }
}