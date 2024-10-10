using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.PMF;
using Graph = Models.Graph;

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
            Section cultivarSection = null;

            newTags.Add(new Paragraph($"The model is constructed from the following list of software components. Details of the implementation and model parameterisation are provided in the following sections."));

            // Write Plant Model Table
            newTags.Add(new Paragraph("**List of Plant Model Components.**"));
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Component Name", typeof(string));
            tableData.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() != typeof(Memo) && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(Folder) && child.GetType() != typeof(CompositeBiomass))
                {
                    DataRow row = tableData.NewRow();
                    row[0] = child.Name;
                    row[1] = child.GetType().ToString();
                    tableData.Rows.Add(row);
                }
            }
            newTags.Add(new Table(tableData));

            foreach (IModel child in this.model.Children)
            {
                if (child is Folder && child.Name == "Cultivars")
                {
                    DataTable cultivarNameTable = new();
                    cultivarNameTable.Columns.Add("Cultivar Name");
                    cultivarNameTable.Columns.Add("Alternative Name(s)");
                   
                    foreach (Folder folder in child.FindAllChildren<Folder>())
                    {
                        List<Cultivar> cultivars = folder.FindAllChildren<Cultivar>().ToList();
                        foreach (Cultivar cultivarChild in cultivars)
                        {
                            string altNames = cultivarChild.GetNames().Any() ? string.Join(',', cultivarChild.GetNames()) : string.Empty;
                            cultivarNameTable.Rows.Add(new string[] { cultivarChild.Name, altNames});
                        }
                    }
                    cultivarSection = new Section("Cultivars", new Table(cultivarNameTable));
                }
                else if (child is Memo)
                {
                    newTags.Add(new Paragraph((child as Memo).Text));
                }
                else
                {
                    newTags.AddRange(new List<ITag>() { GetSummaryAndRemarksSection(child) });
                }
            }
            mainSection.Add(newTags);

            newTags = new List<ITag>() { mainSection };
            if (cultivarSection != null)
                newTags.Add(cultivarSection);

            return newTags;
        }
    }
}
