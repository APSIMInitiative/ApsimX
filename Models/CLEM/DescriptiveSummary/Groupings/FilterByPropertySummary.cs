using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for AnimalPriceGroup
/// </summary>
public class FilterByPropertySummary : FilterSummaryBase<FilterByProperty>
{
    /// <inheritdoc/>
    public override string FilterString(bool htmltags)
    {
        var model = ModelTyped;
        if (model is null) return "UNKNOWN";

        model.Initialise();

        using StringWriter filterWriter = new();
        if (model.AllPropertyInfoFound.Any() == false)
        {
            filterWriter.Write($"Filter:");
            string errorLink = (htmltags) ? " <span class=\"errorlink\">" : " ";
            string spanClose = (htmltags) ? "</span>" : "";
            string message = (model.PropertyOfIndividual == null || model.PropertyOfIndividual == "") ? "Not Set" : $"Unknown: {model.PropertyOfIndividual}";
            filterWriter.Write($"{errorLink}{message}{spanClose}");
            return filterWriter.ToString();
        }

        filterWriter.Write($"Filter:");
        bool trueFalse = model.IsOperatorTrueFalseTest();
        if (trueFalse | (model.AllPropertyInfoFound != null && model.AllPropertyInfoFound.Last().PropertyType.IsEnum))
        {
            if (model.AllPropertyInfoFound.Last().PropertyType == typeof(bool))
            {
                if (model.Operator == ExpressionType.IsFalse || model.Value?.ToString().ToLower() == "false")
                {
                    filterWriter.Write(" not");
                }

                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
            }
            else
            {
                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                if (model.IsOperatorValid)
                {
                    filterWriter.Write((model.Operator == ExpressionType.IsFalse || model.Value?.ToString().ToLower() == "false") ? " not" : " is");
                }
                else
                {
                    string errorLink = (htmltags) ? "<span class=\"errorlink\">" : "";
                    string spanClose = (htmltags) ? "</span>" : "";
                    filterWriter.Write($"{errorLink}invalid operator {model.OperatorToSymbol()}{spanClose}");
                }
                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
            }
        }
        else
        {
            filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");

            if (model.AllPropertyInfoFound != null)
            {
                if (model.IsOperatorValid)
                {
                    filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                }
                else
                {
                    string errorLink = (htmltags) ? "<span class=\"errorlink\">" : "";
                    string spanClose = (htmltags) ? "</span>" : "";
                    filterWriter.Write($"{errorLink}invalid operator {model.OperatorToSymbol()}{model.AllPropertyInfoFound.Last().PropertyType.Name}{spanClose}");
                }
            }
            else
            {
                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
            }

            filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
        }
        return filterWriter.ToString();
    }



}
