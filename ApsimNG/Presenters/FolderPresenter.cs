// -----------------------------------------------------------------------
// <copyright file="FolderPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
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
        private List<GraphPresenter> presenters = new List<GraphPresenter>();

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            IModel folder = model as IModel;

            List<GraphView> views = new List<GraphView>();

            foreach (Graph graph in Apsim.Children(folder, typeof(Graph)))
            {
                GraphView graphView = new GraphView(null /* TBI */);
                GraphPresenter presenter = new GraphPresenter();
                presenter.Attach(graph, graphView, explorerPresenter);
                presenters.Add(presenter);
                views.Add(graphView);
            }

            if (views.Count > 0)
                (view as IFolderView).SetContols(views);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            foreach (GraphPresenter presenter in presenters)
                presenter.Detach();

            presenters.Clear();
        }

    }
}
