// -----------------------------------------------------------------------
// <copyright file="ForestryView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using Gtk;
    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.GtkSharp;
    using Interfaces;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// <remarks>
    /// TODO : set the background colour of the first two rows to lightgray.
    /// </remarks>
    public class TreeProxyView : ViewBase
    {
        /// <summary>
        /// Plot of the below ground data.
        /// </summary>
        private PlotView belowGroundGraph;

        /// <summary>
        /// Plot of the above ground data.
        /// </summary>
        private PlotView aboveGroundGraph;

        /// <summary>
        /// A list to hold all plots to make enumeration easier.
        /// </summary>
        private List<PlotView> plots = new List<PlotView>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeProxyView" /> class.
        /// </summary>
        public TreeProxyView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.TreeProxyView.glade");
            ScrolledWindow temporalDataTab = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            ScrolledWindow spatialDataTab = (ScrolledWindow)builder.GetObject("scrolledwindow2");
            VPaned mainPanel = (VPaned)builder.GetObject("vpaned1");
            Alignment constantsTab = (Alignment)builder.GetObject("alignment1");
            HBox graphContainer = (HBox)builder.GetObject("hbox1");
            _mainWidget = mainPanel;

            TemporalDataGrid = new GridView(this as ViewBase);
            TemporalDataGrid.CellsChanged += GridCellEdited;
            temporalDataTab.Add((TemporalDataGrid as ViewBase).MainWidget);

            SpatialDataGrid = new GridView(this as ViewBase);
            SpatialDataGrid.CellsChanged += GridCellEdited;
            spatialDataTab.Add((SpatialDataGrid as ViewBase).MainWidget);

            belowGroundGraph = new PlotView();
            aboveGroundGraph = new PlotView();
            aboveGroundGraph.Model = new PlotModel();
            belowGroundGraph.Model = new PlotModel();
            plots.Add(aboveGroundGraph);
            plots.Add(belowGroundGraph);
            aboveGroundGraph.SetSizeRequest(-1, 100);
            graphContainer.PackStart(aboveGroundGraph, true, true, 0);
            belowGroundGraph.SetSizeRequest(-1, 100);
            graphContainer.PackStart(belowGroundGraph, true, true, 0);

            ConstantsGrid = new GridView(this);
            constantsTab.Add((ConstantsGrid as ViewBase).MainWidget);
            MainWidget.ShowAll();
            _mainWidget.Destroyed += MainWidgetDestroyed;
        }

        /// <summary>
        /// Depth midpoints of the soil layers.
        /// </summary>
        public double[] SoilMidpoints { get; set; }

        /// <summary>
        /// Grid which displays the temporal data.
        /// </summary>
        public IGridView TemporalDataGrid { get; private set; }

        /// <summary>
        /// Grid which displays the spatial data.
        /// </summary>
        public IGridView SpatialDataGrid { get; private set; }

        /// <summary>
        /// Constants grid.
        /// </summary>
        public IGridView ConstantsGrid { get; private set; }

        /// <summary>
        /// Data to be displayed in the spatial data grid.
        /// </summary>
        public List<List<string>> SpatialData
        {
            get
            {
                List<List<string>> newTable = new List<List<string>>();
                newTable.Add(SpatialDataGrid.DataSource.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList());
                // i is the column index.
                for (int col = 0; col < SpatialDataGrid.DataSource.Columns.Count; col++)
                {
                    // The first list in the forestry model's table holds the column headers.
                    // We don't want to modify these.
                    // The second list in the forestry model's table holds the first column,
                    // which holds the row names. We don't want to modify this either.
                    List<string> column = new List<string>();
                    for (int row = 0; row < SpatialDataGrid.DataSource.Rows.Count; row++)
                        column.Add(SpatialDataGrid.DataSource.Rows[row][col].ToString());
                    newTable.Add(column);
                }
                return newTable;
            }
            set
            {
                if (value == null || !value.Any())
                    throw new ArgumentNullException("Spatial data cannot be null.");
                DataTable newTable = new DataTable();
                // data[0] holds the column names
                foreach (string s in value[0])
                    newTable.Columns.Add(new DataColumn(s, typeof(string)));
                for (int i = 0; i < value[1].Count; i++)
                {
                    string[] row = new string[newTable.Columns.Count];
                    for (int j = 1; j < newTable.Columns.Count + 1; j++)
                    {
                        row[j - 1] = value[j][i];
                    }
                    newTable.Rows.Add(row);
                }
                SpatialDataGrid.DataSource = newTable;
                SetupGraphs();
            }
        }

        /// <summary>
        /// Gets the dates shown in the temporal data grid.
        /// </summary>
        /// <returns></returns>
        public DateTime[] Dates
        {
            get
            {
                List<DateTime> dates = new List<DateTime>();
                foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                    if (!string.IsNullOrEmpty((string)row[0]))
                        dates.Add(DateTime.Parse((string)row[0]));
                return dates.ToArray();
            }
        }

        /// <summary>
        /// Gets the heights shown in the temporal data grid.
        /// </summary>
        public double[] Heights
        {
            get
            {
                List<double> heights = new List<double>();
                foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                    if (!string.IsNullOrEmpty((string)row[1]))
                        heights.Add(Convert.ToDouble((string)row[1], System.Globalization.CultureInfo.InvariantCulture) * 1000.0);
                return heights.ToArray();
            }
        }

        /// <summary>
        /// Gets the N Demands shown in the temporal data grid.
        /// </summary>
        public double[] NDemands
        {
            get
            {
                List<double> NDemands = new List<double>();
                foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                    if (!string.IsNullOrEmpty((string)row[2]))
                        NDemands.Add(Convert.ToDouble((string)row[2], System.Globalization.CultureInfo.InvariantCulture));
                return NDemands.ToArray();
            }
        }

        /// <summary>
        /// Gets the canopy widths shown in the temporal data grid.
        /// </summary>
        public double[] CanopyWidths
        {
            get
            {
                List<double> CanopyWidths = new List<double>();
                foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                    if (!string.IsNullOrEmpty((string)row[3]))
                        CanopyWidths.Add(Convert.ToDouble((string)row[3], System.Globalization.CultureInfo.InvariantCulture));
                return CanopyWidths.ToArray();
            }
        }

        /// <summary>
        /// Gets the tree leaf areas shown in the temporal data grid.
        /// </summary>
        public double[] TreeLeafAreas
        {
            get
            {
                List<double> TreeLeafAreas = new List<double>();
                foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                    if (!string.IsNullOrEmpty((string)row[4]))
                        TreeLeafAreas.Add(Convert.ToDouble((string)row[4], System.Globalization.CultureInfo.InvariantCulture));
                return TreeLeafAreas.ToArray();
            }
        }

        /// <summary>
        /// Invoked when the user finishes editing a cell.
        /// </summary>
        public event EventHandler OnCellEndEdit;

        /// <summary>
        /// Setup the spatial data grid.
        /// </summary>
        /// <param name="dates">Dates to be displayed in the dates column.</param>
        /// <param name="heights">Heights to be displayed in the heights column.</param>
        /// <param name="NDemands">N Demands to be displayed in the N Demands column.</param>
        /// <param name="CanopyWidths">Canopy widths to be displayed in the canopy widths column.</param>
        /// <param name="TreeLeafAreas">Tree leaf areas to be displayed in the leaf areas column.</param>
        public void SetupHeights(DateTime[] dates, double[] heights, double[] NDemands, double[] CanopyWidths, double[] TreeLeafAreas)
        {
            string[] colLabels = new string[] { "Date", "Height (m)", "N Demands (g/m2)", "Canopy Width (m)", "Tree Leaf Area (m2)" };
            DataTable table = new DataTable("Height Data");

            // Use the string column type for everything.
            for (int i = 0; i < 5; i++)
                table.Columns.Add(colLabels[i], typeof(string));

            for (int i = 0; i < dates.Length; i++)
                table.Rows.Add(dates[i].ToShortDateString(), (heights[i] / 1000).ToString(), NDemands[i].ToString(), CanopyWidths[i].ToString(), TreeLeafAreas[i].ToString());
            TemporalDataGrid.DataSource = table;
        }

        /// <summary>
        /// Setup the graphs shown below the grids.
        /// </summary>
        private void SetupGraphs()
        {
            double[] x = { 0, 0.5, 1, 1.5, 2, 2.5, 3, 4, 5, 6 };
            try
            {
                aboveGroundGraph.Model.Axes.Clear();
                aboveGroundGraph.Model.Series.Clear();
                belowGroundGraph.Model.Axes.Clear();
                belowGroundGraph.Model.Series.Clear();
                aboveGroundGraph.Model.Title = "Above Ground";
                aboveGroundGraph.Model.PlotAreaBorderColor = OxyColors.White;
                aboveGroundGraph.Model.LegendBorder = OxyColors.Transparent;
                LinearAxis agxAxis = new LinearAxis();
                agxAxis.Title = "Multiple of Tree Height";
                agxAxis.AxislineStyle = LineStyle.Solid;
                agxAxis.AxisDistance = 2;
                agxAxis.Position = AxisPosition.Top;

                LinearAxis agyAxis = new LinearAxis();
                agyAxis.Title = "%";
                agyAxis.AxislineStyle = LineStyle.Solid;
                agyAxis.AxisDistance = 2;
                Utility.LineSeriesWithTracker seriesShade = new Utility.LineSeriesWithTracker();
                List<DataPoint> pointsShade = new List<DataPoint>();
                DataRow rowShade = SpatialDataGrid.DataSource.Rows[0];
                DataColumn col = SpatialDataGrid.DataSource.Columns[0];
                double[] yShade = new double[SpatialDataGrid.DataSource.Columns.Count - 1];

                aboveGroundGraph.Model.Axes.Add(agxAxis);
                aboveGroundGraph.Model.Axes.Add(agyAxis);

                for (int i = 1; i < SpatialDataGrid.DataSource.Columns.Count; i++)
                {
                    if (rowShade[i].ToString() == "")
                        return;
                    yShade[i - 1] = Convert.ToDouble(rowShade[i], 
                                                     System.Globalization.CultureInfo.InvariantCulture);
                }

                for (int i = 0; i < x.Length; i++)
                {
                    pointsShade.Add(new DataPoint(x[i], yShade[i]));
                }
                seriesShade.Title = "Shade";
                seriesShade.ItemsSource = pointsShade;
                aboveGroundGraph.Model.Series.Add(seriesShade);
            }
            //don't draw the series if the format is wrong
            catch (FormatException)
            {
                belowGroundGraph.Model.Series.Clear();
            }

            /////////////// Below Ground
            try
            {
                belowGroundGraph.Model.Title = "Below Ground";
                belowGroundGraph.Model.PlotAreaBorderColor = OxyColors.White;
                belowGroundGraph.Model.LegendBorder = OxyColors.Transparent;
                LinearAxis bgxAxis = new LinearAxis();
                LinearAxis bgyAxis = new LinearAxis();

                bgyAxis.Position = AxisPosition.Left;
                bgxAxis.Position = AxisPosition.Top;
                bgyAxis.Title = "Depth (mm)";

                bgxAxis.Title = "Root Length Density (cm/cm3)";
                bgxAxis.Minimum = 0;
                bgxAxis.MinorTickSize = 0;
                bgxAxis.AxislineStyle = LineStyle.Solid;
                bgxAxis.AxisDistance = 2;
                belowGroundGraph.Model.Axes.Add(bgxAxis);

                bgyAxis.StartPosition = 1;
                bgyAxis.EndPosition = 0;
                bgyAxis.MinorTickSize = 0;
                bgyAxis.AxislineStyle = LineStyle.Solid;
                belowGroundGraph.Model.Axes.Add(bgyAxis);

                for (int i = 1; i < SpatialDataGrid.DataSource.Columns.Count; i++)
                {
                    Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                    series.Title = SpatialDataGrid.DataSource.Columns[i].ColumnName;
                    double[] data = new double[SpatialDataGrid.DataSource.Rows.Count - 4];
                    for (int j = 4; j < SpatialDataGrid.DataSource.Rows.Count; j++)
                    {
                        data[j - 4] = Convert.ToDouble(SpatialDataGrid.DataSource.Rows[j].Field<string>(i), 
                                                       System.Globalization.CultureInfo.InvariantCulture);
                    }

                    List<DataPoint> points = new List<DataPoint>();

                    for (int j = 0; j < Math.Min(data.Length, SoilMidpoints.Length); j++)
                    {
                        points.Add(new DataPoint(data[j], SoilMidpoints[j]));
                    }
                    series.ItemsSource = points;
                    belowGroundGraph.Model.Series.Add(series);
                }
            }
            // Don't draw the series if the format is wrong.
            catch (FormatException)
            {
                belowGroundGraph.Model.Series.Clear();
            }
            finally
            {
                aboveGroundGraph.InvalidatePlot(true);
                belowGroundGraph.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Invoked when the main widget is destroyed.
        /// Performs some cleanup, (hopefully) allowing this instance
        /// to be garbage collected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                TemporalDataGrid.Dispose();
                SpatialDataGrid.Dispose();
                _mainWidget.Dispose();
                _mainWidget.Destroyed -= MainWidgetDestroyed;
                _owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked whenever one of the grids is modified.
        /// Propagates the signal up to the presenter.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void GridCellEdited(object sender, EventArgs args)
        {
            try
            {
                OnCellEndEdit?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}