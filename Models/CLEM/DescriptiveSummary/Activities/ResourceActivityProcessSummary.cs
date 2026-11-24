using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Resource Activity Process
/// </summary>
public class ResourceActivityProcessSummary : DescriptiveSummaryProviderBase<ResourceActivityProcess>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Process {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeProcessedName, entryStyle: HTMLSummaryStyle.Resource)}" +
            $" into {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeCreatedName, entryStyle: HTMLSummaryStyle.Resource)} at a rate of " +
            $"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ConversionRate, errorNotSet: true, warnZero: true)}");

        if (ModelTyped.Reserve > 0)
        {
            generator.AddBlockWithText($"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Reserve)} will be reserved.");
        }
    }
}
