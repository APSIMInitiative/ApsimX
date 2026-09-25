using System.Net;
using Models.CLEM.Interfaces;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Simple fallback provider; override lifecycle methods if you want richer defaults. 
/// </summary>
public sealed class DefaultDescriptiveSummaryProvider : DescriptiveSummaryProvider
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var cm = CLEMModel;
        if (cm is null) return;

        // safe, minimal default output
        generator.AddBlockWithText($"No descriptive summary provider is available for {generator.DisplaySummaryValueSnippet(cm?.GetType().Name ?? "Unknown")}.");
    }
}