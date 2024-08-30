using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public int GraphsPerPage { get; set; } = 6;

        /// <summary>
        /// Gets child graphs from a folder model.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IEnumerable<APSIM.Shared.Documentation.Graph> GetChildGraphs(IModel parent)
        {
            var graphs = new List<APSIM.Shared.Documentation.Graph>();
            var page = new GraphPage();
            page.Graphs.AddRange(parent.FindAllChildren<Graph>().Where(g => g.Enabled));
            var storage = parent.FindInScope<Storage.IDataStore>();
            List<GraphPage.GraphDefinitionMap> definitionMaps = new();
            if (storage != null)
                definitionMaps.AddRange(page.GetAllSeriesDefinitions(this, storage.Reader));
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
