using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Resource Activity External Multiplier
/// </summary>
public class ResourceActivityExternalMultiplierSummary : FilterSummaryBase<ResourceActivityExternalMultiplier>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
        htmlWriter.Write($" x ");
        htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.Multiplier, "Not set", HTMLSummaryStyle.Default, errorNotSet: true, warnZero: true));
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem floatLeft");
    }
}
