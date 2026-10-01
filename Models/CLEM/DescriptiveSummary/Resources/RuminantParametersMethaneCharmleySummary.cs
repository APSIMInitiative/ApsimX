using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersMethaneCharmleySummary : RuminantParametersSummaryBase<RuminantParametersMethaneCharmley>
{
    /// <inheritdoc/>
    public override bool IsNeeded()
    {
        var charmParams = ModelTyped.Structure.FindAll<RuminantParametersMethaneCharmleySummary>();
        if (charmParams is null)
        {
            return false;
        }
        return true;
    }
}
