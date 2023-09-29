namespace UserInterface.Presenters
{
    using APSIM.Shared.Graphing;
    using Commands;
    using Models.Soils;
    using System;
    using System.Globalization;
    using System.Linq;
    using Views;

    /// <summary>A presenter for the water model.</summary>
    public class WaterPresenter : IPresenter
    {
        /// <summary>The grid presenter.</summary>
        private NewGridPresenter gridPresenter;

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
            this.explorerPresenter = explorerPresenter;
            gridPresenter = new NewGridPresenter();
            gridPresenter.Attach(model, v, explorerPresenter);

            percentFullEdit = view.GetControl<EditView>("percentFullEdit");
            filledFromTopCheckbox = view.GetControl<CheckBoxView>("filledFromTopCheckbox");
            relativeToDropDown = view.GetControl<DropDownView>("relativeToDropDown");
            depthWetSoilEdit = view.GetControl<EditView>("depthWetSoilEdit");
            pawEdit = view.GetControl<EditView>("pawEdit");
            graph = view.GetControl<GraphView>("graph");
            graph.SetPreferredWidth(0.3);

            Refresh();
            ConnectEvents();
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
            DisconnectEvents();
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
        /// <param name="colIndex">The index of the column of the cell that was changed.</param>
        /// <param name="rowIndex">The index of the row of the cell that was changed.</param>
        private void OnCellChanged(ISheetDataProvider dataProvider, int colIndex, int rowIndex)
        {
            Refresh();
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
            double paw = Convert.ToDouble(pawEdit.Text, CultureInfo.CurrentCulture);
            ChangePropertyValue(new ChangeProperty(water, "InitialPAWmm", paw));
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
            double fractionFull = Convert.ToDouble(percentFullEdit.Text, CultureInfo.CurrentCulture) / 100;
            ChangePropertyValue(new ChangeProperty(water, nameof(water.FractionFull), fractionFull));
        }

        /// <summary>Invoked when the filled from top checkbox is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnFilledFromTopChanged(object sender, EventArgs e)
        {
            var changeFilledFromTop = new ChangeProperty.Property(water, nameof(water.FilledFromTop), filledFromTopCheckbox.Checked);

            double fractionFull = Convert.ToDouble(percentFullEdit.Text, CultureInfo.CurrentCulture) / 100;
            var changeFractionFull = new ChangeProperty.Property(water, nameof(water.FractionFull), fractionFull);

            // Create a single ChangeProperty object with two actual changes.
            // This will cause both changes to be applied (and be undo-able) in
            // a single atomic action.
            ChangeProperty changes = new ChangeProperty(new[] { changeFilledFromTop, changeFractionFull });
            ChangePropertyValue(changes);
        }

        /// <summary>Invoked when the relative to drop down is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRelativeToChanged(object sender, EventArgs e)
        {
            var changeRelativeTo = new ChangeProperty.Property(water, nameof(water.RelativeTo), relativeToDropDown.SelectedValue);

            double fractionFull = Convert.ToDouble(percentFullEdit.Text, CultureInfo.CurrentCulture) / 100;
            var changeFractionFull = new ChangeProperty.Property(water, nameof(water.FractionFull), fractionFull);

            // Create a single ChangeProperty object with two actual changes.
            // This will cause both changes to be applied (and be undo-able) in
            // a single atomic action.
            ChangeProperty changes = new ChangeProperty(new[] { changeRelativeTo, changeFractionFull });
            ChangePropertyValue(changes);
        }

        /// <summary>Invoked when the depth of wet soil is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDepthWetSoilChanged(object sender, EventArgs e)
        {
            double depthWetSoil = Convert.ToDouble(depthWetSoilEdit.Text, CultureInfo.CurrentCulture);
            ChangePropertyValue(nameof(water.DepthWetSoil), depthWetSoil);
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
            Refresh();
            gridPresenter.Refresh();
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

            if (llsoil != null && llsoilsName != null)
            {       //draw the area relative to the water LL instead.
                graph.DrawRegion($"PAW relative to {llsoilsName}", llsoil, swCumulativeThickness,
                             sw, swCumulativeThickness,
                             AxisPosition.Top, AxisPosition.Left,
                             System.Drawing.Color.LightSkyBlue, true);
            } 
            else
            {       //draw the area relative to whatever the water node is currently relative to
                graph.DrawRegion($"PAW relative to {cllName}", cll, swCumulativeThickness,
                            sw, swCumulativeThickness,
                            AxisPosition.Top, AxisPosition.Left,
                            System.Drawing.Color.LightSkyBlue, true);
            }
            

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

            graph.FormatAxis(AxisPosition.Top, "Volumetric water (mm/mm)", inverted: false, double.NaN, double.NaN, double.NaN, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, double.NaN, double.NaN, false);
            graph.FormatLegend(LegendPosition.RightBottom, LegendOrientation.Vertical);
            graph.Refresh();
        }
    }
}