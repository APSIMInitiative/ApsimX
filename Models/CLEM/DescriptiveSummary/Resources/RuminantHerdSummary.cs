using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for RuminantHerd
/// </summary>
public class RuminantHerdSummary : DescriptiveSummaryProviderBase<RuminantHerd>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "defaulttype",
                model: CLEMModel,
                childType: typeof(RuminantType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string text = "Activities reporting on herds will group individuals";
        text += model.TransactionStyle switch
        {
            RuminantTransactionsGroupingStyle.Combined => " into a single transaction per RuminantType.",
            RuminantTransactionsGroupingStyle.ByPriceGroup => " by the pricing groups provided for the RuminantType.",
            RuminantTransactionsGroupingStyle.ByClass => " by the class of individuals.",
            RuminantTransactionsGroupingStyle.BySexAndClass => " by the sex and class of individuals.",
            RuminantTransactionsGroupingStyle.ByFullClass => " by the full class of individuals.",
            RuminantTransactionsGroupingStyle.BySexAndFullClass => " by the sex and full class of individuals.",
            _ => " by [Unknown grouping style]",
        };
        Generator.AddBlockWithText(text);
    }
}