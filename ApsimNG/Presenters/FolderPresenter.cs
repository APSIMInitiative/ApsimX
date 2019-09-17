// -----------------------------------------------------------------------
// <copyright file="FolderPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Graph;
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
        private IModel folder;

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
            this.folder = model as IModel;
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

            foreach (Graph graph in Apsim.Children(folder, typeof(Graph)))
            {
                if (graph.Enabled)
                {
                    GraphView graphView = new GraphView();
                    GraphPresenter presenter = new GraphPresenter();

                    this.presenter.ApsimXFile.Links.Resolve(presenter);
                    presenter.Attach(graph, graphView, this.presenter);
                    presenters.Add(presenter);
                    views.Add(graphView);
                }
            }

            if (views.Count > 0)
            {
                view.NumCols = (folder as Folder)?.NumCols ?? 2;
                view.SetControls(views);
            }
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
