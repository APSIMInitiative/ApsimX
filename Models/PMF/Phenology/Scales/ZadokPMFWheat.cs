using System;
using System.Collections.Generic;
using System.Data;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model calculates a Zadok growth stage value based upon the current phenological growth stage within the model. 
    /// The model uses information regarding germination, emergence, leaf appearance and tiller appearance for early growth stages (Zadok stages 0 to 30).
    /// The model then uses simulated phenological growth stages for Zadok stages 30 to 100.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ZadokPMFWheat : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>Haun stage is used for zadok stages 10 to 30</summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].HaunStage")]
        IFunction haunStage = null;

        [Link]
        private IPlant plant = null;

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("Zadok Stage")]
        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                double zadok_stage = 0.0;
                if (plant != null && !plant.IsAlive)
                    return 0;
                if (Phenology.InPhase("Germinating"))
                    zadok_stage = 5.0f * fracInCurrent;
                else if (Phenology.InPhase("Emerging"))
                    zadok_stage = 5.0f + 5 * fracInCurrent;
                else if (Phenology.Stage < 5.3)
                {
                    zadok_stage = 10.0f + haunStage.Value();
                }
                else if (!Phenology.InPhase("ReadyForHarvesting"))
                {
                    double[] zadok_code_y = { 30.0, 34, 39.0, 55.0, 65.0, 71.0, 87.0, 90.0 };
                    double[] zadok_code_x = { 5.0, 5.99, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0 };
                    bool DidInterpolate;
                    zadok_stage = MathUtilities.LinearInterpReal(Phenology.Stage,
                                                               zadok_code_x, zadok_code_y,
                                                               out DidInterpolate);
                }
                else if (Phenology.InPhase("ReadyForHarvesting"))
                {
                    zadok_stage = 90.0f;
                }

                return zadok_stage;
            }
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            foreach (var tag in this.GetModelDescription())
                yield return tag;

            // Write memos.
            foreach (var tag in DocumentChildren<Memo>())
                yield return tag;

            // Write a table containing growth phases and descriptions.
            yield return new Paragraph("**List of growth phases**");

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
            yield return new Table(table);

            // Write a table containing growth stages
            yield return new Paragraph("**List of growth stages**");
            table = new DataTable();
            table.Columns.Add("Growth Stage", typeof(double));
            table.Columns.Add("Stage Name", typeof(string));
            table.Columns.Add("ZadokStage", typeof(int));

            row = table.NewRow();
            row[0] = 4.3;
            row[1] = "Pseudostem";
            row[2] = 30;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 4.9;
            row[1] = "Third node detectable";
            row[2] = 33;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 5.0;
            row[1] = "Flag leaf ligule just visible";
            row[2] = 39;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 6.0;
            row[1] = "Heading (Ear half emerged)";
            row[2] = 55;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 7.0;
            row[1] = "Flowering (Anthesis half-way)";
            row[2] = 65;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 8.0;
            row[1] = "Kernel water ripe";
            row[2] = 71;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 9.0;
            row[1] = "Hard dough";
            row[2] = 87;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 10.0;
            row[1] = "Ripening";
            row[2] = 90;
            table.Rows.Add(row);
            yield return new Table(table);
        }
    }
}