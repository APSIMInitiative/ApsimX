using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Mapsui.Providers;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour resource
/// </summary>
public class LabourSummary : DescriptiveSummaryProviderBase<Labour>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "defaulttype",
                model: CLEMModel,
                childType: typeof(LabourType),
                missing: "default"
                ),
            new ChildComponentGroup(
                id: "aerelationship",
                models: model.Structure.FindChildren<Relationship>().Where(a => a.Identifier == "Adult equivalent"),
                childType: typeof(Relationship),
                introduction: "The following relationship is used to calculate the Adult Equivalent of each person",
                missing: $"No {CLEMModel.DisplaySummaryValueSnippet("Relationship", entryStyle:HTMLSummaryStyle.Helper)} with the identifier {generator.DisplaySummaryValueSnippet("Adult equivalent")} was provided. All individuals are assumed to be 1 AE."
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        generator.AddBlockWithText("activityentry", $"Individuals {(model.AllowAgeing? "" : "do not")} age with time");
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        if (group.Id == "defaulttype" && group.SelectedModels.Any())
        {
            Generator.CreateTable(new string[] { "Name", "Sex", "Age (yrs)", "Number", "Hired" });
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id == "defaulttype" && group.SelectedModels.Any())
        {
            Generator.CloseTable();
        }
    }
}