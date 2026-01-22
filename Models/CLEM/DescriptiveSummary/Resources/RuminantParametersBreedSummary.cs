using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersBreedSummary : RuminantParametersSummaryBase<RuminantParametersBreeding>
{
    /// <inheritdoc/>
    public override List<(string componentName, string propertyName, string category, string description, string value)> GetCustomSummaryParameters()
    {
        var summary = new List<(string, string, string, string, string)>
        {
            (ModelTyped.Name, "OestrusCycleLength", "Breeding", $"Oestrus cycle is {generator.DisplaySummaryValueSnippet(ModelTyped.OestrusCycleLength, warnZero: true)} days with {generator.DisplaySummaryValueSnippet(ModelTyped.DaysInHeat, warnZero: true)} days in heat", ""),
        };

        if (ModelTyped.ProportionOffspringMale == 0.5)
            summary.Add((ModelTyped.Name, "ProportionOffspringMale", "Breeding", $"Proportion of offspring male of {generator.DisplaySummaryValueSnippet(ModelTyped.ProportionOffspringMale, warnZero: true)} is not 0.5", ""));
        if (ModelTyped.AllowFreemartins)
            summary.Add((ModelTyped.Name, "AllowFreemartins", "Breeding", $"Freemartins are produced", ""));
        if (ModelTyped.ConceptionDuringLactationProbability < 1.0)
            summary.Add((ModelTyped.Name, "ConceptionDuringLactationProbability", "Breeding", $"Conception rate is multiplied by {generator.DisplaySummaryValueSnippet(ModelTyped.ConceptionDuringLactationProbability)} during lactation", ""));
        if (ModelTyped.DystociaCoefficients.Sum(a => a) > 0)
            summary.Add((ModelTyped.Name, "DystociaCoefficients", "Breeding", $"Mortality from dystocia is included", ""));
        return summary;
    }

    /// <inheritdoc/>
    public override List<string> SummaryParametersToRemove()
    {
        return ["DaysInHeat",
             "ProportionOffspringMale",
             "AllowFreemartins",
             "ConceptionDuringLactationProbability",
             "DystociaCoefficients"
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        if (!FormatForParentControl)
            return;

        foreach (var param in GetSummaryParameters().OrderBy(a => a.category))
        {
            generator.AddSummaryParameterSnippet(param.category, $"{param.description} {param.value}");
        }
    }
}
