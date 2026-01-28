using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

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

        Generator.AddBlockWithText($"Conception rates are being calculated for all breeding females using the same curve.");
        Generator.AddBlockWithText($"Conception rate coefficient = {generator.DisplaySummaryValueSnippet(model.ConceptionRateCoefficent)}");
        Generator.AddBlockWithText($"Conception rate intercept = {generator.DisplaySummaryValueSnippet(model.ConceptionRateIntercept)}");
        Generator.AddBlockWithText($"Conception rate asymptote = {generator.DisplaySummaryValueSnippet(model.ConceptionRateAsymptote)}");
    }
}