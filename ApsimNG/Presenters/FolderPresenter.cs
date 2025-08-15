namespace UserInterface.Presenters
{
    using System.Collections.Generic;
    using Models.Core;
    using Models;
    using Views;
    using Models.Storage;
    using System.Linq;

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
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            IModel folder = model as IModel;

            List<GraphView> views = new List<GraphView>();

            var storage = folder.Node.Find<IDataStore>();
            var graphPage = new GraphPage();
            graphPage.Graphs.AddRange(folder.FindAllChildren<Graph>().Where(g => g.Enabled));

            if (storage != null && graphPage.Graphs.Any())
            {
                foreach (var graphSeries in graphPage.GetAllSeriesDefinitions(folder, storage.Reader))
                {
                    GraphView graphView = new GraphView(null);
                    graphView.DisableScrolling();
                    GraphPresenter presenter = new GraphPresenter();
                    explorerPresenter.ApsimXFile.Links.Resolve(presenter);
                    presenter.Attach(graphSeries.Graph, graphView, explorerPresenter, graphSeries.SeriesDefinitions);
                    presenters.Add(presenter);
                    views.Add(graphView);
                }

                if (views.Count > 0)
                {
                    (view as IFolderView).SetContols(views);
                }
            }
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            foreach (GraphPresenter presenter in presenters)
            {
                presenter.Detach();
            }

            presenters.Clear();
        }
    }
}
