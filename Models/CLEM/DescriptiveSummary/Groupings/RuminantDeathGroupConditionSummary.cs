using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Ruminant Group filter
/// </summary>
public class RuminantDeathGroupConditionSummary : GroupSummaryBase<RuminantDeathGroupCondition>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        string style = "";
        switch (ModelTyped.ConditionMetric)
        {
            case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                style = "proportion of current weight to maximum weight attained";
                break;
            case ConditionBasedCalculationStyle.RelativeCondition:
                style = "relative condition";
                break;
            case ConditionBasedCalculationStyle.BodyConditionScore:
                style = "body condition score";
                break;
            case ConditionBasedCalculationStyle.EmptyBodyFatProportion:
                style = "proportion of body fat";
                break;
            default:
                break;
        }
        htmlWriter.Write($"Specified individuals with a {generator.DisplaySummaryValueSnippet(style, errorNotSet: true)}");
        htmlWriter.Write($" less than {generator.DisplaySummaryValueSnippet(ModelTyped.CutOff, warnZero: true)}");
        htmlWriter.Write($" have a probability of death of {generator.DisplaySummaryValueSnippet(ModelTyped.ProbabilityOfDying, warnZero: true)} for the time step.");
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());
    }
}
