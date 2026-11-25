using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for SetAttributeFromHerd
/// </summary>
public class SetAttributeFromHerdSummary : DescriptiveSummaryProviderBase<SetAttributeFromHerd>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public SetAttributeFromHerdSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResource;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string attrName = DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.AttributeName);
        string calc = DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.CalculationStyle.ToString());

        if (FormatForParentControl)
        {
            string multiplier = "";
            if (model.Multiplier != 1.0)
                multiplier = $" and multiplied by {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Multiplier)}";
            Generator.AddBlockWithText($"Attribute {attrName} is calculated as {calc} from the specified individuals{multiplier}", "resourcebanneralone clearfix");
        }
        else
        {
            Generator.AddBlockWithText($"Provide an attribute with the label {attrName} determined from the herd using {calc}{(model.Mandatory ? " and required by all individuals" : "")}");
            Generator.AddBlockWithText($"Resulting value is multiplied by {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Multiplier)} and inherited with style {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.InheritanceStyle.ToString())}");
        }
    }
}