using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersGrazingSummary : RuminantParametersSummaryBase<RuminantParametersGrazing>
{
    /// <inheritdoc/>
    public override bool IsNeeded()
    {
        List<Type> suitableActivities = [typeof(RuminantActivityGrazeAll), typeof(RuminantActivityGrazePasture), typeof(RuminantActivityGrazePastureHerd)];

        var allModels = ModelTyped.Structure.FindAll<CLEMRuminantActivityBase>().Select(a => a.GetType()).ToList();
        return suitableActivities.Any(a => allModels.Contains(a));
    }

}
