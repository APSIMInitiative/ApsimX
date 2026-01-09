using Models.CLEM.Activities;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// ResourceHolder component descriptive summary
    /// </summary>
    public class ResourcesHolderSummary : DescriptiveSummaryProviderBase<ResourcesHolder>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            Generator.Append("<h1>Resources summary</h1>");
        }

        /// <inheritdoc/>
        public override void CreateSummaryOpeningBlocks()
        {
            var model = ModelTyped;
            if (model is null) return;
            Generator.OpenBlock("resource", styleString: $"opacity: {model.SummaryOpacity(FormatForParentControl)};", id: $"{model.Name}_main");
        }
    }
}
