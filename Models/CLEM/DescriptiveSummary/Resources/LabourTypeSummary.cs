using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Resources;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for LabourType (sub-resource)
/// </summary>
public class LabourTypeSummary : DescriptiveSummaryProviderBase<LabourType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        if (FormatForParentControl)
        {
            generator.AddTableRow(new List<(string, bool)>()
            {
                (model.Name, false),
                (generator.DisplaySummaryValueSnippet(model.Sex.ToString()), false),
                (generator.DisplaySummaryValueSnippet(model.InitialAge, warnZero:true), false),
                (generator.DisplaySummaryValueSnippet(model.Individuals, warnZero:true), false),
                ("", model.IsHired)
            }, model.Enabled);
        }
        else
        {
            if (model.Individuals == 0)
            {
                generator.AddBlockWithText("No individuals are specified for this labour type", "infoBanner error");
            }
            else
            {
                string number = $"{generator.DisplaySummaryValueSnippet(model.Individuals, warnZero: true)} x {generator.DisplaySummaryValueSnippet(model.InitialAge, warnZero: true)} year old {generator.DisplaySummaryValueSnippet(model.Sex)}";
                if (model.IsHired)
                {
                    number += " as hired labour";
                }
                generator.AddBlockWithText(number);
            }

            if (model.Individuals > 1)
            {
                generator.AddBlockWithText($"You will be unable to identify these individuals with {generator.DisplaySummaryValueSnippet("Name")} but need to use the Attribute with tag {generator.DisplaySummaryValueSnippet("Group")} and value {generator.DisplaySummaryValueSnippet(model.Name)}", "infoBanner warning");
            }
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryOpeningBlocks();
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryClosingBlocks();
    }

}