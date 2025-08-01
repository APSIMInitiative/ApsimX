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

            Soil soilNode = this.model.FindAncestor<Soil>();
            if (soilNode != null)
            {
                physical = soilNode.Node.FindChild<Physical>();
                physical.InFill();
                var chemical = soilNode.Node.FindChild<Chemical>();
                var organic = soilNode.Node.FindChild<Organic>();
                if (chemical != null && organic != null)
                    chemical.InFill(physical, organic);
                water = soilNode.Node.FindChild<Water>();
            }
            ContainerView gridContainer = view.GetControl<ContainerView>("grid");
            gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All", "Units" });

            var propertyView = view.GetControl<PropertyView>("properties");
            propertyPresenter = new PropertyPresenter();
            propertyPresenter.Attach(model, propertyView, explorerPresenter);

            graph = view.GetControl<GraphView>("graph");
            graph.AddContextAction("Copy graph to clipboard", CopyGraphToClipboard);

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
                try
                {
                    if (water != null && (model is Physical || model is Water || model is SoilCrop))
                    {
                        string llsoilName = null;
                        double[] llsoil = null;
                        string cllName = "LL15";
                        double[] relativeLL = physical.LL15;

                        if (model is SoilCrop soilCrop)
                        {
                            llsoilName = (model as SoilCrop).Name;
                            string cropName = llsoilName.Substring(0, llsoilName.IndexOf("Soil"));
                            llsoilName = cropName + " LL";
                            llsoil = (model as SoilCrop).LL;
                            cllName = llsoilName;
                            relativeLL = (model as SoilCrop).LL;
                        }
                        //Since we can view the soil relative to water, lets not have the water node graphing options effect this graph.
                        PopulateWaterGraph(graph, physical.Thickness, physical.AirDry, physical.LL15, physical.DUL, physical.SAT,
                                                        cllName, water.Thickness, relativeLL, water.InitialValues, llsoilName, llsoil);
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
                        PopulateChemicalGraph(graph, chemical.Thickness, chemical.PH, chemical.PHUnits, Chemical.GetStandardisedSolutes(chemical));
                    }

                    numLayersLabel.Text = $"{gridPresenter.RowCount()-1} layers";  // -1 to not count the empty row at bottom of sheet.
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

            double padding = 0.01; //add 1% to bounds
            double xTopMin = MathUtilities.Min(fom);
            double xTopMax = MathUtilities.Max(fom);
            xTopMin -= xTopMax * padding;
            xTopMax += xTopMax * padding;

            double height = MathUtilities.Max(cumulativeThickness);
            height += height * padding;

            graph.FormatAxis(AxisPosition.Top, "Fresh organic matter (kg/ha)", inverted: false, xTopMin, xTopMax, double.NaN, false, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, height, double.NaN, false, false);
            graph.FormatAxis(AxisPosition.Bottom, "Fraction ", inverted: false, 0, 1.01, 0.2, false, false);
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

            double padding = 0.01; //add 1% to bounds
            double xTopMin = 0;
            double xTopMax = MathUtilities.Max(values);


            double height = MathUtilities.Max(cumulativeThickness);
            height += height * padding;

            if (xTopMax == xTopMin)
            {
                xTopMin -= 0.5;
                xTopMax += 0.5;
            }
            else
            {
                xTopMin -= xTopMax * padding;
                xTopMax += xTopMax * padding;
            }

            graph.FormatAxis(AxisPosition.Top, $"Initial {soluteName} (ppm)", inverted: false, xTopMin, xTopMax, double.NaN, false, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, height, double.NaN, false, false);
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

            List<double> sols = new List<double>();
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
                foreach (double v in vals)
                    sols.Add(v);
            }

            double padding = 0.01; //add 1% to bounds
            double xBottomMin = MathUtilities.Min(sols);
            double xBottomMax = MathUtilities.Max(sols);
            xBottomMin -= xBottomMax * padding;
            xBottomMax += xBottomMax * padding;

            double height = MathUtilities.Max(cumulativeThickness);
            height += height * padding;

            graph.FormatAxis(AxisPosition.Top, $"pH ({units})", inverted: false, 2, 12, 2, false, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, height, double.NaN, false, false);
            graph.FormatAxis(AxisPosition.Bottom, "Initial solute (ppm) ", inverted: false, xBottomMin, xBottomMax, double.NaN, false, false);
            graph.FormatLegend(LegendPosition.BottomRight, LegendOrientation.Vertical);
            graph.Refresh();
        }

        public static void PopulateWaterGraph(GraphView graph, double[] thickness, double[] airdry, double[] ll15, double[] dul, double[] sat,
                                               string cllName, double[] swThickness, double[] cll, double[] sw, string llsoilsName, double[] llsoil)
        {

            double[] cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            double[] cumulativeSWThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(swThickness);

            double[] cllMapped = null;
            if (cll.Length == thickness.Length)
                cllMapped = cll;
            else if (cll.Length == swThickness.Length)
                cllMapped = SoilUtilities.MapConcentration(cll, swThickness, thickness, 0);

            double[] swMapped = null;
            if (sw.Length == thickness.Length)
                swMapped = sw;
            else if (sw.Length == swThickness.Length)
                swMapped = SoilUtilities.MapConcentration(sw, swThickness, thickness, 0);

            graph.Clear();

            //draw the area relative to whatever the water node is currently relative to
            graph.DrawRegion($"PAW relative to {cllName}", cllMapped, cumulativeThickness,
                            swMapped, cumulativeThickness,
                            AxisPosition.Top, AxisPosition.Left,
                            System.Drawing.Color.LightSkyBlue, true);

            graph.DrawLineAndMarkers("Airdry", airdry,
                                    cumulativeThickness,
                                    "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                    System.Drawing.Color.Red, LineType.DashDot, MarkerType.None,
                                    LineThickness.Normal, MarkerSize.Normal, 1, true);

            if (llsoil == null)
            {
                graph.DrawLineAndMarkers(cllName, cllMapped,
                                        cumulativeThickness,
                                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                        System.Drawing.Color.Red, LineType.Solid, MarkerType.None,
                                        LineThickness.Normal, MarkerSize.Normal, 1, true);
            }
            else
            {
                graph.DrawLineAndMarkers("LL15", ll15,
                                        cumulativeThickness,
                                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                        System.Drawing.Color.Red, LineType.Solid, MarkerType.None,
                                        LineThickness.Normal, MarkerSize.Normal, 1, true);
            }


            graph.DrawLineAndMarkers("DUL", dul,
                        cumulativeThickness,
                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                        System.Drawing.Color.Blue, LineType.Solid, MarkerType.None,
                        LineThickness.Normal, MarkerSize.Normal, 1, true);

            graph.DrawLineAndMarkers("SAT", sat,
                                    cumulativeThickness,
                                    "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                    System.Drawing.Color.Blue, LineType.DashDot, MarkerType.None,
                                    LineThickness.Normal, MarkerSize.Normal, 1, true);

            if (llsoil != null && llsoilsName != null)
            {
                graph.DrawLineAndMarkers(llsoilsName, llsoil,
                        cumulativeThickness,
                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                        System.Drawing.Color.Green, LineType.Dash, MarkerType.None,
                        LineThickness.Normal, MarkerSize.Normal, 1, true);
            }

            List<double> vols = new List<double>();
            if (airdry != null)
                vols.AddRange(airdry);
            if (cll != null)
                vols.AddRange(cll);
            if (dul != null)
                vols.AddRange(dul);
            if (sat != null)
                vols.AddRange(sat);
            if (llsoil != null)
                vols.AddRange(llsoil);

            double padding = 0.01; //add 1% to bounds
            double xTopMin = MathUtilities.Min(vols);
            double xTopMax = MathUtilities.Max(vols);
            xTopMin -= xTopMax * padding;
            xTopMax += xTopMax * padding;


            double physicalHeight = MathUtilities.Max(cumulativeThickness);
            double waterHeight = MathUtilities.Max(cumulativeSWThickness);
            double height = physicalHeight;
            if (waterHeight < physicalHeight)
                height = waterHeight;

            graph.FormatAxis(AxisPosition.Top, "Volumetric water (mm/mm)", inverted: false, xTopMin, xTopMax, double.NaN, false, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, height, double.NaN, false, false);
            graph.FormatLegend(LegendPosition.RightBottom, LegendOrientation.Vertical);

            graph.Refresh();
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

        /// <summary>User has clicked "copy graph" menu item.</summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void CopyGraphToClipboard(object sender, EventArgs e)
        {
            graph.ExportToClipboard();
        }
    }
}