using Models.Core;
using UserInterface.Views;
using Gtk.Sheet;
using System.Collections.Generic;
using Models.Functions;
using APSIM.Shared.Utilities;
using Models.Soils;
using Models.WaterModel;

namespace UserInterface.Presenters
{
    /// <summary>A generic presenter displaying four boxes of info with grid, graph, text and properties</summary>
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
        /// <param name="model">The data store model to work with. Must be a QuadView</param>
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
                else if (presenter is QuadGraphPresenter graph)
                    graph.Detach();
            }
            view.Dispose();
        }

        /// <summary>Refresh this presenter and all sub presenters</summary>
        public void Refresh()
        {
            DisconnectEvents();
            foreach (IPresenter presenter in presenters)
            {
                if (presenter is GridPresenter grid)
                    grid.Refresh();
                else if (presenter is QuadGraphPresenter graph)
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

        /// <summary>
        /// Add a graph presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddGraph(WidgetPosition position)
        {
            GraphView graphView = view.AddComponent(WidgetType.Graph, position) as GraphView;
            QuadGraphPresenter graphPresenter = new QuadGraphPresenter();
            graphPresenter.Attach(model, graphView, explorerPresenter);
            graphPresenter.Refresh();

            //Check if graph actually has content, hide if not
            if (graphView.Width > 0 && graphView.Height > 0)
            {
                presenters.Add(graphPresenter);
            }
            else
            {
                graphPresenter.Detach();
                view.RemoveComponent(position);
            }
        }

        /// <summary>
        /// Add a grid presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddGrid(WidgetPosition position)
        {
            ViewBase gridContainer = view.AddComponent(WidgetType.Grid, position);
            GridPresenter gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });
            gridPresenter.Refresh();

            presenters.Add(gridPresenter);
        }

        /// <summary>
        /// Add a markdown view to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        /// <param name="text">Text to display in this view</param>
        private void AddText(WidgetPosition position, string text)
        {
            view.AddComponent(WidgetType.Text, position);
            view.SetLabelText(text);
        }

        /// <summary>
        /// Add a property presenter to one of the quads
        /// </summary>
        /// <param name="position">Which quad to use</param>
        private void AddProperty(WidgetPosition position)
        {
            PropertyView propertyView = view.AddComponent(WidgetType.Property, position) as PropertyView;
            PropertyPresenter propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            //Check if properties actually has content, hide if not
            if (propertyView.AnyProperties)
            {
                presenters.Add(propertyPresenter);
            }
            else
            {
                propertyPresenter.Detach();
                view.RemoveComponent(position);
            }
        }

        /// <summary>
        /// Setup a generic layout with grid, graph and properties
        /// </summary>
        private void CreateLayoutGeneric()
        {
            AddGrid(WidgetPosition.BottomLeft);
            AddGraph(WidgetPosition.BottomRight);
            AddProperty(WidgetPosition.TopRight);
        }

        /// <summary>
        /// Create layout for an XY pairs, text, grid and graph
        /// </summary>
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
            AddGraph(WidgetPosition.BottomRight);
        }

        /// <summary>
        /// Create layout for a physical, text, grid and graph
        /// </summary>
        private void CreateLayoutPhysical()
        {
            CreateLayoutGeneric();
            string warnings = "<span color=\"red\">Note: values in red are estimates only and needed for the simulation of soil temperature. Overwrite with local values wherever possible.</span>";
            AddText(WidgetPosition.TopLeft, warnings);
            view.OverrideSlider(0.6);
        }

        /// <summary>
        /// Create layout for a waterbalance, grid, graph and properties
        /// </summary>
        private void CreateLayoutWaterBalance()
        {
            CreateLayoutGeneric();
            view.OverrideSlider(0.3);
        }
    }
}