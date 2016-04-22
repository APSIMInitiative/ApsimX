// -----------------------------------------------------------------------
// <copyright file="MapPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using Models;
    using Models.Core;
    using System;
    using System.Drawing;
    using System.IO;
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
        /// The parent explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

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
            this.explorerPresenter = explorerPresenter;

            // Tell the view to populate the axis.
            this.PopulateView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
        }

        /// <summary>
        /// Populate the view.
        /// </summary>
        private void PopulateView()
        {
            view.ShowMap(map.GetCoordinates());
        }

        /// <summary>Export the map to PDF</summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        internal string ExportToPDF(string folder)
        {
            string path = Apsim.FullPath(map).Replace(".Simulations.", "");
            string fileName = Path.Combine(folder, path + ".png");

            Image rawImage = view.Export();
            rawImage.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

            return fileName;
        }
    }
}
