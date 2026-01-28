using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

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
            start = generator.DisplayErrorSnippet("Not Set");
        }
        else
        {
            start = $"{generator.DisplaySummaryValueSnippet(ModelTyped.StartSeasonMonth)}";
        }
        generator.AddBlockWithText($"The rainfall year starts in {start}");
        if (ModelTyped.DoNotShuffleIteration != -1)
        {
            generator.AddBlockWithText($"Rainfall will NOT be shuffled in the CLEM multi-run iteration {ModelTyped.DoNotShuffleIteration}");
        }
        generator.AddBlockWithText($"WARNING: Rainfall years are being shuffled as a proxy for stochastic rainfall variation in this simulation.<br />This is an advance feature provided for particular projects.", "warningbanner");
    }
}
