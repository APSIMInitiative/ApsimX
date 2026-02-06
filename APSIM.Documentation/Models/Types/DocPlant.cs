using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using APSIM.Shared.Documentation;
using Models;
using Models.Functions;
using Models.Core;
using Models.PMF;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Constant = Models.Functions.Constant;
using System;

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


            mainSection.Add(new Paragraph($"The model is constructed from the following list of software components. Links provided will direct the user to the code for each model.  Details of the implementation and model parameterisation are provided in the following sections."));

            // Write Plant Model Components Table
            // ------------------------------------------------------------------------------
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Component Name", typeof(string));
            tableData.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child as IText == null && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(Folder) && child.GetType() != typeof(CompositeBiomass))
                {
                    DataRow row = tableData.NewRow();
                    row[0] = child.Name;
                    string childtype = DocumentationUtilities.GetFilepathOfNamespace(child.GetType());
                    row[1] = $"[{child.GetType()}](https://github.com/APSIMInitiative/ApsimX/blob/master/"+childtype.Replace(".","/")+".cs)";
                    tableData.Rows.Add(row);
                }
            }
            mainSection.Add(new Table(tableData));

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
                mainSection.Add(new Section("Composite Biomass Components", new Table(tableDataBiomass)));
            }

            newTags.Add(mainSection);

            // Document Phenology Model
            // -------------------------------------------------------------------------------
            Phenology phenology = (Phenology)this.model.Children.Find(m => m.GetType() == typeof(Phenology));
            if (phenology != null)
            {
                DataTable dataTable = DocumentationUtilities.GetPhaseTable(phenology);
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
            foreach (IModel child in this.model.Children)
                if (child as IText == null && child as CompositeBiomass == null && child as Folder == null && child as Cultivar == null && child as Phenology == null && child as OrganArbitrator == null && child as SimpleLeaf == null && child as Constant == null)
                {
                    Section S = GetSummaryAndRemarksSection(child);
                    newTags.Add(S);
                }

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
                    string altNames = cultivarChild.GetNames().Any() ? string.Join(',', cultivarChild.GetNames()) : string.Empty;
                    altNames = altNames.Replace(cultivarChild.Name, "");
                    if (altNames.StartsWith(','))
                        altNames = altNames.Remove(0);
                    if (altNames.Length > 0)
                        altNames = $"*{altNames}*";

                    List<string> commandsList = new List<string>(cultivarChild.Command);
                    commandsList.Sort(StringComparer.OrdinalIgnoreCase);
                    string commands = "";
                    if (cultivarChild.Command != null)
                        foreach (string cmd in commandsList)
                            commands += $"<p>{cmd}</p>";
                    cultivarNameTable.Rows.Add(new string[] { $"<p>{cultivarChild.Name}</p><p>{altNames}</p>", commands });
                }
                newTags.Add(new Section("Appendix 1 - Cultivar specifications", new Table(cultivarNameTable)));
            }

            return newTags;
        }
    }
}
