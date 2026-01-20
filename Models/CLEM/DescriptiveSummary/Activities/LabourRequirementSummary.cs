using DocumentFormat.OpenXml.EMMA;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using StdUnits;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Requirement
/// </summary>
public class LabourRequirementSummary : DescriptiveSummaryProviderBase<LabourRequirement>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public LabourRequirementSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubActivity;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(LabourGroup),
                introduction: "The required labour will be taken from the following groups:",
                missing: "No labour groups provided to defined labour",
                borderClass: "childgroupfilterborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ModelTyped.LabourPerUnit, "Rate not set")} {generator.DisplayPlural(((ModelTyped.LabourPerUnit == 1.0) ? 1 : 2), "day", " is", "s are")} required ");

        if ((ModelTyped.Measure ?? "").Equals("fixed", StringComparison.CurrentCultureIgnoreCase) == false)
        {
            switch (ModelTyped)
            {
                case LabourRequirementNoUnitSize _:
                    htmlWriter.Write($"where block size is defined by the parent activity");
                    break;
                case LabourRequirementSimple _:
                    htmlWriter.Write($"where units and block size are defined by the parent activity");
                    break;
                case LabourRequirement _:
                    htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.Measure, "Measure not set", errorNotSet: true)}");
                    htmlWriter.Write($" in {((ModelTyped.WholeUnitBlocks) ? $"{generator.DisplaySummaryValueSnippet("whole")} " : "")}blocks");
                    if (ModelTyped.UnitSize != 1)
                    {
                        htmlWriter.Write($" of {generator.DisplaySummaryValueSnippet(ModelTyped.UnitSize, "Unit size not set", errorNotSet: true)}");
                    }
                    if (ModelTyped.ApplyToAll)
                    {
                        htmlWriter.Write($" applied to each person specified");
                    }
                    break;
            }
        }
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());

        string extraLimit = "";
        string limitedBy = "";
        switch (ModelTyped.LimitStyle)
        {
            case LabourLimitType.AsRatePerUnitsAllowed:
                limitedBy = "rates allowed per unit block";
                extraLimit = " times the number of unit blocks requested (i.e. using the same calculation as LabourPerUnits)";
                break;
            case LabourLimitType.AsTotalDaysAllowed:
                limitedBy = $"the total days allowed per month";
                extraLimit = " days";
                break;
            case LabourLimitType.ProportionOfDaysRequired:
                limitedBy = $"a proportion of the total days required";
                extraLimit = " times the total days required";
                break;
            default:
                break;
        }

        using (generator.OpenBlock("childgroupactivityborder", id: $"{ModelTyped.Name}_labreq"))
        {
            generator.AddBlockWithText("childgrouplabel", $"Labour allocation will be limited using {generator.DisplaySummaryValueSnippet(limitedBy)}");

            if (ModelTyped.MaximumPerGroup > 0)
            {
                generator.AddBlockWithText("activityentry", $"Each {generator.DisplaySummaryValueSnippet("LabourGroup", entryStyle: HTMLSummaryStyle.Filter)} can supply up to {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumPerGroup)}{extraLimit}");
            }

            if (ModelTyped.MinimumPerPerson > 0)
            {
                generator.AddBlockWithText("activityentry", $"No labour is allocated if less than {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumPerPerson)}{extraLimit}");
            }

            if (ModelTyped.MaximumPerPerson > 0 && ModelTyped.MaximumPerPerson < 31)
            {
                generator.AddBlockWithText("activityentry", $"No individual can provide more than {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumPerPerson)}{extraLimit}");
            }
        }

        if (ModelTyped.ApplyToAll)
        {
            generator.AddBlockWithText("activityentry", $"All people matching the below criteria (first level) will perform this task. (e.g. all children)");
        }
    }
}
