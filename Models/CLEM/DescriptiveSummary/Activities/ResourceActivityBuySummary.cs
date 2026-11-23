using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Resource Activity Buy
/// </summary>
public class ResourceActivityBuySummary : DescriptiveSummaryProviderBase<ResourceActivityBuy>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Obtain {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Units, errorNotSet: true)} ");
        htmlWriter.Write(" packets of ");
        htmlWriter.Write(DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
        if (ModelTyped.AccountName != "No finance required")
        {
            htmlWriter.Write(" using ");
            htmlWriter.Write(DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.AccountName, "Account not set", HTMLSummaryStyle.Resource));
        }
        generator.AddBlockWithText(htmlWriter.ToString());
    }
}
