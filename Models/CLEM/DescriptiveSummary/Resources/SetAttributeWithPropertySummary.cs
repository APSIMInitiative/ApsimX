using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
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

            string attrName = CLEMModel.DisplaySummaryValueSnippet(model.AttributeName);
            string prop = CLEMModel.DisplaySummaryValueSnippet(model.PropertyOfIndividual);

            if (FormatForParentControl)
            {
                bool isGroupAttribute = (CurrentAncestorList?.Count >= 2 && CurrentAncestorList[CurrentAncestorList.Count - 2] == typeof(RuminantInitialCohorts).Name);
                if (model.StandardDeviation == 0)
                    Generator.AddBlockWithText("activityentry", $"Attribute {attrName} is linked to {prop} and provided {(isGroupAttribute ? "to all cohorts " : "")}with a value of {CLEMModel.DisplaySummaryValueSnippet(model.Value)}");
                else
                    Generator.AddBlockWithText("activityentry", $"Attribute {attrName} is linked to {prop} and provided {(isGroupAttribute ? "to all cohorts " : "")}with mean = {CLEMModel.DisplaySummaryValueSnippet(model.Value)} and s.d. = {CLEMModel.DisplaySummaryValueSnippet(model.StandardDeviation)}");
            }
            else
            {
                Generator.AddBlockWithText("activityentry", $"Provide an attribute with the label {attrName} that will be inherited with the {CLEMModel.DisplaySummaryValueSnippet(model.InheritanceStyle.ToString())} style{(model.Mandatory ? " and is required by all individuals in the population" : "")}");
                if (model.StandardDeviation == 0)
                    Generator.AddBlockWithText("activityentry", $"This attribute is linked to {prop} and has a value of {CLEMModel.DisplaySummaryValueSnippet(model.Value)}");
                else
                    Generator.AddBlockWithText("activityentry", $"This attribute's value is linked to {prop} and is randomly taken from a normal distribution with mean {CLEMModel.DisplaySummaryValueSnippet(model.Value)} and standard deviation {CLEMModel.DisplaySummaryValueSnippet(model.StandardDeviation)}");
            }
        }
    }
}