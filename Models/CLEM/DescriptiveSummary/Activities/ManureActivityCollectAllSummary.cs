using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Manure Activity Collect All
/// </summary>
public class ManureActivityCollectAllSummary : DescriptiveSummaryProviderBase<ManureActivityCollectAll>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", $"Collect manure from all pasture");
    }
}
