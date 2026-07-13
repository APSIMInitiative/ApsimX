using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for the main CLEM component
/// </summary>
public class ZoneCLEMSummary : DescriptiveSummaryProviderBase<ZoneCLEM>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ZoneCLEMSummary()
    {
        WrapChildren = false;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "events",
                models: ModelTyped.Structure.FindAll<Clock>(),
                childType: typeof(Clock),
                missing: ""
                ),
            new ChildComponentGroup(
                id: "random",
                models: ModelTyped.Structure.FindAll<RandomNumberGenerator>(),
                childType: typeof(RandomNumberGenerator),
                missing: ""
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {

        generator.AddBlockWithText($"This farm is identified as region {generator.DisplaySummaryValueSnippet(ModelTyped.ClimateRegion)}");
        ResourcesHolder resources = Model.Node.FindChild<ResourcesHolder>();
        if (resources != null)
        {
            if (resources.FoundMarket != null)
            {
                generator.AddBlockWithText($"This farm represents {generator.DisplaySummaryValueSnippet(ModelTyped.FarmMultiplier, warnZero: true)} farm(s) when trading with the Market");
            }
        }
    }
}
