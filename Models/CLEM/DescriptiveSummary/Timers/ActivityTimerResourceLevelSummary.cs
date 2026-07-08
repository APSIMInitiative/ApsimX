using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Timers;
using System.IO;
using System.Linq.Expressions;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerResourceLevel component
/// </summary>
public class ActivityTimerResourceLevelSummary : TimerSummaryBase<ActivityTimerResourceLevel>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Perform when {generator.DisplaySummaryValueSnippet(ModelTyped.ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource)} ");
        string str = "";
        switch (ModelTyped.Operator)
        {
            case ExpressionType.Equal:
                str += "equals";
                break;
            case ExpressionType.NotEqual:
                str += "does not equal";
                break;
            case ExpressionType.LessThan:
                str += "is less than";
                break;
            case ExpressionType.LessThanOrEqual:
                str += "is less than or equal to";
                break;
            case ExpressionType.GreaterThan:
                str += "is greater than";
                break;
            case ExpressionType.GreaterThanOrEqual:
                str += "is greater than or equal to";
                break;
            default:
                break;
        }
        htmlWriter.Write(str);
        generator.DisplaySummaryValueSnippet(ModelTyped.Amount);
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem");
    }
}
