using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersGrowPFCGSummary : RuminantParametersSummaryBase<RuminantParametersGrowPFCG>
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
