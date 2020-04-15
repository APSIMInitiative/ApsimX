namespace UserInterface.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using Gtk;
    using Interfaces;
    using Models;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.GtkSharp;
    using EventArguments;
    using APSIM.Shared.Utilities;
    using System.Linq;
    using System.Globalization;
    using MathNet.Numerics.Statistics;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public class GraphView : ViewBase, IGraphView
    {
        private double fontSize = 14;

        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        public double FontSize
        {
            get
            {
                return fontSize;
            }
            set
            {
                fontSize = value;
                if (plot1 != null && plot1.Model != null)
                {
                    plot1.Model.DefaultFontSize = value;
                    plot1.Model.LegendFontSize = value;

                    foreach (OxyPlot.Annotations.Annotation annotation in this.plot1.Model.Annotations)
                        if (annotation is OxyPlot.Annotations.TextAnnotation textAnnotation)
                            textAnnotation.FontSize = value;
                }
            }
        }

        private MarkerSizeType markerSize;

        /// <summary>
        /// Marker size.
        /// </summary>
        public MarkerSizeType MarkerSize
        {
            get
            {
                return markerSize;
            }
            set
            {
                markerSize = value;
                double numericValue = GetMarkerSizeNumericValue(value);
                if (plot1 != null && plot1.Model != null)
                    foreach (var series in plot1.Model.Series.OfType<Utility.LineSeriesWithTracker>())
                        series.MarkerSize = numericValue;
            }
        }

        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        //public double MarkerSize = -1;

        /// <summary>
        /// Overall font to use.
        /// </summary>
        private const string Font = "Calibri Light";

        /// <summary>
        /// Margin to use
        /// </summary>
        private const int TopMargin = 75;

        /// <summary>The smallest date used on any axis.</summary>
        private DateTime smallestDate = DateTime.MaxValue;

        /// <summary>The largest date used on any axis</summary>
        private DateTime largestDate = DateTime.MinValue;

        private bool inRightClick = false;

        private OxyPlot.GtkSharp.PlotView plot1;
        private VBox vbox1 = null;
        private Expander expander1 = null;
        private VBox vbox2 = null;
        private Label captionLabel = null;
        private EventBox captionEventBox = null;
        private Label label2 = null;
        private Menu popup = new Menu();

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        public GraphView(ViewBase owner = null) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.GraphView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            expander1 = (Expander)builder.GetObject("expander1");
            vbox2 = (VBox)builder.GetObject("vbox2");
            captionLabel = (Label)builder.GetObject("captionLabel");
            captionEventBox = (EventBox)builder.GetObject("captionEventBox");
            label2 = (Label)builder.GetObject("label2");
            mainWidget = vbox1;

            plot1 = new PlotView();
            plot1.Model = new PlotModel();
            plot1.SetSizeRequest(-1, 100);
            vbox2.PackStart(plot1, true, true, 0);

            smallestDate = DateTime.MaxValue;
            largestDate = DateTime.MinValue;
            this.LeftRightPadding = 40;
            expander1.Visible = false;
            captionEventBox.Visible = true;

            plot1.Model.MouseDown += OnChartClick;
            plot1.Model.MouseUp += OnChartMouseUp;
            plot1.Model.MouseMove += OnChartMouseMove;
            popup.AttachToWidget(plot1, null);

            captionLabel.Text = null;
            captionEventBox.ButtonPressEvent += OnCaptionLabelDoubleClick;
            Color foreground = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;
            ForegroundColour = Utility.Colour.ToOxy(foreground);
            if (!Utility.Configuration.Settings.DarkTheme)
                BackColor = Utility.Colour.ToOxy(Color.White);
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                plot1.Model.MouseDown -= OnChartClick;
                plot1.Model.MouseUp -= OnChartMouseUp;
                plot1.Model.MouseMove -= OnChartMouseMove;
                captionEventBox.ButtonPressEvent -= OnCaptionLabelDoubleClick;
                // It's good practice to disconnect the event handlers, as it makes memory leaks
                // less likely. However, we may not "own" the event handlers, so how do we 
                // know what to disconnect?
                // We can do this via reflection. Here's how it currently can be done in Gtk#.
                // Windows.Forms would do it differently.
                // This may break if Gtk# changes the way they implement event handlers.
                foreach (Widget w in popup)
                {
                    if (w is MenuItem)
                    {
                        PropertyInfo pi = w.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pi != null)
                        {
                            System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                            if (handlers != null && handlers.ContainsKey("activate"))
                            {
                                EventHandler handler = (EventHandler)handlers["activate"];
                                (w as MenuItem).Activated -= handler;
                            }
                        }
                    }
                }
                Clear();
                popup.Dispose();
                plot1.Destroy();
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user clicks on the plot area (the area inside the axes)
        /// </summary>
        public event EventHandler OnPlotClick;

        /// <summary>
        /// Invoked when the user clicks on an axis.
        /// </summary>
        public event ClickAxisDelegate OnAxisClick;

        /// <summary>
        /// Invoked when the user clicks on a legend.
        /// </summary>
        public event EventHandler<LegendClickArgs> OnLegendClick;

        /// <summary>
        /// Invoked when the user clicks on the graph title.
        /// </summary>
        public event EventHandler OnTitleClick;

        /// <summary>
        /// Invoked when the user clicks on the graph caption.
        /// </summary>
        public event EventHandler OnCaptionClick;

        /// <summary>
        /// Invoked when the user hovers over a series point.
        /// </summary>
        public event EventHandler<EventArguments.HoverPointArgs> OnHoverOverPoint;

        /// <summary>Invoked when the user single clicks on the graph</summary>
        public event EventHandler SingleClick;

        /// <summary>
        /// Left margin in pixels.
        /// </summary>
        public int LeftRightPadding { get; set; }

        /// <summary>
        /// Controls the background colour of the graph.
        /// </summary>
        public OxyColor BackColor
        {
            get
            {
                if (plot1 == null || plot1.Model == null)
                    return OxyColors.White;
                return this.plot1.Model.Background;
            }
            set
            {
                if (plot1 != null && plot1.Model != null)
                    this.plot1.Model.Background = value;
            }
        }

        /// <summary>
        /// Controls the foreground colour of the graph.
        /// </summary>
        public OxyColor ForegroundColour
        {
            get
            {
                if (plot1 == null || plot1.Model == null)
                    return OxyColors.Black; // Fallback to black
                return this.plot1.Model.TextColor;
            }
            set
            {
                if (plot1 != null && plot1.Model != null)
                    this.plot1.Model.TextColor = value;
            }
        }

        public int Width
        {
            get { return this.plot1.Allocation.Width > 1 ? this.plot1.Allocation.Width : this.plot1.ChildRequisition.Width; }
            set { this.plot1.WidthRequest = value; }
        }

        public int Height
        {
            get { return this.plot1.Allocation.Height > 1 ? this.plot1.Allocation.Height : this.plot1.ChildRequisition.Height; }
            set { this.plot1.HeightRequest = value; }
        }

        /// <summary>Gets or sets a value indicating if the legend is visible.</summary>
        public bool IsLegendVisible
        {
            get { return this.plot1.Model.IsLegendVisible; }
            set { this.plot1.Model.IsLegendVisible = value; }
        }

        /// <summary>
        /// Iff set to true, the legend will appear inside the graph boundaries.
        /// </summary>
        public bool LegendInsideGraph
        {
            get
            {
                return plot1.Model.LegendPlacement == LegendPlacement.Inside;
            }
            set
            {
                plot1.Model.LegendPlacement = value ? LegendPlacement.Inside : LegendPlacement.Outside;
            }
        }

        /// <summary>
        /// Clear the graph of everything.
        /// </summary>
        public void Clear()
        {
            foreach (OxyPlot.Series.Series series in this.plot1.Model.Series)
                if (series is Utility.LineSeriesWithTracker)
                    (series as Utility.LineSeriesWithTracker).OnHoverOverPoint -= OnHoverOverPoint;
            this.plot1.Model.Series.Clear();
            this.plot1.Model.Axes.Clear();
            this.plot1.Model.Annotations.Clear();
            // modLMC - 11/05/2016 - Need to clear the chart title as well
            this.FormatTitle("");
        }

        /// <summary>
        /// Update the graph data sources; this causes the axes minima and maxima to be calculated
        /// </summary>
        public void UpdateView()
        {
            IPlotModel theModel = this.plot1.Model as IPlotModel;
            if (theModel != null)
                theModel.Update(true);
        }

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        public void Refresh()
        {
            this.plot1.Model.DefaultFontSize = FontSize;
            this.plot1.Model.PlotAreaBorderThickness = new OxyThickness(0.0);
            this.plot1.Model.LegendBorder = OxyColors.Transparent;
            this.plot1.Model.LegendBackground = OxyColors.Transparent;

            if (this.LeftRightPadding != 0)
                this.plot1.Model.Padding = new OxyThickness(10, 10, this.LeftRightPadding, 10);

            foreach (OxyPlot.Axes.Axis axis in this.plot1.Model.Axes)
                this.FormatAxisTickLabels(axis);
         
            this.plot1.Model.LegendFontSize = FontSize;

            foreach (OxyPlot.Annotations.Annotation annotation in this.plot1.Model.Annotations)
            {
                if (annotation is OxyPlot.Annotations.TextAnnotation)
                {
                    OxyPlot.Annotations.TextAnnotation textAnnotation = annotation as OxyPlot.Annotations.TextAnnotation;
                    if (textAnnotation != null)
                        textAnnotation.FontSize = FontSize;
                }
            }

            this.plot1.Model.InvalidatePlot(true);
        }

        /// <summary>
        ///  Draw a line and markers series with the specified arguments.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x">The x values for the series</param>
        /// <param name="y">The y values for the series</param>
        /// <param name="xFieldName">The name of the x variable.</param>
        /// <param name="yFieldName">The name of the y variable.</param>
        /// <param name="error">The error values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        /// <param name="lineType">The type of series line</param>
        /// <param name="markerType">The type of series markers</param>
        /// <param name="lineThickness">The line thickness</param>
        /// <param name="markerSize">The size of the marker</param>
        /// <param name="showInLegend">Show in legend?</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawLineAndMarkers(
             string title,
             IEnumerable x,
             IEnumerable y,
             string xFieldName,
             string yFieldName,
             IEnumerable error,
             Models.Axis.AxisType xAxisType,
             Models.Axis.AxisType yAxisType,
             Color colour,
             Models.LineType lineType,
             Models.MarkerType markerType,
             Models.LineThicknessType lineThickness,
             Models.MarkerSizeType markerSize,
             double markerModifier,
             bool showOnLegend)
        {
            Utility.LineSeriesWithTracker series = null;
            if (x != null && y != null)
            {
                series = new Utility.LineSeriesWithTracker();
                series.OnHoverOverPoint += OnHoverOverPoint;
                if (showOnLegend)
                    series.Title = title;
                else
                    series.ToolTip = title;

                if (colour.ToArgb() == Color.Empty.ToArgb())
                    colour = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;
                series.Color = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);

                series.ItemsSource = this.PopulateDataPointSeries(x, y, xAxisType, yAxisType);

                series.XAxisKey = xAxisType.ToString();
                series.YAxisKey = yAxisType.ToString();

                series.XFieldName = xFieldName;
                series.YFieldName = yFieldName;

                series.CanTrackerInterpolatePoints = false;

                bool filled = false;
                string oxyMarkerName = markerType.ToString();
                if (oxyMarkerName.StartsWith("Filled"))
                {
                    oxyMarkerName = oxyMarkerName.Remove(0, 6);
                    filled = true;
                }

                // Line type.
                LineStyle oxyLineType;
                if (Enum.TryParse<LineStyle>(lineType.ToString(), out oxyLineType))
                {
                    series.LineStyle = oxyLineType;
                    if (series.LineStyle == LineStyle.None)
                        series.Color = OxyColors.Transparent;
                }

                // Line thickness
                if (lineThickness == LineThicknessType.Thin)
                    series.StrokeThickness = 0.5;

                // Marker type.
                OxyPlot.MarkerType type;
                if (Enum.TryParse<OxyPlot.MarkerType>(oxyMarkerName, out type))
                {
                    series.MarkerType = type;
                }

                //MarkerSize = markerSize;
                series.MarkerSize = GetMarkerSizeNumericValue(markerSize) * markerModifier;

                series.MarkerStroke = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                if (filled)
                {
                    series.MarkerFill = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                    series.MarkerStroke = OxyColors.White;
                }

                this.plot1.Model.Series.Add(series);
                if (error != null)
                {
                    ScatterErrorSeries errorSeries = new ScatterErrorSeries();
                    errorSeries.ItemsSource = this.PopulateErrorPointSeries(x, y, error, xAxisType, yAxisType);
                    errorSeries.XAxisKey = xAxisType.ToString();
                    errorSeries.YAxisKey = yAxisType.ToString();
                    errorSeries.ErrorBarColor = series.MarkerFill;
                    this.plot1.Model.Series.Add(errorSeries);
                }
            }

        }

        private double GetMarkerSizeNumericValue(MarkerSizeType markerSize)
        {
            if (markerSize == MarkerSizeType.Large)
                return 9.0;

            if (markerSize == MarkerSizeType.Normal)
                return 7.0;

            if (markerSize == MarkerSizeType.Small)
                return 5.0;

            if (markerSize == MarkerSizeType.VerySmall)
                return 3.0;

            throw new NotImplementedException($"No supported marker size translation for {markerSize}");
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
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
            if (x != null && y != null)
            {
                ColumnXYSeries series = new ColumnXYSeries();
                if (showOnLegend)
                    series.Title = title;
                series.FillColor = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                series.StrokeColor = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                series.ItemsSource = this.PopulateDataPointSeries(x, y, xAxisType, yAxisType);
                series.XAxisKey = xAxisType.ToString();
                series.YAxisKey = yAxisType.ToString();
                // By default, clicking on a datapoint (a bar) of a bar graph
                // will create a pop-up showing the x/y values at the beginning
                // and end of the bar. We override this here, so that it only
                // shows the x/y pair at the end of the bar. Perhaps we should
                // accept the tracker string as an argument to this function?
                series.TrackerFormatString = "{0}\n{1}: {3}\n{4}: {6}";
                this.plot1.Model.Series.Add(series);
            }
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
        public void DrawRegion(
            string title,
            IEnumerable x1,
            IEnumerable y1,
            IEnumerable x2,
            IEnumerable y2,
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
            AreaSeries series = new AreaSeries();
            series.Color = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
            series.Fill = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
            List<DataPoint> points = this.PopulateDataPointSeries(x1, y1, xAxisType, yAxisType);
            List<DataPoint> points2 = this.PopulateDataPointSeries(x2, y2, xAxisType, yAxisType);

            if (showOnLegend)
                series.Title = title;
            if (points != null && points2 != null)
            {
                foreach (DataPoint point in points)
                {
                    series.Points.Add(point);
                }

                foreach (DataPoint point in points2)
                {
                    series.Points2.Add(point);
                }
            }
            series.CanTrackerInterpolatePoints = false;

            this.plot1.Model.Series.Add(series);
        }

        /// <summary>
        /// Checks that the given data is equidistant. Shows a warning
        /// message if this is not true.
        /// </summary>
        /// <param name="x">Data to be tested.</param>
        private void EnsureMonotonic(double[] x)
        {
            double diff = x[1] - x[0];
            for (int i = 1; i < x.Length; i++)
            {
                double newDiff = x[i] - x[i - 1];
                if (!MathUtilities.FloatsAreEqual(diff, newDiff))
                    MasterView.ShowMessage($"WARNING: x data is not monotonic at index {i}; x = [..., {x[i - 2]}, {x[i - 1]}, {x[i]}, ...]", Models.Core.Simulation.ErrorLevel.Warning, withButton: false);
            }
        }

        /// <summary>
        /// Draw an area series with the specified arguments. Similar to a
        /// line series, but the area under the curve will be filled with colour.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x">The x values for the series</param>
        /// <param name="y">The y values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawArea(
            string title,
            IEnumerable x,
            IEnumerable y,
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
            // Just use a region series (colours area between two curves), and use y = 0 for the second curve.
            List<double> y2 = new List<double>();
            y2.AddRange(Enumerable.Repeat(0d, ((ICollection)y).Count));

            DrawRegion(title, x, y2, x, y, xAxisType, yAxisType, colour, showOnLegend);
        }

        /// <summary>
        /// Draw a stacked area series with the specified arguments.Similar to
        /// an area series except that the area between this curve and the
        /// previous curve (or y = 0 if this is first) will be filled with
        /// colour. Currently this only works if y-data is numeric.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x">The x values for the series</param>
        /// <param name="y">The y values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawStackedArea(
            string title,
            object[] x,
            double[] y,
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend)
        {
            if (this.plot1.Model.Series.Count < 1 || plot1.Model.Series.OfType<LineSeries>().Count() < 1)
            {
                // This is the first series to be added to the chart. Just use
                // a region series (colours area between two curves), and use
                // y = 0 for the second curve.
                List<double> y0 = new List<double>();
                y0.AddRange(Enumerable.Repeat(0d, y.Length));
                DrawRegion(title, x, y, x, y0, xAxisType, yAxisType, colour, showOnLegend);
                return;
            }

            // Get x/y data from previous series
            LineSeries previous = plot1.Model.Series.OfType<LineSeries>().Last();

            // This will work if the previous series was an area series.
            double[] x1 = previous.Points.Select(p => p.X).ToArray();
            double[] y1 = previous.Points.Select(p => p.Y).ToArray();

            // This will work if the previous series was a line/scatter series.
            if (x1 == null || x1.Length < 1)
                x1 = previous.ItemsSource.Cast<DataPoint>().Select(p => p.X).ToArray();

            if (y1 == null || y1.Length < 1)
                y1 = previous.ItemsSource.Cast<DataPoint>().Select(p => p.Y).ToArray();

            if (x1 == null || x1.Length < 1 || y1 == null || y1.Length < 1)
                return;

            // Now, for each datapoint in the previous series, we need
            // to add its y-value onto the corresponding data point in
            // the new series so that this area series appears to sit
            // on top of the previous series.
            List<double> y2 = new List<double>();

            Type xType = x[0].GetType();
            bool xIsFloatingPoint = xType == typeof(double) || xType == typeof(float);

            for (int i = 0; i < x1.Length; i++)
            {
                double xVal = x1[i]; // x-value in the previous series

                // The previous series might not have exactly the same set of x
                // values as the new series. First we check if the new series
                // contains this x value. If it does not, we do a linear interp
                // to find an appropriate y-value.
                int index = -1;
                if (xIsFloatingPoint)
                    index = MathUtilities.SafeIndexOf(x.Cast<double>().ToList(), xVal);
                else if (xType == typeof(DateTime))
                    index = Array.IndexOf(x, DateTimeAxis.ToDateTime(xVal));
                else
                    index = i; // Array.IndexOf(x, xVal); // this is unlikely to work

                double yVal = y1[i];
                if (index >= 0)
                    yVal += y[i];
                else if (xIsFloatingPoint)
                    yVal += MathUtilities.LinearInterpReal(xVal, x.Cast<double>().ToArray(), y, out bool didInterp);
                y2.Add(yVal);
            }

            DrawRegion(title, x1, y2, x1, y1, xAxisType, yAxisType, colour, showOnLegend);

            // If the X data is not monotonic, the area will not be
            // filled with colour. In this case, show a warning to the
            // user so they know why their area series is not working.
            AreaSeries series = plot1.Model.Series.OfType<AreaSeries>().LastOrDefault();
            if (series != null)
            {
                EnsureMonotonic(series.Points.Select(p => p.X).ToArray());
                EnsureMonotonic(series.Points2.Select(p => p.X).ToArray());
            }
        }

        /// <summary>
        /// Draw a box-and-whisker plot.
        /// colour.
        /// </summary>
        /// <param name="title">The series title</param>
        /// <param name="x">The x values for the series</param>
        /// <param name="y">The y values for the series</param>
        /// <param name="xAxisType">The axis type the x values are related to</param>
        /// <param name="yAxisType">The axis type the y values are related to</param>
        /// <param name="colour">The series color</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawBoxPLot(
            string title,
            object[] x,
            double[] y,
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType,
            Color colour,
            bool showOnLegend,
            Models.LineType lineType,
            Models.MarkerType markerType,
            Models.LineThicknessType lineThickness)
        {
            if (x?.Length > 0 && y?.Length > 0)
            {
                BoxPlotSeries series = new BoxPlotSeries();
                series.Items = GetBoxPlotItems(x, y, xAxisType, yAxisType);
                if (showOnLegend)
                    series.Title = title;

                // Line style
                if (Enum.TryParse(lineType.ToString(), out LineStyle oxyLineType))
                {
                    series.LineStyle = oxyLineType;
                    if (series.LineStyle == LineStyle.None)
                        series.Fill = OxyColors.Transparent;
                    series.Stroke = OxyColors.Transparent;
                }

                // Min/max lines = marker type
                string marker = markerType.ToString();
                if (marker.StartsWith("Filled"))
                    marker = marker.Remove(0, 6);

                if (Enum.TryParse(marker, out OxyPlot.MarkerType oxyMarkerType))
                    series.OutlierType = oxyMarkerType;

                // Line thickness
                if (lineThickness == LineThicknessType.Thin)
                {
                    double thickness = 0.5;
                    series.StrokeThickness = thickness;
                    series.MeanThickness = thickness;
                    series.MedianThickness = thickness;
                }

                // Colour
                if (colour.ToArgb() == Color.Empty.ToArgb())
                    colour = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;

                OxyColor oxyColour = Utility.Colour.ToOxy(colour);
                series.Fill = oxyColour;
                series.Stroke = oxyColour;

                series.XAxisKey = xAxisType.ToString();
                series.YAxisKey = yAxisType.ToString();

                double width = 0.5;
                series.BoxWidth = width;
                series.WhiskerWidth = width;

                plot1.Model.Series.Add(series);

                OxyPlot.Axes.Axis xAxis = GetAxis(xAxisType);

                //xAxis.Minimum = 0 - width;
                //xAxis.Maximum = plot1.Model.Series.OfType<BoxPlotSeries>().Count() - 1 + width;
            }
        }

        private List<BoxPlotItem> GetBoxPlotItems(object[] x, double[] data, 
                                                  Models.Axis.AxisType xAxisType,
                                                  Models.Axis.AxisType yAxisType)
        {
            data = data.Where(d => !double.IsNaN(d)).ToArray();
            double[] fiveNumberSummary = data.FiveNumberSummary();
            double min = fiveNumberSummary[0];
            double lowerQuartile = fiveNumberSummary[1];
            double median = fiveNumberSummary[2];
            double upperQuartile = fiveNumberSummary[3];
            double max = fiveNumberSummary[4];

            double xValue = plot1.Model.Series.OfType<BoxPlotSeries>().Count();
            if (x[0] is double)
            {
                xValue = (double)x[0];
                EnsureAxisExists(xAxisType, typeof(double));
            }
            else
            {
                EnsureAxisExists(xAxisType, typeof(string));
                CategoryAxis axis = GetAxis(xAxisType) as CategoryAxis;
                if (axis != null)
                {
                    var xLabel = x[0].ToString();
                    int index = axis.Labels.IndexOf(xLabel);
                    if (index == -1)
                    {
                        axis.Labels.Add(xLabel);
                        index = axis.Labels.IndexOf(xLabel);
                    }
                    xValue = index;
                }
            }
            
            EnsureAxisExists(yAxisType, typeof(double));

            return new List<BoxPlotItem>() { new BoxPlotItem(xValue, min, lowerQuartile, median, upperQuartile, max) };
        }

        /// <summary>
        /// Draw text on the graph at the specified coordinates.
        /// </summary>
        /// <param name="text">The text to put on the graph</param>
        /// <param name="x">The x position in graph coordinates</param>
        /// <param name="y">The y position in graph coordinates</param>
        /// <param name="leftAlign">Left align the text?</param>
        /// <param name="textRotation">Text rotation</param>
        /// <param name="xAxisType">The axis type the x value relates to</param>
        /// <param name="yAxisType">The axis type the y value are relates to</param>
        /// <param name="colour">The color of the text</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawText(
            string text,
            object x,
            object y,
            bool leftAlign,
            double textRotation,
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType,
            Color colour)
        {
            OxyPlot.Annotations.TextAnnotation annotation = new OxyPlot.Annotations.TextAnnotation();
            annotation.Text = text;
            if (leftAlign)
                annotation.TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left;
            else
                annotation.TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center;
            annotation.TextVerticalAlignment = VerticalAlignment.Top;
            annotation.Stroke = OxyColors.White;
            annotation.Font = Font;
            annotation.TextRotation = textRotation;

            double xPosition = 0.0;
            if (x is DateTime)
                xPosition = DateTimeAxis.ToDouble(x);
            else
                xPosition = Convert.ToDouble(x, System.Globalization.CultureInfo.InvariantCulture);
            double yPosition = 0.0;
            if ((double)y == double.MinValue)
            {
                yPosition = AxisMinimum(yAxisType);
                annotation.TextVerticalAlignment = VerticalAlignment.Bottom;
            }
            else if ((double)y == double.MaxValue)
                yPosition = AxisMaximum(yAxisType);
            else
                yPosition = (double)y;
            annotation.TextPosition = new DataPoint(xPosition, yPosition);

            if (colour == Color.Empty)
                annotation.TextColor = ForegroundColour;
            else
                annotation.TextColor = Utility.Colour.ToOxy(colour);

            this.plot1.Model.Annotations.Add(annotation);
        }
        /// <summary>
        /// Draw line on the graph at the specified coordinates.
        /// </summary>
        /// <param name="x1">The x1 position in graph coordinates</param>
        /// <param name="y1">The y1 position in graph coordinates</param>
        /// <param name="x2">The x2 position in graph coordinates</param>
        /// <param name="y2">The y2 position in graph coordinates</param>
        /// <param name="type">Line type</param>
        /// <param name="textRotation">Text rotation</param>
        /// <param name="thickness">Line thickness</param>
        /// <param name="colour">The color of the text</param>
        /// <param name="inFrontOfSeries">Show annotation in front of series?</param>
        /// <param name="toolTip">Annotation tool tip.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawLine(
            object x1,
            object y1,
            object x2,
            object y2,
            Models.LineType type,
            Models.LineThicknessType thickness,
            Color colour,
            bool inFrontOfSeries,
            string toolTip)
        {
            double x1Position = 0.0;
            if (x1 is DateTime)
                x1Position = DateTimeAxis.ToDouble(x1);
            else
                x1Position = Convert.ToDouble(x1, System.Globalization.CultureInfo.InvariantCulture);
            double y1Position = 0.0;
            if ((double)y1 == double.MinValue)
                y1Position = AxisMinimum(Models.Axis.AxisType.Left);
            else if ((double)y1 == double.MaxValue)
                y1Position = AxisMaximum(Models.Axis.AxisType.Left);
            else
                y1Position = (double)y1;
            double x2Position = 0.0;
            if (x2 is DateTime)
                x2Position = DateTimeAxis.ToDouble(x2);
            else
                x2Position = Convert.ToDouble(x2, System.Globalization.CultureInfo.InvariantCulture);
            double y2Position = 0.0;
            if ((double)y2 == double.MinValue)
                y2Position = AxisMinimum(Models.Axis.AxisType.Left);
            else if ((double)y2 == double.MaxValue)
                y2Position = AxisMaximum(Models.Axis.AxisType.Left);
            else
                y2Position = (double)y2;

            OxyPlot.Annotations.Annotation annotation;

            if (x1Position == x2Position)
            {
                var lineAnnotation = new OxyPlot.Annotations.LineAnnotation();
                lineAnnotation.X = x1Position;
                lineAnnotation.Y = y1Position;
                lineAnnotation.MinimumX = x1Position;
                lineAnnotation.MinimumY = y1Position;
                lineAnnotation.MaximumX = x2Position;
                lineAnnotation.MaximumY = y2Position;
                lineAnnotation.Type = LineAnnotationType.Vertical;
                lineAnnotation.Color = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                if (thickness == LineThicknessType.Thin)
                    lineAnnotation.StrokeThickness = 0.5;
                annotation = lineAnnotation;
            }
            else
            {
                var rectangleAnnotation = new RectangleAnnotation();
                rectangleAnnotation.MinimumX = x1Position;
                rectangleAnnotation.MinimumY = y1Position;
                rectangleAnnotation.MaximumX = x2Position;
                rectangleAnnotation.MaximumY = y2Position;
                rectangleAnnotation.Fill = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                annotation = rectangleAnnotation;
            }
            if (inFrontOfSeries)
                annotation.Layer = AnnotationLayer.AboveSeries;
            else
                annotation.Layer = AnnotationLayer.BelowSeries;
            annotation.ToolTip = toolTip;
            this.plot1.Model.Annotations.Add(annotation);
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
        /// <param name="crossAtZero">Axis crosses at zero?</param>
        public void FormatAxis(
            Models.Axis.AxisType axisType,
            string title,
            bool inverted,
            double minimum,
            double maximum,
            double interval,
            bool crossAtZero)
        {
            OxyPlot.Axes.Axis oxyAxis = this.GetAxis(axisType);

            if (oxyAxis != null)
            {
                oxyAxis.AxislineColor = this.ForegroundColour;
                oxyAxis.ExtraGridlineColor = this.ForegroundColour;
                oxyAxis.MajorGridlineColor = this.ForegroundColour;
                oxyAxis.MinorGridlineColor = this.ForegroundColour;
                oxyAxis.TicklineColor = this.ForegroundColour;
                oxyAxis.MinorTicklineColor = this.ForegroundColour;
                oxyAxis.TitleColor = this.ForegroundColour;
                oxyAxis.TextColor = this.ForegroundColour;

                oxyAxis.Title = title.Trim();
                oxyAxis.MinorTickSize = 0;
                oxyAxis.AxislineStyle = LineStyle.Solid;
                oxyAxis.AxisTitleDistance = 10;
                oxyAxis.PositionAtZeroCrossing = crossAtZero;

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
                
                if (oxyAxis is DateTimeAxis)
                {
                    DateTimeIntervalType intervalType = double.IsNaN(interval) ? DateTimeIntervalType.Auto : (DateTimeIntervalType)interval;
                    (oxyAxis as DateTimeAxis).IntervalType = intervalType;
                    (oxyAxis as DateTimeAxis).MinorIntervalType = intervalType - 1;
                    (oxyAxis as DateTimeAxis).StringFormat = "dd/MM/yyyy";
                }
                else if(!double.IsNaN(interval) && interval > 0)
                    oxyAxis.MajorStep = interval;
            }
        }

        /// <summary>
        /// Format the legend.
        /// </summary>
        /// <param name="legendPositionType">Position of the legend</param>
        /// <param name="orientation">Orientation of items in the legend.</param>
        public void FormatLegend(Graph.LegendPositionType legendPositionType, Graph.LegendOrientationType orientation)
        {
            LegendPosition oxyLegendPosition;
            if (Enum.TryParse(legendPositionType.ToString(), out oxyLegendPosition))
            {
                this.plot1.Model.LegendFont = Font;
                this.plot1.Model.LegendFontSize = FontSize;
                this.plot1.Model.LegendPosition = oxyLegendPosition;
                if (Enum.TryParse(orientation.ToString(), out LegendOrientation legendOrientation))
                    plot1.Model.LegendOrientation = legendOrientation;
            }

            this.plot1.Model.LegendSymbolLength = 30;

            // If 2 series have the same title then remove their titles (this will
            // remove them from the legend) and create a new series solely for the
            // legend that has line type and marker type combined.
            var newSeriesToAdd = new List<LineSeries>();
            foreach (var series in plot1.Model.Series)
            {
                if (series is LineSeries && !string.IsNullOrEmpty(series.Title))
                {
                    var matchingSeries = FindMatchingSeries(series);
                    if (matchingSeries != null)
                    {
                        var newFakeSeries = new LineSeries();
                        newFakeSeries.Title = series.Title;
                        newFakeSeries.Color = (series as LineSeries).Color;
                        newFakeSeries.LineStyle = (series as LineSeries).LineStyle;
                        if (newFakeSeries.LineStyle == LineStyle.None)
                            (series as LineSeries).LineStyle = (matchingSeries as LineSeries).LineStyle;
                        if ((series as LineSeries).MarkerType == OxyPlot.MarkerType.None)
                        {
                            newFakeSeries.MarkerType = (matchingSeries as LineSeries).MarkerType;
                            newFakeSeries.MarkerFill = (matchingSeries as LineSeries).MarkerFill;
                            newFakeSeries.MarkerOutline = (matchingSeries as LineSeries).MarkerOutline;
                            newFakeSeries.MarkerSize = (matchingSeries as LineSeries).MarkerSize;
                        }
                        else
                        {
                            newFakeSeries.MarkerType = (series as LineSeries).MarkerType;
                            newFakeSeries.MarkerFill = (series as LineSeries).MarkerFill;
                            newFakeSeries.MarkerOutline = (series as LineSeries).MarkerOutline;
                            newFakeSeries.MarkerSize = (series as LineSeries).MarkerSize;
                        }

                        newSeriesToAdd.Add(newFakeSeries);

                        series.Title = null;          // remove from legend.
                        matchingSeries.Title = null;  // remove from legend.
                    }
                }
            }
            newSeriesToAdd.ForEach(s => plot1.Model.Series.Add(s));
        }

        /// <summary>
        /// Format the title.
        /// </summary>
        /// <param name="text">Text of the title</param>
        public void FormatTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                plot1.Model.Title = null;
            else
            {
                this.plot1.Model.Title = text;
                this.plot1.Model.TitleFont = Font;
                this.plot1.Model.TitleFontSize = 30;
                this.plot1.Model.TitleFontWeight = OxyPlot.FontWeights.Bold;
            }
        }

        /// <summary>
        /// Format the footer.
        /// </summary>
        /// <param name="text">The text for the footer</param>
        /// <param name="italics">Italics?</param>
        public void FormatCaption(string text, bool italics)
        {
            if (text != null && text != string.Empty)
            {
                captionLabel.Text = text;
                if (italics)
                    text = "<i>" + text + "</i>";
                captionLabel.Markup = text;
            }
            else
            {
                captionLabel.Text = "          ";
            }

        }

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        /// <param name="editor">The editor to show</param>
        public void ShowEditorPanel(object editorObj, string expanderLabel)
        {
            Widget editor = editorObj as Widget;
            if (editor != null)
            {
                expander1.Foreach(delegate(Widget widget) 
                {
                    if (widget != label2)
                    {
                        expander1.Remove(widget);
                        widget.Destroy();
                    }
                });
                expander1.Add(editor);
                expander1.Visible = true;
                expander1.Expanded = true;
                label2.Text = expanderLabel;
            }
        }

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        /// <param name="legendOutside">Put legend outside of graph?</param>
        public void Export(ref Bitmap bitmap, Rectangle r, bool legendOutside)
        {
            MemoryStream stream = new MemoryStream();
            PngExporter pngExporter = new PngExporter();
            pngExporter.Width = r.Width;
            pngExporter.Height = r.Height;
            pngExporter.Export(plot1.Model, stream);
            using (Graphics gfx = Graphics.FromImage(bitmap))
            {
                Bitmap newBitmap = new Bitmap(stream);
                gfx.DrawImage(newBitmap, r);
            }
        }

        /// <summary>
        /// Export the graph to the clipboard
        /// </summary>
        public void ExportToClipboard()
        {
            Gdk.Color colour = MainWidget.Style.Background(StateType.Normal);
            string fileName = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), ".png");
            PngExporter.Export(plot1.Model, fileName, 800, 600, new Cairo.SolidPattern(new Cairo.Color(BackColor.R / 255.0, BackColor.G / 255.0, BackColor.B/ 255.0, 1), false));
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            cb.Image = new Gdk.Pixbuf(fileName);
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="menuText">Menu item text</param>
        /// <param name="ticked">Menu ticked?</param>
        /// <param name="onClick">Event handler for menu item click</param>
        public void AddContextAction(string menuText, System.EventHandler onClick)
        {
            ImageMenuItem item = new ImageMenuItem(menuText);
            item.Activated += onClick;
            popup.Append(item);
            popup.ShowAll();
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextOption(string menuItemText, System.EventHandler onClick, bool active)
        {
            CheckMenuItem item = null;
            foreach (Widget w in popup)
            {
                CheckMenuItem oldItem = w as CheckMenuItem;
                if (oldItem != null)
                {
                    AccelLabel itemText = oldItem.Child as AccelLabel;
                    if (itemText.Text == menuItemText)
                    {
                        item = oldItem;
                        // Deactivate the "Activate" handler
                        PropertyInfo pi = item.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pi != null)
                        {
                            System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                            if (handlers != null && handlers.ContainsKey("activate"))
                            {
                                EventHandler handler = (EventHandler)handlers["activate"];
                                item.Activated -= handler;
                            }
                        }
                        break;
                    }
                }
            }
            if (item == null)
            {
                item = new CheckMenuItem(menuItemText);
                item.DrawAsRadio = false;
                popup.Append(item);
                popup.ShowAll();
            }
            // Be sure to set the Active property before attaching the Activated event, since
            // the event handler will call this function again when Active is changed.
            // This can lead to infinite recursion. This is also why we deactivate the handler
            // (done above) when the item is already found in the menu
            item.Active = active;
            item.Activated += onClick;
        }

        /// <summary>
        /// Find a graph series that has the same title as the specified series.
        /// </summary>
        /// <param name="series">The series to match.</param>
        /// <returns>The series or null if not found.</returns>
        private OxyPlot.Series.Series FindMatchingSeries(OxyPlot.Series.Series series)
        {
            foreach (var s in plot1.Model.Series)
            {
                if (s != series && s.Title == series.Title)
                    return s;
            }
            return null;
        }

        /// <summary>
        /// Event handler for when user clicks close
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCloseEditorPanel(object sender, EventArgs e)
        {
            /* TBI
            try
            {
                this.bottomPanel.Visible = false;
                this.splitter.Visible = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
            */
        }

        /// <summary>
        /// Format axis tick labels so that there is a leading zero on the tick
        /// labels when necessary.
        /// </summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxisTickLabels(OxyPlot.Axes.Axis axis)
        {
            // axis.IntervalLength = 100;

            if (axis is DateTimeAxis && (axis as DateTimeAxis).IntervalType == DateTimeIntervalType.Auto)
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
                else if (numDays <= 720)
                {
                    dateAxis.IntervalType = DateTimeIntervalType.Months;
                    dateAxis.StringFormat = "MMM-yyyy";
                }
                else
                {
                    dateAxis.IntervalType = DateTimeIntervalType.Years;
                    dateAxis.StringFormat = "yyyy";
                }
            }

            if (axis is LinearAxis &&
                !(axis is DateTimeAxis) &&
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

                if (axis.PlotModel.Series[0] is BoxPlotSeries && 
                    axis.Position == AxisPosition.Bottom && 
                    axis.PlotModel.Series?.Count > 0)
                {
                    // Need to put a bit of extra space on the x axis.
                    axis.MinimumPadding = (axis.ActualMaximum - axis.ActualMinimum) * 0.005;
                    axis.MaximumPadding = (axis.ActualMaximum - axis.ActualMinimum) * 0.005;

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
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType)
        {
            List<DataPoint> points = new List<DataPoint>();
            if (x != null && y != null && ((ICollection)x).Count > 0 && ((ICollection)y).Count > 0)
            {
                // Create a new data point for each x.
                double[] xValues = GetDataPointValues(x.GetEnumerator(), xAxisType);
                double[] yValues = GetDataPointValues(y.GetEnumerator(), yAxisType);

                // Create data points
                for (int i = 0; i < Math.Min(xValues.Length, yValues.Length); i++)
                    if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]))
                        points.Add(new DataPoint(xValues[i], yValues[i]));

                return points;
            }
            else
                return null;
        }

        /// <summary>
        /// Populate the specified DataPointSeries with data from the data table.
        /// </summary>
        /// <param name="x">The x values</param>
        /// <param name="y">The y values</param>
        /// <param name="error">The error size values</param>
        /// <param name="xAxisType">The x axis the data is associated with</param>
        /// <param name="yAxisType">The y axis the data is associated with</param>
        /// <returns>A list of 'DataPoint' objects ready to be plotted</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        private List<ScatterErrorPoint> PopulateErrorPointSeries(
            IEnumerable x,
            IEnumerable y,
            IEnumerable error,
            Models.Axis.AxisType xAxisType,
            Models.Axis.AxisType yAxisType)
        {
            List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
            if (x != null && y != null && error != null)
            {
                // Create a new data point for each x.
                double[] xValues = GetDataPointValues(x.GetEnumerator(), xAxisType);
                double[] yValues = GetDataPointValues(y.GetEnumerator(), yAxisType);
                double[] errorValues = GetDataPointValues(error.GetEnumerator(), yAxisType);

                if (xValues.Length == yValues.Length && xValues.Length == errorValues.Length)
                {
                    // Create data points
                    for (int i = 0; i < xValues.Length; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(errorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], 0, errorValues[i], 0));

                    return points;
                }
            }
            return null;
        }

        /// <summary>Gets an array of values for the given enumerator</summary>
        /// <param name="xEnum">The enumumerator</param>
        /// <param name="axisType">Type of the axis.</param>
        /// <returns></returns>
        private double[] GetDataPointValues(IEnumerator enumerator, Models.Axis.AxisType axisType)
        {
            List<double> dataPointValues = new List<double>();
            double x; // Used only as an out parameter, to maintain backward
                      // compatibility with older versions VS/C#.
            if (!enumerator.MoveNext())
                return null;
            if (enumerator.Current.GetType() == typeof(DateTime))
            {
                this.EnsureAxisExists(axisType, typeof(DateTime));
                do
                {
                    DateTime d = Convert.ToDateTime(enumerator.Current, CultureInfo.InvariantCulture);
                    dataPointValues.Add(DateTimeAxis.ToDouble(d));
                    if (d < smallestDate)
                        smallestDate = d;
                    if (d > largestDate)
                        largestDate = d;
                }
                while (enumerator.MoveNext());
            }
            else if (enumerator.Current.GetType() == typeof(double) || enumerator.Current.GetType() == typeof(float) || double.TryParse(enumerator.Current.ToString(), out x))
            {
                this.EnsureAxisExists(axisType, typeof(double));
                do
                    dataPointValues.Add(Convert.ToDouble(enumerator.Current, 
                                                         System.Globalization.CultureInfo.InvariantCulture));
                while (enumerator.MoveNext());
            }
            else
            {
                this.EnsureAxisExists(axisType, typeof(string));
                CategoryAxis axis = GetAxis(axisType) as CategoryAxis;
                if (axis != null)
                {
                    do
                    {
                        int index = axis.Labels.IndexOf(enumerator.Current.ToString());
                        if (index == -1)
                        {
                            axis.Labels.Add(enumerator.Current.ToString());
                            index = axis.Labels.IndexOf(enumerator.Current.ToString());
                        }

                        dataPointValues.Add(index);
                    }
                    while (enumerator.MoveNext());
                }
            }

            return dataPointValues.ToArray();
        }

        /// <summary>
        /// Ensure the specified X exists. Uses the 'DataType' property of the DataColumn
        /// to determine the type of axis.
        /// </summary>
        /// <param name="axisType">The axis type to check</param>
        /// <param name="dataType">The data type of the axis</param>
        private void EnsureAxisExists(Models.Axis.AxisType axisType, Type dataType)
        {
            // Make sure we have an x axis at the correct position.
            if (this.GetAxis(axisType) == null)
            {
                AxisPosition position = this.AxisTypeToPosition(axisType);
                OxyPlot.Axes.Axis axisToAdd;
                if (dataType == typeof(DateTime))
                {
                    axisToAdd = new DateTimeAxis();
                }
                else if (dataType == typeof(double))
                {
                    axisToAdd = new LinearAxis();
                }
                else
                {
                    axisToAdd = new CategoryAxis();
                }

                axisToAdd.Position = position;
                axisToAdd.Key = axisType.ToString();
                this.plot1.Model.Axes.Add(axisToAdd);
            }
        }

        /// <summary>
        /// Return an axis that has the specified AxisType. Returns null if not found.
        /// </summary>
        /// <param name="axisType">The axis type to retrieve </param>
        /// <returns>The axis</returns>
        private OxyPlot.Axes.Axis GetAxis(Models.Axis.AxisType axisType)
        {
            int i = this.GetAxisIndex(axisType);
            if (i == -1)
                return null;
            else
                return this.plot1.Model.Axes[i];
        }

        /// <summary>
        /// Return an axis that has the specified AxisType. Returns null if not found.
        /// </summary>
        /// <param name="axisType">The axis type to retrieve </param>
        /// <returns>The axis</returns>
        private int GetAxisIndex(Models.Axis.AxisType axisType)
        {
            AxisPosition position = this.AxisTypeToPosition(axisType);
            for (int i = 0; i < this.plot1.Model.Axes.Count; i++)
            {
                if (this.plot1.Model.Axes[i].Position == position)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Convert the Axis.AxisType into an OxyPlot.AxisPosition.
        /// </summary>
        /// <param name="type">The axis type</param>
        /// <returns>The position of the axis.</returns>
        private AxisPosition AxisTypeToPosition(Models.Axis.AxisType type)
        {
            if (type == Models.Axis.AxisType.Bottom)
            {
                return AxisPosition.Bottom;
            }
            else if (type == Models.Axis.AxisType.Left)
            {
                return AxisPosition.Left;
            }
            else if (type == Models.Axis.AxisType.Top)
            {
                return AxisPosition.Top;
            }

            return AxisPosition.Right;
        }

        /// <summary>
        /// Convert the OxyPlot.AxisPosition into an Axis.AxisType.
        /// </summary>
        /// <param name="type">The axis type</param>
        /// <returns>The position of the axis.</returns>
        private Models.Axis.AxisType AxisPositionToType(AxisPosition type)
        {
            if (type == AxisPosition.Bottom)
                return Models.Axis.AxisType.Bottom;
            else if (type == AxisPosition.Left)
                return Models.Axis.AxisType.Left;
            else if (type == AxisPosition.Top)
                return Models.Axis.AxisType.Top;

            return Models.Axis.AxisType.Right;
        }

        /// <summary>
        /// User has double clicked somewhere on a graph.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMouseDoubleClick(object sender, OxyMouseDownEventArgs e)
        {
            try
            {
                Point location = new Point((int)e.Position.X, (int)e.Position.Y);
                Cairo.Rectangle plotRect = this.plot1.Model.PlotArea.ToRect(false);
                Rectangle plotArea = new Rectangle((int)plotRect.X, (int)plotRect.Y, (int)plotRect.Width, (int)plotRect.Height);
                if (plotArea.Contains(location))
                {
                    Cairo.Rectangle legendRect = this.plot1.Model.LegendArea.ToRect(true);
                    Rectangle legendArea = new Rectangle((int)legendRect.X, (int)legendRect.Y, (int)legendRect.Width, (int)legendRect.Height);
                    if (legendArea.Contains(location))
                    {
                        int y = Convert.ToInt32(location.Y - this.plot1.Model.LegendArea.Top, CultureInfo.InvariantCulture);
                        int itemHeight = Convert.ToInt32(this.plot1.Model.LegendArea.Height, CultureInfo.InvariantCulture) / this.plot1.Model.Series.Count;
                        int seriesIndex = y / itemHeight;
                        if (this.OnLegendClick != null)
                        {
                            LegendClickArgs args = new LegendClickArgs();
                            args.SeriesIndex = seriesIndex;
                            args.ControlKeyPressed = e.IsControlDown;
                            this.OnLegendClick.Invoke(sender, args);
                        }
                    }
                    else
                    {
                        if (this.OnPlotClick != null)
                        {
                            this.OnPlotClick.Invoke(sender, e);
                        }
                    }
                }
                else
                {
                    Rectangle leftAxisArea = new Rectangle(0, plotArea.Y, plotArea.X, plotArea.Height);
                    Rectangle titleArea = new Rectangle(plotArea.X, 0, plotArea.Width, plotArea.Y);
                    Rectangle topAxisArea = new Rectangle(plotArea.X, 0, plotArea.Width, 0);

                    if (this.GetAxis(Models.Axis.AxisType.Top) != null)
                    {
                        titleArea = new Rectangle(plotArea.X, 0, plotArea.Width, plotArea.Y / 2);
                        topAxisArea = new Rectangle(plotArea.X, plotArea.Y / 2, plotArea.Width, plotArea.Y / 2);
                    }

                    Rectangle rightAxisArea = new Rectangle(plotArea.Right, plotArea.Top, MainWidget.Allocation.Width - plotArea.Right, plotArea.Height);
                    Rectangle bottomAxisArea = new Rectangle(plotArea.Left, plotArea.Bottom, plotArea.Width, MainWidget.Allocation.Height - plotArea.Bottom);
                    if (titleArea.Contains(location))
                    {
                        if (this.OnTitleClick != null)
                        {
                            this.OnTitleClick(sender, e);
                        }
                    }

                    if (this.OnAxisClick != null)
                    {
                        if (leftAxisArea.Contains(location))
                        {
                            this.OnAxisClick.Invoke(Models.Axis.AxisType.Left);
                        }
                        else if (topAxisArea.Contains(location))
                        {
                            this.OnAxisClick.Invoke(Models.Axis.AxisType.Top);
                        }
                        else if (rightAxisArea.Contains(location))
                        {
                            this.OnAxisClick.Invoke(Models.Axis.AxisType.Right);
                        }
                        else if (bottomAxisArea.Contains(location))
                        {
                            this.OnAxisClick.Invoke(Models.Axis.AxisType.Bottom);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// User has clicked the caption
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCaptionLabelDoubleClick(object sender, ButtonPressEventArgs e)
        {
            try
            {
                if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1 && OnCaptionClick != null)
                    OnCaptionClick.Invoke(this, e);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        public Models.Axis[] Axes
        {
            get
            {
                List<Models.Axis> axes = new List<Models.Axis>();
                foreach (var oxyAxis in plot1.Model.Axes)
                {
                    var axis = new Models.Axis();
                    axis.CrossesAtZero = oxyAxis.PositionAtZeroCrossing;
                    axis.DateTimeAxis = oxyAxis is DateTimeAxis;
                    axis.Interval = oxyAxis.ActualMajorStep;
                    axis.Inverted = MathUtilities.FloatsAreEqual(oxyAxis.StartPosition, 1);
                    axis.Maximum = oxyAxis.ActualMaximum;
                    axis.Minimum = oxyAxis.ActualMinimum;
                    axis.Title = oxyAxis.Title;
                    axis.Type = AxisPositionToType(oxyAxis.Position);

                    axes.Add(axis);
                }

                return axes.ToArray();
            }
        }

        /// <summary>
        /// Gets the maximum scale of the specified axis.
        /// </summary>
        public double AxisMaximum(Models.Axis.AxisType axisType)
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
        public double AxisMinimum(Models.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
            {
                return axis.ActualMinimum;
            }
            else
                return double.NaN;
        }

        /// <summary>
        /// Gets the interval (major step) of the specified axis.
        /// </summary>
        public string AxisTitle(Models.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
                return axis.Title;

            return string.Empty;
        }

        /// <summary>
        /// Gets the interval (major step) of the specified axis.
        /// </summary>
        public double AxisMajorStep(Models.Axis.AxisType axisType)
        {
            OxyPlot.Axes.Axis axis = GetAxis(axisType);

            if (axis != null)
            {
                return axis.IntervalLength;
            }
            else
                return double.NaN;
        }

        /// <summary>Gets the series names.</summary>
        /// <returns></returns>
        public string[] GetSeriesNames()
        {
            List<string> names = new List<string>();
            foreach (OxyPlot.Series.Series series in this.plot1.Model.Series)
            {
                names.Add(series.Title);
            }
            return names.ToArray();
        }

        /// <summary>Sets the margins.</summary>
        public void SetMargins(int margin)
        {
            this.plot1.Model.Padding = new OxyThickness(margin, margin, margin, margin);
        }

        /// <summary>Graph has been clicked.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChartClick(object sender, OxyMouseDownEventArgs e)
        {
            try
            {
                e.Handled = false;

                inRightClick = e.ChangedButton == OxyMouseButton.Right;
                if (e.ChangedButton == OxyMouseButton.Left) /// Left clicks only
                {
                    if (e.ClickCount == 1 && SingleClick != null)
                        SingleClick.Invoke(this, e);
                    else if (e.ClickCount == 2)
                        OnMouseDoubleClick(sender, e);
                }

                // Annotation tool tips.
                if (e.HitTestResult != null && e.HitTestResult.Element is OxyPlot.Annotations.Annotation)
                    plot1.TooltipText = (e.HitTestResult.Element as OxyPlot.Annotations.Annotation).ToolTip;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Mouse up event on chart. If in a right click, display the popup menu.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChartMouseUp(object sender, OxyMouseEventArgs e)
        {
            try
            {
                e.Handled = false;
                if (inRightClick)
                    popup.Popup();
                inRightClick = false;
                plot1.TooltipText = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Mouse has moved on the chart.
        /// If the user was just dragging the chart, we won't want to 
        /// display the popup menu when the mouse is released</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChartMouseMove(object sender, OxyMouseEventArgs e)
        {
            try
            {
                e.Handled = false;
                inRightClick = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        public void ShowControls(bool visible)
        {
            try
            {
                captionEventBox.Visible = visible;
                expander1.Visible = visible && expander1.Expanded;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
