using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Activity Feed Target Purchase
/// </summary>
public class LabourActivityFeedTargetPurchaseSummary : DescriptiveSummaryProviderBase<LabourActivityFeedTargetPurchase>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", $"{generator.DisplaySummaryValueSnippet(ModelTyped.FoodStoreName, "Store not set", entryStyle: HTMLSummaryStyle.Resource, errorNotSet: true)}" +
            $"will be purchased to provide {generator.DisplaySummaryValueSnippet(ModelTyped.TargetProportion, errorNotSet: true)} of remaining intake needed to meet current targets");
    }
}
