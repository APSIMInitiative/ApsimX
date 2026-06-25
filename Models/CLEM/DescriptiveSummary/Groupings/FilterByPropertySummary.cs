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
    public override string FilterString()
    {
        var model = ModelTyped;
        if (model is null) return "UNKNOWN";

        model.Initialise();

        using StringWriter filterWriter = new();
        if (model.AllPropertyInfoFound.Any() == false)
        {
            string message = (model.PropertyOfIndividual == null || model.PropertyOfIndividual == "") ? "Not Set" : $"Unknown: {model.PropertyOfIndividual}";
            filterWriter.Write($"Filter:{generator.DisplayErrorSnippet(message)}");
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

                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter)}");
            }
            else
            {
                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter)}");
                if (model.IsOperatorValid)
                {
                    filterWriter.Write((model.Operator == ExpressionType.IsFalse || model.Value?.ToString().ToLower() == "false") ? " not" : " is");
                }
                else
                {
                    filterWriter.Write($"invalid operator {model.OperatorToSymbol()}");
                }
                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Value?.ToString(), "No value", HTMLSummaryStyle.Filter)}");
            }
        }
        else
        {
            filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter)}");

            if (model.AllPropertyInfoFound != null)
            {
                if (model.IsOperatorValid)
                {
                    filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter)}");
                }
                else
                {
                    filterWriter.Write($"invalid operator {model.OperatorToSymbol()}{model.AllPropertyInfoFound.Last().PropertyType.Name}");
                }
            }
            else
            {
                filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter)}");
            }

            filterWriter.Write($" {generator.DisplaySummaryValueSnippet(model.Value?.ToString(), "No value", HTMLSummaryStyle.Filter)}");
        }
        return filterWriter.ToString();
    }



}
