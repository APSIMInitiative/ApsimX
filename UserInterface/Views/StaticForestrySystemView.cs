// -----------------------------------------------------------------------
// <copyright file="ForestryView.cs" company="CSIRO">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Drawing;
    using System.Data;
    using System.Windows.Forms;
    using Interfaces;
    using Models.Graph;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.WindowsForms;
    using EventArguments;
    using Classes;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class StaticForestrySystemView : UserControl, Interfaces.IGraphView
    {
        /// <summary>
        /// A list to hold all plots to make enumeration easier.
        /// </summary>
        private List<PlotView> plots = new List<PlotView>();

        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        private const double FontSize = 14;

        /// <summary>
        /// A table to hold tree data which is bound to the grid.
        /// </summary>
        private DataTable table;

        /// <summary>
        /// Overall font to use.
        /// </summary>
        private new const string Font = "Calibri Light";

        /// <summary>
        /// Margin to use
        /// </summary>
        private const int TopMargin = 75;

        /// <summary>The smallest date used on any axis.</summary>
        private DateTime smallestDate = DateTime.MaxValue;

        /// <summary>The largest date used on any axis</summary>
        private DateTime largestDate = DateTime.MinValue;

        /// <summary>Current grid cell.</summary>
        private int[] currentCell = new int[2] {-1, -1};

        /// <summary>
        /// Depth midpoints of the soil layers
        /// </summary>
        public double[] SoilMidpoints;

        /// <summary>
        /// Nitrogen demand across all Zones
        /// </summary>
        public double NDemand;

        /// <summary>
        /// Root radius in cm
        /// </summary>
        public double RootRadius;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticForestrySystemView" /> class.
        /// </summary>
        public StaticForestrySystemView()
        {
            this.InitializeComponent();
            this.pAboveGround.Model = new PlotModel();
            this.pBelowGround.Model = new PlotModel();
            plots.Add(pAboveGround);
            plots.Add(pBelowGround);
            smallestDate = DateTime.MaxValue;
            largestDate = DateTime.MinValue;
        }

        /// <summary>
        /// Invoked when the user clicks on the plot area (the area inside the axes)
        /// </summary>
        public event EventHandler OnPlotClick
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Update the graph data sources; this causes the axes minima and maxima to be calculated
        /// </summary>
        public void UpdateView()
        {
            foreach (PlotView plotView in plots)
            {
                IPlotModel theModel = plotView.Model as IPlotModel;
                if (theModel != null)
                    theModel.Update(true);
            }
        }

        /// <summary>
        /// Stub method for interface. This method is not used as the plots are not editable.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="xAxisType"></param>
        /// <param name="yAxisType"></param>
        /// <param name="colour"></param>
        /// <param name="lineType"></param>
        /// <param name="markerType"></param>
        /// <param name="showOnLegend"></param>
        public void DrawLineAndMarkers(
     string title,
     IEnumerable x,
     IEnumerable y,
     Models.Graph.Axis.AxisType xAxisType,
     Models.Graph.Axis.AxisType yAxisType,
     Color colour,
     Models.Graph.LineType lineType,
     Models.Graph.MarkerType markerType,
     bool showOnLegend)
        {
        }

        /// <summary>
        /// Stub method for interface. This method is not used as the plots are not editable.
        /// </summary>
        /// <param name="text">The text for the footer</param>
        /// <param name="italics">Italics?</param>
        public void FormatCaption(string text, bool italics)
        {
        }

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        /// <param name="legendOutside">Put legend outside of graph?</param>
        public void Export(Bitmap bitmap, bool legendOutside)
        {
            //TODO: This will only save the last bitmap. Might need to change the interface.
            foreach (PlotView plot in plots)
            {
                DockStyle saveStyle = plot.Dock;
                plot.Dock = DockStyle.None;
                plot.Width = bitmap.Width;
                plot.Height = bitmap.Height;

                LegendPosition savedLegendPosition = LegendPosition.RightTop;
                if (legendOutside)
                {
                    savedLegendPosition = plot.Model.LegendPosition;
                    plot.Model.LegendPlacement = LegendPlacement.Outside;
                    plot.Model.LegendPosition = LegendPosition.RightTop;
                }

                plot.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                if (legendOutside)
                {
                    plot.Model.LegendPlacement = LegendPlacement.Inside;
                    plot.Model.LegendPosition = savedLegendPosition;
                }

                plot.Dock = saveStyle;
            }
        }

        /// <summary>
        /// Invoked when the user clicks on an axis.
        /// </summary>
        public event ClickAxisDelegate OnAxisClick
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Invoked when the user finishes editing a cell.
        /// </summary>
        public event EventHandler OnCellEndEdit;

        /// <summary>
        /// Invoked when the user clicks on a legend.
        /// </summary>
        public event EventHandler<LegendClickArgs> OnLegendClick
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Invoked when the user clicks on the graph title.
        /// </summary>
        public event EventHandler OnTitleClick
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Invoked when the user clicks on the graph caption.
        /// </summary>
        public event EventHandler OnCaptionClick
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Invoked when the user hovers over a series point.
        /// </summary>
        public event EventHandler<EventArguments.HoverPointArgs> OnHoverOverPoint
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Left margin in pixels.
        /// </summary>
        public int LeftRightPadding { get; set; }

        /// <summary>
        /// Clear the graphs of everything.
        /// </summary>
        public void Clear()
        {
            foreach (PlotView p in plots)
            {
                p.Model.Series.Clear();
                p.Model.Axes.Clear();
                p.Model.Annotations.Clear();
            }
        }

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        public override void Refresh()
        {
            foreach (PlotView p in plots)
            {
                p.Model.DefaultFont = Font;
                p.Model.DefaultFontSize = FontSize;

                p.Model.PlotAreaBorderThickness = new OxyThickness(0);
                p.Model.LegendBorder = OxyColors.Transparent;
                p.Model.LegendBackground = OxyColors.White;
                p.Model.InvalidatePlot(true);

                if (this.LeftRightPadding != 0)
                    this.Padding = new Padding(this.LeftRightPadding, 0, this.LeftRightPadding, 0);

                foreach (OxyPlot.Axes.Axis axis in p.Model.Axes)
                {
                    this.FormatAxisTickLabels(axis);
                }

                p.Model.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Draw a bar series with the specified arguments.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x">The x values for the series</param>
        /// <param name="y">The y values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawBar(
            string title,
            IEnumerable x,
            IEnumerable y,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
        }

        /// <summary>
        /// Draw an  area series with the specified arguments. A filled polygon is
        /// drawn with the x1, y1, x2, y2 coordinates.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x1">The x1 values for the series</param>
        /// <param name="y1">The y1 values for the series</param>
        /// <param name="x2">The x2 values for the series</param>
        /// <param name="y2">The y2 values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawArea(
            string title,
            IEnumerable x1,
            IEnumerable y1,
            IEnumerable x2,
            IEnumerable y2,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
        }

        /// <summary>
        /// Draw text on the graph at the specified coordinates.
        /// </summary>
        /// <param name="text">The text to put on the graph</param>
        /// <param name="x">The x position in graph coordinates</param>
        /// <param name="y">The y position in graph coordinates</param>
        /// <param name="xAxisType">The axis type the x value relates to</param>
        /// <param name="yAxisType">The axis type the y value are relates to</param>
        /// <param name="colour">The color of the text</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawText(
            string text,
            double x,
            double y,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour)
        {
        }

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        /// <param name="axisType">The axis type to format</param>
        /// <param name="title">The axis title. If null then a default axis title will be shown</param>
        /// <param name="inverted">Invert the axis?</param>
        /// <param name="minimum">Minimum axis scale</param>
        /// <param name="maximum">Maximum axis scale</param>
        /// <param name="interval">Axis scale interval</param>
        public void FormatAxis(
            Models.Graph.Axis.AxisType axisType,
            string title,
            bool inverted,
            double minimum,
            double maximum,
            double interval)
        {
            OxyPlot.Axes.Axis oxyAxis = this.GetAxis(axisType);
            if (oxyAxis != null)
            {
                oxyAxis.Title = title;
                oxyAxis.MinorTickSize = 0;
                oxyAxis.AxislineStyle = LineStyle.Solid;
                oxyAxis.AxisTitleDistance = 10;
                if (inverted)
                {
                    oxyAxis.StartPosition = 1;
                    oxyAxis.EndPosition = 0;
                }
                else
                {
                    oxyAxis.StartPosition = 0;
                    oxyAxis.EndPosition = 1;
                }
                if (!double.IsNaN(minimum))
                    oxyAxis.Minimum = minimum;
                if (!double.IsNaN(maximum))
                    oxyAxis.Maximum = maximum;
                if (!double.IsNaN(interval) && interval > 0)
                    oxyAxis.MajorStep = interval;
            }
        }

        /// <summary>
        /// Format the legend.
        /// </summary>
        /// <param name="legendPositionType">Position of the legend</param>
        public void FormatLegend(Models.Graph.Graph.LegendPositionType legendPositionType)
        {
            LegendPosition oxyLegendPosition;
            if (Enum.TryParse<LegendPosition>(legendPositionType.ToString(), out oxyLegendPosition))
            {
                foreach (PlotView p in plots)
                {
                    p.Model.LegendFont = Font;
                    p.Model.LegendFontSize = FontSize;
                    p.Model.LegendPosition = oxyLegendPosition;
                    p.Model.LegendSymbolLength = 30;
                }
            }
        }

        /// <summary>
        /// Format the title.
        /// </summary>
        /// <param name="text">Text of the title</param>
        public void FormatTitle(string text)
        {
        }

        /// <summary>
        /// Format the footer.
        /// </summary>
        /// <param name="text">The text for the footer</param>
        public void FormatCaption(string text)
        {
        }

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        /// <param name="editor">The editor to show</param>
        public void ShowEditorPanel(UserControl editor)
        {
        }

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        public void Export(Bitmap bitmap)
        {
            int i = 0;
            foreach (PlotView p in plots)
            {
                p.Dock = DockStyle.None;
                p.Width = bitmap.Width;
                p.Height = bitmap.Height / 2;
                p.DrawToBitmap(bitmap, new Rectangle(0, p.Height * i, bitmap.Width, bitmap.Height / 2));
                p.Dock = DockStyle.Fill;
                i++;
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="menuText">Menu item text</param>
        /// <param name="ticked">Menu ticked?</param>
        /// <param name="onClick">Event handler for menu item click</param>
        public void AddContextAction(string menuText, bool ticked, System.EventHandler onClick)
        {
        }

        /// <summary>
        /// Gets the interval (major step) of the specified axis.
        /// </summary>
        public double AxisMajorStep(Models.Graph.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
            {
                return axis.IntervalLength;
            }
            else
                return double.NaN;
        }

        /// <summary>
        /// Event handler for when user clicks close
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCloseEditorPanel(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Format axis tick labels so that there is a leading zero on the tick
        /// labels when necessary.
        /// </summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxisTickLabels(OxyPlot.Axes.Axis axis)
        {
            axis.IntervalLength = 100;

            if (axis is DateTimeAxis)
            {
                DateTimeAxis dateAxis = axis as DateTimeAxis;

                int numDays = (largestDate - smallestDate).Days;
                if (numDays < 100)
                    dateAxis.IntervalType = DateTimeIntervalType.Days;
                else if (numDays <= 366)
                {
                    dateAxis.IntervalType = DateTimeIntervalType.Months;
                    dateAxis.StringFormat = "dd-MMM";
                }
                else
                    dateAxis.IntervalType = DateTimeIntervalType.Years;
            }

            if (axis is LinearAxis &&
                (axis.ActualStringFormat == null || !axis.ActualStringFormat.Contains("yyyy")))
            {
                // We want the axis labels to always have a leading 0 when displaying decimal places.
                // e.g. we want 0.5 rather than .5

                // Use the current culture to format the string.
                string st = axis.ActualMajorStep.ToString(System.Globalization.CultureInfo.InvariantCulture);

                // count the number of decimal places in the above string.
                int pos = st.IndexOfAny(".,".ToCharArray());
                if (pos != -1)
                {
                    int numDecimalPlaces = st.Length - pos - 1;
                    axis.StringFormat = "F" + numDecimalPlaces.ToString();
                }
            }
        }

        /// <summary>
        /// Populate the specified DataPointSeries with data from the data table.
        /// </summary>
        /// <param name="x">The x values</param>
        /// <param name="y">The y values</param>
        /// <param name="xAxisType">The x axis the data is associated with</param>
        /// <param name="yAxisType">The y axis the data is associated with</param>
        /// <returns>A list of 'DataPoint' objects ready to be plotted</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        private List<DataPoint> PopulateDataPointSeries(
            IEnumerable x,
            IEnumerable y,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType)
        {
                return null;
        }

        /// <summary>
        /// Return an axis that has the specified AxisType. Returns null if not found.
        /// </summary>
        /// <param name="axisType">The axis type to retrieve </param>
        /// <returns>The axis</returns>
        private OxyPlot.Axes.Axis GetAxis(Models.Graph.Axis.AxisType axisType)
        {
            return null;
        }

        /// <summary>
        /// Gets the maximum scale of the specified axis.
        /// </summary>
        public double AxisMaximum(Models.Graph.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);
            if (axis != null)
            {
                return axis.ActualMaximum;
            }
            else
                return double.NaN;
        }

        /// <summary>
        /// Gets the minimum scale of the specified axis.
        /// </summary>
        public double AxisMinimum(Models.Graph.Axis.AxisType axisType)
        {
            foreach (PlotView p in plots)
                p.Refresh();
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
            {
                return axis.ActualMinimum;
            }
            else
                return double.NaN;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns></returns>
        public string[] GetSeriesNames()
        {
            List<string> names = new List<string>();
            foreach (OxyPlot.Series.Series series in this.pAboveGround.Model.Series)
            {
                names.Add("AG" + series.Title);
            }
            foreach (OxyPlot.Series.Series series in this.pBelowGround.Model.Series)
            {
                names.Add("BG" + series.Title);
            }
            return names.ToArray();
        }

        public void SetupGrid(List<List<string>> data, double[] zoneWidths)
        {
            // setup scalar variables
            Scalars.Rows.Clear();
            Scalars.Rows.Add("Nitrogen demand (kg/ha)", NDemand);
            Scalars.Rows.Add("Root radius (cm)", RootRadius);

            table = new DataTable();
            // data[0] holds the column names
            foreach (string s in data[0])
            {
                table.Columns.Add(new DataColumn(s, typeof(string)));
            }

            for (int i = 0; i < data[1].Count; i++)
            {
                string[] row = new string[table.Columns.Count];
                for (int j = 1; j < table.Columns.Count + 1; j++)
                {
                    row[j - 1] = data[j][i];
                }
                table.Rows.Add(row);
            }
            Grid.DataSource = table;
            Grid.Columns[0].ReadOnly = true; //name column
            Grid.Rows[1].ReadOnly = true; //RLD title row
            Grid.Rows[1].DefaultCellStyle.BackColor = Color.LightGray;
            Grid.Rows[2].ReadOnly = true; //Depth title row
            Grid.Rows[2].DefaultCellStyle.BackColor = Color.LightGray;
            ResizeControls();

            SetupGraphs(zoneWidths);
        }

        public void SetupHeights(DateTime[] dates, double[] heights)
        {
            for (int i = 0; i < dates.Count(); i++)
            {
                dgvHeights.Rows.Add(dates[i].ToShortDateString(), heights[i] / 1000);
            }
        }

        private void ResizeControls()
        {
            //resize Scalars
            int width = 0;
            int height = 0;
            foreach (DataGridViewColumn col in Scalars.Columns)
                width += col.Width;
            foreach (DataGridViewRow row in Scalars.Rows)
                height += row.Height;
            Scalars.Width = width + 3;
            if (height + 25 > Scalars.Parent.Height / 3)
            {
                Scalars.Height = Scalars.Parent.Height / 3;
                Scalars.Width += 20; //extra width for scrollbar
            }
            else
                Scalars.Height = height + 25;

            //relocate date picker
            calendar.Location = new Point(0, Scalars.Location.Y + Scalars.Height + 10);

            //resize tree heights grid
            int hWidth = 0;
            int hHeight = 0;
            foreach (DataGridViewColumn col in dgvHeights.Columns)
                hWidth += col.Width;
            foreach (DataGridViewRow row in dgvHeights.Rows)
                hHeight += row.Height;
            dgvHeights.Width = hWidth + dgvHeights.RowHeadersWidth + 3;
            if (hHeight + 25 >= Grid.Parent.Height / 3)
            {
                dgvHeights.Height = Grid.Parent.Height / 3;
                dgvHeights.Width += 25;
            }
            else
                dgvHeights.Height = hHeight + dgvHeights.ColumnHeadersHeight + 3; //ternary is to catch case where Rows.Count == 0
            dgvHeights.Location = new Point(calendar.Width + 3, 0);

            //resize Grid
            width = 0;
            height = 0;

            foreach (DataGridViewColumn col in Grid.Columns)
                width += col.Width;
            foreach (DataGridViewRow row in Grid.Rows)
                height += row.Height;
            Grid.Width = width + 3;
            if (height + 25 > Grid.Parent.Height / 3)
            {
                Grid.Height = Grid.Parent.Height / 3;
                Grid.Width += 25; //extra width for scrollbar
            }
            else
                Grid.Height = height + 25;

            Grid.Location = new Point(dgvHeights.Location.X + dgvHeights.Width + 3, 0);
            if (Grid.Width + Grid.Location.X > Grid.Parent.Width)
                Grid.Width = Grid.Parent.Width - Grid.Location.X;

            height = Math.Max(Grid.Height, dgvHeights.Height);
            width = Math.Max(Grid.Width, dgvHeights.Width);


            //resize above ground graph
            pAboveGround.Width = pAboveGround.Parent.Width / 2;
            pAboveGround.Height = pAboveGround.Parent.Height - height;
            pAboveGround.Location = new Point(0, height);

            //resize below ground graph
            pBelowGround.Width = pBelowGround.Parent.Width / 2;
            pBelowGround.Height = pBelowGround.Parent.Height - height;
            pBelowGround.Location = new Point(pAboveGround.Width, height);
        }

        private void SetupGraphs(double[] x)
        {
            try
            {
                pAboveGround.Model.Axes.Clear();
                pAboveGround.Model.Series.Clear();
                pBelowGround.Model.Axes.Clear();
                pBelowGround.Model.Series.Clear();
                pAboveGround.Model.Title = "Above Ground";
                pAboveGround.Model.PlotAreaBorderColor = OxyColors.White;
                pAboveGround.Model.LegendBorder = OxyColors.Transparent;
                LinearAxis agxAxis = new LinearAxis();
                agxAxis.Title = "Zone";
                agxAxis.AxislineStyle = LineStyle.Solid;
                agxAxis.AxisDistance = 2;
                agxAxis.Position = AxisPosition.Top;

                LinearAxis agyAxis = new LinearAxis();
                agyAxis.Title = "%";
                agyAxis.AxislineStyle = LineStyle.Solid;
                agyAxis.AxisDistance = 2;
                Utility.LineSeriesWithTracker seriesShade = new Utility.LineSeriesWithTracker();
                List<DataPoint> pointsShade = new List<DataPoint>();
                DataRow rowShade = table.Rows[0];
                DataColumn col = table.Columns[0];
                double[] yShade = new double[table.Columns.Count - 1];

                pAboveGround.Model.Axes.Add(agxAxis);
                pAboveGround.Model.Axes.Add(agyAxis);

                for (int i = 1; i < table.Columns.Count; i++)
                {
                    if (rowShade[i].ToString() == "")
                        return;
                    yShade[i - 1] = Convert.ToDouble(rowShade[i]);
                }

                for (int i = 0; i < x.Length; i++)
                {
                    pointsShade.Add(new DataPoint(x[i], yShade[i]));
                }
                seriesShade.Title = "Shade";
                seriesShade.ItemsSource = pointsShade;
                pAboveGround.Model.Series.Add(seriesShade);
            }
            //don't draw the series if the format is wrong
            catch (FormatException)
            {
                pBelowGround.Model.Series.Clear();
            }

            /////////////// Below Ground
            try
            {
                pBelowGround.Model.Title = "Below Ground";
                pBelowGround.Model.PlotAreaBorderColor = OxyColors.White;
                pBelowGround.Model.LegendBorder = OxyColors.Transparent;
                LinearAxis bgxAxis = new LinearAxis();
                LinearAxis bgyAxis = new LinearAxis();
                List<Utility.LineSeriesWithTracker> seriesList = new List<Utility.LineSeriesWithTracker>();

                bgyAxis.Position = AxisPosition.Left;
                bgxAxis.Position = AxisPosition.Top;
                bgyAxis.Title = "Depth (mm)";

                bgxAxis.Title = "Root Length Density (cm/cm3)";
                bgxAxis.Minimum = 0;
                bgxAxis.MinorTickSize = 0;
                bgxAxis.AxislineStyle = LineStyle.Solid;
                bgxAxis.AxisDistance = 2;
                pBelowGround.Model.Axes.Add(bgxAxis);

                bgyAxis.StartPosition = 1;
                bgyAxis.EndPosition = 0;
                bgyAxis.MinorTickSize = 0;
                bgyAxis.AxislineStyle = LineStyle.Solid;
                pBelowGround.Model.Axes.Add(bgyAxis);

                for (int i = 1; i < table.Columns.Count; i++)
                {
                    Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                    series.Title = table.Columns[i].ColumnName;
                    double[] data = new double[table.Rows.Count - 4];
                    for (int j = 4; j < table.Rows.Count; j++)
                    {
                        data[j - 4] = Convert.ToDouble(table.Rows[j].Field<string>(i));
                    }

                    List<DataPoint> points = new List<DataPoint>();
                    
                    for (int j = 0; j < data.Length; j++)
                    {
                        points.Add(new DataPoint(data[j], SoilMidpoints[j]));
                    }
                    series.ItemsSource = points;
                    pBelowGround.Model.Series.Add(series);
                }
            }
            //don't draw the series if the format is wrong
            catch (FormatException)
            {
                pBelowGround.Model.Series.Clear();
            }
            finally
            {
                pAboveGround.InvalidatePlot(true);
                pBelowGround.InvalidatePlot(true);
            }
        }


        public DataTable GetTable()
        {
            return table;
        }

        public DateTime[] SaveDates()
        {
            List<DateTime> dates = new List<DateTime>();
            foreach(DataGridViewRow row in dgvHeights.Rows)
            {
                dates.Add(DateTime.Parse(row.Cells[0].Value.ToString()));
            }
            return dates.ToArray();
        }

        public double[] SaveHeights()
        {
            List<double> heights= new List<double>();
            foreach (DataGridViewRow row in dgvHeights.Rows)
            {
                heights.Add(Convert.ToDouble(row.Cells[1].Value.ToString()) * 1000);
            }
            return heights.ToArray();
        }

        private void ForestryView_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (!Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected)
                return;
            Invoke(OnCellEndEdit);
        }

        private void Scalars_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            double val;
            if (double.TryParse(Scalars.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string, out val))
            {
                NDemand = Convert.ToDouble(Scalars.Rows[0].Cells[1].Value);
                RootRadius = Convert.ToDouble(Scalars.Rows[1].Cells[1].Value);
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                e.Handled = true;
            }
        }

        private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            TextBox box = (TextBox)e.Control;
            box.PreviewKeyDown += Grid_PreviewKeyDown;
        }

        private void Grid_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                int col = Grid.CurrentCell.ColumnIndex;
                int row = Grid.CurrentCell.RowIndex;

                if (row < Grid.RowCount - 1)
                {
                    row++;
                }
                else
                {
                    row = 0;
                    col++;
                }

                if (col < Grid.ColumnCount)
                    currentCell = new int[2] {col, row};
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (currentCell[0] != -1)
            {
                Grid.CurrentCell = Grid[currentCell[0], currentCell[1]];
                currentCell = new int[2] {-1,-1};
            }
        }

        private void dgvHeights_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            ResizeControls();
        }

        private void calendar_DateSelected(object sender, DateRangeEventArgs e)
        {
            dgvHeights.Rows.Add(calendar.SelectionStart.ToShortDateString(), 0);
            ResizeControls();
        }
    }
}