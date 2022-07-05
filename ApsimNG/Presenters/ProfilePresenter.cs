namespace UserInterface.Presenters
{
    using APSIM.Shared.Graphing;
    using Models.Core;
    using Models.Soils;
    using System;
    using Views;

    /// <summary>A presenter for the soil profile models.</summary>
    public class ProfilePresenter : IPresenter
    {
        /// <summary>The grid presenter.</summary>
        private NewGridPresenter gridPresenter;

        ///// <summary>The property presenter.</summary>
        //private PropertyPresenter propertyPresenter;

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
        private GraphView graph;

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
            gridPresenter = new NewGridPresenter();
            gridPresenter.Attach(model, v, explorerPresenter);

            physical = this.model.FindInScope<Physical>();
            water = this.model.FindInScope<Water>();

            var propertyView = view.GetControl<PropertyView>("properties");
            var propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            graph = view.GetControl<GraphView>("graph");
            graph.SetPreferredWidth(0.3);

            if (!propertyView.AnyProperties)
            {
                var layeredLabel = view.GetControl<LabelView>("layeredLabel");
                var propertiesLabel = view.GetControl<LabelView>("parametersLabel");
                propertiesLabel.Visible = false;
                layeredLabel.Visible = false;
            }

            Refresh();
            ConnectEvents();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            view.Dispose();
        }

        /// <summary>Populate the graph with data.</summary>
        public void Refresh()
        {
            try
            {
                DisconnectEvents();

                if (water != null && (model is Physical || model is Water))
                    WaterPresenter.PopulateWaterGraph(graph, physical.Thickness, physical.AirDry, physical.LL15, physical.DUL, physical.SAT,
                                                      water.RelativeTo, water.Thickness, water.RelativeToLL, water.InitialValues);
                else if (model is Organic organic)
                    PopulateOrganicGraph(graph, organic.Thickness, organic.FOM, organic.SoilCNRatio, organic.FBiom, organic.FInert);
                
                ConnectEvents();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err.ToString());
            }
        }

        public static void PopulateOrganicGraph(GraphView graph, double[] thickness, double[] fom, double[] SoilCNRatio, double[] fbiom, double[] finert)
        {
            var cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            graph.Clear();
            graph.DrawArea("FOM", fom,
                           cumulativeThickness,
                           AxisPosition.Top, AxisPosition.Left,
                           System.Drawing.Color.LightBlue, true);

            graph.DrawLineAndMarkers("FBIOM", fbiom,
                                     cumulativeThickness,
                                     "", "", null, null, AxisPosition.Bottom, AxisPosition.Left,
                                     System.Drawing.Color.Blue, LineType.Solid, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

            graph.DrawLineAndMarkers("FINERT", finert,
                                     cumulativeThickness,
                                     "", "", null, null, AxisPosition.Bottom, AxisPosition.Left,
                                     System.Drawing.Color.Red, LineType.Solid, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

            graph.FormatAxis(AxisPosition.Top, "Fresh organic matter (kg/ha)", inverted: false, double.NaN, double.NaN, double.NaN, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, double.NaN, double.NaN, false);
            graph.FormatAxis(AxisPosition.Bottom, "Fraction ", inverted: false, 0, 1, 0.2, false);
            graph.FormatLegend(LegendPosition.BottomRight, LegendOrientation.Vertical);
            graph.Refresh();
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            DisconnectEvents();
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
        /// <param name="colIndex">The index of the column of the cell that was changed.</param>
        /// <param name="rowIndex">The index of the row of the cell that was changed.</param>
        private void OnCellChanged(ISheetDataProvider dataProvider, int colIndex, int rowIndex)
        {
            Refresh();
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            Refresh();
        }
    }
}