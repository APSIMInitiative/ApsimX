namespace UserInterface.Presenters
{
    using APSIM.Shared.Graphing;
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
        private LabelView pawLabel;

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
            pawLabel = view.GetControl<LabelView>("pawLabel");
            graph = view.GetControl<GraphView>("graph");
            graph.SetPreferredWidth(0.3);

            Refresh();
            ConnectEvents();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            DisconnectEvents();
            view.Dispose();
        }

        /// <summary>Populate the grid control with data.</summary>
        public void Refresh()
        {
            try
            {
                DisconnectEvents();
                pawLabel.Text = water.InitialPAWmm.ToString("F0");
                percentFullEdit.Text = (water.FractionFull * 100).ToString("F0");
                filledFromTopCheckbox.Checked = water.FilledFromTop;
                relativeToDropDown.Values = water.AllowedRelativeTo.ToArray();
                relativeToDropDown.SelectedValue = water.RelativeTo;
                depthWetSoilEdit.Text = water.DepthWetSoil.ToString("F0");
                PopulateWaterGraph(graph, water.Physical.Thickness, water.Physical.AirDry, water.Physical.LL15, water.Physical.DUL, water.Physical.SAT,
                                   water.RelativeTo, water.Thickness, water.RelativeToLL, water.InitialValues);
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

        /// <summary>Invoked when the percent full edit box is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPercentFullChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(percentFullEdit.Text))
                water.FractionFull = Convert.ToDouble(percentFullEdit.Text, CultureInfo.InvariantCulture) / 100;
            else
                water.FractionFull = 0;
            Refresh();
            gridPresenter.Refresh();
        }

        /// <summary>Invoked when the filled from top checkbox is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnFilledFromTopChanged(object sender, EventArgs e)
        {
            water.FilledFromTop = filledFromTopCheckbox.Checked;
            water.FractionFull = Convert.ToDouble(percentFullEdit.Text, CultureInfo.InvariantCulture) / 100;
            Refresh();
            gridPresenter.Refresh();
        }

        /// <summary>Invoked when the relative to drop down is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRelativeToChanged(object sender, EventArgs e)
        {
            water.RelativeTo = relativeToDropDown.SelectedValue;
            water.FractionFull = Convert.ToDouble(percentFullEdit.Text, CultureInfo.InvariantCulture) / 100;
            Refresh();
            gridPresenter.Refresh();
        }

        /// <summary>Invoked when the depth of wet soil is changed.</summary>
        /// <param name="sender">The send of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDepthWetSoilChanged(object sender, EventArgs e)
        {
            water.DepthWetSoil = Convert.ToDouble(depthWetSoilEdit.Text, CultureInfo.InvariantCulture);
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
                                               string cllName, double[] swThickness, double[] cll, double[] sw)
        {
            var cumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(thickness);
            var swCumulativeThickness = APSIM.Shared.Utilities.SoilUtilities.ToCumThickness(swThickness);
            graph.Clear();

            graph.DrawRegion($"PAW relative to {cllName}", cll, swCumulativeThickness,
                             sw, swCumulativeThickness,
                             AxisPosition.Top, AxisPosition.Left,
                             System.Drawing.Color.LightSkyBlue, true);

            graph.DrawLineAndMarkers("Airdry", airdry,
                                     cumulativeThickness,
                                     "", "", null, null, AxisPosition.Top, AxisPosition.Left,
                                     System.Drawing.Color.Red, LineType.DashDot, MarkerType.None,
                                     LineThickness.Normal, MarkerSize.Normal, 1, true);

            graph.DrawLineAndMarkers("CLL", cll,
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


            graph.FormatAxis(AxisPosition.Top, "Volumetric water (mm/mm)", inverted: false, double.NaN, double.NaN, double.NaN, false);
            graph.FormatAxis(AxisPosition.Left, "Depth (mm)", inverted: true, 0, double.NaN, double.NaN, false);
            graph.FormatLegend(LegendPosition.RightBottom, LegendOrientation.Vertical);
            graph.Refresh();
        }
    }
}