using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for SetAttributeFromHerd
/// </summary>
public class SetAttributeFromHerdSummary : DescriptiveSummaryProviderBase<SetAttributeFromHerd>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string attrName = generator.DisplaySummaryValueSnippet(model.AttributeName);
        string calc = generator.DisplaySummaryValueSnippet(model.CalculationStyle.ToString());

        if (FormatForParentControl)
        {
            Generator.AddBlockWithText("activityentry", $"Attribute {attrName} is calculated from the herd using method {calc} and multiplied by {generator.DisplaySummaryValueSnippet(model.Multiplier)}");
        }
        else
        {
            Generator.AddBlockWithText("activityentry", $"Provide an attribute with the label {attrName} determined from the herd using {calc}{(model.Mandatory ? " and required by all individuals" : "")}");
            Generator.AddBlockWithText("activityentry", $"Resulting value is multiplied by {generator.DisplaySummaryValueSnippet(model.Multiplier)} and inherited with style {generator.DisplaySummaryValueSnippet(model.InheritanceStyle.ToString())}");
        }
    }
}