using Models.CLEM.Limiters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for the Activity Carry Limiter
    /// </summary>
    public class ActivityCarryLimiterSummary : DescriptiveSummaryProviderBase<ActivityCarryLimiter>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            string name = "";
            if (!ModelTyped.Name.Contains(this.GetType().Name.Split('.').Last()))
            {
                name = ModelTyped.Name;
            }
            Generator.OpenBlock("filtername", name);
            using (Generator.OpenBlock("filterborder clearfix"))
            {
                string limit = $"Limit cut and carry activities to ";
                if (!(ModelTyped.WeightLimitPerDay is null) && ModelTyped.WeightLimitPerDay.Count() >= 1)
                {
                    limit += CLEMModel.DisplaySummaryValueSnippet(ModelTyped.WeightLimitPerDay);
                }
                else
                {
                    limit += "<span class=\"errorlink\">Not Set</span>";
                }
                Generator.OpenBlock("filter", limit+ " dry kg/day");
            }
        }
    }
}
