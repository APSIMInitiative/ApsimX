using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for ProductStoreTypeManure (sub-resource)
    /// </summary>
    public class ProductStoreTypeManureSummary : DescriptiveSummaryProviderBase<ProductStoreTypeManure>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            Generator.AddBlockWithText("activityentry", $"Manure will decay at a rate of {CLEMModel.DisplaySummaryValueSnippet(model.DecayRate)} each month and will only last for {CLEMModel.DisplaySummaryValueSnippet(model.MaximumAge.InDays, warnZero: true)} days.");
            Generator.AddBlockWithText("activityentry", $"Fresh manure is {CLEMModel.DisplaySummaryValueSnippet(model.ProportionMoistureFresh)} moisture and declines by {CLEMModel.DisplaySummaryValueSnippet(model.MoistureDecayRate, warnZero: true)} each month.");
        }
    }
}