using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for RuminantConceptionByCondition
/// </summary>
public class RuminantConceptionByConditionSummary : DescriptiveSummaryProviderBase<RuminantConceptionByCondition>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string prefix;
        switch (model.ConditionBasedConceptionStyle)
        {
            case ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive:
                prefix = $"Females with a {generator.DisplaySummaryValueSnippet("Ratio of live weight to highest weight achieved")} greater than or equal to ";
                break;
            case ConditionBasedCalculationStyle.RelativeCondition:
                prefix = $"Females with a {generator.DisplaySummaryValueSnippet("Relative condition (live weight over normalised weight)")} greater than or equal to ";
                break;
            case ConditionBasedCalculationStyle.BodyConditionScore:
                prefix = $"Females with a {generator.DisplaySummaryValueSnippet("Body Condition Score")} greater than or equal to ";
                break;
            default:
                prefix = "Females ";
                break;
        }

        if (model.ConditionBasedConceptionStyle == ConditionBasedCalculationStyle.None)
        {
            Generator.AddBlockWithText($"{prefix}will have a probability of conceiving of {generator.DisplaySummaryValueSnippet(model.ConditionBasedConceptionProbability, warnZero: true)}.");
        }
        else
        {
            Generator.AddBlockWithText($"{prefix}{generator.DisplaySummaryValueSnippet(model.ConditionBasedConceptionCutOff, warnZero: true)} will have a probability of conceiving of {generator.DisplaySummaryValueSnippet(model.ConditionBasedConceptionProbability, warnZero: true)}.");
        }
    }
}