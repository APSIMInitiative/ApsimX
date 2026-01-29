using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using DeepCloner.Core;
using MathNet.Numerics.Distributions;
using Models;
using Models.Core;
using Models.PMF;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Graph = Models.Graph;
using DocumentationNode = Models.Documentation;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocPlant : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPlant" /> class.
        /// </summary>
        public DocPlant(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            List<ITag> newTags = new List<ITag>();

            Section mainSection = GetSummaryAndRemarksSection(model);


            mainSection.Add(new Paragraph($"The model is constructed from the following list of software components. Details of the implementation and model parameterisation are provided in the following sections."));

            // Write Plant Model Components Table
            // ------------------------------------------------------------------------------
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Component Name", typeof(string));
            tableData.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() != typeof(Memo) && child.GetType() != typeof(DocumentationNode) && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(Folder) && child.GetType() != typeof(CompositeBiomass))
                {
                    DataRow row = tableData.NewRow();
                    row[0] = child.Name;
                    string childtype = child.GetType().ToString();
                    row[1] = "["+childtype+ "](https://github.com/APSIMInitiative/ApsimX/blob/master/"+childtype.Replace(".","/")+".cs)";
                    tableData.Rows.Add(row);
                }
            }
            mainSection.Add(new Table(tableData));

            newTags.Add(mainSection);

            // Write Composite Biomass Table
            // -------------------------------------------------------------------------------
            DataTable tableDataBiomass = new DataTable();
            tableDataBiomass.Columns.Add("Component Name", typeof(string));
            tableDataBiomass.Columns.Add("Component Organs", typeof(string));
            tableDataBiomass.Columns.Add("Live Material", typeof(string));
            tableDataBiomass.Columns.Add("Dead Material", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() == typeof(CompositeBiomass))
                {
                    string organList="";
                    foreach (string organ in ((CompositeBiomass)child).OrganNames)
                        organList += organ+" ";
                    DataRow row = tableDataBiomass.NewRow();
                    row[0] = child.Name;
                    row[1] = organList;
                    row[2] = ((CompositeBiomass)child).IncludeLive.ToString();
                    row[3] = ((CompositeBiomass) child).IncludeDead.ToString();
                    tableDataBiomass.Rows.Add(row);
                }
            }
            if (tableDataBiomass.Rows.Count > 0)
            {
                newTags.Add(new Section("Composite Biomass Components", new Table(tableDataBiomass)));
            }

            // Document Phenology Model
            // -------------------------------------------------------------------------------
            Phenology phenology = (Phenology)this.model.Children.Find(m => m.GetType() == typeof(Phenology));
            if (phenology != null)
            {
                DataTable dataTable = phenology.GetPhaseTable();
                Section PhenologySection = new Section("Phenology", new Table(dataTable));
                PhenologySection.Add(GetSummaryAndRemarksSection(phenology).Children);
                newTags.Add(PhenologySection);
            }

            // Document Arbitrator Model
            // -------------------------------------------------------------------------------
            OrganArbitrator arbitrator = (OrganArbitrator)this.model.Children.Find(m => m.GetType() == typeof(OrganArbitrator));
            if (arbitrator != null)
                newTags.Add(new Section("Organ Arbitrator", GetSummaryAndRemarksSection(arbitrator).Children));

            // Document SimpleLeaf Model
            SimpleLeaf simpleLeaf = (SimpleLeaf)this.model.Children.Find(m => m.GetType() == typeof(SimpleLeaf));
            if (simpleLeaf != null)
            {
                Section S = GetSummaryAndRemarksSection(simpleLeaf);

                S.Add(new Paragraph(CodeDocumentation.GetSummary(model.GetType())));
                newTags.Add(S);
            }

            //Write children
            // -------------------------------------------------------------------------------
            List<ITag> children = new List<ITag>();
            foreach (IModel child in this.model.Children)
                if (child as IText == null && child as CompositeBiomass == null && child as Folder == null && child as Cultivar == null && child as Phenology == null && child as OrganArbitrator == null && child as SimpleLeaf == null)
                {
                    Section S = GetSummaryAndRemarksSection(child);
                    //S.Add(new Paragraph("Hello World"));
                    children.AddRange(new List<ITag>() {S});
                }
            newTags.Add(new Section("Child Components", children));

            //Write cultivars table
            // -------------------------------------------------------------------------------
            List<Cultivar> cultivars = model.Node.FindChildren<Cultivar>(recurse: true).ToList();
            if (cultivars.Count > 0)
            {
                cultivars = cultivars.OrderBy(c => c.Name).ToList();
                DataTable cultivarNameTable = new();
                cultivarNameTable.Columns.Add("Name (Aternatives)");
                cultivarNameTable.Columns.Add("Overrides");
                foreach (Cultivar cultivarChild in cultivars)
                {
                    string altNames = cultivarChild.GetNames().Any() ? string.Join(' ', cultivarChild.GetNames()) : string.Empty;
                    altNames = altNames.Replace(cultivarChild.Name, "");
                    if (altNames != "") altNames = "(" + altNames + ")";
                    altNames = altNames.Replace("( ", "(");

                    string commands = "";
                    if (cultivarChild.Command != null)
                        foreach (string cmd in cultivarChild.Command)
                            commands += cmd + " \n\n";
                    cultivarNameTable.Rows.Add(new string[] { cultivarChild.Name + " \n\n" + altNames, commands });
                }
                newTags.Add(new Section("Appendix 1 - Cultivar specifications", new Table(cultivarNameTable)));
            }

            return newTags;
        }
    }
}
