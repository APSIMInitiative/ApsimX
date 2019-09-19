using Models.Core;
using Models.Graph;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    public class GraphPanelPresenter : IPresenter
    {
        /// <summary>
        /// The view.
        /// </summary>
        private IGraphPanelView view;

        /// <summary>
        /// The graph panel.
        /// </summary>
        private GraphPanel panel;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// Presenter for the model's properties.
        /// </summary>
        private PropertyPresenter properties;

        /// <summary>
        /// List of graph tabs. Each graph tab consists of a graph, view, and presenter.
        /// </summary>
        private List<GraphTab> graphs;

        /// <summary>
        /// Attaches the model to the view.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = view as IGraphPanelView;
            this.panel = model as GraphPanel;
            this.presenter = explorerPresenter;
            graphs = new List<GraphTab>();

            if (this.view == null || this.panel == null || this.presenter == null)
                throw new ArgumentException();

            presenter.CommandHistory.ModelChanged += OnModelChanged;
            PopulateView();
        }

        /// <summary>
        /// Detaches the model from the view.
        /// </summary>
        public void Detach()
        {
            panel.CurrentTab = view.CurrentTab;
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            ClearGraphs();
            properties.Detach();
        }

        /// <summary>
        /// Populates the view.
        /// </summary>
        private void PopulateView()
        {
            properties = new PropertyPresenter();
            properties.Attach(panel, view.PropertiesGrid, presenter);

            Refresh();
            view.CurrentTab = panel.CurrentTab;
        }

        private void Refresh()
        {
            try
            {
                presenter.MainPresenter.ShowWaitCursor(true);
                ClearGraphs();
                Graph[] graphs = Apsim.Children(panel, typeof(Graph)).Cast<Graph>().ToArray();

                IGraphPanelScript script = panel.Script;
                if (script != null)
                {
                    IStorageReader reader = GetStorage();
                    string[] simNames = script.GetSimulationNames(reader, panel);
                    if (simNames != null)
                        foreach (string sim in simNames)
                            CreatePageOfGraphs(sim, graphs);
                }

                if (panel.SameAxes)
                    StandardiseAxes();
            }
            finally
            {
                presenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        private void ClearGraphs()
        {
            if (graphs != null)
                foreach (GraphTab tab in graphs)
                    foreach (GraphPresenter graphPresenter in tab.Presenters)
                        graphPresenter.Detach();

            graphs.Clear();
            view.RemoveGraphTabs();
        }

        private void CreatePageOfGraphs(string sim, Graph[] graphs)
        {
            GraphTab tab = new GraphTab(sim);
            for (int i = 0; i < graphs.Length; i++)
            {
                if (graphs[i].Enabled)
                {
                    GraphView graphView = new GraphView();
                    GraphPresenter presenter = new GraphPresenter();
                    presenter.SimulationFilter = new List<string>() { sim };

                    panel.Script.TransformGraph(graphs[i], sim);

                    this.presenter.ApsimXFile.Links.Resolve(presenter);
                    if (panel.Cache.ContainsKey(sim) && panel.Cache[sim].Count > i)
                        presenter.Attach(graphs[i], graphView, this.presenter, panel.Cache[sim][i]);
                    else
                    {
                        presenter.Attach(graphs[i], graphView, this.presenter);
                        if (!panel.Cache.ContainsKey(sim))
                            panel.Cache.Add(sim, new Dictionary<int, List<SeriesDefinition>>());

                        panel.Cache[sim][i] = presenter.SeriesDefinitions;
                    }

                    tab.AddGraph(graphView, presenter, graphs[i]);
                }
            }

            this.graphs.Add(tab);
            view.AddTab(tab.Views, panel.NumCols, sim);
        }

        /// <summary>
        /// Forces the equivalent graphs in each tab to use the same axes.
        /// ie. The LAI graphs in each simulation will have the same axes.
        /// </summary>
        private void StandardiseAxes()
        {
            IStorageReader reader = GetStorage();

            // Loop over each graph. ie if each tab contains five
            // graphs, then loop over these five graphs.
            int graphsPerPage = panel.Cache.First().Value.Count;
            for (int i = 0; i < graphsPerPage; i++)
            {
                // Get all graph series for this graph from each simulation.
                // ie. get the data behind each lai graph in each simulation.
                List<SeriesDefinition> series = panel.Cache.Values.SelectMany(v => v[i]).ToList();

                // Now draw all these series onto a single graph.
                GraphPresenter graphPresenter = new GraphPresenter();
                GraphView graphView = new GraphView(view as ViewBase);
                presenter.ApsimXFile.Links.Resolve(graphPresenter);
                graphPresenter.Attach(graphs[0].Graphs[i], graphView, presenter);
                graphPresenter.DrawGraph(series);

                Axis[] axes = graphView.Axes.ToArray(); // This should always be length 2
                foreach (GraphTab tab in graphs)
                    FormatAxes(tab.Views[i], axes);
            }
        }

        /// <summary>
        /// Force a graph to use a given set of axes.
        /// </summary>
        /// <param name="graphView">Graph view to be modified.</param>
        /// <param name="axes">Axes to use.</param>
        private void FormatAxes(GraphView graphView, Axis[] axes)
        {
            foreach (Axis axis in axes)
            {
                graphView.FormatAxis(axis.Type,
                                     graphView.AxisTitle(axis.Type),
                                     axis.Inverted,
                                     axis.Minimum,
                                     axis.Maximum,
                                     axis.Interval,
                                     axis.CrossesAtZero);
            }
        }

        /// <summary>
        /// Gets a reference to the data store.
        /// </summary>
        private IStorageReader GetStorage()
        {
            return (Apsim.Find(panel, typeof(IDataStore)) as IDataStore).Reader;
        }

        /// <summary>
        /// Invoked when the graph panel's properties are modified. Refreshes each tab.
        /// </summary>
        /// <param name="changedModel"></param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == panel || Apsim.ChildrenRecursively(panel).Contains(changedModel as Model))
                Refresh();
        }

        private class GraphTab
        {
            public List<GraphView> Views { get; set; }

            public List<GraphPresenter> Presenters { get; set; }

            public List<Graph> Graphs { get; set; }

            public string SimulationName { get; set; }

            public GraphTab(string simulationName)
            {
                Views = new List<GraphView>();
                Presenters = new List<GraphPresenter>();
                Graphs = new List<Graph>();

                SimulationName = simulationName;
            }

            public void AddGraph(GraphView view, GraphPresenter presenter, Graph chart)
            {
                Views.Add(view);
                Presenters.Add(presenter);
                Graphs.Add(chart);
            }
        }
    }
}
