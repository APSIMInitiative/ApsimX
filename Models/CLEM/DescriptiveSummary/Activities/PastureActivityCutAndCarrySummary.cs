using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Pasture Activity CutAndCarry
/// </summary>
public class PastureActivityCutAndCarrySummary : DescriptiveSummaryProviderBase<PastureActivityCutAndCarry>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Cut {generator.DisplaySummaryValueSnippet(ModelTyped.Supply, errorNotSet: true)}");
        switch (ModelTyped.CutStyle)
        {
            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                htmlWriter.Write(" kg ");
                break;
            case RuminantFeedActivityTypes.ProportionOfWeight:
                htmlWriter.Write($" of herd {generator.DisplaySummaryValueSnippet("live weight")} ");
                break;
            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                htmlWriter.Write($" of herd {generator.DisplaySummaryValueSnippet("potential intake")} ");
                break;
            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                htmlWriter.Write($" of herd {generator.DisplaySummaryValueSnippet("remaining intake required")} ");
                break;
            default:
                break;
        }
        htmlWriter.Write("from ");
        htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.PaddockName, "Pasture not set", HTMLSummaryStyle.Resource));
        htmlWriter.Write(" and carry to ");
        htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.AnimalFoodStoreName, "Store not set", HTMLSummaryStyle.Resource));
    }
}
