using Models.CLEM.Limiters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Activity Carry Limiter
/// </summary>
public class ActivityCarryLimiterSummary : TimerSummaryBase<ActivityCarryLimiter>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        string limit = $"Limit cut and carry activities to ";
        if (ModelTyped.WeightLimitPerDay is not null && ModelTyped.WeightLimitPerDay.Length >= 1)
        {
            limit += generator.DisplaySummaryValueSnippet(ModelTyped.WeightLimitPerDay);
        }
        else
        {
            limit += generator.DisplayErrorSnippet("Not Set");
        }
        generator.OpenBlock("entryValue filterItem", limit+ " dry kg/day");
    }
}
