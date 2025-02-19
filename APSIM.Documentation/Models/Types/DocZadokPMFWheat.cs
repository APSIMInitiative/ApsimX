using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Phen;
using System.Data;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocZadokPMFWheat : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZadokPMFWheat" /> class.
        /// </summary>
        public DocZadokPMFWheat(IModel model): base(model) {}

        /// <summary>
        /// Stage names specific to Wheat.
        /// </summary>
        public static readonly string[] stageNames = [
            "Pseudostem",
            "Third node detectable", 
            "Flag leaf ligule just visible", 
            "Heading (hEar half emerged)", 
            "Flowering (Anthesis half-way)",
            "Kernel water ripe",
            "Hard dough",
            "Ripening"];

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            // Write a table containing growth phases and descriptions.
            DataTable table = new DataTable();
            table.Columns.Add("Growth Phase", typeof(string));
            table.Columns.Add("Descriptipon", typeof(string));
            DataRow row = table.NewRow();
            row[0] = "Germinating";
            row[1] = "ZadokStage = 5 x FractionThroughPhase";
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = "Emerging";
            row[1] = "ZadokStage = 5 + 5 x FractionThroughPhase";
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = "Vegetative";
            row[1] = "ZadokStage = 10 + Structure.LeafTipsAppeared";
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = "Reproductive";
            row[1] = "ZadokStage is interpolated from values of stage number using the following table";
            table.Rows.Add(row);
            Table growthPhaseTable = new(table);
            section.Add(new Section("List of growth phases", growthPhaseTable));

            // Write a table containing growth stages
            table = new DataTable();
            table.Columns.Add("Growth Stage", typeof(double));
            table.Columns.Add("Stage Name", typeof(string));
            table.Columns.Add("ZadokStage", typeof(int));

            for(int i = 0; i < stageNames.Length; i++)
            {
                row = table.NewRow();
                row[0] = ZadokPMFWheat.ZADOK_CODE_X[i];
                row[1] = stageNames[i];
                row[2] = ZadokPMFWheat.ZADOK_CODE_Y[i];
                table.Rows.Add(row);
            }
            Table growthStageTable = new(table);
            section.Add(new Section("List of growth stages", growthStageTable));
            return new List<ITag>() {section};
        }

    }
}

           
           