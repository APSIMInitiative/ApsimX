using Models.CLEM.Resources;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Resource Pricing (sub-resource)
/// </summary>
public class ResourcePricingSummary : DescriptiveSummaryProviderBase<ResourcePricing>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ResourcePricingSummary()
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
        generator.OpenBlock("childgroupborder resourcegroup clearfix", "", id: "priceDetails");
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        generator.CloseMostRecentBlock("priceDetails");
    }


    /// <inheritdoc/>
    public override void BuildSummary()
    {
        string output = "";
        switch (ModelTyped.PurchaseOrSale)
        {
            case PurchaseOrSalePricingStyleType.Both:
                output = "purchase and sell";
                break;
            case PurchaseOrSalePricingStyleType.Purchase:
                output = "purchase";
                break;
            case PurchaseOrSalePricingStyleType.Sale:
                output = "sell";
                break;
            default:
                break;
        }

        generator.AddBlockWithText($"This is a {output} price.");
        output = "This resource is managed ";

        if (ModelTyped.UseWholePackets)
            output += "only in whole packets ";
        else
            output += "in packets ";
        output += $"{generator.DisplaySummaryValueSnippet(ModelTyped.PacketSize, warnZero: true)}";

        output += $" unit {((ModelTyped.PacketSize == 1) ? "" : "s")} in size";
        generator.AddBlockWithText(output);

        generator.AddBlockWithText($"Each packet is worth {generator.DisplaySummaryValueSnippet(ModelTyped.PricePerPacket, warnZero: true)}.");
    }
}