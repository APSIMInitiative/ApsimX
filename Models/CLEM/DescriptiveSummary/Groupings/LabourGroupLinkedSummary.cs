using BruTile.Wms;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Groupings;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Labour Group linked filter
/// </summary>
public class LabourGroupLinkedSummary : GroupSummaryBase<LabourGroupLinked>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public LabourGroupLinkedSummary()
    {
        SummaryStyle = HTMLSummaryStyle.Filter;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var zone = ModelTyped.Structure.FindParent<Zone>(recurse: true);
        var foundGroup = ModelTyped.Structure.FindChildren<RuminantGroup>(relativeTo: zone, recurse: true).Where(a => a.Enabled).Cast<Model>().Where(a => $"{a.Parent.Name}.{a.Name}" == ModelTyped.ExistingGroupName).FirstOrDefault() as RuminantGroup;
       
        ChildComponentGroup childC = new ChildComponentGroup()
        {
            Id = "linked",
            Missing = $"Linked LabourGroup {generator.DisplaySummaryValueSnippet(ModelTyped.ExistingGroupName, errorNotSet: true, errorString: "Not specified")} not found",
            SelectedModels = new List<IModel>(),
            Introduction = "Linked to:"
        };
        
        if (foundGroup is not null)
        {
            childC.SelectedModels = new List<IModel>() { foundGroup };
        }
        return [childC];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        generator.OpenBlock("childgroupborder filteritems clearfix", "", id: "groupitems");
    }

}
