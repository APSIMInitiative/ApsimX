using System;
using APSIM.Shared.Graphing;
using Models.Core;
using UserInterface.Views;
using APSIM.Numerics;
using Models.Functions;
using Models.Soils;
using APSIM.Shared.Utilities;
using System.Collections.Generic;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A presenter for a graph component
    /// </summary>
    public class QuadGraphPresenter : IPresenter
    {
        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The base view.</summary>
        private GraphView view = null;

        /// <summary>The model.</summary>
        private IModel model;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            
            this.view = view as GraphView;
            this.view.AddContextAction("Copy graph to clipboard", CopyGraphToClipboard);

            this.explorerPresenter = explorerPresenter;
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>Populate the graph with data.</summary>
        public void Refresh()
        {
            try
            {
                DisconnectEvents();
                try
                {
                    DrawGraph(model, view);
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

        /// <summary>Connect all widget events.</summary>
        private static void DrawGraph(IModel model, GraphView view)
        {
            if (model is XYPairs xyPairs)
            {
                PopulateGraph(view, $"{model.Name}", xyPairs.X, xyPairs.Y, xyPairs.GetXName(), xyPairs.GetYName());
            }
            else if (model is Physical || model is Water || model is SoilCrop)
            {
                Water water = null;
                Physical physical = null;
                SoilCrop crop = null;
                if (model is Water)
                {
                    water = model as Water;
                    physical = model.Node.FindSibling<Physical>();
                }
                else if (model is Physical)
                {
                    physical = model as Physical;
                    water = model.Node.FindSibling<Water>();
                }
                else if (model is SoilCrop)
                {
                    crop = model as SoilCrop;
                    physical = model.Node.FindSibling<Physical>();
                    water = model.Node.FindSibling<Water>();
                }

                if (water != null)
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
                    if (physical.Thickness != null)
                        PopulateWaterGraph(view, physical.Thickness, physical.AirDry, physical.LL15, physical.DUL, physical.SAT,
                                                        cllName, water.Thickness, relativeLL, water.InitialValues, llsoilName, llsoil);
                }
            }
            else if (model is Organic organic && organic.Thickness != null)
            {
                PopulateOrganicGraph(view, organic.Thickness, organic.FOM, organic.SoilCNRatio, organic.FBiom, organic.FInert);
            }
            else if (model is Solute solute && solute.Thickness != null)
            {
                double[] vals = solute.InitialValues;
                if (solute.InitialValuesUnits == Solute.UnitsEnum.kgha)
                    vals = SoilUtilities.kgha2ppm(solute.Thickness, solute.SoluteBD, vals);
                PopulateSoluteGraph(view, solute.Thickness, solute.Name, vals);
            }
            else if (model is Chemical chemical && chemical.Thickness != null)
            {
                var standardisedSoil = (chemical.Parent as Soil)?.CloneAndSanitise(chemical.Thickness);
                var solutes = standardisedSoil?.Node.FindChildren<Solute>();
                PopulateChemicalGraph(view, chemical.Thickness, chemical.PH, chemical.PHUnits, solutes);
            }

            //numLayersLabel.Text = $"{gridPresenter.RowCount()-1} layers";  // -1 to not count the empty row at bottom of sheet.
        }

        /// <summary>Connect all widget events.</summary>
        private void ConnectEvents()
        {
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
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
            view.ExportToClipboard();
        }

        private static void PopulateGraph(GraphView view, string title, double[] x, double[] y, string xTitle, string yTitle)
        {
            if (view == null)
                throw new Exception($"GraphPresenter has a null GraphView. Cannot draw graph of {title}");

            view.Clear();
            view.DrawLineAndMarkers("", x, y,
                                     "", "", null, null, AxisPosition.Bottom, AxisPosition.Left,
                                     System.Drawing.Color.Blue, LineType.Solid, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

            double padding = 0.01; //add 1% to bounds
            double xTopMin = MathUtilities.Min(x);
            double xTopMax = MathUtilities.Max(x);
            xTopMin -= xTopMax * padding;
            xTopMax += xTopMax * padding;

            double yTopMin = MathUtilities.Min(y);
            double yTopMax = MathUtilities.Max(y);
            yTopMin -= yTopMax * padding;
            yTopMax += yTopMax * padding;

            view.FormatAxis(AxisPosition.Bottom, xTitle, inverted: false, xTopMin, xTopMax, double.NaN, false, false);
            view.FormatAxis(AxisPosition.Left, yTitle, inverted: false, yTopMin, yTopMax, double.NaN, false, false);
            view.Refresh();
        }

        private static void PopulateOrganicGraph(GraphView graph, double[] thickness, double[] fom, double[] SoilCNRatio, double[] fbiom, double[] finert)
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

        private static void PopulateSoluteGraph(GraphView graph, double[] thickness, string soluteName, double[] values)
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

        private static void PopulateChemicalGraph(GraphView graph, double[] thickness, double[] pH, Chemical.PHUnitsEnum phUnits, IEnumerable<Solute> solutes)
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

        private static void PopulateWaterGraph(GraphView graph, double[] thickness, double[] airdry, double[] ll15, double[] dul, double[] sat,
                                               string cllName, double[] swThickness, double[] cll, double[] sw, string llsoilsName, double[] llsoil)
        {

            double[] cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            double[] cumulativeSWThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(swThickness);

            double[] cllMapped = null;
            if (cll != null)
            {
                if (cll.Length == thickness.Length)
                    cllMapped = cll;
                else if (cll.Length == swThickness.Length)
                    cllMapped = SoilUtilities.MapConcentration(cll, swThickness, thickness, 0);
            }

            double[] swMapped = null;
            if (sw != null)
            {
                if (sw.Length == thickness.Length)
                    swMapped = sw;
                else if (sw.Length == swThickness.Length)
                    swMapped = SoilUtilities.MapConcentration(sw, swThickness, thickness, 0);
            }

            graph.Clear();

            if (swMapped != null && cllMapped != null)
            {
                //draw the area relative to whatever the water node is currently relative to
                graph.DrawRegion($"PAW relative to {cllName}", cllMapped, cumulativeThickness,
                            swMapped, cumulativeThickness,
                            AxisPosition.Top, AxisPosition.Left,
                            System.Drawing.Color.LightSkyBlue, true);
            }

            if (airdry != null)
                graph.DrawLineAndMarkers("Airdry", airdry,
                                        cumulativeThickness,
                                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                        System.Drawing.Color.Red, LineType.DashDot, MarkerType.None,
                                        LineThickness.Normal, MarkerSize.Normal, 1, true);

            if (llsoil == null && cllMapped != null)
            {
                graph.DrawLineAndMarkers(cllName, cllMapped,
                                        cumulativeThickness,
                                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                        System.Drawing.Color.Red, LineType.Solid, MarkerType.None,
                                        LineThickness.Normal, MarkerSize.Normal, 1, true);
            }
            else if (ll15 != null)
            {
                graph.DrawLineAndMarkers("LL15", ll15,
                                        cumulativeThickness,
                                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                        System.Drawing.Color.Red, LineType.Solid, MarkerType.None,
                                        LineThickness.Normal, MarkerSize.Normal, 1, true);
            }

            if (dul != null)
                graph.DrawLineAndMarkers("DUL", dul,
                                        cumulativeThickness,
                                        "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                        System.Drawing.Color.Blue, LineType.Solid, MarkerType.None,
                                        LineThickness.Normal, MarkerSize.Normal, 1, true);

            if (sat != null)
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
    }
}
