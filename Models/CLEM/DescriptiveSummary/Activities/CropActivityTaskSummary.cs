using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Crop Activity Task
/// </summary>
public class CropActivityTaskSummary : DescriptiveSummaryProviderBase<CropActivityTask>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        if (ModelTyped.Structure.FindChildren<ActivityFee>().Count() + ModelTyped.Structure.FindChildren<LabourRequirement>().Count() == 0)
            generator.AddBlockWithText("This task is not needed as it has no fee or labour requirement", "infoBanner warning");
    }
}
