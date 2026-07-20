using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System.Collections.Generic;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Tag
    /// </summary>
    public class RuminantActivityTagSummary : RuminantActivitySummaryBase<RuminantActivityTag>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            string intro = $"{generator.DisplaySummaryValueSnippet(ModelTyped.ApplicationStyle)} the tag {((ModelTyped.ApplicationStyle == TagApplicationStyle.Add) ? "to" : "from")} all individuals in the following groups";

            return
            [
                new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(RuminantGroup),
                missing: "No individuals identified",
                introduction: intro,
                borderClass: "childgroupborder filtergroup"
                )
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            string tag = $"The tag {generator.DisplaySummaryValueSnippet(ModelTyped.TagLabel, "Tag not set")}";

            if (ModelTyped.ApplicationStyle == TagApplicationStyle.Add && ModelTyped.TagCategory != RuminantAttributeCategoryTypes.None)
            {
                tag += $", is of category {generator.DisplaySummaryValueSnippet(ModelTyped.TagCategory)}";
            }
            generator.AddBlockWithText($"{tag}");
        }
    }
}