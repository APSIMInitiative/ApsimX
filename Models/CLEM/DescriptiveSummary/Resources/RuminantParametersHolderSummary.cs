using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Ruminant Parameters component descriptive summary
    /// </summary>
    public class RuminantParametersHolderSummary : DescriptiveSummaryProviderBase<RuminantParametersHolder>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersHolderSummary()
        {
            SummaryStyle = HTMLSummaryStyle.SubResource;
        }

        /// <inheritdoc/>
        public override void BuildSummary(RuminantParametersHolder model)
        {
            Generator.Append("A summary of important ruminant parameter settings from parameters supplied");
        }

    }
}
