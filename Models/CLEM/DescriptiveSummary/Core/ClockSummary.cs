using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for APSIM Clock
/// </summary>
public class ClockSummary : DescriptiveSummaryProviderBase<Clock>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        Generator.AddBlockWithText("activityentry", $"The simulation is performed from {generator.DisplaySummaryValueSnippet(ModelTyped.StartDate.ToShortDateString())} to {generator.DisplaySummaryValueSnippet(ModelTyped.EndDate.ToShortDateString())}");
    }
}
