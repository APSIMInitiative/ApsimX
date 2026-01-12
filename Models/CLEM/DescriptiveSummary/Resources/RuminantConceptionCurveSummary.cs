using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantConceptionCurve
    /// </summary>
    public class RuminantConceptionCurveSummary : DescriptiveSummaryProviderBase<RuminantConceptionCurve>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            Generator.AddBlockWithText("activityentry", $"Conception rates are being calculated for all breeding females using the same curve.");
            Generator.AddBlockWithText("activityentry", $"Conception rate coefficient = {CLEMModel.DisplaySummaryValueSnippet(model.ConceptionRateCoefficent)}");
            Generator.AddBlockWithText("activityentry", $"Conception rate intercept = {CLEMModel.DisplaySummaryValueSnippet(model.ConceptionRateIntercept)}");
            Generator.AddBlockWithText("activityentry", $"Conception rate asymptote = {CLEMModel.DisplaySummaryValueSnippet(model.ConceptionRateAsymptote)}");
        }
    }
}