using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
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
                    prefix = $"Females with a {CLEMModel.DisplaySummaryValueSnippet("Ratio of live weight to highest weight achieved")} greater than or equal to ";
                    break;
                case ConditionBasedCalculationStyle.RelativeCondition:
                    prefix = $"Females with a {CLEMModel.DisplaySummaryValueSnippet("Relative condition (live weight over normalised weight)")} greater than or equal to ";
                    break;
                case ConditionBasedCalculationStyle.BodyConditionScore:
                    prefix = $"Females with a {CLEMModel.DisplaySummaryValueSnippet("Body Condition Score")} greater than or equal to ";
                    break;
                default:
                    prefix = "Females ";
                    break;
            }

            if (model.ConditionBasedConceptionStyle == ConditionBasedCalculationStyle.None)
            {
                Generator.AddBlockWithText("activityentry", $"{prefix}will have a probability of conceiving of {CLEMModel.DisplaySummaryValueSnippet(model.ConditionBasedConceptionProbability, warnZero: true)}.");
            }
            else
            {
                Generator.AddBlockWithText("activityentry", $"{prefix}{CLEMModel.DisplaySummaryValueSnippet(model.ConditionBasedConceptionCutOff, warnZero: true)} will have a probability of conceiving of {CLEMModel.DisplaySummaryValueSnippet(model.ConditionBasedConceptionProbability, warnZero: true)}.");
            }
        }
    }
}