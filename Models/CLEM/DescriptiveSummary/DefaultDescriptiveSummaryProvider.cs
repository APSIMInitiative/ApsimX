using System.Net;
using Models.CLEM.Interfaces;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Simple fallback provider; override lifecycle methods if you want richer defaults. 
    /// </summary>
    public sealed class DefaultDescriptiveSummaryProvider : DescriptiveSummaryProvider
    {
        /// <inheritdoc/>
        public override void BuildSummary(IModel model)
        {
            // safe, minimal default output
            Generator.AddBlockWithText("activityentry", $"No descriptive summary provider has been supplied for [{WebUtility.HtmlEncode(model?.GetType().Name ?? "Unknown")}].");
        }
    }
}