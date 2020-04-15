using APSIM.Shared.Utilities;
using Models.Core;
using Models;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Views;
using ApsimNG.EventArguments;

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
        /// Background thread responsible for refreshing the view.
        /// </summary>
        private BackgroundWorker processingThread;

        private WorkerStatus status = new WorkerStatus();

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
            this.view.GraphViewCreated += ModifyGraphView;

            properties = new PropertyPresenter();
            properties.Attach(panel, this.view.PropertiesGrid, presenter);

            processingThread = new BackgroundWorker();
            processingThread.DoWork += WorkerThread;
            processingThread.RunWorkerCompleted += OnProcessingFinished;
            processingThread.WorkerSupportsCancellation = true;

            processingThread.RunWorkerAsync();
        }

        /// <summary>
        /// Detaches the model from the view.
        /// </summary>
        public void Detach()
        {
            processingThread.CancelAsync();
            processingThread.DoWork -= WorkerThread;

            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            this.view.GraphViewCreated -= ModifyGraphView;
            ClearGraphs();
            properties.Detach();
        }

        private void Refresh()
        {
            lock (status)
            {
                if (status.IsWorking)
                {
                    status.Restart = true;
                    processingThread.CancelAsync();
                }
                else
                    processingThread.RunWorkerAsync();
            }
        }

        private void WorkerThread(object sender, DoWorkEventArgs e)
        {
            lock (status)
                status.IsWorking = true;

            ClearGraphs();
            Graph[] graphs = Apsim.Children(panel, typeof(Graph)).Cast<Graph>().ToArray();

            IGraphPanelScript script = panel.Script;
            if (script != null)
            {
                IStorageReader reader = GetStorage();
                string[] simNames = script.GetSimulationNames(reader, panel);
                if (simNames != null)
                {
                    foreach (string sim in simNames)
                    {
                        CreatePageOfGraphs(sim, graphs);

                        if (processingThread.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
        }

        private void OnProcessingFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                bool restart = false;
                lock (status)
                {
                    status.IsWorking = false;

                    if (status.Restart)
                    {
                        restart = true;
                        status.Restart = false;
                    }
                }

                if (restart)
                    processingThread.RunWorkerAsync();
                else if (e.Error != null)
                    presenter.MainPresenter.ShowError(e.Error);
                else if (!e.Cancelled)
                {
                    // The worker thread has finished. Now standardise the axes (if necessary).
                    // This will freeze the UI thread while working, but it's easier than the
                    // alternative which is to have certain parts of this running on the main
                    // thread, and certain parts running on the worker thread. In such an
                    // implementation, large chunks of functionality would need to be moved
                    // into the view and the synchronisation would be a nightmare.
                    if (panel.SameXAxes || panel.SameYAxes)
                    {
                        presenter.MainPresenter.ShowWaitCursor(true);
                        StandardiseAxes();
                        presenter.MainPresenter.ShowWaitCursor(false);
                    }
                    int numGraphs = graphs.SelectMany(g => g.Graphs).Count();
                    presenter.MainPresenter.ShowMessage($"{panel.Name}: finished loading {numGraphs} graphs.", Simulation.MessageType.Information);
                }
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        private void ClearGraphs()
        {
            if (graphs != null)
                foreach (GraphTab tab in graphs)
                    foreach (GraphPresenter graphPresenter in tab.Graphs.Select(g => g.Presenter))
                        graphPresenter?.Detach();

            graphs.Clear();
            view.RemoveGraphTabs();
        }

        private void CreatePageOfGraphs(string sim, Graph[] graphs)
        {
            IStorageReader storage = GetStorage();
            GraphTab tab = new GraphTab(sim, this.presenter);
            for (int i = 0; i < graphs.Length; i++)
            {
                Graph graph = ReflectionUtilities.Clone(graphs[i]) as Graph;
                graph.Parent = panel;
                Apsim.ParentAllChildren(graph);

                if (panel.LegendOutsideGraph)
                    graph.LegendOutsideGraph = true;

                if (panel.LegendOrientation != GraphPanel.LegendOrientationType.Default)
                    graph.LegendOrientation = (Graph.LegendOrientationType)Enum.Parse(typeof(Graph.LegendOrientationType), panel.LegendOrientation.ToString());

                if (graph != null && graph.Enabled)
                {
                    // Apply transformation to graph.
                    panel.Script.TransformGraph(graph, sim);

                    if (panel.LegendPosition != GraphPanel.LegendPositionType.Default)
                        graph.LegendPosition = (Graph.LegendPositionType)Enum.Parse(typeof(Graph.LegendPositionType), panel.LegendPosition.ToString());

                    // Create and fill cache entry if it doesn't exist.
                    if (!panel.Cache.ContainsKey(sim) || panel.Cache[sim].Count <= i)
                    {
                        try
                        {
                            int x = storage.GetSimulationID(sim);
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new Exception($"Illegal simulation name: '{sim}'. Try running the simulation, and if that doesn't fix it, there is a problem with your config script.");
                        }
                        List<SeriesDefinition> definitions = graph.GetDefinitionsToGraph(storage, new List<string>() { sim });
                        if (!panel.Cache.ContainsKey(sim))
                            panel.Cache.Add(sim, new Dictionary<int, List<SeriesDefinition>>());

                        panel.Cache[sim][i] = definitions;
                    }
                    tab.AddGraph(graph, panel.Cache[sim][i]);
                }

                if (processingThread.CancellationPending)
                    return;
            }

            this.graphs.Add(tab);
            view.AddTab(tab, panel.NumCols);
        }

        /// <summary>
        /// Forces the equivalent graphs in each tab to use the same axes.
        /// ie. The LAI graphs in each simulation will have the same axes.
        /// </summary>
        private void StandardiseAxes()
        {
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
                graphPresenter.Attach(graphs[0].Graphs[i].Graph, graphView, presenter);
                graphPresenter.DrawGraph(series);

                Axis[] axes = graphView.Axes.ToArray(); // This should always be length 2
                Axis[] xAxes = axes.Where(a => a.Type == Axis.AxisType.Bottom || a.Type == Axis.AxisType.Top).ToArray();
                Axis[] yAxes = axes.Where(a => a.Type == Axis.AxisType.Left|| a.Type == Axis.AxisType.Right).ToArray();

                foreach (GraphTab tab in graphs)
                {
                    if (tab.Graphs[i].View != null)
                    {
                        if (panel.SameXAxes)
                            FormatAxes(tab.Graphs[i].View, xAxes);
                        if (panel.SameYAxes)
                            FormatAxes(tab.Graphs[i].View, yAxes);
                    }
                }
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

        /// <summary>
        /// Called whenever the view creates a graph. Allows for modifications
        /// to the graph view to be applied.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void ModifyGraphView(object sender, CustomDataEventArgs<IGraphView> args)
        {
            // n.b. this will be called on the main thread.
            if (panel.HideTitles)
                args.Data.FormatTitle(null);
            args.Data.FontSize = panel.FontSize;
            args.Data.MarkerSize = panel.MarkerSize;
        }

        private class WorkerStatus
        {
            public bool IsWorking { get; set; }
            public bool Restart { get; set; }
        }

        public class PanelGraph
        {
            public GraphView View { get; set; }
            public GraphPresenter Presenter { get; set; }
            public Graph Graph { get; set; }
            public List<SeriesDefinition> Cache { get; set; }

            public PanelGraph(Graph chart, List<SeriesDefinition> cache)
            {
                Graph = chart;
                Cache = cache;
            }
        }

        public class GraphTab
        {
            public List<PanelGraph> Graphs { get; set; }
            public string SimulationName { get; set; }
            public ExplorerPresenter Presenter { get; set; }

            public GraphTab(string simulationName, ExplorerPresenter presenter)
            {
                Graphs = new List<PanelGraph>();
                SimulationName = simulationName;
                Presenter = presenter;
            }

            public void AddGraph(Graph chart, List<SeriesDefinition> cache)
            {
                Graphs.Add(new PanelGraph(chart, cache));
            }
        }
    }
}
