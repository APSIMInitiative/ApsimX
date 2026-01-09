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
        public override void BuildSummary(ResourcesHolder model)
        {
            Generator.Append("<h1>Resources summary</h1>");
        }

        /// <inheritdoc/>
        public override void CreateSummaryOpeningBlocks(CLEMModel cm)
        {
            Generator.OpenBlock("resource", styleString: $"opacity: {cm.SummaryOpacity(FormatForParentControl)};", id: $"{cm.Name}_main");
        }
    }
}
