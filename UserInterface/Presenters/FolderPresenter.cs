// -----------------------------------------------------------------------
// <copyright file="FolderPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using Models.Graph;
    using Views;
    using Models.Core;
    using System.Collections.Generic;
    using System.Windows.Forms;

    /// <summary>
    /// This presenter connects an instance of a folder model with a 
    /// folder view.
    /// </summary>
    public class FolderPresenter : IPresenter
    {
        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            IModel folder = model as IModel;

            List<UserControl> views = new List<UserControl>();

            foreach (Graph graph in Apsim.Children(folder, typeof(Graph)))
            {
                GraphView graphView = new GraphView();
                GraphPresenter presenter = new GraphPresenter();
                presenter.Attach(graph, graphView, explorerPresenter);
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
        }

    }
}
