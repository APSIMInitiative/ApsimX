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
        /// Grid which displays the constants.
        /// </summary>
        private GridView constantsGrid;

        /// <summary>
        /// A list to hold all plots to make enumeration easier.
        /// </summary>
        private List<PlotView> plots = new List<PlotView>();

        /// <summary>Current grid cell.</summary>
        private int[] currentCell = new int[2] { -1, -1 };

        /// <summary>
        /// The main panel which holds the grid tabs and the graphs.
        /// </summary>
        private VPaned mainPanel = null;

        /// <summary>
        /// Widget in constants tab which holds the constants grid.
        /// </summary>
        private Alignment constantsTab = null;

        /// <summary>
        /// The container which holds the graphs.
        /// </summary>
        private HBox graphContainer = null;

        /// <summary>
        /// Scrolled window which houses the temporal data.
        /// </summary>
        private ScrolledWindow temporalDataTab = null;

        private ScrolledWindow spatialDataTab = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeProxyView" /> class.
        /// </summary>
        public TreeProxyView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.TreeProxyView.glade");
            temporalDataTab = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            spatialDataTab = (ScrolledWindow)builder.GetObject("scrolledwindow2");
            mainPanel = (VPaned)builder.GetObject("vpaned1");
            constantsTab = (Alignment)builder.GetObject("alignment1");
            graphContainer = (HBox)builder.GetObject("hbox1");
            _mainWidget = mainPanel;

            TemporalDataGrid = new GridView(this as ViewBase);
            TemporalDataGrid.CellsChanged += GridCellEdited;
            temporalDataTab.Add((TemporalDataGrid as GridView).MainWidget);

            SpatialDataGrid = new GridView(this as ViewBase);
            SpatialDataGrid.CellsChanged += GridCellEdited;
            spatialDataTab.Add((SpatialDataGrid as GridView).MainWidget);

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

            constantsGrid = new GridView(this);
            constantsTab.Add(this.constantsGrid.MainWidget);
            //spatialDataGrid.CursorChanged += GridCursorChanged;
            MainWidget.ShowAll();
            _mainWidget.Destroyed += MainWidgetDestroyed;
        }

        /// <summary>
        /// Depth midpoints of the soil layers
        /// </summary>
        public double[] SoilMidpoints { get; set; }

        /// <summary>
        /// Grid which displays the temporal data.
        /// </summary>
        public IGridView TemporalDataGrid { get; }

        /// <summary>
        /// Grid which displays the spatial data.
        /// </summary>
        public IGridView SpatialDataGrid { get; }

        /// <summary>
        /// The data being displayed in the grid.
        /// </summary>
        public DataTable Table { get; private set; }

        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            TemporalDataGrid.Dispose();
            SpatialDataGrid.Dispose();
            _mainWidget.Destroyed -= MainWidgetDestroyed;
            _owner = null;
        }

        /// <summary>
        /// Constants grid.
        /// </summary>
        public GridView ConstantsGrid { get { return constantsGrid; } }

        /// <summary>
        /// Invoked when the user finishes editing a cell.
        /// </summary>
        public event EventHandler OnCellEndEdit;

        /// <summary>
        /// Return an axis that has the specified AxisType. Returns null if not found.
        /// </summary>
        /// <param name="axisType">The axis type to retrieve </param>
        /// <returns>The axis</returns>
        private OxyPlot.Axes.Axis GetAxis(Models.Graph.Axis.AxisType axisType)
        {
            return null;
        }

        public void SetupGrid(List<List<string>> data)
        {
            Table = new DataTable();

            // data[0] holds the column names
            foreach (string s in data[0])
                Table.Columns.Add(new DataColumn(s, typeof(string)));
            
            for (int i = 0; i < data[1].Count; i++)
            {
                string[] row = new string[Table.Columns.Count];
                for (int j = 1; j < Table.Columns.Count + 1; j++)
                {
                    row[j - 1] = data[j][i];
                }
                Table.Rows.Add(row);
            }
            SpatialDataGrid.DataSource = Table;
            SetupGraphs();
        }

        /// <summary>
        /// Invoked whenever one of the grids is modified.
        /// Propagates the signal up to the presenter.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void GridCellEdited(object sender, EventArgs args)
        {
            OnCellEndEdit?.Invoke(this, EventArgs.Empty);
        }

        public void OnSetGridData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            // This currently doesn't work, as this sort of low-level display stuff is now handled by the GridView.
            TreePath path = model.GetPath(iter);
            int row = path.Indices[0];
            if (cell is CellRendererText)
                (cell as CellRendererText).Background = row == 1 || row == 2 ? "lightgray" : "white";
        }

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
                DataRow rowShade = Table.Rows[0];
                DataColumn col = Table.Columns[0];
                double[] yShade = new double[Table.Columns.Count - 1];

                aboveGroundGraph.Model.Axes.Add(agxAxis);
                aboveGroundGraph.Model.Axes.Add(agyAxis);

                for (int i = 1; i < Table.Columns.Count; i++)
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

                for (int i = 1; i < Table.Columns.Count; i++)
                {
                    Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                    series.Title = Table.Columns[i].ColumnName;
                    double[] data = new double[Table.Rows.Count - 4];
                    for (int j = 4; j < Table.Rows.Count; j++)
                    {
                        data[j - 4] = Convert.ToDouble(Table.Rows[j].Field<string>(i), 
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

        public DateTime[] SaveDates()
        {
            List<DateTime> dates = new List<DateTime>();
            foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                if (!string.IsNullOrEmpty((string)row[0]))
                   dates.Add(DateTime.Parse((string)row[0]));
            return dates.ToArray();
        }

        public double[] SaveHeights()
        {
            List<double> heights = new List<double>();
            foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                if (!string.IsNullOrEmpty((string)row[1]))
                    heights.Add(Convert.ToDouble((string)row[1], System.Globalization.CultureInfo.InvariantCulture) * 1000.0);
            return heights.ToArray();
        }

        public double[] SaveNDemands()
        {
            List<double> NDemands = new List<double>();
            foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                if (!string.IsNullOrEmpty((string)row[2]))
                    NDemands.Add(Convert.ToDouble((string)row[2], System.Globalization.CultureInfo.InvariantCulture));
            return NDemands.ToArray();
        }

        public double[] SaveCanopyWidths()
        {
            List<double> CanopyWidths = new List<double>();
            foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                if (!string.IsNullOrEmpty((string)row[3]))
                    CanopyWidths.Add(Convert.ToDouble((string)row[3], System.Globalization.CultureInfo.InvariantCulture));
            return CanopyWidths.ToArray();
        }

        public double[] SaveTreeLeafAreas()
        {
            List<double> TreeLeafAreas = new List<double>();
            foreach (DataRow row in TemporalDataGrid.DataSource.Rows)
                if (!string.IsNullOrEmpty((string)row[4]))
                    TreeLeafAreas.Add(Convert.ToDouble((string)row[4], System.Globalization.CultureInfo.InvariantCulture));
            return TreeLeafAreas.ToArray();
        }
    }
}