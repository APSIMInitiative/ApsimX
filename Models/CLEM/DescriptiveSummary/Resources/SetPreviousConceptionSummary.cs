using Models.CLEM.Resources;
using System.Globalization;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for SpecifyRuminant
    /// </summary>
    public class SpecifyPreviousConceptionSummary : DescriptiveSummaryProviderBase<SetPreviousConception>
    {
        /// <inheritdoc/>
        public override void BuildSummary(SetPreviousConception model)
        {
            if (FormatForParentControl)
            {
                // skip if this is inside the table summary of Initial Chohort
                if (!(CurrentAncestorList.Count >= 3 && CurrentAncestorList[CurrentAncestorList.Count - 3] == typeof(RuminantInitialCohorts).Name))
                {
                    Generator.AddBlockWithText("activityentry", $"These individuals will be {CLEMModel.DisplaySummaryValueSnippet<int>(model.NumberDaysPregnant, warnZero: true)} days pregnant");
                }
            }
            else
            {
                Generator.AddBlockWithText("activityentry", $"Set last conception age to make these females {CLEMModel.DisplaySummaryValueSnippet<int>(model.NumberDaysPregnant, warnZero: true)} days pregnant");
            }

        }
    }
}