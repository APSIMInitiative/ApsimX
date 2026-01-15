using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

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
        switch (model.TransactionStyle)
        {
            case RuminantTransactionsGroupingStyle.Combined:
                text += " into a single transaction per RuminantType.";
                break;
            case RuminantTransactionsGroupingStyle.ByPriceGroup:
                text += " by the pricing groups provided for the RuminantType.";
                break;
            case RuminantTransactionsGroupingStyle.ByClass:
                text += " by the class of individuals.";
                break;
            case RuminantTransactionsGroupingStyle.BySexAndClass:
                text += " by the sex and class of individuals.";
                break;
            case RuminantTransactionsGroupingStyle.ByFullClass:
                text += " by the full class of individuals.";
                break;
            case RuminantTransactionsGroupingStyle.BySexAndFullClass:
                text += " by the sex and full class of individuals.";
                break;
            default:
                text += " by [Unknown grouping style]";
                break;
        }
        Generator.AddBlockWithText("activityentry", text);
    }
}