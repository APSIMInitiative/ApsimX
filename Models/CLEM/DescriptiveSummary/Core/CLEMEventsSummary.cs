using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary for CLEM Events Component
    /// </summary>
    public class CLEMEventsSummary : DescriptiveSummaryProviderBase<CLEMEvents>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            string output = $"CLEM is running using a {CLEMModel.DisplaySummaryValueSnippet(model.TimeStep)} time step";
            if (model.TimeStep == TimeStepTypes.Custom)
            {
                output += $" of {CLEMModel.DisplaySummaryValueSnippet(model.CustomTimeStep)} days";
            }
            Generator.AddBlockWithText("activityentry", output);

            if (model.Structure.FindAll<RuminantActivityGrazeAll>().Any() || model.Structure.FindAll<RuminantActivityGrazePasture>().Any() || model.Structure.FindAll<RuminantActivityGrazePastureHerd>().Any())
            {
                Generator.AddBlockWithText("activityentry", $"Ecological indicators will be calculated every {CLEMModel.DisplaySummaryValueSnippet(model.EcologicalIndicatorsCalculationInterval)} months starting at the end of {CLEMModel.DisplaySummaryValueSnippet(model.EcologicalIndicatorsCalculationMonth)}");
            }
        }
    }
}
