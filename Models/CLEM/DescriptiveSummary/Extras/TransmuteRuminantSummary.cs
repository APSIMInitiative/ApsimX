using Models.CLEM.Resources;
using Models.Core;
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
public class TransmuteRuminantSummary : DescriptiveSummaryProviderBase<TransmuteRuminant>
{
    ///<inheritdoc/>
    public override string GetSummaryNameTypeHeaderText()
    {
        return TransmuteSummary.GenerateTransmuteHeaderText(ModelTyped);
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        if (ModelTyped.TransmuteStyle == TransmuteStyle.Direct)
        {
            string directexchangeStyleText = "";
            switch (ModelTyped.DirectExchangeStyle)
            {
                case PricingStyleType.perHead:
                    directexchangeStyleText = "head of ";
                    break;
                case PricingStyleType.perKg:
                    directexchangeStyleText = "kg live weight head of ";
                    break;
                case PricingStyleType.perAE:
                    directexchangeStyleText = "animal equivalents of ";
                    break;
                default:
                    break;
            }
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.AmountPerPacket, errorNotSet: true)} {directexchangeStyleText} ");
        }

        IModel ruminants = ModelTyped.Structure.FindParent<ResourcesHolder>(recurse: true).FindResourceGroup<RuminantHerd>();
        if (ruminants is null)
        {
            htmlWriter.Write($"{generator.DisplayErrorSnippet("Herd not found")}");
        }
        else
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ruminants.Name, entryStyle: HTMLSummaryStyle.Resource)}");
        }

        htmlWriter.Write($" (B) are taken from the following groups to supply shortfall resource (A) ");

        if (ModelTyped.TransmuteStyle == TransmuteStyle.UsePricing)
        {
            htmlWriter.Write($" using the herd pricing details");
            if (ModelTyped.FinanceTypeForTransactionsName != null && ModelTyped.FinanceTypeForTransactionsName != "")
            {
                htmlWriter.Write($" with all financial Transactions of sales and purchases using {generator.DisplaySummaryValueSnippet(ModelTyped.FinanceTypeForTransactionsName, entryStyle: HTMLSummaryStyle.Resource)}");
            }
        }
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());
    }
}
