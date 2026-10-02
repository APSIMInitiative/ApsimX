using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersGrowPFCISummary : RuminantParametersSummaryBase<RuminantParametersGrowPFCI>
{
    /// <inheritdoc/>
    public override bool IsNeeded()
    {
        var component = ModelTyped.Structure.Find<RuminantActivityGrowPF>();
        if (component is null || component.Enabled == false)
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override List<(string componentName, string propertyName, string category, string description, string value)> GetCustomSummaryParameters()
    {
        var summary = new List<(string, string, string, string, string)>();

        if (ModelTyped.RelativeConditionEffect_CI20 == 1.0)
        {
            summary.Add((ModelTyped.Name, "RelativeConditionEffect_CI20", "Growth", $"Ruminant intake reduction based on high condition is disabled{generator.AddLineBreak}To allow this functionality set [GrowPF CI].RelativeConditionEffect_CI20 to a value >= {generator.DisplaySummaryValueSnippet(1)} (default 1.5)", ""));
        }
        if (ModelTyped.IgnoreFeedQualityIntakeAdjustment)
        {
            summary.Add((ModelTyped.Name, "IgnoreFeedQualityIntakeAdjustment", "Growth", $"Ruminant intake reduction based on high condition is disabled{generator.AddLineBreak}To allow this functionality set [GrowPF CI].IgnoreFeedQualityIntakeAdjustment to {generator.DisplaySummaryValueSnippet("False")}", ""));
        }
        return summary;
    }
}
