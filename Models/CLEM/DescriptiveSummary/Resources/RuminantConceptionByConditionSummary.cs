using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for RuminantConceptionByCondition
/// </summary>
public class RuminantConceptionByConditionSummary : DescriptiveSummaryProviderBase<RuminantConceptionByCondition>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantConceptionByConditionSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;
        string prefix = model.ConditionBasedConceptionStyle switch
        {
            ConditionBasedCalculationStyle.ProportionOfMaxWeightToSurvive => $"Females with a {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet("Ratio of live weight to highest weight achieved")} greater than or equal to ",
            ConditionBasedCalculationStyle.RelativeCondition => $"Females with a {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet("Relative condition (live weight over normalised weight)")} greater than or equal to ",
            ConditionBasedCalculationStyle.BodyConditionScore => $"Females with a {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet("Body Condition Score")} greater than or equal to ",
            _ => "Females ",
        };
        if (model.ConditionBasedConceptionStyle == ConditionBasedCalculationStyle.None)
        {
            Generator.AddBlockWithText($"{prefix}will have a probability of conceiving of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.ConditionBasedConceptionProbability, warnZero: true)}.");
        }
        else
        {
            Generator.AddBlockWithText($"{prefix}{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.ConditionBasedConceptionCutOff, warnZero: true)} will have a probability of conceiving of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.ConditionBasedConceptionProbability, warnZero: true)}.");
        }
    }
}