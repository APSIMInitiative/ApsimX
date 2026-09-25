using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Ruminant Parameters component descriptive summary
/// </summary>
public class RuminantParametersGrowPFSummary : RuminantParametersSummaryBase<RuminantParametersGrowPF>
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
}
