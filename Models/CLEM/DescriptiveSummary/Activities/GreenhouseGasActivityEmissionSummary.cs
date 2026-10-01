using Docker.DotNet.Models;
using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Greenhouse Gas Emission Activity
/// </summary>
public class GreenhouseGasActivityEmissionSummary : DescriptiveSummaryProviderBase<GreenhouseGasActivityEmission>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Produce {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GreenhouseGasStoreName)} at a rate of {generator.DisplaySummaryValueSnippet(ModelTyped.Amount, warnZero: true)} {generator.DisplaySummaryValueSnippet(ModelTyped.Measure, warnZero: true)}");
    }
}
