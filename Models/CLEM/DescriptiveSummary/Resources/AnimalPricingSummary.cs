using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for AnimalPricing
/// </summary>
public class AnimalPricingSummary : DescriptiveSummaryProviderBase<AnimalPricing>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public AnimalPricingSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "prices",
                model: CLEMModel,
                childType: typeof(AnimalPriceGroup),
                missing: "default",
                introduction: "The following Animal Price Groups are applied in the order provided to determine the purchase and sale price of any individual."
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        if (group.Id == "prices" && group.SelectedModels.Any())
        {
            generator.OpenBlock(id: "tablewrap", addTopBottomMargin: true);
            generator.CreateTable(new string[] { "Name", "Filter", "Type", "Value", "Style" });
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id == "prices" && group.SelectedModels.Any())
        {
            generator.CloseTable();
            generator.CloseMostRecentBlock(id: "tablewrap");
        }
    }
}