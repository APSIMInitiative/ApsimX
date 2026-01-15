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
    /// <inheritdoc/>
    public override void BuildSummary()
    {

        generator.AddBlockWithText("activityentry", $"This farm is identified as region {generator.DisplaySummaryValueSnippet(ModelTyped.ClimateRegion)}");
        ResourcesHolder resources = Model.Node.FindChild<ResourcesHolder>();
        if (resources != null)
        {
            if (resources.FoundMarket != null)
            {
                generator.AddBlockWithText("activityentry", $"This farm represents {generator.DisplaySummaryValueSnippet(ModelTyped.FarmMultiplier, warnZero: true)} farm(s) when trading with the Market");
            }
        }
    }
}
