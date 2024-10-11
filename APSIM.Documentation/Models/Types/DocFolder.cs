using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models;
using Models.Storage;
using Models.Factorial;
using System.Data;
using System;
using ModelsGraph = Models.Graph;
using ModelsGraphPage = Models.GraphPage;

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
                experimentsTag.Add(new Table(table));
                section.Add(new Section("List of experiments", experimentsTag));

            }
            else
            {
                // No experiments - look for free standing simulations.
                foreach (Simulation simulation in model.FindAllChildren<Simulation>().Where(simulation => simulation.Enabled))
                {
                    List<ITag> graphPageTags = new List<ITag>();
                    foreach (Folder folder in simulation.FindAllChildren<Folder>().Where(folder => folder.Enabled && folder.ShowInDocs))
                    {
                        var childGraphs = GetChildGraphs();
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
                    var childGraphs = new List<Shared.Documentation.Graph>();
                    if (GetChildGraphs() != null)
                    {
                        childGraphs = GetChildGraphs().ToList();
                        if (childGraphs.Count > 0)
                            section.Add(new Shared.Documentation.GraphPage(childGraphs));
                    }
                }

                // Document graphs under a simulation
                foreach (Experiment exp in model.FindAllChildren<Experiment>().Where(f => f.Enabled))
                {
                    List<ITag> simTags = new List<ITag>();
                    foreach (Memo memo in exp.FindAllChildren<Memo>())
                        simTags.AddRange(AutoDocumentation.DocumentModel(memo));

                    foreach (ModelsGraph graph in exp.FindAllChildren<ModelsGraph>().Where(f => f.Enabled)) 
                        simTags.AddRange(AutoDocumentation.DocumentModel(graph));

                    section.Add(new Section(exp.Name, simTags));
                }

                // Document graphs under a experiment
                foreach (Simulation sim in model.FindAllChildren<Simulation>().Where(f => f.Enabled))
                {
                    List<ITag> simTags = new List<ITag>();
                    foreach (Memo memo in sim.FindAllChildren<Memo>())
                        simTags.AddRange(AutoDocumentation.DocumentModel(memo));

                    foreach (ModelsGraph graph in sim.FindAllChildren<ModelsGraph>().Where(f => f.Enabled)) 
                        simTags.AddRange(AutoDocumentation.DocumentModel(graph));

                    section.Add(new Section(sim.Name, simTags));
                }
            }

            // Document child folders.
            foreach (Folder folder in model.FindAllChildren<Folder>().Where(f => f.Enabled))
                section.Add(AutoDocumentation.DocumentModel(folder));
            
            return new List<ITag>() {section};
        }

        /// <summary>
        /// Gets child graphs from a folder model.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<APSIM.Shared.Documentation.Graph> GetChildGraphs()
        {
            var graphs = new List<APSIM.Shared.Documentation.Graph>();
            var page = new ModelsGraphPage();
            page.Graphs.AddRange(model.FindAllChildren<ModelsGraph>().Where(g => g.Enabled));
            var storage = model.FindInScope<IDataStore>();
            List<ModelsGraphPage.GraphDefinitionMap> definitionMaps = new();
            if (storage != null)
                definitionMaps.AddRange(page.GetAllSeriesDefinitions(model, storage.Reader));
            foreach (var map in definitionMaps)
            {
                try
                {
                    graphs.Add(map.Graph.ToGraph(map.SeriesDefinitions));
                }
                catch (Exception err)
                {
                    Console.Error.WriteLine(err);
                }
            }
            return graphs;
        }

    }


}
