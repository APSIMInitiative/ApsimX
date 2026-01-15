using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for AnimalPriceGroup
/// </summary>
public class FilterByAttributeSummary : FilterSummaryBase<FilterByAttribute>
{
    /// <inheritdoc/>
    public override string FilterString(bool htmltags)
    {
        var model = ModelTyped;
        if (model is null) return "UNKNOWN";

        using StringWriter filterWriter = new();
        filterWriter.Write($"Filter:");
        bool trueFalse = model.IsOperatorTrueFalseTest();
        if (model.FilterStyle == AttributeFilterStyle.Exists | trueFalse)
        {
            bool nothingAdded = true;
            if (trueFalse)
            {
                if (model.Operator == ExpressionType.IsFalse | model.Value?.ToString().ToLower() == "false")
                {
                    filterWriter.Write(" does not have");
                    nothingAdded = false;
                }
            }

            if (nothingAdded)
            {
                filterWriter.Write(" has");
            }

            filterWriter.Write($" attribute {generator.DisplaySummaryValueSnippet(model.AttributeTag, "No tag", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
        }
        else
        {
            filterWriter.Write($" Attribute {generator.DisplaySummaryValueSnippet(model.AttributeTag, "No tag", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
            filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.OperatorToSymbol(), "Unknown operator", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
            filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Value?.ToString(), "No value", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
        }
        return filterWriter.ToString();
    }



}
