using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Crop Activity Manage Crop
/// </summary>
public class CropActivityManageCropSummary : DescriptiveSummaryProviderBase<CropActivityManageCrop>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CropActivityManageCropSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubActivityLevel2;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        string intro = (ModelTyped.Structure.FindChildren<CropActivityManageProduct>().Count() > 1) ? "Rotating through crops" : "";

        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(CropActivityManageProduct),
                missing: "No CropActivityManageProduct component provided",
                introduction: intro,
                borderClass: "childgrouprotationborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write("This crop uses ");

        Land parentLand = null;
        var clemParent = ModelTyped.Structure.FindParent<ZoneCLEM>(relativeTo: ModelTyped, recurse: true);
        if (ModelTyped.LandItemNameToUse != null && ModelTyped.LandItemNameToUse != "")
        {
            if (clemParent != null && clemParent.Enabled)
            {
                parentLand = ModelTyped.Structure.Find<Land>(ModelTyped.LandItemNameToUse.Split('.')[0], relativeTo: clemParent);
            }
        }

        if (ModelTyped.UseAreaAvailable)
        {
            htmlWriter.Write("the unallocated portion of ");
        }
        else
        {
            htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ModelTyped.AreaRequested, errorString: "Not Set")}");

            if (parentLand == null)
            {
                htmlWriter.Write($"{generator.DisplayErrorSnippet("UNITS NOT SET")}");
            }
            else
            {
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(parentLand.UnitsOfArea)}");
            }
        }
        htmlWriter.Write($"of {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.LandItemNameToUse, "Resource not set", HTMLSummaryStyle.Resource)}");
        generator.AddBlockWithText(htmlWriter.ToString());
    }
}
