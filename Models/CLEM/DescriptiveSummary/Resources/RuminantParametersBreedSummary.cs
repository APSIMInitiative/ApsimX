using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersBreedSummary : RuminantParametersSummaryBase<RuminantParametersBreeding>, IRuminantParameterSummaryProvider
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        if (!FormatForParentControl)
            return;

        foreach (var param in GetSummaryParameters().OrderBy(a => a.Category))
        { 
            generator.AddSummaryParameterSnippet(param.Category, param.Value);
        }

        //generator.AddSummaryParameterSnippet(ModelTyped.Name, $"Oestrus cycle is {generator.DisplaySummaryValueSnippet(ModelTyped.OestrusCycleLength, warnZero: true)} days with {generator.DisplaySummaryValueSnippet(ModelTyped.DaysInHeat, warnZero: true)} days in heat");
        //if (ModelTyped.ProportionOffspringMale != 0.5)
        //    generator.AddSummaryParameterSnippet(ModelTyped.Name, $"Proportion of offspring male of {generator.DisplaySummaryValueSnippet(ModelTyped.ProportionOffspringMale, warnZero: true)} is not 0.5");
        //if (ModelTyped.AllowFreemartins)
        //    generator.AddSummaryParameterSnippet(ModelTyped.Name, $"Freemartins are produced");
        //if (ModelTyped.ConceptionDuringLactationProbability < 1.0)
        //    generator.AddSummaryParameterSnippet(ModelTyped.Name, $"Conception rate is multiplied by {generator.DisplaySummaryValueSnippet(ModelTyped.ConceptionDuringLactationProbability)} during lactation");
        //if (ModelTyped.DystociaCoefficients.Sum(a => a) > 0)
        //    generator.AddSummaryParameterSnippet(ModelTyped.Name, $"Mortality from dystocia is included");
    }

    /// <inheritdoc/>
    public override List<(string ComponentName, string Category, string Value)> GetSummaryParameters()
    {
        var summary = new List<(string, string, string)>
        {
            (ModelTyped.Name, "Breeding", $"Oestrus cycle is {generator.DisplaySummaryValueSnippet(ModelTyped.OestrusCycleLength, warnZero: true)} days with {generator.DisplaySummaryValueSnippet(ModelTyped.DaysInHeat, warnZero: true)} days in heat")
        };
        if (ModelTyped.ProportionOffspringMale != 0.5)
            summary.Add((ModelTyped.Name, "Breeding", $"Proportion of offspring male of {generator.DisplaySummaryValueSnippet(ModelTyped.ProportionOffspringMale, warnZero: true)} is not 0.5"));
        if (ModelTyped.AllowFreemartins)
            summary.Add((ModelTyped.Name, "Breeding", $"Freemartins are produced"));
        if (ModelTyped.ConceptionDuringLactationProbability < 1.0)
            summary.Add((ModelTyped.Name, "Breeding", $"Conception rate is multiplied by {generator.DisplaySummaryValueSnippet(ModelTyped.ConceptionDuringLactationProbability)} during lactation"));
        if (ModelTyped.DystociaCoefficients.Sum(a => a) > 0)
            summary.Add((ModelTyped.Name, "Breeding", $"Mortality from dystocia is included"));
        return summary;
    }
}
