using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Finances Resource
/// </summary>
public class FinanceSummary : DescriptiveSummaryProviderBase<Finance>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "defaulttypes",
                model: CLEMModel,
                childType: typeof(FinanceType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        Generator.AddBlockWithText($"Currency is {generator.DisplaySummaryValueSnippet(model.CurrencyName, "Not specified")}");
        Generator.AddBlockWithText($"The financial year starts in {generator.DisplaySummaryValueSnippet(model.FirstMonthOfFinancialYear, "Not specified", warnZero: true)}");
    }
}
