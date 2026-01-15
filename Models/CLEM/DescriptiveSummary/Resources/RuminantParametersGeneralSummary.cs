using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// General ruminant parameters component descriptive summary
/// </summary>
internal class RuminantParametersGeneralSummary : DescriptiveSummaryProviderBase<RuminantParametersGeneral>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        Generator.AddBlockWithText("activityentry", $"Standard reference weight (kg) female: {generator.DisplaySummaryValueSnippet<double>(model.SRWFemale, warnZero: true)} kg, male: {generator.DisplaySummaryValueSnippet<double>(model.SRWFemale * model.SRWMaleMultiplier, warnZero: true)} kg, male castrate: {generator.DisplaySummaryValueSnippet<double>(model.SRWFemale * model.SRWCastrateMaleMultiplier, warnZero: true)} kg");
        if (model.IsCN1EstimatedFromWeaningDetails)
        {
            Generator.AddBlockWithText("activityentry", $"The AgeGrowthRateCoefficient (CN1) is estimated using the average weaning weight of [{model.CN1EstimatedWeaningWeight}] and [{model.CN1EstimatedWeaningAge.InDays}] days at weaning.");
        }
        Generator.AddBlockWithText("activityentry", $"Females mature at {generator.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityFemale*model.SRWFemale, warnZero: true)} kg ({generator.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityFemale, warnZero: true)} x female SRW)");
        Generator.AddBlockWithText("activityentry", $"Males mature at {generator.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityMale * model.SRWFemale * model.SRWMaleMultiplier, warnZero: true)} kg ({generator.DisplaySummaryValueSnippet(model.MinimumSizeForMaturityMale, warnZero: true)} x male SRW)");

        if (model.MultipleBirthRate.Sum(a => a) > 0)
            Generator.AddBlockWithText("activityentry", $"Multiple births are possible");

        Generator.AddBlockWithText("activityentry", $"An adult equivalent is {generator.DisplaySummaryValueSnippet(model.BaseAnimalEquivalent, warnZero: true)} kg");
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
    {
        if (!FormatForParentControl)
            Generator.AddBlockWithText("detailsnote", $"General parameters used by multiple activities and growth components.");
    }


}
