using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Gtk.Sheet;
using Models.Interfaces;
using Models.Soils;
using UserInterface.Commands;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>A presenter for the water model.</summary>
    public class WaterPresenter : IPresenter
    {
        /// <summary>The grid presenter.</summary>
        private GridPresenter gridPresenter;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The base view.</summary>
        private ViewBase view = null;

        /// <summary>The water model.</summary>
        private Water water;

        /// <summary>Percent full edit box.</summary>
        private EditView percentFullEdit;

        /// <summary>Filled from top check box.</summary>
        private CheckBoxView filledFromTopCheckbox;

        /// <summary>Relative to combo.</summary>
        private DropDownView relativeToDropDown;

        /// <summary>Percent full edit box.</summary>
        private EditView depthWetSoilEdit;

        /// <summary>Plant available water label.</summary>
        private EditView pawEdit;

        /// <summary>Graph.</summary>
        private GraphView graph;

        /// <summary>Default constructor</summary>
        public WaterPresenter()
        {
        }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="v">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            water = model as Water;
            view = v as ViewBase;

            ContainerView gridContainer = view.GetControl<ContainerView>("grid");

            this.explorerPresenter = explorerPresenter;
            gridPresenter = new GridPresenter();
            gridPresenter.Attach((model as IGridModel).Tables[0], gridContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            percentFullEdit = view.GetControl<EditView>("percentFullEdit");
            filledFromTopCheckbox = view.GetControl<CheckBoxView>("filledFromTopCheckbox");
            relativeToDropDown = view.GetControl<DropDownView>("relativeToDropDown");
            depthWetSoilEdit = view.GetControl<EditView>("depthWetSoilEdit");
            pawEdit = view.GetControl<EditView>("pawEdit");
            graph = view.GetControl<GraphView>("graph");
            graph.SetPreferredWidth(0.3);
            graph.AddContextAction("Copy graph to clipboard", CopyGraphToClipboard);

            Refresh();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            gridPresenter.Detach();
            view.Dispose();
        }

        /// <summary>Populate the grid control with data.</summary>
        public void Refresh()
        {
            try
            {
                DisconnectEvents();
                pawEdit.Text = water.InitialPAWmm.ToString("F0", CultureInfo.CurrentCulture);
                percentFullEdit.Text = (water.FractionFull * 100).ToString("F0", CultureInfo.CurrentCulture);
                filledFromTopCheckbox.Checked = water.FilledFromTop;
                relativeToDropDown.Values = water.AllowedRelativeTo.ToArray();
                relativeToDropDown.SelectedValue = water.RelativeTo;
                depthWetSoilEdit.Text = water.DepthWetSoil.ToString("F0", CultureInfo.CurrentCulture);
                PopulateWaterGraph(graph, water.Physical.Thickness, water.Physical.AirDry, water.Physical.LL15, water.Physical.DUL, water.Physical.SAT,
                                   water.RelativeTo, water.Thickness, water.RelativeToLL, water.InitialValues, null, null);
                gridPresenter.Refresh();
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
            pawEdit.Changed += OnPawChanged;
            percentFullEdit.Changed += OnPercentFullChanged;
            filledFromTopCheckbox.Changed += OnFilledFromTopChanged;
            relativeToDropDown.Changed += OnRelativeToChanged;
            depthWetSoilEdit.Changed += OnDepthWetSoilChanged;
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>Disconnect all widget events.</summary>
        private void DisconnectEvents()
        {
            gridPresenter.CellChanged -= OnCellChanged;
            pawEdit.Changed -= OnPawChanged;
            percentFullEdit.Changed -= OnPercentFullChanged;
            filledFromTopCheckbox.Changed -= OnFilledFromTopChanged;
            relativeToDropDown.Changed -= OnRelativeToChanged;
            depthWetSoilEdit.Changed -= OnDepthWetSoilChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>Invoked when a grid cell has changed.</summary>
        /// <param name="dataProvider">The provider that contains the data.</param>
        /// <param name="colIndices">The indices of the columns of the cells that were changed.</param>
        /// <param name="rowIndices">The indices of the rows of the cells that were changed.</param>
        /// <param name="values">The cell values.</param>
        private void OnCellChanged(ISheetDataProvider dataProvider, int[] colIndices, int[] rowIndices, string[] values)
        {

            if (water.AreInitialValuesWithinPhysicalBoundaries(water.InitialValues))
                Refresh();
            else
            {
                this.explorerPresenter.CommandHistory.Undo();
                this.explorerPresenter.MainPresenter.ShowMessage("A water initial value exceeded acceptable bounds. Initial value has been reset to it's previous value.", Models.Core.Simulation.MessageType.Information);
            }

        }

        /// <summary>Invoked when the PAW edit box is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPawChanged(object sender, EventArgs e)
        {
            // Due to a quirk of gtk, selecting the entire textbox contents and
            // then commencing typing (so as to overwrite the old value) will
            // cause this method to be called with an empty string and then
            // again with each new character in turn. The best thing to do with
            // an empty string is to just ignore it, but only if there are
            // pending events.
            if (string.IsNullOrEmpty(pawEdit.Text) && Gtk.Application.EventsPending())
                return;
            if (double.TryParse(pawEdit.Text, out double val) && string.Compare(pawEdit.Text,"-") != 0)
                ChangePropertyValue(new ChangeProperty(water, "InitialPAWmm", val));                
        }

        /// <summary>Invoked when the percent full edit box is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPercentFullChanged(object sender, EventArgs e)
        {
            // Due to a quirk of gtk, selecting the entire textbox contents and
            // then commencing typing (so as to overwrite the old value) will
            // cause this method to be called with an empty string and then
            // again with each new character in turn. The best thing to do with
            // an empty string is to just ignore it, but only if there are
            // pending events.
            if (string.IsNullOrEmpty(percentFullEdit.Text) && Gtk.Application.EventsPending())
                return;
            if (double.TryParse(percentFullEdit.Text, out double val))
                ChangePropertyValue(new ChangeProperty(water, nameof(water.FractionFull), val / 100));
        }

        /// <summary>Invoked when the filled from top checkbox is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnFilledFromTopChanged(object sender, EventArgs e)
        {
            var changeFilledFromTop = new ChangeProperty.Property(water, nameof(water.FilledFromTop), filledFromTopCheckbox.Checked);

            if (string.IsNullOrEmpty(percentFullEdit.Text))
            {
                ChangeProperty change = new ChangeProperty(new[] { changeFilledFromTop });
                ChangePropertyValue(change);
            }
            else
            {
                ChangeProperty changes = new ChangeProperty(new[] { changeFilledFromTop });
                ChangePropertyValue(changes);
            }
        }

        /// <summary>Invoked when the relative to drop down is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRelativeToChanged(object sender, EventArgs e)
        {
            var changeRelativeTo = new ChangeProperty.Property(water, nameof(water.RelativeTo), relativeToDropDown.SelectedValue);
            ChangeProperty changes = new ChangeProperty(new[] { changeRelativeTo });
            ChangePropertyValue(changes);
        }

        /// <summary>Invoked when the depth of wet soil is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDepthWetSoilChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(depthWetSoilEdit.Text) && Gtk.Application.EventsPending())
                return;
            if (double.TryParse(depthWetSoilEdit.Text, out double val) && string.Compare(depthWetSoilEdit.Text, "-") != 0)
                ChangePropertyValue(nameof(water.DepthWetSoil), val);
        }

        /// <summary>
        /// Change a property of the water model via the command system, then
        /// update the GUI.
        /// </summary>
        /// <param name="propertyName">Name of the property to be changed.</param>
        /// <param name="propertyValue">New value of the property.</param>
        private void ChangePropertyValue(string propertyName, object propertyValue)
        {
            ChangePropertyValue(new ChangeProperty(water, propertyName, propertyValue));
        }

        /// <summary>
        /// Change a property of the water model via the command system, then
        /// update the GUI.
        /// </summary>
        /// <param name="command">The property change to be applied.</param>
        private void ChangePropertyValue(ChangeProperty command)
        {
            explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            Refresh();
        }

        public static void PopulateWaterGraph(GraphView graph, double[] thickness, double[] airdry, double[] ll15, double[] dul, double[] sat,
                                               string cllName, double[] swThickness, double[] cll, double[] sw, string llsoilsName, double[] llsoil)
        {
            var cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            var swCumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(swThickness);
            graph.Clear();

            //draw the area relative to whatever the water node is currently relative to
                graph.DrawRegion($"PAW relative to {cllName}", cll, swCumulativeThickness,
                            sw, swCumulativeThickness,
                            AxisPosition.Top, AxisPosition.Left,
                            System.Drawing.Color.LightSkyBlue, true);

            graph.DrawLineAndMarkers("Airdry", airdry,
                                     cumulativeThickness,
                                     "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                     System.Drawing.Color.Red, LineType.DashDot, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

            graph.DrawLineAndMarkers(cllName, cll,
                                     swCumulativeThickness,
                                     "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                     System.Drawing.Color.Red, LineType.Solid, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

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
            foreach (double val in airdry)
                vols.Add(val);
            foreach (double val in cll)
                vols.Add(val);
            foreach (double val in dul)
                vols.Add(val);
            foreach (double val in sat)
                vols.Add(val);

            if (llsoil != null)
                foreach (double val in llsoil)
                    vols.Add(val);

            double padding = 0.01; //add 1% to bounds
            double xTopMin = MathUtilities.Min(vols);
            double xTopMax = MathUtilities.Max(vols);
            xTopMin -= xTopMax * padding;
            xTopMax += xTopMax * padding;

            double height = MathUtilities.Max(cumulativeThickness);
            height += height * padding;

            graph.FormatAxis(AxisPosition.Top, "Volumetric water (mm/mm)", inverted: false, xTopMin, xTopMax, double.NaN, false, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, height, double.NaN, false, false);
            graph.FormatLegend(LegendPosition.RightBottom, LegendOrientation.Vertical);

            graph.Refresh();
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