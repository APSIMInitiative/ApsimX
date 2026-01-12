using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
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
            SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            Generator.AddBlockWithText("detailsnote", "A summary of important ruminant parameter settings from parameters supplied");
        }

        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> grps = new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>();
            var model = ModelTyped;
            if (model is null) return grps;

            grps.Add((model.Structure.FindChildren<ISubParameters>(recurse: true).Cast<IModel>(), true, "", "", ""));
            grps.Add((model.Structure.FindChildren<RuminantParametersGrowPF>().Cast<IModel>(), false, "", "", ""));

            return grps;
        }
    }
}
