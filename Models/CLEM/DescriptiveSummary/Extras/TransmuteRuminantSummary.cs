using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Extras
{
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
                if (ModelTyped.AmountPerPacket > 0)
                {
                    htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ModelTyped.AmountPerPacket)} {directexchangeStyleText} ");
                }
                else
                {
                    htmlWriter.Write($"<span class=\"errorlink\">Not set</span> {directexchangeStyleText} ");
                }
            }

            IModel ruminants = ModelTyped.Structure.FindParent<ResourcesHolder>(recurse: true).FindResourceGroup<RuminantHerd>();
            if (ruminants is null)
            {
                htmlWriter.Write("<span class=\"errorlink\">Herd not found</span>");
            }
            else
            {
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ruminants.Name, entryStyle: HTMLSummaryStyle.Resource)}");
            }

            htmlWriter.Write($" (B) are taken from the following groups to supply shortfall resource (A) ");

            if (ModelTyped.TransmuteStyle == TransmuteStyle.UsePricing)
            {
                htmlWriter.Write($" using the herd pricing details");
                if (ModelTyped.FinanceTypeForTransactionsName != null && ModelTyped.FinanceTypeForTransactionsName != "")
                {
                    htmlWriter.Write($" with all financial Transactions of sales and purchases using {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.FinanceTypeForTransactionsName, entryStyle: HTMLSummaryStyle.Resource)}");
                }
            }
            generator.AddBlockWithText("activityentry", htmlWriter.ToString());
        }
    }
}
