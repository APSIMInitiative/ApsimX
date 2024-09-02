using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Factorial;
using Models.PMF;
using Models.PMF.Interfaces;

namespace Models.Core
{

    /// <summary>
    /// A folder model
    /// </summary>
    [ViewName("UserInterface.Views.FolderView")]
    [PresenterName("UserInterface.Presenters.FolderPresenter")]
    [ScopedModel]
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    [ValidParent(ParentType = typeof(IPlant))]
    public class Folder : Model
    {
        /// <summary>Show in the autodocs?</summary>
        /// <remarks>
        /// Apparently, not all folders of graphs are intended to be shown in the autodocs.
        /// Hence, this flag.
        /// </remarks>
        public bool ShowInDocs { get; set; }

        /// <summary>Number of graphs to show per page.</summary>
        private int GraphsPerPage { get; set; } = 6;

        /// <summary>
        /// Document the model, and any child models which should be documented.
        /// </summary>
        /// <remarks>
        /// It is a mistake to call this method without first resolving links.
        /// </remarks>
        public override IEnumerable<ITag> Document()
        {
            yield return new Section(Name, DocumentChildren());
        }

        /// <summary>
        /// Document the appropriate child models (in this case, memos,
        /// experiments, graphs, and folders).
        /// </summary>
        private IEnumerable<ITag> DocumentChildren()
        {
            // Write memos.
            foreach (Memo memo in FindAllChildren<Memo>().Where(memo => memo.Enabled))
                foreach (ITag tag in memo.Document())
                    yield return tag;

            foreach (ITag tag in DocumentChildren<Map>())
                yield return tag;

            // Write experiment descriptions. We don't call experiment.Document() here,
            // because we want to just show the experiment design (a string) and put it
            // inside a table cell.
            IEnumerable<Experiment> experiments = FindAllChildren<Experiment>().Where(experiment => experiment.Enabled);
            if (experiments.Any())
            {
                yield return new Paragraph("**List of experiments.**");
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
                yield return new Table(table);
            }
            else
            {
                // No experiments - look for free standing simulations.
                foreach (Simulation simulation in FindAllChildren<Simulation>().Where(simulation => simulation.Enabled))
                {
                    yield return new Paragraph($"**{simulation.Name}**");
                    foreach (Folder folder in simulation.FindAllChildren<Folder>().Where(folder => folder.Enabled && folder.ShowInDocs))
                    {
                        var childGraphs = GetChildGraphs(folder);
                        while (childGraphs.Any())
                        {
                            yield return new APSIM.Shared.Documentation.GraphPage(childGraphs.Take(GraphsPerPage));
                            childGraphs = childGraphs.Skip(GraphsPerPage);
                        }
                    }
                }
            }

            // Write page of graphs.
            if (ShowInDocs)
            {
                var childGraphs = GetChildGraphs(this);
                while (childGraphs.Any())
                {
                    yield return new APSIM.Shared.Documentation.GraphPage(childGraphs.Take(GraphsPerPage));
                    childGraphs = childGraphs.Skip(GraphsPerPage);
                }
            }

            // Document experiments individually.
            foreach (Experiment experiment in experiments.Where(expt => expt.Enabled))
                foreach (ITag tag in experiment.Document())
                    yield return tag;

            // Document child folders.
            foreach (Folder folder in FindAllChildren<Folder>().Where(f => f.Enabled))
                foreach (ITag tag in folder.Document())
                    yield return tag;
        }

        private IEnumerable<APSIM.Shared.Documentation.Graph> GetChildGraphs(IModel parent)
        {
            var graphs = new List<APSIM.Shared.Documentation.Graph>();
            var page = new Models.GraphPage();
            page.Graphs.AddRange(parent.FindAllChildren<Models.Graph>().Where(g => g.Enabled));
            var storage = parent.FindInScope<Models.Storage.IDataStore>();
            foreach (var map in page.GetAllSeriesDefinitions(this, storage.Reader))
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
