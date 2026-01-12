using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Resources
{
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
                        units = $" {CLEMModel.DisplaySummaryValueSnippet(units)}";
                    }
                    string propBuildings = "";
                    if (model.PortionBuildings > 0)
                        propBuildings = $" of which {CLEMModel.DisplaySummaryValueSnippet(model.PortionBuildings)} is buildings";

                    Generator.AddBlockWithText("activityentry", $"This land type has an area of {CLEMModel.DisplaySummaryValueSnippet(model.LandArea * model.ProportionOfTotalArea)}{units}{propBuildings}");
                }
            }
            Generator.AddBlockWithText("activityentry", $"This land type is identified as {CLEMModel.DisplaySummaryValueSnippet(model.SoilType)}");
        }
    }
}