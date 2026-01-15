using Models.CLEM.Interfaces;
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
public class TransmuteSummary : DescriptiveSummaryProviderBase<Transmute>
{
    ///<inheritdoc/>
    public override string GetSummaryNameTypeHeaderText()
    {
        return GenerateTransmuteHeaderText(ModelTyped);
    }

    /// <summary>
    /// Method to generate a transmute header text
    /// </summary>
    /// <param name="model">Transmute model to use</param>
    /// <returns></returns>
    public static string GenerateTransmuteHeaderText(ITransmute model)
    {
        using StringWriter htmlWriter = new();
        htmlWriter.WriteLine((model as IModel).Name);
        if (model.TransmuteStyle == TransmuteStyle.Direct)
        {
            htmlWriter.WriteLine(": B&#8594;A");
        }
        else
        {
            if (model.FinanceTypeForTransactionsName != null && model.FinanceTypeForTransactionsName != "")
            {
                htmlWriter.WriteLine(": B&#8594;$ $&#8594;A");
            }
            else
            {
                htmlWriter.WriteLine(": B&#8594;$&#8594;A");
            }
        }
        return htmlWriter.ToString();
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        if (ModelTyped.TransmuteStyle == TransmuteStyle.Direct && ModelTyped.AmountPerPacket > 0)
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.AmountPerPacket)} x ");
        }

        if (ModelTyped.TransmuteResourceTypeName != null && ModelTyped.TransmuteResourceTypeName != "")
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.TransmuteResourceTypeName, entryStyle: HTMLSummaryStyle.Resource)}");
        }
        else
        {
            htmlWriter.Write($"{generator.DisplayErrorSnippet("No Transmute resource (B) set")}");
        }

        if (ModelTyped.TransmuteStyle == TransmuteStyle.UsePricing)
        {
            htmlWriter.Write($" using the resource pricing details");
            if (ModelTyped.FinanceTypeForTransactionsName != null && ModelTyped.FinanceTypeForTransactionsName != "")
            {
                htmlWriter.Write($" and all financial Transactions of sales and purchases using {generator.DisplaySummaryValueSnippet(ModelTyped.FinanceTypeForTransactionsName, entryStyle: HTMLSummaryStyle.Resource)}");
            }
        }
        generator.AddBlockWithText("activityentry", htmlWriter.ToString());
    }

}
