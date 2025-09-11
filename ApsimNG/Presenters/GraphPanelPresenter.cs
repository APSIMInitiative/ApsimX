using APSIM.Shared.Utilities;
using Models.Core;
using Models;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Views;
using ApsimNG.EventArguments;
using APSIM.Shared.Graphing;
using Models.Core.Run;
using System.Threading;

namespace UserInterface.Presenters
{
    public sealed class GraphPanelPresenter : IPresenter, IDisposable
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
        private Task processingThread;

        /// <summary>
        /// Cancellation token used to cancel the work.
        /// </summary>
        private CancellationTokenSource cts = new CancellationTokenSource();

        private DateTime startTime;

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
            properties.Attach(panel, this.view.PropertiesView, presenter);

            (processingThread, startTime) = StartWork();
        }

        /// <summary>
        /// Start drawing graphs in a background thread and return a task
        /// instance representing this task.
        /// </summary>
        private (Task, DateTime) StartWork()
        {
            cts = new CancellationTokenSource();
            Task task = Task.Run(WorkerThread).ContinueWith(_ => OnProcessingFinished());
            return (task, DateTime.Now);
        }

        /// <summary>
        /// Detaches the model from the view.
        /// </summary>
        public void Detach()
        {
            cts.Cancel();
            processingThread.Wait();

            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            this.view.GraphViewCreated -= ModifyGraphView;
            ClearGraphs();
            properties.Detach();
        }

        private void Refresh()
        {
            cts.Cancel();
            processingThread.Wait();
            (processingThread, startTime) = StartWork();
        }

        private void WorkerThread()
        {
            ClearGraphs();
            Graph[] graphs = panel.Node.FindChildren<Graph>().ToArray();

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

                        if (cts.Token.IsCancellationRequested)
                            return;
                    }
                }
            }
        }

        private void OnProcessingFinished()
        {
            try
            {
                if (processingThread.Exception != null)
                    presenter.MainPresenter.ShowError(processingThread.Exception);
                else if (!cts.Token.IsCancellationRequested)
                {
                    // The worker thread has finished. Now standardise the axes (if necessary).
                    // There are a few complications here:
                    // - This must be run on the UI thread
                    // - This must not run until after the graph panel view has
                    //   finished processing all graph tabs (ie it has set the
                    //   view objects to GraphView instances).
                    // I've opted for the simple approach of Gtk.Application.Invoke().
                    // Arguably we shouldn't be relying on the Gtk API here but
                    // this is going to be much simpler than the alternatives.
                    if (panel.SameXAxes || panel.SameYAxes)
                        Gtk.Application.Invoke((_, __) => StandardiseAxes());

                    int numGraphs = graphs.SelectMany(g => g.Graphs).Count();
                    TimeSpan elapsed = DateTime.Now - startTime;
                    presenter.MainPresenter.ShowMessage($"{panel.Name}: finished loading {numGraphs} graphs in {elapsed.TotalSeconds}s.", Simulation.MessageType.Information);
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
            if (!panel.Cache.ContainsKey(sim))
                panel.Cache.Add(sim, new Dictionary<int, List<SeriesDefinition>>());

            IStorageReader storage = GetStorage();
            GraphPage graphPage = new GraphPage();

            // Configure graphs by applying user overrides.
            for (int i = 0; i < graphs.Length; i++)
            {
                if (graphs[i] != null && graphs[i].Enabled)
                    graphPage.Graphs.Add(ConfigureGraph(graphs[i], sim));

                if (cts.Token.IsCancellationRequested)
                    return;
            }

            // If any sims in this tab are missing from the cache, then populate
            // the cache via the GraphPage instance.
            if (panel.Cache[sim].Count != graphs.Length)
            {
                // Read data from storage.
                IReadOnlyList<GraphPage.GraphDefinitionMap> definitions = graphPage.GetAllSeriesDefinitions(panel, storage, new List<string>() { sim }).ToList();

                // Now populate the cache for this simulation. The definitions
                // should - in theory - be the same length as the graphs array.
                for (int i = 0; i < graphs.Length; i++)
                {
                    GraphPage.GraphDefinitionMap definition = definitions[i];
                    panel.Cache[sim][i] = definition.SeriesDefinitions;
                    if (cts.Token.IsCancellationRequested)
                        return;
                }
            }

            // Finally, add the graphs to the tab.
            GraphTab tab = new GraphTab(sim, this.presenter);
            for (int i = 0; i < graphPage.Graphs.Count; i++)
                tab.AddGraph(graphPage.Graphs[i], panel.Cache[sim][i]);

            this.graphs.Add(tab);
            view.AddTab(tab, panel.NumCols);
        }

        private Graph ConfigureGraph(Graph graph, string simulationName)
        {
            graph = (Graph)ReflectionUtilities.Clone(graph);
            graph.Parent = panel;
            graph.ParentAllDescendants();

            // Apply transformation to graph.
            panel.Script.TransformGraph(graph, simulationName);

            if (panel.LegendOutsideGraph)
                graph.LegendOutsideGraph = true;

            if (panel.LegendOrientation != GraphPanel.LegendOrientationType.Default)
                graph.LegendOrientation = (LegendOrientation)Enum.Parse(typeof(LegendOrientation), panel.LegendOrientation.ToString());

            if (panel.LegendPosition != GraphPanel.LegendPositionType.Default)
                graph.LegendPosition = (LegendPosition)Enum.Parse(typeof(LegendPosition), panel.LegendPosition.ToString());
            return graph;
        }

        private GraphPage.GraphDefinitionMap FindMatchingDefinition(IReadOnlyList<GraphPage.GraphDefinitionMap> allDefinitions, Graph graph)
        {
            GraphPage.GraphDefinitionMap match = allDefinitions.FirstOrDefault(m => m.Graph == graph);
            if (match == null)
                throw new KeyNotFoundException($"Graph {graph.Name} not found. Programming error...");
            return match;
        }

        /// <summary>
        /// Forces the equivalent graphs in each tab to use the same axes.
        /// ie. The LAI graphs in each simulation will have the same axes.
        /// </summary>
        private void StandardiseAxes()
        {
            if (!panel.Cache.Any())
                return;

            // Loop over each graph. ie if each tab contains five
            // graphs, then loop over these five graphs.
            int graphsPerPage = panel.Cache.First().Value.Count;
            for (int i = 0; i < graphsPerPage; i++)
            {
                if (panel.SameXAxes)
                {
                    StandardiseAxis(AxisPosition.Bottom, i);
                    StandardiseAxis(AxisPosition.Top, i);
                }

                if (panel.SameYAxes)
                {
                    StandardiseAxis(AxisPosition.Left, i);
                    StandardiseAxis(AxisPosition.Right, i);
                }
            }
        }

        /// <summary>
        /// Modify the nth graph in each tab such that it has the same axis
        /// max and min on the given axis.
        /// </summary>
        /// <param name="axisType">The axis to be modified.</param>
        /// <param name="index">The index of the graph in each tab to be modified.</param>
        private void StandardiseAxis(AxisPosition axisType, int index)
        {
            double max = GetAxisMax(axisType, index);
            if (!double.IsNaN(max))
                SetAxisMax(axisType, index, max);

            double min = GetAxisMin(axisType, index);
            if (!double.IsNaN(min))
                SetAxisMin(axisType, index, min);
        }

        /// <summary>
        /// Set the axis minimum for the nth graph in each graph tab.
        /// </summary>
        /// <param name="axisType">The type of axis to be modified.</param>
        /// <param name="graphIndex">The index of the graph in each tab to be modified.</param>
        /// <param name="value">The new axis minimum value.</param>
        private void SetAxisMin(AxisPosition axisType, int graphIndex, double value)
        {
            foreach (GraphView view in GetGraphViews(graphIndex))
                view.SetAxisMin(value, axisType);
        }

        /// <summary>
        /// Set the axis maximum for the nth graph in each graph tab.
        /// </summary>
        /// <param name="axisType">The type of axis to be modified.</param>
        /// <param name="graphIndex">The index of the graph in each tab to be modified.</param>
        /// <param name="value">The new axis maximum value.</param>
        private void SetAxisMax(AxisPosition axisType, int graphIndex, double value)
        {
            foreach (GraphView view in GetGraphViews(graphIndex))
                view.SetAxisMax(value, axisType);
        }

        /// <summary>
        /// Get the graph view instances for the nth graph in each tab.
        /// </summary>
        /// <param name="index">Index of the graph in each tab.</param>
        private IEnumerable<GraphView> GetGraphViews(int index)
        {
            return graphs.Select(t => t.Graphs[index].View);
        }

        /// <summary>
        /// Get the smallest value on the given axis of the given graph across
        /// all tabs.
        /// </summary>
        /// <param name="axisType">The axis type (e.g. top, left, ...).</param>
        /// <param name="graphIndex">The index of the graph to be examined in each tab.</param>
        private double GetAxisMin(AxisPosition axisType, int graphIndex) => graphs.Min(t => t.Graphs[graphIndex].View.AxisMinimum(axisType));

        /// <summary>
        /// Get the largest value on the given axis of the given graph across
        /// all tabs.
        /// </summary>
        /// <param name="axisType">The axis type (e.g. top, left, ...).</param>
        /// <param name="graphIndex">The index of the graph to be examined in each tab.</param>
        private double GetAxisMax(AxisPosition axisType, int graphIndex) => graphs.Max(t => t.Graphs[graphIndex].View.AxisMaximum(axisType));

        /// <summary>
        /// Force a graph to use a given set of axes.
        /// </summary>
        /// <param name="graphView">Graph view to be modified.</param>
        /// <param name="axes">Axes to use.</param>
        private void FormatAxes(GraphView graphView, Axis[] axes)
        {
            foreach (Axis axis in axes)
            {
                graphView.FormatAxis(axis.Position,
                                     graphView.AxisTitle(axis.Position),
                                     axis.Inverted,
                                     axis.Minimum ?? double.NaN,
                                     axis.Maximum ?? double.NaN,
                                     axis.Interval ?? double.NaN,
                                     axis.CrossesAtZero, false);
            }
        }

        /// <summary>
        /// Gets a reference to the data store.
        /// </summary>
        private IStorageReader GetStorage()
        {
            return (panel.Node.Find<IDataStore>()).Reader;
        }

        /// <summary>
        /// Invoked when the graph panel's properties are modified. Refreshes each tab.
        /// </summary>
        /// <param name="changedModel"></param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == panel || panel.Node.FindChildren<IModel>(recurse: true).Contains(changedModel as Model))
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

        public void Dispose()
        {
            if (processingThread != null)
                processingThread.Dispose();
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
