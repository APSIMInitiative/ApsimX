namespace UserInterface.Presenters
{
    using APSIM.Shared.Graphing;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.GrazPlan;
    using Models.Soils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Views;

    /// <summary>A presenter for the soil profile models.</summary>
    public class ProfilePresenter : IPresenter
    {
        /// <summary>The grid presenter.</summary>
        private NewGridPresenter gridPresenter;

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
        private GraphView graph;

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
            gridPresenter = new NewGridPresenter();
            gridPresenter.Attach(model, v, explorerPresenter);

            Soil soilNode = this.model.FindAncestor<Soil>();
            physical = soilNode.FindChild<Physical>();
            water = soilNode.FindChild<Water>();

            var propertyView = view.GetControl<PropertyView>("properties");
            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            graph = view.GetControl<GraphView>("graph");

            //get the paned object that holds the graph and grid
            Gtk.Paned bottomPane = view.GetGladeObject<Gtk.Paned>("bottom");
            int paneWidth = view.MainWidget.ParentWindow.Width; //this shoudl get the width of this view
            bottomPane.Position = (int)Math.Round(paneWidth * 0.75); //set the slider for the pane at about 75% across

            numLayersLabel = view.GetControl<LabelView>("numLayersLabel");

            if (!propertyView.AnyProperties)
            {
                var layeredLabel = view.GetControl<LabelView>("layeredLabel");
                var propertiesLabel = view.GetControl<LabelView>("parametersLabel");
                propertiesLabel.Visible = false;
                layeredLabel.Visible = false;
            }
            else
            {
                // Position the splitter to give the "Properties" section as much space as it needs, and no more
                if (view.MainWidget is Gtk.Paned paned)
                {
                    paned.Child1.GetPreferredHeight(out int minHeight, out int natHeight);
                    paned.Position = natHeight;
                }
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
                try
                {
                    if (water != null && (model is Physical || model is Water || model is SoilCrop))
                    {
                        string llsoilName = null;
                        double[] llsoil = null;

                        if (model is SoilCrop)
                        {
                            llsoilName = (model as SoilCrop).Name;
                            llsoilName = llsoilName.Substring(0, llsoilName.IndexOf("Soil"));
                            llsoilName = llsoilName + " LL";

                            llsoil = (model as SoilCrop).LL;
                            
                        }
                        //Since we can view the soil relative to water, lets not have the water node graphing options effect this graph.
                        WaterPresenter.PopulateWaterGraph(graph, physical.Thickness, physical.AirDry, physical.LL15, physical.DUL, physical.SAT,
                                                          "LL15", water.Thickness, physical.LL15, water.InitialValues, llsoilName, llsoil);
                    }
                        
                    else if (model is Organic organic)
                        PopulateOrganicGraph(graph, organic.Thickness, organic.FOM, organic.SoilCNRatio, organic.FBiom, organic.FInert);
                    else if (model is Solute solute && solute.Thickness != null)
                    {
                        double[] vals = solute.InitialValues;
                        if (solute.InitialValuesUnits == Solute.UnitsEnum.kgha)
                            vals = SoilUtilities.kgha2ppm(solute.Thickness, solute.SoluteBD, vals);
                        PopulateSoluteGraph(graph, solute.Thickness, solute.Name, vals);
                    }
                    else if (model is Chemical chemical)
                    {
                        PopulateChemicalGraph(graph, chemical.Thickness, chemical.PH, chemical.PHUnits, chemical.GetStandardisedSolutes());
                    }

                    numLayersLabel.Text = $"{gridPresenter.NumRows()} layers";
                }
                finally
                {
                    ConnectEvents();
                }
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

        public static void PopulateSoluteGraph(GraphView graph, double[] thickness, string soluteName, double[] values)
        {
            var cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            graph.Clear();
            graph.DrawLineAndMarkers($"{soluteName}", values,
                                     cumulativeThickness,
                                     "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                     System.Drawing.Color.Blue, LineType.Solid, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

            graph.FormatAxis(AxisPosition.Top, $"Initial {soluteName} (ppm)", inverted: false, 0, double.NaN, double.NaN, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, double.NaN, double.NaN, false);
            graph.FormatLegend(LegendPosition.BottomRight, LegendOrientation.Vertical);
            graph.Refresh();
        }

        public static void PopulateChemicalGraph(GraphView graph, double[] thickness, double[] pH, Chemical.PHUnitsEnum phUnits, IEnumerable<Solute> solutes)
        {
            var cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            graph.Clear();
            int nColor = 0;
            string units = (phUnits == Chemical.PHUnitsEnum.Water) ? "water" : "CaCl2";
            graph.DrawLineAndMarkers($"pH", pH,
                                     cumulativeThickness,
                                     "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                     ColourUtilities.ChooseColour(nColor++), LineType.Solid, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);
            foreach (var solute in solutes)
            {
                double[] vals = solute.InitialValues;
                if (solute.InitialValuesUnits == Solute.UnitsEnum.kgha)
                    vals = SoilUtilities.kgha2ppm(solute.Thickness, solute.SoluteBD, vals);
                graph.DrawLineAndMarkers($"{solute.Name}", vals,
                                         cumulativeThickness,
                                         "", "", null, null, AxisPosition.Bottom, AxisPosition.Left,
                                         ColourUtilities.ChooseColour(nColor++), LineType.Solid, MarkerType.None,
                                         LineThickness.Normal, MarkerSize.Normal, 1, true);

            }

            graph.FormatAxis(AxisPosition.Top, $"pH ({units})", inverted: false, 2, 12, 2, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, double.NaN, double.NaN, false);
            graph.FormatAxis(AxisPosition.Bottom, "Initial solute (ppm) ", inverted: false, 0, double.NaN, double.NaN, false);
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