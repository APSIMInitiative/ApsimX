using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Mapsui.Providers;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for Labour resource
    /// </summary>
    public class LabourSummary : DescriptiveSummaryProviderBase<Labour>
    {
        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<LabourType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("LabourType", entryStyle: HTMLSummaryStyle.Resource)} provided!"),
                (model.Structure.FindChildren<Relationship>().Where(a => a.Identifier == "Adult equivalent"), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("Relationship", entryStyle:HTMLSummaryStyle.Helper)} with the identifier {CLEMModel.DisplaySummaryValueSnippet("Adult equivalent")} was provided. All individuals are assumed to be 1 AE."),
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText("activityentry", $"Individuals {(model.AllowAgeing? "" : "do not")} age with time");
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocks()
        {
            var cm = CLEMModel;
            if (cm is null) return;

            if (cm.Structure.FindChildren<AnimalPriceGroup>().Any())
            {
                Generator.CreateTable(new string[] { "Name", "Sex", "Age (yrs)", "Number", "Hired" });
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerClosingBlocks()
        {
            var cm = CLEMModel;
            if (cm is null) return;

            if (cm.Structure.FindChildren<AnimalPriceGroup>().Any())
            {
                Generator.CloseTable();
            }
        }
    }
}