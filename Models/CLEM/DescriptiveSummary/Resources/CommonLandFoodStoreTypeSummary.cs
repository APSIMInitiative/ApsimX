using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the CommonLandFoodStoreType Resource
    /// </summary>
    public class CommonLandFoodStoreTypeSummary : DescriptiveSummaryProviderBase<CommonLandFoodStoreType>
    {
        /// <inheritdoc/>
        public override void BuildSummary(CommonLandFoodStoreType model)
        {

            if (model.Parent.GetType() == typeof(AnimalFoodStore))
            {
                Generator.AddBlockWithText("activityentry", $"This common land can be used by animal feed activities only");
            }
            else
            {
                Generator.AddBlockWithText("activityentry", $"This common land can be used by grazing and cut and carry activities");
            }

            if (model.PastureLink != null)
            {
                Generator.AddBlockWithText("activityentry", $"The quality of this common land is based on {CLEMModel.DisplaySummaryResourceTypeSnippet(model.PastureLink)} with a further nitrogen reduction of {CLEMModel.DisplaySummaryValueSnippet(model.NitrogenReductionFromPasture)} percent.");
            }
            else
            {
                Generator.AddBlockWithText("activityentry", $"The percent nitrogen of new pasture is {CLEMModel.DisplaySummaryValueSnippet(model.Nitrogen, warnZero:true)} and can be reduced to {CLEMModel.DisplaySummaryValueSnippet(model.MinimumNitrogen)}");
                Generator.AddBlockWithText("activityentry", $"The minimum Dry Matter Digestaibility (%) is {CLEMModel.DisplaySummaryValueSnippet(model.MinimumDMD)} and is estimated from N%");
            }
        }
    }
}
