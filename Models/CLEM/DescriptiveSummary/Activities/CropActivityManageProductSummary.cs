using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Crop Activity Manage Product
/// </summary>
public class CropActivityManageProductSummary : DescriptiveSummaryProviderBase<CropActivityManageProduct>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        string intro = ModelTyped.Structure.FindChildren<CropActivityManageProduct>().Any() ? "Mixed crop" : "";
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
            generator.AddBlockWithText($"This is a tree crop with a density of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.TreesPerHa)} per hectare");

        generator.AddBlockWithText($"{((ModelTyped.ProportionKept == 1) ? "This " : $"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ProportionKept, warnZero: true)} of this product is placed in {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.StoreItemName, "Resource not set", HTMLSummaryStyle.Resource)}")}");
        generator.AddBlockWithText($"Data is retrieved from {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ModelNameFileCrop, "Resource not set", HTMLSummaryStyle.FileReader)} ");
    }
}
