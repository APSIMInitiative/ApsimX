using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Activity Feed
/// </summary>
public class LabourActivityFeedTargetsSummary : DescriptiveSummaryProviderBase<LabourActivityFeedTarget>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ModelTyped.Metric, "Metric not set"));
        htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.TargetValue)} units per AE per day");
        htmlWriter.Write($" up to a maximum of {generator.DisplaySummaryValueSnippet(ModelTyped.TargetMaximumValue, errorNotSet: true)}");
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());

        if (ModelTyped.OtherSourcesValue > 0)
        {
            generator.AddBlockWithText("activityentry", $"{generator.DisplaySummaryValueSnippet(ModelTyped.TargetMaximumValue, errorNotSet: true)} is provided from sources outside the human food store");
        }
    }
}
