using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.EMMA;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.DCAPST;
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
        if (ModelTyped.DisplaySummaryParameters == false)
        {
            generator.AddBlockWithText("Summary parameter display is turned OFF!", "infoBanner warning");
            return;
        }

        List<(string componentName, string propertyName, string category, string description, string value)> parameters = [];
        foreach (var item in GetChildrenInSummary()[0].SelectedModels)
        {
            var provider = DescriptiveSummaryResolver.GetProviderInstance(item, generator);
            if (provider is null)
                continue;
            if (provider is not IRuminantParameterSummaryProvider)
                continue;
            var results = (provider as IRuminantParameterSummaryProvider).GetSummaryParameters();
            if (results is not null)
                parameters.AddRange(results);
        }

        if (parameters.Count == 0)
        {
            generator.AddBlockWithText("No summary ruminant parameters to display", "infoBanner error");
        }
        else
        {
            generator.AddBlockWithText("A summary of important ruminant parameter settings from parameters supplied", "childgrouplabel");

            foreach (var param in parameters.GroupBy(a => a.category).OrderBy(a => a.Key != "General"))
            {
                using (generator.OpenBlock("childgroupborder"))
                {
                    generator.AddBlockWithText($"Parameters related to {param.Key}", "detailsnote");
                    foreach (var item in param)
                    {
                        generator.AddSummaryParameterSnippet(item.componentName, $"{item.description} {item.value}");
                    }
                }
            }
        }
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
                missing: "",
                include: false
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
