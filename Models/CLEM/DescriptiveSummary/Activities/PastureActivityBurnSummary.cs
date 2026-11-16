using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Pasture Activity Burn 
/// </summary>
public class PastureActivityBurnSummary : DescriptiveSummaryProviderBase<PastureActivityBurn>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Burn {DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet(ModelTyped.PaddockName, "Pasture Not Set", nullGeneralYards: false)}");
        htmlWriter.Write($" if less than {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.MinimumProportionGreen.ToString("0.#%"), warnZero: true)} green");
        htmlWriter.Write($" with a burning efficiency of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.BurningEfficiency, warnZero: true)} and ");
        htmlWriter.Write($" and a carbon content of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.CarbonPercent, warnZero: true)}%");
        generator.AddBlockWithText(htmlWriter.ToString());

        ReportEmission("Methane", ModelTyped.MethaneStoreName);
        ReportEmission("NitrousOxide", ModelTyped.NitrousOxideStoreName);
    }

    private void ReportEmission(string gasName, string storeName)
    {
        string store;
        if (storeName is null || storeName == $"Use store named {gasName} if present")
        {
            store = $"{DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet($"[GreenhouseGases].{gasName}")} if present";
        }
        else
        {
            store = $"{DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet(storeName)}";
        }
        generator.AddBlockWithText( $"{gasName} emissions will be placed in {store}");

    }

}
