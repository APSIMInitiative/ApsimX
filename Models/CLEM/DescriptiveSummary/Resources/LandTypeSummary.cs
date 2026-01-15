using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for LandType (sub-resource)
/// </summary>
public class LandTypeSummary : DescriptiveSummaryProviderBase<LandType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        if (model.LandArea == 0)
        {
            Generator.AddBlockWithText("errorbanner", "No area has been set for this land type");
        }
        else
        {
            if (model.ProportionOfTotalArea == 0)
                Generator.AddBlockWithText("errorbanner", "The proportion of total area assigned to this land type is <span class=\"errorlink\">0</span> so no area is assigned.");
            else
            {
                string units = "";
                if (string.IsNullOrWhiteSpace(model.Units) == false)
                {
                    units = $" {generator.DisplaySummaryValueSnippet(model.Units)}";
                }
                string propBuildings = "";
                if (model.PortionBuildings > 0)
                    propBuildings = $", {generator.DisplaySummaryValueSnippet(model.PortionBuildings*model.UsableArea)} of which is occupied by buildings";
                string usable = "";
                if (model.UsableArea != model.LandArea)
                    usable = $" of which {generator.DisplaySummaryValueSnippet(model.UsableArea)} is usable";


                Generator.AddBlockWithText("activityentry", $"This land type has an {generator.DisplaySummaryValueSnippet(model.LandArea, warnZero: true)}{units}{usable}{propBuildings}");
            }
        }
        Generator.AddBlockWithText("activityentry", $"This land type is identified as {generator.DisplaySummaryValueSnippet(model.SoilType)}");
    }
}