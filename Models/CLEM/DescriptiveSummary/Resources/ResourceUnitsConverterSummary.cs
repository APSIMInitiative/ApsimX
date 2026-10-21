using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Resource Units Converter (sub-resource)
/// </summary>
public class ResourceUnitsConverterSummary : DescriptiveSummaryProviderBase<ResourceUnitsConverter>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ResourceUnitsConverterSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        string name = "";
        if (!Model.Name.Contains(GetType().Name.Split('.').Last()))
        {
            name = Model.Name;
        }
        generator.AddBlockWithText(name, "childTitle resource", disabled: !Model.Enabled);
        generator.OpenBlock("childgroupborder resourcegroup clearfix", "", id: "unitDetails");
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        generator.CloseMostRecentBlock("unitDetails");
    }


    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"{generator.DisplaySummaryValueSnippet((ModelTyped.Parent as IResourceType).Units, errorString:"Parent Units Not Set")} " +
            $"{generator.DisplaySummaryResourceTypeSnippet(ModelTyped.Parent.Name)} = {generator.DisplaySummaryValueSnippet(ModelTyped.Factor, warnZero: true, errorString:"Factor Not Set")} " +
            $"{generator.DisplaySummaryValueSnippet(ModelTyped.Units, errorString:"New Units Not Set")}");
    }
}