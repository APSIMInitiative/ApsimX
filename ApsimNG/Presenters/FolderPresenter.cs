namespace UserInterface.Presenters
{
    using System.Collections.Generic;
    using Models.Core;
    using Models;
    using Views;
    using Models.Storage;
    using System.Linq;
    using System;

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

        /// <summary>Bound the queries and widgets created when opening a folder.</summary>
        private const int graphsPerPage = 6;

        private List<Graph> graphs;
        private IModel folder;
        private IFolderView folderView;
        private ExplorerPresenter explorerPresenter;
        private IDataStore storage;
        private int pageIndex;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            folder = model as IModel;
            folderView = view as IFolderView;
            this.explorerPresenter = explorerPresenter;
            storage = folder.Node.Find<IDataStore>();
            graphs = folder.Node.FindChildren<Graph>().Where(g => g.Enabled).ToList();
            pageIndex = 0;
            folderView.PreviousPage += OnPreviousPage;
            folderView.NextPage += OnNextPage;
            if (storage != null && graphs.Any())
                DisplayPage(0);
            else
                folderView.SetPage(0, 0, 0);
        }

        /// <summary>Read and render only the requested page, leaving other graphs unloaded.</summary>
        private void DisplayPage(int requestedPage)
        {
            if (requestedPage < 0 || requestedPage * graphsPerPage >= graphs.Count)
                return;

            var graphPage = new GraphPage();
            graphPage.Graphs.AddRange(graphs.Skip(requestedPage * graphsPerPage).Take(graphsPerPage));
            var definitions = graphPage.GetAllSeriesDefinitions(folder, storage.Reader);
            ClearPresenters();
            folderView.SetContols(new List<GraphView>());
            var views = new List<GraphView>();
            try
            {
                foreach (var graphSeries in definitions)
                {
                    GraphView graphView = new GraphView(null);
                    views.Add(graphView);
                    graphView.DisableScrolling();
                    GraphPresenter presenter = new GraphPresenter();
                    explorerPresenter.ApsimXFile.Links.Resolve(presenter);
                    presenters.Add(presenter);
                    presenter.Attach(graphSeries.Graph, graphView, explorerPresenter, graphSeries.SeriesDefinitions);
                }
                folderView.SetContols(views);
                pageIndex = requestedPage;
                folderView.SetPage(pageIndex * graphsPerPage, views.Count, graphs.Count);
            }
            catch
            {
                ClearPresenters();
                folderView.SetContols(new List<GraphView>());
                foreach (var graphView in views)
                    graphView.Dispose();
                folderView.SetPage(0, 0, 0);
                throw;
            }
        }

        private void OnPreviousPage(object sender, EventArgs e) => ChangePage(pageIndex - 1);

        private void OnNextPage(object sender, EventArgs e) => ChangePage(pageIndex + 1);

        private void ChangePage(int requestedPage)
        {
            try
            {
                DisplayPage(requestedPage);
            }
            catch (Exception error)
            {
                explorerPresenter.MainPresenter.ShowError(error);
            }
        }

        private void ClearPresenters()
        {
            foreach (GraphPresenter presenter in presenters)
                presenter.Detach();
            presenters.Clear();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            folderView.PreviousPage -= OnPreviousPage;
            folderView.NextPage -= OnNextPage;
            ClearPresenters();
            folderView.SetContols(new List<GraphView>());
        }
    }
}
