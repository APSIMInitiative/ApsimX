using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// General ruminant parameters component descriptive summary
    /// </summary>
    internal class RuminantParametersGeneralSummary : DescriptiveSummaryProviderBase<RuminantParametersGeneral>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantParametersGeneral model)
        {

            Generator.AddBlockWithText("activityentry", $"Standard reference weight (kg) female: {CLEMModel.DisplaySummaryValueSnippet<double>(model.SRWFemale, warnZero: true)} kg, male: {CLEMModel.DisplaySummaryValueSnippet<double>(model.SRWFemale * model.SRWMaleMultiplier, warnZero: true)} kg, male castrate: {CLEMModel.DisplaySummaryValueSnippet<double>(model.SRWFemale * model.SRWCastrateMaleMultiplier, warnZero: true)} kg");
            if (model.IsCN1EstimatedFromWeaningDetails)
            {
                Generator.AddBlockWithText("activityentry", $"The AgeGrowthRateCoefficient (CN1) is estimated using the average weaning weight of [{model.CN1EstimatedWeaningWeight}] and [{model.CN1EstimatedWeaningAge.InDays}] days at weaning.");
            }
            Generator.AddBlockWithText("activityentry", $"Females mature at {CLEMModel.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityFemale*model.SRWFemale, warnZero: true)} kg ({CLEMModel.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityFemale, warnZero: true)} x female SRW)");
            Generator.AddBlockWithText("activityentry", $"Males mature at {CLEMModel.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityMale * model.SRWFemale * model.SRWMaleMultiplier, warnZero: true)} kg ({CLEMModel.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityMale, warnZero: true)} x male SRW)");

            if (model.MultipleBirthRate.Sum(a => a) > 0)
                Generator.AddBlockWithText("activityentry", $"Multiple births are possible");

            Generator.AddBlockWithText("activityentry", $"An adult equivalent is {CLEMModel.DisplaySummaryValueSnippet(model.BaseAnimalEquivalent, warnZero: true)} kg");
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            Generator.AddBlockWithText("detailsnote", $"General parameters used by multiple activities and growth components.");
        }


    }
}
