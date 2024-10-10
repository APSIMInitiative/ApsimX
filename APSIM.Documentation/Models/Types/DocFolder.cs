using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models;
using Models.Factorial;
using System.Data;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocFolder : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocFolder(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            // Remove summary, summary is superfluous.
            if (section.Children.Count >= 2)
                section.Children.RemoveAt(0);
            
            foreach(Map map in model.FindAllChildren<Map>().Where(map => map.Enabled))
            {
                section.Add(AutoDocumentation.DocumentModel(map));
            }

            // Write experiment descriptions. We don't call experiment.Document() here,
            // because we want to just show the experiment design (a string) and put it
            // inside a table cell.
            IEnumerable<Experiment> experiments = model.FindAllChildren<Experiment>().Where(experiment => experiment.Enabled);
            if (experiments.Any())
            {
                List<ITag> experimentsTag = new List<ITag>();
                DataTable table = new DataTable();
                table.Columns.Add("Experiment Name", typeof(string));
                table.Columns.Add("Design (Number of Treatments)", typeof(string));

                foreach (Experiment experiment in experiments)
                {
                    DataRow row = table.NewRow();
                    row[0] = experiment.Name;
                    row[1] = experiment.GetDesign();
                    table.Rows.Add(row);
                }
                experimentsTag.Add(new Paragraph("**List of experiments.**"));
                experimentsTag.Add(new Table(table));
                section.Add(new Section("Experiments", experimentsTag));

            }
            else
            {
                // No experiments - look for free standing simulations.
                foreach (Simulation simulation in model.FindAllChildren<Simulation>().Where(simulation => simulation.Enabled))
                {
                    List<ITag> graphPageTags = new List<ITag>();
                    foreach (Folder folder in simulation.FindAllChildren<Folder>().Where(folder => folder.Enabled && folder.ShowInDocs))
                    {
                        var childGraphs = (model as Folder).GetChildGraphs(folder);
                        foreach(Shared.Documentation.Graph graph in childGraphs)
                            graphPageTags.Add(graph);
                    }
                    section.Add(new Paragraph($"**{simulation.Name}**"));
                    section.Add(new Section(graphPageTags));
                }
            }

            // Check to see if any ancestor folders are not shown in doc.
            // If any ancestors are not shown in doc, this should not be either.
            bool showGraphs = true;
            List<Folder> folderAncestorList = (model as Folder).FindAllAncestors<Folder>().ToList();
            foreach (Folder folder in folderAncestorList)
                if (folder.ShowInDocs == false)
                    showGraphs = false;

            if (showGraphs)
            {
                // Write page of graphs.
                if ((model as Folder).ShowInDocs)
                {
                    if (model.Parent != null)
                    {
                        var childGraphs = new List<Shared.Documentation.Graph>();
                        if ((model as Folder).GetChildGraphs(model) != null)
                        {
                            childGraphs = (model as Folder).GetChildGraphs(model).ToList();
                            if (childGraphs.Count > 0)
                                section.Add(new Shared.Documentation.GraphPage(childGraphs));
                        }
                    }             
                }
            }



            // Document experiments individually.
            foreach (Experiment experiment in experiments.Where(expt => expt.Enabled))
                section.Add(AutoDocumentation.DocumentModel(experiment));

            // Document child folders.
            foreach (Folder folder in model.FindAllChildren<Folder>().Where(f => f.Enabled))
                section.Add(AutoDocumentation.DocumentModel(folder));

            return new List<ITag>() {section};
        }

    }
}
