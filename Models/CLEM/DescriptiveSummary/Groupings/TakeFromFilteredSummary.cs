using Models.CLEM.Groupings;
using System;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Groupings;

/// <summary>
/// Descriptive summary provider for TakeFromFiltered
/// </summary>
public class TakeFromFilteredSummary : FilterSummaryBase<TakeFromFiltered>
{
    /// <inheritdoc/>
    public override string FilterString()
    {
        bool isTake = (ModelTyped.TakeStyle.ToString().Contains("Take"));
        bool isIndividuals = (ModelTyped.TakeStyle == TakeFromFilterStyle.TakeIndividuals || ModelTyped.TakeStyle == TakeFromFilterStyle.SkipIndividuals);
        string output = ((isTake) ? $"Take: " : "Skip: ");
        bool isInvalid = (ModelTyped.Value < 0 || (isIndividuals & ModelTyped.Value > 1));
        if (isInvalid)
        {   
            output += DescriptiveSummaryGenerator.DisplayErrorSnippet("Invalid");
        }
        else
        {
            output = DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(!isIndividuals ? ModelTyped.Value.ToString("P0") : $"{Convert.ToInt32(ModelTyped.Value)}");
        }
        return output;
    }

}