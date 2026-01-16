using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Activity Pay Hired
/// </summary>
public class LabourActivityPayHiredSummary : DescriptiveSummaryProviderBase<LabourActivityOffFarm>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", $"Pay all hired labour based on associated Fee components");
    }
}
