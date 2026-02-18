using Models.Core;
using UserInterface.Views;
using Gtk.Sheet;
using System.Collections.Generic;
using Models.Functions;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.Interfaces;
using Models.Soils.NutrientPatching;
using Models.WaterModel;

namespace UserInterface.Presenters
{
    /// <summary>A presenter for the soil profile models.</summary>
    public class QuadPresenter : IPresenter
    {
        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The base view.</summary>
        private QuadView view = null;

        /// <summary>The model.</summary>
        private IModel model;

        /// <summary>Sub-presenters that are added to this presenter</summary>
        private List<IPresenter> presenters;

        /// <summary>Default constructor</summary>
        public QuadPresenter() {}

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="v">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = v as QuadView;
            this.explorerPresenter = explorerPresenter;
            this.presenters = new List<IPresenter>();

            if (this.view == null)
                throw new System.Exception("QuadPresenter only works with a QuadView");

            if (model is XYPairs)
                CreateLayoutXYPairs();
            else if (model is Physical)
                CreateLayoutPhysical();
            else if (model is WaterBalance)
                CreateLayoutWaterBalance();
            else
                CreateLayoutGeneric();

            Refresh();
            ConnectEvents();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            foreach (IPresenter presenter in presenters)
            {
                if (presenter is GridPresenter grid)
                    grid.Detach();
                else if (presenter is PropertyPresenter properties)
                    properties.Detach();
                else if (presenter is GraphPresenter2 graph)
                    graph.Detach();
            }
            view.Dispose();
        }

        /// <summary>Populate the graph with data.</summary>
        public void Refresh()
        {
            DisconnectEvents();
            foreach (IPresenter presenter in presenters)
            {
                if (presenter is GridPresenter grid)
                    grid.Refresh();
                else if (presenter is GraphPresenter2 graph)
                    graph.Refresh();
            }
            view.Refresh();
            ConnectEvents();
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            foreach (IPresenter presenter in presenters)
            {
                if (presenter is GridPresenter grid)
                    grid.CellChanged += OnCellChanged;
            }
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
            foreach (IPresenter presenter in presenters)
            {
                if (presenter is GridPresenter grid)
                    grid.CellChanged -= OnCellChanged;
            }
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

        private void AddGraph(WidgetPosition position)
        {
            ViewBase graphView = view.AddComponent(WidgetType.Graph, position);
            GraphPresenter2 graphPresenter = new GraphPresenter2();
            graphPresenter.Attach(model, graphView, explorerPresenter);
            presenters.Add(graphPresenter);
        }

        private void AddGrid(WidgetPosition position)
        {
            ViewBase gridContainer = view.AddComponent(WidgetType.Grid, WidgetPosition.TopLeft);
            GridPresenter gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });
            presenters.Add(gridPresenter);
        }

        private void AddText(WidgetPosition position, string text)
        {
            view.AddComponent(WidgetType.Text, WidgetPosition.BottomLeft);
            view.SetLabelText(text);
        }
        private void AddProperty(WidgetPosition position)
        {
            ViewBase propertyView = view.AddComponent(WidgetType.Property, WidgetPosition.TopRight);
            PropertyPresenter propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);
            presenters.Add(propertyPresenter);
        }

        private void CreateLayoutGeneric()
        {
            AddGrid(WidgetPosition.TopLeft);
            AddGraph(WidgetPosition.BottomRight);
            AddProperty(WidgetPosition.TopRight);
        }

        private void CreateLayoutXYPairs()
        {
            DescriptionAttribute descriptionName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;

            XYPairs xypairs = model as XYPairs;
            if (xypairs == null)
                throw new System.Exception($"Model {model.Name} is not an XY Pairs but is trying to use the XY Pairs view layout");
            
            string description = "";
            if (descriptionName != null)
                description = descriptionName.ToString();

            if (!string.IsNullOrEmpty(description))
                AddText(WidgetPosition.TopLeft, description);
            AddGrid(WidgetPosition.BottomLeft);
            AddGraph(WidgetPosition.TopRight);
        }

        private void CreateLayoutPhysical()
        {
            CreateLayoutGeneric();

            string warnings = "<span color=\"red\">Note: values in red are estimates only and needed for the simulation of soil temperature. Overwrite with local values wherever possible.</span>";
            AddText(WidgetPosition.BottomLeft, warnings);
            view.OverrideSlider(0.6);
        }

        private void CreateLayoutWaterBalance()
        {
            CreateLayoutGeneric();
            view.OverrideSlider(0.3);
        }
    }
}