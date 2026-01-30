using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Crop Activity Task
/// </summary>
public class ReportActivitiesPerformedSummary : DescriptiveSummaryProviderBase<ReportActivitiesPerformed>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        string rotateText = ModelTyped.RotateReport ? "months as columns and activities as rows" : "months as rows and activities as columns";

        if (ModelTyped.CreateHTML)
        {
            generator.AddBlockWithText( $"A HTML version of this report is available with {rotateText}. (See Summary tab for current link)");
        }

        if (ModelTyped.AutoCreateHTML)
        {
            generator.AddBlockWithText( $"A HTML version of this report will automatically be created for its parent CLEMZone and named the same as the simulation file with a html extension");
        }
    }
}
