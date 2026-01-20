using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Move
    /// </summary>
    public class RuminantActivityMoveSummary : RuminantActivitySummaryBase<RuminantActivityMove>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            string sucklings = (model.MoveSucklings)?"":", moving sucklings with mothers";

            generator.AddBlockWithText("activityentry",
                $"Move individuals to {generator.DisplaySummaryValueSnippet(model.ManagedPastureName, "Not specified - general yards", HTMLSummaryStyle.Resource)}{sucklings}.");

            string timing = "";
            switch (ModelTyped.TimeStepTiming)
            {
                case WithinTimeStepTimingStyle.Early:
                    timing = "This will occur at the start of the time step ensuring they are present for all activities.";
                    break;
                case WithinTimeStepTimingStyle.Normal:
                    timing = "This will occur during the time step during GetResourcesRequired therefore competing with other activities.";
                    break;
                case WithinTimeStepTimingStyle.Late:
                    timing = "This will occur at the end of the time step after all sales prior to aging.";
                    break;
                default:
                    break;
            }

            generator.AddBlockWithText("activityentry", timing);

            if (ModelTyped.PerformAtStartOfSimulation)
                generator.AddBlockWithText("activityentry",
                $"This activity will occur at the start of the simulation.");
        }
    }
}