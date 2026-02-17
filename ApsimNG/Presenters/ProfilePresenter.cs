using System;
using System.Collections.Generic;
using APSIM.Numerics;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Gtk.Sheet;
using Models.Core;
using Models.Soils;
using Models.WaterModel;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>A presenter for the soil profile models.</summary>
    public class ProfilePresenter : IPresenter
    {
        /// <summary>The grid presenter.</summary>
        private GridPresenter gridPresenter;

        ///// <summary>The property presenter.</summary>
        private PropertyPresenter propertyPresenter;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The base view.</summary>
        private ViewBase view = null;

        /// <summary>The model.</summary>
        private IModel model;

        /// <summary>The physical model.</summary>
        private Physical physical;

        /// <summary>The water model.</summary>
        private Water water;

        /// <summary>Graph.</summary>
        private GraphPresenter2 graphPresenter;

        /// <summary>Label showing number of layers.</summary>
        private LabelView numLayersLabel;

        /// <summary>Default constructor</summary>
        public ProfilePresenter()
        {
        }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="v">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            view = v as ViewBase;
            this.explorerPresenter = explorerPresenter;

            Soil soilNode = this.model.Node.FindParent<Soil>(recurse: true);
            if (soilNode == null)
                throw new Exception($"ProfilePresenter could not find the Soil node above {this.model.Name} ({this.model.GetType().Name})");
                
            physical = model as Physical ?? soilNode?.Node.FindChild<Physical>();
            if (physical?.Thickness != null)
                physical?.InFill();

            var chemical = model as Chemical ?? soilNode?.Node.FindChild<Chemical>();
            var organic = model as Organic ?? soilNode?.Node.FindChild<Organic>();
            if (chemical != null && organic != null)
                chemical.InFill(physical, organic);
            water = model as Water ?? soilNode?.Node.FindChild<Water>();

            ContainerView gridContainer = view.GetControl<ContainerView>("grid");
            gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });

            var propertyView = view.GetControl<PropertyView>("properties");
            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            GraphView graphView = view.GetControl<GraphView>("graph");
            graphPresenter = new GraphPresenter2();
            graphPresenter.Attach(model, graphView, explorerPresenter);

            //get the paned object that holds the graph and grid
            Gtk.Paned topPane = view.GetGladeObject<Gtk.Paned>("top");
            int paneWidth = view.MainWidget.ParentWindow.Width; //this should get the width of this view
            topPane.Position = (int)Math.Round(paneWidth * 0.5); //set the slider for the pane at about 50% across

            Gtk.Paned leftPane = view.GetGladeObject<Gtk.Paned>("left");
            leftPane.Position = view.MainWidget.ParentWindow.Height;
            Gtk.Label redValuesWarningLbl = view.GetGladeObject<Gtk.Label>("output_lbl");
            redValuesWarningLbl.Visible = false;

            if (model is Physical)
            {
                redValuesWarningLbl.Text = new("<span color=\"red\">Note: values in red are estimates only and needed for the simulation of soil temperature. Overwrite with local values wherever possible.</span>");
                redValuesWarningLbl.UseMarkup = true;
                redValuesWarningLbl.Wrap = true;
                redValuesWarningLbl.Visible = true;

                redValuesWarningLbl.GetPreferredHeight(out int minHeight, out int natHeight);
                leftPane.Position = view.MainWidget.ParentWindow.Height - natHeight;

                //set the slider for the pane at about 60% across
                topPane.Position = (int)Math.Round(paneWidth * 0.6);
            }
            else if (model is WaterBalance)
            {
                topPane.Position = (int)Math.Round(paneWidth * 0.3);
            }

            numLayersLabel = view.GetControl<LabelView>("numLayers_lbl");
            if (!propertyView.AnyProperties)
            {
                var layeredLabel = view.GetControl<LabelView>("layered_lbl");
                var propertiesLabel = view.GetControl<LabelView>("parameters_lbl");
                propertiesLabel.Visible = false;
                layeredLabel.Visible = false;
            }
            else
            {
                // Position the splitter to give the "Properties" section as much space as it needs, and no more
                Gtk.Paned rightPane = view.GetGladeObject<Gtk.Paned>("right");
                rightPane.Child1.GetPreferredHeight(out int minHeight, out int natHeight);
                rightPane.Position = natHeight;
                //if SoilWater, hide empty graph space
                if (model is WaterBalance)
                    rightPane.Position = view.MainWidget.ParentWindow.Height;
            }

            Refresh();
            ConnectEvents();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            if (this.propertyPresenter != null)
            {
                gridPresenter.Detach();
                propertyPresenter.Detach();
            }
            view.Dispose();
        }

        /// <summary>Populate the graph with data.</summary>
        public void Refresh()
        {
            try
            {
                DisconnectEvents();
                graphPresenter.Refresh();
                ConnectEvents();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err.ToString());
            }
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            gridPresenter.CellChanged += OnCellChanged;
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
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