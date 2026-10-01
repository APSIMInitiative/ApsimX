using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Pasture Activity Manage
/// </summary>
public class PastureActivityManageSummary : DescriptiveSummaryProviderBase<PastureActivityManage>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(Relationship),
                introduction: "Relationships for change in land condition and grass basal area as function of utilisation:",
                borderClass: "childgroupborder activitygroup"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.FeedTypeName, "Pasture not set", HTMLSummaryStyle.Resource));
        htmlWriter.Write(" occupies ");
        Land parentLand = null;
        if (ModelTyped.LandTypeNameToUse != null && ModelTyped.LandTypeNameToUse != "")
        {
            parentLand = ModelTyped.Structure.Find<Land>(ModelTyped.LandTypeNameToUse.Split('.')[0]);
        }

        if (ModelTyped.UseAreaAvailable)
        {
            htmlWriter.Write("the unallocated portion of ");
        }
        else
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.AreaRequested, errorNotSet: true)} {generator.DisplaySummaryValueSnippet((parentLand?.UnitsOfArea), errorString: "Unknown parent units", errorNotSet: true)} of ");
        }
        htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.LandTypeNameToUse, "Land not set", HTMLSummaryStyle.Resource));
    }
}
