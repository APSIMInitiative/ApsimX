using Docker.DotNet.Models;
using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Greenhouse Gas Emission Activity
/// </summary>
public class GreenhouseGasActivityEmissionSummary : DescriptiveSummaryProviderBase<GreenhouseGasActivityEmission>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Produce {DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet(ModelTyped.GreenhouseGasStoreName)} at a rate of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Amount, warnZero: true)} {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Measure, warnZero: true)}");
    }
}
