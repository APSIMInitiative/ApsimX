using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for APSIM Clock
/// </summary>
public class ClockSummary : DescriptiveSummaryProviderBase<Clock>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        string missing = "";
        if (ModelTyped.Node.Find<ZoneCLEM>() is not null)
        {
            missing = $"No CLEMEvents component found!";
        }

        return
        [
            new ChildComponentGroup(
                id: "events",
                models: ModelTyped.Node.FindChildren<CLEMEvents>(),
                childType: typeof(CLEMEvents),
                borderClass: "childgroupborder", 
                missing: missing
                ),
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"The simulation is performed from {generator.DisplaySummaryValueSnippet(ModelTyped.StartDate.ToShortDateString())} to {generator.DisplaySummaryValueSnippet(ModelTyped.EndDate.ToShortDateString())}");
    }
}
