// -----------------------------------------------------------------------
// <copyright file="MapPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System.Drawing;
    using System.IO;
    using Models;
    using Models.Core;
    using Views;

    /// <summary>
    /// This presenter connects an instance of a Model.Map with a 
    /// UserInterface.Views.MapView
    /// </summary>
    public class MapPresenter : IPresenter
    {
        /// <summary>
        /// The axis model
        /// </summary>
        private Map map;

        /// <summary>
        /// The axis view
        /// </summary>
        private IMapView view;

        /// <summary>
        /// Attach the specified Model and View.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.map = model as Map;
            this.view = view as MapView;

            // Tell the view to populate the axis.
            this.PopulateView();
            this.view.Zoom = this.map.Zoom;
            this.view.Center = this.map.Center;
            this.view.ZoomChanged += this.OnZoomChanged;
            this.view.PositionChanged += this.OnPositionChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.view.StoreSettings();
            this.view.ZoomChanged -= this.OnZoomChanged;
            this.view.PositionChanged -= this.OnPositionChanged;
        }

        /// <summary>Export the map to PDF</summary>
        /// <param name="folder">The working directory name</param>
        /// <returns>The filename string</returns>
        internal string ExportToPDF(string folder)
        {
            string path = Apsim.FullPath(this.map).Replace(".Simulations.", string.Empty);
            string fileName = Path.Combine(folder, path + ".png");

            Image rawImage = this.view.Export();
            rawImage.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return fileName;
        }

        /// <summary>
        /// Populate the view.
        /// </summary>
        private void PopulateView()
        {
            this.view.ShowMap(this.map.GetCoordinates());
        }

        /// <summary>
        /// Respond to changes in the map zoom level
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnZoomChanged(object sender, System.EventArgs e)
        {
            this.map.Zoom = this.view.Zoom;
        }

        /// <summary>
        /// Respond to changes in the map position by saving the new position
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnPositionChanged(object sender, System.EventArgs e)
        {
            this.map.Center = this.view.Center;
        }
    }
}
