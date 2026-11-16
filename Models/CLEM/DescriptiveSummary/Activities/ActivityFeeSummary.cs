using Docker.DotNet.Models;
using Models.CLEM.Activities;
using System;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Activity Fee
/// </summary>
public class ActivityFeeSummary : DescriptiveSummaryProviderBase<ActivityFee>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ActivityFeeSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubActivity;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Pay {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Amount, "Rate not set")} " +
            $"per {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Measure, "Measure not set")} " +
            $"from {DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet(ModelTyped.BankAccountName)}");
    }
}
