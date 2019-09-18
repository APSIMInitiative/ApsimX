// -----------------------------------------------------------------------
// <copyright file="FolderPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models.Core;
    using Models.Graph;
    using Models.Storage;
    using Views;

    /// <summary>
    /// This presenter connects an instance of a folder model with a 
    /// folder view.
    /// </summary>
    public class FolderPresenter : IPresenter
    {
        /// <summary>
        /// The list of graph presenters
        /// </summary>
        private List<GraphPresenter> presenters = new List<GraphPresenter>();

        /// <summary>
        /// The folder model.
        /// </summary>
        private Folder folder;

        /// <summary>
        /// The explorer presenter.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// The view.
        /// </summary>
        private IFolderView view;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.folder = model as Folder;
            this.view = view as IFolderView;
            this.presenter = explorerPresenter;

            if (folder == null || view == null || presenter == null)
                throw new ArgumentException();
            DrawGraphs();

            this.presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            ClearGraphs();
        }

        public void Refresh()
        {
            ClearGraphs();
            DrawGraphs();
        }

        private void DrawGraphs()
        {
            List<GraphView> views = new List<GraphView>();
            Graph[] graphs = Apsim.Children(folder, typeof(Graph)).Cast<Graph>().ToArray();
            Axis[] axes = GetStandardisedAxes(graphs);

            foreach (Graph graph in graphs)
            {
                if (graph.Enabled)
                {
                    GraphView graphView = new GraphView();
                    GraphPresenter presenter = new GraphPresenter();

                    this.presenter.ApsimXFile.Links.Resolve(presenter);
                    presenter.Attach(graph, graphView, this.presenter);

                    if (folder.SameAxes)
                        FormatAxes(graphView, axes);

                    presenters.Add(presenter);
                    views.Add(graphView);
                }
            }

            if (views.Count > 0)
            {
                view.NumCols = folder.NumCols;
                view.SetControls(views);
            }
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
            IDataStore storage = Apsim.Find(folder, typeof(DataStore)) as IDataStore;
            return storage.Reader;
        }

        private void ClearGraphs()
        {
            foreach (GraphPresenter presenter in presenters)
                presenter.Detach();

            presenters.Clear();
        }

        private void OnModelChanged(object changedModel)
        {
            if (changedModel == folder)
                Refresh();
        }
    }
}
