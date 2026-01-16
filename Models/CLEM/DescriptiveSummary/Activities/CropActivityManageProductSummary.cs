using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Crop Activity Manage Product
/// </summary>
public class CropActivityManageProductSummary : DescriptiveSummaryProviderBase<CropActivityManageProduct>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        string intro = (ModelTyped.Structure.FindChildren<CropActivityManageProduct>().Count() >= 1) ? "Mixed crop" : "";
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(CropActivityManageProduct),
                missing: "",
                introduction: intro,
                borderClass: "childgrouprotationborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        if (ModelTyped.TreesPerHa > 0)
            generator.AddBlockWithText("activityentry", $"This is a tree crop with a density of {generator.DisplaySummaryValueSnippet(ModelTyped.TreesPerHa)} per hectare");

        generator.AddBlockWithText("activityentry", $"{((ModelTyped.ProportionKept == 1) ? "This " : $"{generator.DisplaySummaryValueSnippet(ModelTyped.ProportionKept, warnZero: true)} of this product is placed in {generator.DisplaySummaryValueSnippet(ModelTyped.StoreItemName, "Resource not set", HTMLSummaryStyle.Resource)}")}");
        generator.AddBlockWithText("activityentry", $"Data is retrieved from {generator.DisplaySummaryValueSnippet(ModelTyped.ModelNameFileCrop, "Resource not set", HTMLSummaryStyle.FileReader)} ");
    }
}
