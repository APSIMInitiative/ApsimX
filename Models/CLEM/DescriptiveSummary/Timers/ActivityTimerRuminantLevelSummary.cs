using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Groupings;
using Models.CLEM.Timers;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerRuminantLevel component
/// </summary>
public class ActivityTimerRuminantLevelSummary : TimerSummaryBase<ActivityTimerRuminantLevel>
{
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
                    childType: typeof(RuminantGroup),
                    introduction: "Based on unique individuals selected from:",
                    missing: ""
                    )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write("Perform when ");
        if (ModelTyped.TimerStyle == ActivityTimerRuminantLevelStyle.NumberOfIndividuals)
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet("the number of individuals", "Not set", HTMLSummaryStyle.Default)}");
        }
        else
        {
            string stl = "[Unknown]";
            switch (ModelTyped.TimerStyle)
            {
                case ActivityTimerRuminantLevelStyle.SumOfProperty:
                    stl = "sum";
                    break;
                case ActivityTimerRuminantLevelStyle.MeanOfProperty:
                    stl = "mean";
                    break;
                case ActivityTimerRuminantLevelStyle.MinimumOfProperty:
                    stl = "minimum";
                    break;
                case ActivityTimerRuminantLevelStyle.MaximumOfProperty:
                    stl = "maximum";
                    break;
            }
            htmlWriter.Write($"the {generator.DisplaySummaryValueSnippet(stl, "Not set", HTMLSummaryStyle.Default)} of {generator.DisplaySummaryValueSnippet(ModelTyped.RuminantProperty,"Not set", HTMLSummaryStyle.Default)}");
        }
        htmlWriter.Write($" {generator.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Default)}");
        htmlWriter.Write($" {generator.DisplaySummaryValueSnippet(ModelTyped.Amount, "Not set", HTMLSummaryStyle.Default)}");
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem");
    }

    /// <summary>
    /// Convert the operator to a symbol
    /// </summary>
    /// <returns>Operator as symbol</returns>
    protected string OperatorToSymbol()
    {
        switch (ModelTyped.Operator)
        {
            case ExpressionType.Equal:
                return "=";
            case ExpressionType.GreaterThan:
                return ">";
            case ExpressionType.GreaterThanOrEqual:
                return ">=";
            case ExpressionType.LessThan:
                return "<";
            case ExpressionType.LessThanOrEqual:
                return "<=";
            case ExpressionType.NotEqual:
                return "!=";
            default:
                return ModelTyped.Operator.ToString();
        }
    }

}