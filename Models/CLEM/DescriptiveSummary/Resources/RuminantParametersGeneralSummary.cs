using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// General ruminant parameters component descriptive summary
/// </summary>
internal class RuminantParametersGeneralSummary : RuminantParametersSummaryBase<RuminantParametersGeneral>
{
    /// <inheritdoc/>
    public override List<(string componentName, string propertyName, string category, string description, string value)> GetCustomSummaryParameters()
    {
        var summary = new List<(string, string, string, string, string)>
        {
            //(ModelTyped.Name, "Breed", "General", $"This breed is", generator.DisplaySummaryValueSnippet(ModelTyped.Breed, warnZero: true)),
            (ModelTyped.Name, "SRWFemale", "Growth", $"Standard reference weight (kg) of a female is", generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWFemale, warnZero: true)),
            (ModelTyped.Name, "SRWMaleMultiplier", "Growth", $"Standard reference weight (kg) of a male is {generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWFemale * ModelTyped.SRWMaleMultiplier, warnZero: true)} ({generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWMaleMultiplier, warnZero: true)} x SRW female)", ""),
            (ModelTyped.Name, "SRWCastrateMaleMultiplier", "Growth", $"Standard reference weight (kg) of a castrate is {generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWFemale * ModelTyped.SRWCastrateMaleMultiplier, warnZero: true)} ({generator.DisplaySummaryValueSnippet<double>(ModelTyped.SRWCastrateMaleMultiplier, warnZero: true)} x SRW female)", ""),
            (ModelTyped.Name, "BaseAnimalEquivalent", "Growth", $"An adult equivalent is", generator.DisplaySummaryValueSnippet(ModelTyped.BaseAnimalEquivalent, warnZero: true))
        };

        if (ModelTyped.NaturalWeaningAge.InDays != 0)
            summary.Add((ModelTyped.Name, "NaturalWeaningAge", "Growth", $"Natural weaning age is {generator.DisplaySummaryValueSnippet(ModelTyped.NaturalWeaningAge.InDays, warnZero: true)} days", ""));
        else
            summary.Add((ModelTyped.Name, "NaturalWeaningAge", "Growth", $"Natural weaning age is set to the same values as gestation length of {generator.DisplaySummaryValueSnippet(ModelTyped.GestationLength.InDays, warnZero: true)} days", ""));

        if (ModelTyped.IsCN1EstimatedFromWeaningDetails)
            summary.Add((ModelTyped.Name, "CN1EstimatedWeaningWeight", "Growth", $"The AgeGrowthRateCoefficient (CN1) is estimated using the average weaning weight of [{generator.DisplaySummaryValueSnippet(ModelTyped.CN1EstimatedWeaningWeight, warnZero: true)}] and [{generator.DisplaySummaryValueSnippet(ModelTyped.CN1EstimatedWeaningAge.InDays, warnZero: true)}] days at weaning.", ""));

        summary.Add((ModelTyped.Name, "MinimumSizeForMaturityFemale", "Breeding", $"Females mature at {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityFemale * ModelTyped.SRWFemale, warnZero: true)} kg ({generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityFemale, warnZero: true)} x female SRW)", ""));
        summary.Add((ModelTyped.Name, "MinimumSizeForMaturityMale", "Breeding", $"Males mature at {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityMale * ModelTyped.SRWFemale * ModelTyped.SRWMaleMultiplier, warnZero: true)} kg ({generator.DisplaySummaryValueSnippet(ModelTyped.MinimumSizeForMaturityMale, warnZero: true)} x male SRW)", ""));
        summary.Add((ModelTyped.Name, "GestationLength", "Breeding", $"Gestation length is {generator.DisplaySummaryValueSnippet(ModelTyped.GestationLength.InDays, warnZero: true)} days", ""));

        if (ModelTyped.MultipleBirthRate.Sum(a => a) > 0)
        {
            List<string> multipleBirthRates = ["single", "twins", "triplets", "quadruplets", "quintuplets"];
            string multiBirth = "";
            string multiBirthWt = "";
            string others = "";
            if (ModelTyped.MultipleBirthRate.Length > multipleBirthRates.Count)
            {
                others = $"... and {ModelTyped.MultipleBirthRate.Length - multipleBirthRates.Count} more";
            }
            for (int i = 0; i < Math.Min(ModelTyped.MultipleBirthRate.Length, multipleBirthRates.Count); i++)
            {
                multiBirth += $"{multipleBirthRates[i]}: {ModelTyped.MultipleBirthRate[i]}";
                if (ModelTyped.BirthScalar.Length >= i)
                {
                    multiBirthWt += $"{multipleBirthRates[i]}: {ModelTyped.BirthScalar[i]}";
                }

            }
            summary.Add((ModelTyped.Name, "MultipleBirthRate", "Breeding", $"Multiple births are possible the following probabilities: {multiBirth}{others}", ""));
            summary.Add((ModelTyped.Name, "MultipleBirthRate", "Breeding", $"Maximum birth weights: {multiBirthWt}{others}", ""));
        }
        else
        {
            if (ModelTyped.BirthScalar.Length > 0)
                summary.Add((ModelTyped.Name, "BirthScalar", "Breeding", $"Maximum birth weight is", $"{generator.DisplaySummaryValueSnippet(ModelTyped.BirthScalar[0], warnZero: true)}"));
            else
                summary.Add((ModelTyped.Name, "BirthScalar", "Breeding", $"{generator.DisplayErrorSnippet("No birth scalars set for birth weight")}", ""));
        }

        if (ModelTyped.IncludeWool)
            summary.Add((ModelTyped.Name, "IncludeWool", "General", $"Wool production is included", ""));

        return summary;
    }

    /// <inheritdoc/>
    public override List<string> SummaryParametersToRemove()
    {
        List<string> remove = [];

        switch (ModelTyped.AgeGrowthRateCoefficientProvisionStyle)
        {
            case AgeGrowthRateCoefficientProvisionTypes.ProvideValue:
                remove.Add("CN1EstimatedWeaningWeight");
                remove.Add("CN1EstimatedWeaningAge");
                break;
            case AgeGrowthRateCoefficientProvisionTypes.EstimateFromAverageWeaningDetails:
                remove.Add("AgeGrowthRateCoefficient_CN1");
                break;
            default:
                break;
        }
        return remove;
    }
}
