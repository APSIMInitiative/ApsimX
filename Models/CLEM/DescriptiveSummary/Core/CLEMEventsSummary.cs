using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for CLEM Events component
/// </summary>
public class CLEMEventsSummary : DescriptiveSummaryProviderBase<CLEMEvents>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        if (!FormatForParentControl)
            generator.AddBlockWithText($"The simulation is performed from {generator.DisplaySummaryValueSnippet(ModelTyped.Clock.StartDate.ToShortDateString())} to {generator.DisplaySummaryValueSnippet(ModelTyped.Clock.EndDate.ToShortDateString())}");

        string output = $"CLEM is running using a {generator.DisplaySummaryValueSnippet(ModelTyped.TimeStep)} time step";
        if (ModelTyped.TimeStep == TimeStepTypes.Custom)
        {
            output += $" of {generator.DisplaySummaryValueSnippet(ModelTyped.CustomTimeStep)} days";
        }
        generator.AddBlockWithText(output);

        if (ModelTyped.Structure.FindAll<RuminantActivityGrazeAll>().Any() || ModelTyped.Structure.FindAll<RuminantActivityGrazePasture>().Any() || ModelTyped.Structure.FindAll<RuminantActivityGrazePastureHerd>().Any())
        {
            generator.AddBlockWithText($"Ecological indicators will be calculated every {generator.DisplaySummaryValueSnippet(ModelTyped.EcologicalIndicatorsCalculationInterval)} months starting at the end of {generator.DisplaySummaryValueSnippet(ModelTyped.EcologicalIndicatorsCalculationMonth)}");
        }
    }
}
