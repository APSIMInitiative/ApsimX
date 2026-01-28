using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Resources;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for SetAttributeWithProperty
/// </summary>
public class SetAttributeWithPropertySummary : DescriptiveSummaryProviderBase<SetAttributeWithProperty>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string attrName = generator.DisplaySummaryValueSnippet(model.AttributeName);
        string prop = generator.DisplaySummaryValueSnippet(model.PropertyOfIndividual);

        if (FormatForParentControl)
        {
            bool isGroupAttribute = (CurrentAncestorList?.Count >= 2 && CurrentAncestorList[CurrentAncestorList.Count - 2] == typeof(RuminantInitialCohorts).Name);
            if (model.StandardDeviation == 0)
                generator.AddBlockWithText($"Attribute {attrName} is linked to {prop} and provided {(isGroupAttribute ? "to all cohorts " : "")}with a value of {generator.DisplaySummaryValueSnippet(model.Value)}", "resourcebanneralone clearfix");
            else
                generator.AddBlockWithText($"Attribute {attrName} is linked to {prop} and provided {(isGroupAttribute ? "to all cohorts " : "")}with mean = {generator.DisplaySummaryValueSnippet(model.Value)} and s.d. = {generator.DisplaySummaryValueSnippet(model.StandardDeviation)}", "resourcebanneralone clearfix");
        }
        else
        {
            generator.AddBlockWithText($"Provide an attribute with the label {attrName} that will be inherited with the {generator.DisplaySummaryValueSnippet(model.InheritanceStyle.ToString())} style{(model.Mandatory ? " and is required by all individuals in the population" : "")}");

            string inherited = "";
            if (model.InheritanceStyle != AttributeInheritanceStyle.None)
                inherited = ($" and is allowed to vary between {generator.DisplaySummaryValueSnippet(model.MinimumValue)} and {generator.DisplaySummaryValueSnippet(model.MaximumValue)} when inherited");


            if (model.StandardDeviation == 0)
                generator.AddBlockWithText($"This attribute is linked to {prop} and has a value of {generator.DisplaySummaryValueSnippet(model.Value)}{inherited}");
            else
                generator.AddBlockWithText($"This attribute's value is linked to {prop} and is randomly taken from a normal distribution with mean {generator.DisplaySummaryValueSnippet(model.Value)} and standard deviation {generator.DisplaySummaryValueSnippet(model.StandardDeviation)}{inherited}");
        }
    }
}