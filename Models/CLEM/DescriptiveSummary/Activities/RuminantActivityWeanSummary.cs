using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Wean
    /// </summary>
    public class RuminantActivityWeanSummary : RuminantActivitySummaryBase<RuminantActivityWean>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("Individuals are weaned at ");
            if (ModelTyped.Style == WeaningStyle.AgeOrWeight | ModelTyped.Style == WeaningStyle.AgeOnly)
            {
                htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.WeaningAge.InDays, errorNotSet: true, warnZero: true)} days");
                if (ModelTyped.Style == WeaningStyle.AgeOrWeight)
                {
                    htmlWriter.Write(" or  ");
                }
            }
            if (ModelTyped.Style == WeaningStyle.AgeOrWeight | ModelTyped.Style == WeaningStyle.WeightOnly)
            {
                htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.WeaningWeight, errorNotSet: true, warnZero: true)} kg");
            }
            generator.AddBlockWithText("activityentry", htmlWriter.ToString());
            htmlWriter.GetStringBuilder().Clear();

            htmlWriter.Write("Weaned individuals will ");
            if (ModelTyped.GrazeFoodStoreName == "Leave at current location")
            {
                htmlWriter.Write("remain at the location they were weaned");
            }
            else
            {
                htmlWriter.Write($"be place in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreName, nullGeneralYards: true)}");
            }
            generator.AddBlockWithText("activityentry", htmlWriter.ToString());
        }
    }
}