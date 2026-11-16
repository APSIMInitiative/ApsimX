using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.CLEM.DescriptiveSummary.Groupings;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Resource Activity External Multiplier
/// </summary>
public class ResourceActivityExternalMultiplierSummary : FilterSummaryBase<ResourceActivityExternalMultiplier>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ResourceActivityExternalMultiplierSummary()
    {
        SummaryStyle = HTMLSummaryStyle.Filter;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write(DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
        htmlWriter.Write($" x ");
        htmlWriter.Write(DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Multiplier, "Not set", HTMLSummaryStyle.Default, errorNotSet: true, warnZero: true));
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem floatLeft");
    }
}
