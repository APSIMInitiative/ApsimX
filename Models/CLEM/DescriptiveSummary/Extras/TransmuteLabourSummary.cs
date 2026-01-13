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
    public class TransmuteLabourSummary : DescriptiveSummaryProviderBase<TransmuteLabour>
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
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ModelTyped.AmountPerPacket, warnZero: true)} days labour");
            }

            htmlWriter.Write(" (B) are taken from the following groups to supply shortfall resource (A) ");

            if (ModelTyped.TransmuteStyle == TransmuteStyle.UsePricing)
            {
                htmlWriter.Write($" using the labour pricing details");
                if (ModelTyped.FinanceTypeForTransactionsName != null && ModelTyped.FinanceTypeForTransactionsName != "")
                {
                    htmlWriter.Write($" with all financial Transactions of sales and purchases using {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.FinanceTypeForTransactionsName, entryStyle: HTMLSummaryStyle.Resource)}");
                }
            }
            generator.AddBlockWithText("activityentry", htmlWriter.ToString());
        }

    }
}
