using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for transmutation
/// </summary>
public class TransmutationSummary: DescriptiveSummaryProviderBase<Transmutation>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public TransmutationSummary()
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
                id: "default",
                model: CLEMModel,
                childType: typeof(ITransmute),
                missing: ""
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();

        var pricing = ModelTyped.Structure.FindChildren<ITransmute>().Where(a => a.TransmuteStyle == TransmuteStyle.UsePricing);
        var direct = ModelTyped.Structure.FindChildren<ITransmute>().Where(a => a.TransmuteStyle == TransmuteStyle.Direct);

        htmlWriter.Write($"The following resources (B) will transmute ");
        if (pricing.Any())
        {
            htmlWriter.Write($"using the resource purchase price ");
            var transmuteResourcePrice = ((ModelTyped.Structure.FindParent<ResourcesHolder>(recurse: true)).FindResourceType<ResourceBaseWithTransactions, IResourceType>(ModelTyped, ModelTyped.ResourceInShortfall, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore))?.Price(PurchaseOrSalePricingStyleType.Purchase);
            if (transmuteResourcePrice != null)
            {
                htmlWriter.Write("found");
            }
            else
            {
                htmlWriter.Write($"{generator.DisplayErrorSnippet("not found")}");
            }
        }
        htmlWriter.WriteLine(" to provide this shortfall resource (A)");


        if (direct.Any())
        {
            htmlWriter.Write($" in {(ModelTyped.UseWholePackets ? " whole" : "")} packets of {generator.DisplaySummaryValueSnippet(ModelTyped.TransmutationPacketSize, warnZero: true)}");
        }

        if (pricing.Count() + direct.Count() > 1)
        {
            htmlWriter.Write($" (or the largest packet size needed the individual transmutes)");
        }

        generator.AddBlockWithText(htmlWriter.ToString());
    }

}
