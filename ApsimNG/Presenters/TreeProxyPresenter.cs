using System.Collections.Generic;
using Models.Agroforestry;
using Models.Soils;
using UserInterface.Views;
using Models.Utilities;
using Gtk.Sheet;

namespace UserInterface.Presenters
{
    /// <summary>
    /// The tree proxy presenter
    /// </summary>
    public class TreeProxyPresenter : IPresenter
    {
        /// <summary>
        /// The forestry model object.
        /// </summary>
        private TreeProxy forestryModel;

        /// <summary>
        /// The viewer for the forestry model
        /// </summary>
        private TreeProxyView forestryViewer;

        /// <summary>
        /// The property presenter
        /// </summary>
        private PropertyPresenter propertyPresenter;

        /// <summary>
        /// Presenter for the view's spatial data grid.
        /// </summary>
        private GridPresenter spatialGridPresenter;

        /// <summary>
        /// Presenter for the view's temporal data grid.
        /// </summary>
        private GridPresenter temporalGridPresenter;

        /// <summary>
        /// The explorer presenter.
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// Attach the presenter to the model and view.
        /// </summary>
        /// <param name="model">The model object.</param>
        /// <param name="view">The view object.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            forestryModel = model as TreeProxy;
            forestryViewer = view as TreeProxyView;
            presenter = explorerPresenter;

            presenter.CommandHistory.ModelChanged += OnModelChanged;

            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(forestryModel, forestryViewer.Constants, explorerPresenter);

            spatialGridPresenter = new GridPresenter();
            spatialGridPresenter.Attach(forestryModel.Spatial, forestryViewer.SpatialDataGrid, explorerPresenter);
            spatialGridPresenter.CellChanged += OnCellChanged;
            spatialGridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            temporalGridPresenter = new GridPresenter();
            temporalGridPresenter.Attach(forestryModel, forestryViewer.TemporalDataGrid, explorerPresenter);
            temporalGridPresenter.CellChanged += OnCellChanged;
            temporalGridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            Refresh();
        }

        /// <summary>
        /// Detach this presenter
        /// </summary>
        public void Detach()
        {
            spatialGridPresenter.CellChanged -= OnCellChanged;
            temporalGridPresenter.CellChanged -= OnCellChanged;

            spatialGridPresenter.Detach();
            temporalGridPresenter.Detach();
            propertyPresenter.Detach();

            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Attach the model
        /// </summary>
        public void Refresh()
        {
            Physical physical = forestryModel.Parent.FindDescendant<Physical>();
            forestryViewer.SoilMidpoints = physical.DepthMidPoints;
            forestryViewer.DrawGraphs(forestryModel.Spatial);
            propertyPresenter.RefreshView(forestryModel);
        }

        /// <summary>
        /// Invoked when the model has been changed via the undo command.
        /// </summary>
        /// <param name="changedModel">The model which has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            Refresh();
        }

        /// <summary>Invoked when a grid cell has changed.</summary>
        /// <param name="dataProvider">The provider that contains the data.</param>
        /// <param name="colIndices">The indices of the columns of the cells that were changed.</param>
        /// <param name="rowIndices">The indices of the rows of the cells that were changed.</param>
        /// <param name="values">The cell values.</param>
        private void OnCellChanged(IDataProvider dataProvider, int[] colIndices, int[] rowIndices, string[] values)
        {
            Refresh();
        }
    }
}