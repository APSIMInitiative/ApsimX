using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Requirement Simple
/// </summary>
public class LabourRequirementSimpleSummary : DescriptiveSummaryProviderBase<LabourRequirementSimple>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(LabourGroup),
                introduction: "The required labour will be taken from the following groups:",
                missing: "No labour groups provided to defined labour",
                borderClass: "childgroupborder filtergroup"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("Labour details defined by parent activity");
    }
}
