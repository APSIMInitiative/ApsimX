using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Pasture Activity Burn 
/// </summary>
public class PastureActivityBurnSummary : DescriptiveSummaryProviderBase<PastureActivityBurn>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Burn {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.PaddockName, "Pasture Not Set", nullGeneralYards: false)}");
        htmlWriter.Write($" if less than {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumProportionGreen.ToString("0.#%"), warnZero: true)} green");
        htmlWriter.Write($" with a burning efficiency of {generator.DisplaySummaryValueSnippet(ModelTyped.BurningEfficiency, warnZero: true)} and ");
        htmlWriter.Write($" and a carbon content of {generator.DisplaySummaryValueSnippet(ModelTyped.CarbonPercent, warnZero: true)}%");
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());

        reportEmission("Methane", ModelTyped.MethaneStoreName);
        reportEmission("NitrousOxide", ModelTyped.NitrousOxideStoreName);
    }

    private void reportEmission(string gasName, string storeName)
    {
        string store = "";
        if (storeName is null || storeName == $"Use store named {gasName} if present")
        {
            store = $"{generator.DisplaySummaryResourceTypeSnippet($"[GreenhouseGases].{gasName}")} if present";
        }
        else
        {
            store = $"{generator.DisplaySummaryResourceTypeSnippet(storeName)}";
        }
        generator.AddBlockWithText("activityentry", $"{gasName} emissions will be placed in {store}");

    }

}
