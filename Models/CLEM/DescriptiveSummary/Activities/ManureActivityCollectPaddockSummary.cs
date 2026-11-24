using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Manure Activity Collect From Paddock
/// </summary>
public class ManureActivityCollectPaddockSummary : DescriptiveSummaryProviderBase<ManureActivityCollectPaddock>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Collect manure from {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.GrazeFoodStoreTypeName, "Pasture not set", HTMLSummaryStyle.Resource)}");
    }
}
