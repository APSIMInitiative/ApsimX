namespace UserInterface.Presenters
{
    using Models.Interfaces;
    using System.Drawing;
    using System.IO;
    using Views;

    /// <summary>
    /// This presenter connects an instance of a Model with a 
    /// UserInterface.Views.DrawingView
    /// </summary>
    public class DirectedGraphPresenter : IPresenter, IExportable
    {
        /// <summary>The view object</summary>
        private IVisualiseAsDirectedGraph model;

        /// <summary>The view object</summary>
        private DirectedGraphView view;
        
        /// <summary>The explorer presenter used</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The model to use</param>
        /// <param name="view">The view for this presenter</param>
        /// <param name="explorerPresenter">The explorer presenter used</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = view as DirectedGraphView;
            this.explorerPresenter = explorerPresenter;
            this.model = model as IVisualiseAsDirectedGraph;

            // Tell the view to populate the axis.
            this.PopulateView();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            model.DirectedGraphInfo = view.DirectedGraph;
        }

        /// <summary>Export the view object to a file and return the file name</summary>
        public string ExportToPNG(string folder)
        {
            Image image = view.Export();

            
            string fileName = Path.ChangeExtension(Path.Combine(folder, Path.GetRandomFileName()), ".png");
            image.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return fileName;
        }

        /// <summary>Populate the view object</summary>
        private void PopulateView()
        {
            view.DirectedGraph = model.DirectedGraphInfo;
        }

    }
}
