using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Ruminant Parameters component descriptive summary
/// </summary>
public class RuminantParametersHolderSummary : DescriptiveSummaryProviderBase<RuminantParametersHolder>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantParametersHolderSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        Generator.AddBlockWithText("detailsnote", "A summary of important ruminant parameter settings from parameters supplied");
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "parameters",
                models: model.Structure.FindChildren<ISubParameters>(recurse: true).Cast<IModel>(),
                childType: typeof(ISubParameters),
                missing: ""
                ),
            new ChildComponentGroup(
                id: "parameters",
                model: CLEMModel,
                childType: typeof(RuminantParametersGrowPF),
                missing: "",
                include: false
                )
        ];
    }
}
