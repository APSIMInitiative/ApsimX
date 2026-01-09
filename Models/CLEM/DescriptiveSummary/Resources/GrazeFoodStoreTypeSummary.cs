using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the GrazeFoodStore Resource
    /// </summary>
    public class GrazeFoodStoreTypeSummary : DescriptiveSummaryProviderBase<GrazeFoodStoreType>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            Generator.AddBlockWithText("activityentry", $"This pasture has an initial green nitrogen content of {CLEMModel.DisplaySummaryValueSnippet(model.GreenNitrogenPercent, warnZero:true)}");
            Generator.AddBlockWithText("activityentry", $"Percent nitrogen decline by {CLEMModel.DisplaySummaryValueSnippet(model.DecayNitrogen, warnZero: true)} per month to a minimum of {CLEMModel.DisplaySummaryValueSnippet(model.MinimumNitrogen)}");
            Generator.AddBlockWithText("activityentry", $"DMD (%) will decay at a rate of {CLEMModel.DisplaySummaryValueSnippet(model.DecayDMD, warnZero: true)} per month to a minimum of {CLEMModel.DisplaySummaryValueSnippet(model.MinimumDMD)}");

            if (model.DetachRate > 0)
            {
                string detachText = $"Pasture is lost through detachment at a rate of {CLEMModel.DisplaySummaryValueSnippet(model.DetachRate)} per month";

                if (model.CarryoverDetachRate > 0)
                    detachText += $" and {CLEMModel.DisplaySummaryValueSnippet(model.CarryoverDetachRate)} per month after 12 months";

                Generator.AddBlockWithText("activityentry", detachText);
            }
            else
            {
                if (model.CarryoverDetachRate > 0)
                {
                    Generator.AddBlockWithText("activityentry", $"Pasture is lost through detachment at a rate of {CLEMModel.DisplaySummaryValueSnippet(model.CarryoverDetachRate)} per month after 12 months");
                }
            }

        }
    }
}
