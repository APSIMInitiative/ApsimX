using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Resources;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Resources;

/// <summary>
/// Descriptive summary provider for SetAttributeWithProperty
/// </summary>
public class SetAttributeWithPropertySummary : DescriptiveSummaryProviderBase<SetAttributeWithProperty>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public SetAttributeWithPropertySummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResource;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string attrName = DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.AttributeName);
        string prop = DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.PropertyOfIndividual);

        if (FormatForParentControl)
        {
            bool isGroupAttribute = (CurrentAncestorList?.Count >= 2 && CurrentAncestorList[^2] == typeof(RuminantInitialCohorts).Name);
            if (model.StandardDeviation == 0)
                generator.AddBlockWithText($"Attribute {attrName} is linked to {prop} and provided {(isGroupAttribute ? "to all cohorts " : "")}with a value of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Value)}", "resourcebanneralone clearfix");
            else
                generator.AddBlockWithText($"Attribute {attrName} is linked to {prop} and provided {(isGroupAttribute ? "to all cohorts " : "")}with mean = {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Value)} and s.d. = {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.StandardDeviation)}", "resourcebanneralone clearfix");
        }
        else
        {
            generator.AddBlockWithText($"Provide an attribute with the label {attrName} that will be inherited with the {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.InheritanceStyle.ToString())} style{(model.Mandatory ? " and is required by all individuals in the population" : "")}");

            string inherited = "";
            if (model.InheritanceStyle != AttributeInheritanceStyle.None)
                inherited = ($" and is allowed to vary between {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MinimumValue)} and {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MaximumValue)} when inherited");


            if (model.StandardDeviation == 0)
                generator.AddBlockWithText($"This attribute is linked to {prop} and has a value of {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Value)}{inherited}");
            else
                generator.AddBlockWithText($"This attribute's value is linked to {prop} and is randomly taken from a normal distribution with mean {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.Value)} and standard deviation {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.StandardDeviation)}{inherited}");
        }
    }
}