using System.Collections.Generic;
using System.Reflection;
using Models.Core;
using Models;
using Models.Functions;
using APSIM.Shared.Graphing;
using Series = Models.Series;
using UserInterface.Views;
using Models.Utilities;
using Gtk.Sheet;
using APSIM.Shared.Utilities;

namespace UserInterface.Presenters
{
    /// <summary>A presenter for the soil profile models.</summary>
    public class QuadPresenter : IPresenter
    {
        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The base view.</summary>
        private ViewBase view = null;

        /// <summary>The model.</summary>
        private IModel model;

        /// <summary>The grid presenter.</summary>
        private GridPresenter gridPresenter;

        ///// <summary>The property presenter.</summary>
        private PropertyPresenter propertyPresenter;

        /// <summary>Graph.</summary>
        private GraphPresenter2 graphPresenter;

        /// <summary>Default constructor</summary>
        public QuadPresenter() {}

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="v">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            view = v as ViewBase;
            this.explorerPresenter = explorerPresenter;

            QuadView quadView = view as QuadView;
            if (quadView == null)
                throw new System.Exception("QuadPresenter only works with a QuadView");

            ViewBase gridContainer = quadView.AddComponent(WidgetType.Grid, WidgetPosition.TopLeft);
            gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });

            ViewBase propertyView = quadView.AddComponent(WidgetType.Property, WidgetPosition.TopRight);
            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            ViewBase graphView = quadView.AddComponent(WidgetType.Graph, WidgetPosition.BottomRight);
            graphPresenter = new GraphPresenter2();
            graphPresenter.Attach(model, graphView, explorerPresenter);

            ViewBase textView = quadView.AddComponent(WidgetType.Text, WidgetPosition.BottomLeft);
            quadView.SetLabelText("test");

            Refresh();
            ConnectEvents();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();

            if (this.gridPresenter != null)
                gridPresenter.Detach();

            if (this.propertyPresenter != null)
                propertyPresenter.Detach();

            view.Dispose();
        }

        /// <summary>Populate the graph with data.</summary>
        public void Refresh()
        {
            DisconnectEvents();
            graphPresenter.Refresh();
            ConnectEvents();
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            if (gridPresenter != null)
                gridPresenter.CellChanged += OnCellChanged;
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
            if (gridPresenter != null)
                gridPresenter.CellChanged -= OnCellChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
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

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            model = changedModel as IModel;
            Refresh();
        }
    }
}