using APSIM.Numerics;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Extras
{
    /// <summary>
    /// Descriptive summary provider for the Rainfall Shuffler
    /// </summary>
    public class RelationshipRunningValueSummary : DescriptiveSummaryProviderBase<RelationshipRunningValue>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"A running value starting at {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.StartingValue)}");
            htmlWriter.Write($" and ranging between {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.Minimum)} and ");
            if (MathUtilities.IsLessThanOrEqual(ModelTyped.Maximum, ModelTyped.Minimum))
            {
                htmlWriter.Write("<span class=\"errorlink\">Invalid</span>");
            }
            else
            {
                htmlWriter.Write($"{CLEMModel.DisplaySummaryValueSnippet(ModelTyped.Maximum)}");
            }
            generator.AddBlockWithText("activityentry", htmlWriter.ToString());
        }
    }
}
