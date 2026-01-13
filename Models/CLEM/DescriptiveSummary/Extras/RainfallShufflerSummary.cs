using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Extras
{
    /// <summary>
    /// Descriptive summary provider for the Rainfall Shuffler
    /// </summary>
    public class RainfallShufflerSummary : DescriptiveSummaryProviderBase<RainfallShuffler>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            string start = "";
            if (ModelTyped.StartSeasonMonth == MonthsOfYear.NotSet)
            {
                start = "<span class=\"errorlink\">Not set</span>";
            }
            else
            {
                start = $"{CLEMModel.DisplaySummaryValueSnippet(ModelTyped.StartSeasonMonth)}";
            }
            generator.AddBlockWithText("activityentry", $"The rainfall year starts in {start}");
            generator.AddBlockWithText("warningbanner", $"WARNING: Rainfall years are being shuffled as a proxy for stochastic rainfall variation in this simulation.<br />This is an advance feature provided for particular projects.");
        }
    }
}
