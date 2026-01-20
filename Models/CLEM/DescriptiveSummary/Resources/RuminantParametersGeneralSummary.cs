using Models.CLEM.Interfaces;
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
internal class RuminantParametersGeneralSummary : RuminantParametersSummaryBase<RuminantParametersGeneral>, IRuminantParameterSummaryProvider
{
    /// <inheritdoc/>
    public override List<(string ComponentName, string Category, string Value)> GetSummaryParameters()
    {
        var summary = new List<(string, string, string)>
        {
            (ModelTyped.Name, "Growth", $"Standard reference weight (kg) female: {generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWFemale * ModelTyped.SRWCastrateMaleMultiplier, warnZero: true)} kg"), 
            (ModelTyped.Name, "Growth", $"Standard reference weight (kg) male: {generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWFemale * ModelTyped.SRWMaleMultiplier, warnZero: true)} kg"), 
            (ModelTyped.Name, "Growth", $"Standard reference weight (kg) castrate: {generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWFemale * ModelTyped.SRWCastrateMaleMultiplier, warnZero: true)} kg"), 
            (ModelTyped.Name, "Growth", $"An adult equivalent is {generator.DisplaySummaryValueSnippet(ModelTyped.BaseAnimalEquivalent, warnZero: true)} kg")
        };
        if (ModelTyped.IsCN1EstimatedFromWeaningDetails)
            summary.Add((ModelTyped.Name, "Growth", $"The AgeGrowthRateCoefficient (CN1) is estimated using the average weaning weight of [{ModelTyped.CN1EstimatedWeaningWeight}] and [{ModelTyped.CN1EstimatedWeaningAge.InDays}] days at weaning."));

        summary.Add((ModelTyped.Name, "Breeding", $"Females mature at {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityFemale * ModelTyped.SRWFemale, warnZero: true)} kg ({generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityFemale, warnZero: true)} x female SRW)"));
        summary.Add((ModelTyped.Name, "Breeding", $"Males mature at {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityMale * ModelTyped.SRWFemale * ModelTyped.SRWMaleMultiplier, warnZero: true)} kg ({generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityMale, warnZero: true)} x male SRW)"));

        if (ModelTyped.MultipleBirthRate.Sum(a => a) > 0)
            summary.Add((ModelTyped.Name, "Breeding", $"Multiple births are possible"));
        return summary;
    }
}
