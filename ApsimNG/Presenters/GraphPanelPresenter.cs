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
        /// Graph presenters.
        /// </summary>
        private List<GraphPresenter> graphPresenters;

        /// <summary>
        /// Graph views.
        /// </summary>
        private List<GraphView> graphViews;

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
            }
            finally
            {
                presenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        private void ClearGraphs()
        {
            if (graphPresenters != null)
            {
                foreach (GraphPresenter graph in graphPresenters)
                    graph.Detach();
            }
            view.RemoveGraphTabs();
            graphPresenters = new List<GraphPresenter>();
            graphViews = new List<GraphView>();
        }

        private void CreatePageOfGraphs(string sim, Graph[] graphs)
        {
            List<GraphView> views = new List<GraphView>();
            foreach (Graph graph in graphs)
            {
                if (graph.Enabled)
                {
                    GraphView graphView = new GraphView();
                    GraphPresenter presenter = new GraphPresenter();
                    presenter.SimulationFilter = new List<string>() { sim };

                    panel.Script.TransformGraph(graph, sim);

                    this.presenter.ApsimXFile.Links.Resolve(presenter);
                    if (panel.Cache.ContainsKey(sim))
                        presenter.Attach(graph, graphView, this.presenter, panel.Cache[sim]);
                    else
                    {
                        presenter.Attach(graph, graphView, this.presenter);
                        panel.Cache[sim] = presenter.SeriesDefinitions;
                    }

                    graphPresenters.Add(presenter);
                    graphViews.Add(graphView);
                    views.Add(graphView);
                }
            }

            view.AddTab(views, panel.NumCols, sim);
        }

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

        private Axis[] GetStandardisedAxes(Graph[] graphs)
        {
            IStorageReader reader = GetStorage();
            List<SeriesDefinition> seriesDefinitions = graphs.SelectMany(g => g.GetDefinitionsToGraph(reader)).ToList();
            GraphPresenter graphPresenter = new GraphPresenter();
            GraphView graphView = new GraphView(view as ViewBase);

            presenter.ApsimXFile.Links.Resolve(graphPresenter);
            graphPresenter.Attach(graphs[0], graphView, presenter);
            graphPresenter.DrawGraph(seriesDefinitions);

            return graphView.Axes;
        }

        private IStorageReader GetStorage()
        {
            return (Apsim.Find(panel, typeof(IDataStore)) as IDataStore).Reader;
        }

        private void OnModelChanged(object changedModel)
        {
            if (changedModel == panel || changedModel == panel.Script || Apsim.ChildrenRecursively(panel).Contains(changedModel as Model))
                Refresh();
        }
    }
}
