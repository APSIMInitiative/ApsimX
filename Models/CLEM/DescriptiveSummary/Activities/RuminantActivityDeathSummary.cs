using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Death
    /// </summary>
    public class RuminantActivityDeathSummary : RuminantActivitySummaryBase<RuminantActivityDeath>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            return
            [
                new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(IRuminantDeathGroup),
                missing: "No death groups provided. Deaths are determined using breed base mortality.",
                introduction: "Deaths are determined using the following configured death groups and rates/conditions:",
                borderClass: "childgroupfilterborder"
                )
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }
    }
}