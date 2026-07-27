using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Resource Activity Manage External
/// </summary>  
public class ResourceActivityManageExternalSummary : DescriptiveSummaryProviderBase<ResourceActivityManageExternal>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(ResourceActivityExternalMultiplier),
                introduction: "The following multipliers will be applied:",
                borderClass: "childgroupborder activitygroup"
                )
        ];
    }


    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Resources added or removed are provided by {generator.DisplaySummaryValueSnippet(ModelTyped.ResourceDataReader, "Reader not set", HTMLSummaryStyle.FileReader)}");

        string output = "";
        if (ModelTyped.AccountName is null || ModelTyped.AccountName == "")
            output = $"Financial transactions will be made to {generator.DisplayErrorSnippet("FinanceType not set")}";
        else if (ModelTyped.AccountName == "No financial implications")
            output = "No financial constraints relating to pricing and packet sizes associated with each resource will be included.";
        else
            output = $"Pricing and packet sizes associated with each resource will be used with {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.AccountName)}";
        generator.AddBlockWithText( output);

        //generator.AddBlockWithText(output);

        using (generator.OpenBlock("childgroupborder filtergroup  clearfix", "", id: "includedresources"))
        {
            generator.AddBlockWithText("The following resources will be included if present in the Resource File", "childgrouplabel");
            var resourceFilter = ((ModelTyped.ResourceColumnsToUse ?? "").Length > 0) ? ModelTyped.ResourceColumnsToUse : "All resources";
            foreach (var res in resourceFilter.Split(",").Select(x => x.Trim()))
                generator.AddBlockWithText(res, "entryValue filterItem floatLeft");
        }
    }
}
