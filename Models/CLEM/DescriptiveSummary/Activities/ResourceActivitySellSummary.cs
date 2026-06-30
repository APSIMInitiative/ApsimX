using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Resource Activity Process
/// </summary>
public class ResourceActivitySellSummary : DescriptiveSummaryProviderBase<ResourceActivitySell>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        string saleAmount = "";
        string salestyle = "";
        switch (ModelTyped.SellStyle)
        {
            case ResourceSellStyle.SpecifiedAmount:
                saleAmount = $"{ModelTyped.Value:#,##0}";
                salestyle = " of ";   
                break;
            case ResourceSellStyle.ProportionOfStore:
                saleAmount = $"{ModelTyped.Value:#0%}";
                salestyle = " percent of ";
                break;
            case ResourceSellStyle.ProportionOfLastGain:
                saleAmount = $"{ModelTyped.Value:#0%}";
                salestyle = " percent of the last gain transaction recorded for ";
                break;
            case ResourceSellStyle.ReserveAmount:
                saleAmount = $"{ModelTyped.Value:#,##0}";
                salestyle = " as reserve of ";
                break;
            case ResourceSellStyle.ReserveProportion:
                saleAmount = $"{ModelTyped.Value:#0%}";
                salestyle = " percent of store as reserve of ";
                break;
            default:
                break;
        }

        string account = "";
        if (ModelTyped.AccountName != "No finance required")
        {
            account = $" with sales placed in {generator.DisplaySummaryValueSnippet(ModelTyped.AccountName, "Account not set", HTMLSummaryStyle.Resource)}";
        }

        generator.AddBlockWithText($"Sell {saleAmount}{salestyle} {generator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource)}{account}");
    }

}
