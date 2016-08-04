// -----------------------------------------------------------------------
// <copyright file="GraphView.cs" company="CSIRO">
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
    using System.IO;
    using Gtk;
    using Glade;
    using Interfaces;
    using Models.Graph;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.GtkSharp;
    using EventArguments;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public class GraphView : ViewBase, Interfaces.IGraphView
    {
        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        public double FontSize = 14;

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

        private OxyPlot.GtkSharp.PlotView plot1;
        [Widget]
        private VBox vbox1 = null;
        [Widget]
        private Expander expander1 = null;
        [Widget]
        private VBox vbox2 = null;
        [Widget]
        private Label captionLabel = null;
        [Widget]
        private EventBox captionEventBox = null;
        [Widget]
        private Label label2 = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        public GraphView(ViewBase owner = null) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.GraphView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;

            plot1 = new PlotView();
            plot1.Model = new PlotModel();
            plot1.SetSizeRequest(-1, 100);
            vbox2.PackStart(plot1, true, true, 0);

            smallestDate = DateTime.MaxValue;
            largestDate = DateTime.MinValue;
            this.LeftRightPadding = 40;
            expander1.Visible = false;

            plot1.Model.MouseDown += OnChartClick;

            captionLabel.Text = null;
            captionEventBox.ButtonPressEvent += OnCaptionLabelDoubleClick;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            plot1.Model.MouseDown -= OnChartClick;
            captionEventBox.ButtonPressEvent -= OnCaptionLabelDoubleClick;
            Clear();
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

        public OxyColor BackColor
        {
            get { return this.plot1.Model.Background; }
            set { this.plot1.Model.Background = value; }
        }

        public int Width
        {
            get { return this.plot1.Allocation.Width; }
            set { this.plot1.WidthRequest = value; }
        }

        public int Height
        {
            get { return this.plot1.Allocation.Height; }
            set { this.plot1.HeightRequest = value; }
        }

        /// <summary>Gets or sets a value indicating if the legend is visible.</summary>
        public bool IsLegendVisible
        {
            get { return this.plot1.Model.IsLegendVisible; }
            set { this.plot1.Model.IsLegendVisible = value; }
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
            //modLMC - 11/05/2016 - Need to clear the chart title as well
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
            this.plot1.Model.LegendBackground = OxyColors.White;

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
             Models.Graph.Axis.AxisType xAxisType,
             Models.Graph.Axis.AxisType yAxisType,
             Color colour,
             Models.Graph.LineType lineType,
             Models.Graph.MarkerType markerType,
             Models.Graph.LineThicknessType lineThickness,
             Models.Graph.MarkerSizeType markerSize,
             bool showOnLegend)
        {
            if (x != null && y != null)
            {
                Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                series.OnHoverOverPoint += OnHoverOverPoint;
                if (showOnLegend)
                    series.Title = title;
                else
                    series.ToolTip = title;
                series.Color = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                series.ItemsSource = this.PopulateDataPointSeries(x, y, xAxisType, yAxisType);
                series.XAxisKey = xAxisType.ToString();
                series.YAxisKey = yAxisType.ToString();
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

                if (markerSize == MarkerSizeType.Normal)
                    series.MarkerSize = 7.0;
                else
                    series.MarkerSize = 5.0;

                series.MarkerStroke = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                if (filled)
                {
                    series.MarkerFill = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
                    series.MarkerStroke = OxyColors.White;
                }

                this.plot1.Model.Series.Add(series);
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
            AreaSeries series = new AreaSeries();
            series.Color = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
            series.Fill = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
            List<DataPoint> points = this.PopulateDataPointSeries(x1, y1, xAxisType, yAxisType);
            List<DataPoint> points2 = this.PopulateDataPointSeries(x2, y2, xAxisType, yAxisType);

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
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
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
                xPosition = Convert.ToDouble(x);
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
            annotation.TextColor = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawLine(
            object x1,
            object y1,
            object x2,
            object y2,
            Models.Graph.LineType type,
            Models.Graph.LineThicknessType thickness,
            Color colour)
        {
            OxyPlot.Annotations.LineAnnotation annotation = new OxyPlot.Annotations.LineAnnotation();

            double x1Position = 0.0;
            if (x1 is DateTime)
                x1Position = DateTimeAxis.ToDouble(x1);
            else
                x1Position = Convert.ToDouble(x1);
            double y1Position = 0.0;
            if ((double)y1 == double.MinValue)
                y1Position = AxisMinimum(Models.Graph.Axis.AxisType.Left);
            else if ((double)y1 == double.MaxValue)
                y1Position = AxisMaximum(Models.Graph.Axis.AxisType.Left);
            else
                y1Position = (double)y1;
            double x2Position = 0.0;
            if (x2 is DateTime)
                x2Position = DateTimeAxis.ToDouble(x2);
            else
                x2Position = Convert.ToDouble(x2);
            double y2Position = 0.0;
            if ((double)y2 == double.MinValue)
                y2Position = AxisMinimum(Models.Graph.Axis.AxisType.Left);
            else if ((double)y2 == double.MaxValue)
                y2Position = AxisMaximum(Models.Graph.Axis.AxisType.Left);
            else
                y2Position = (double)y2;

            annotation.X = x1Position;
            annotation.Y = y1Position;
            annotation.MinimumX = x1Position;
            annotation.MinimumY = y1Position;
            annotation.MaximumX = x2Position;
            annotation.MaximumY = y2Position;
            annotation.Type = LineAnnotationType.Vertical;
            annotation.Color = OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);

            // Line type.
            //LineStyle oxyLineType;
            //if (Enum.TryParse<LineStyle>(type.ToString(), out oxyLineType))
            //    annotation.LineStyle = oxyLineType;

            // Line thickness
            if (thickness == LineThicknessType.Thin)
                annotation.StrokeThickness = 0.5;
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
                this.plot1.Model.LegendFont = Font;
                this.plot1.Model.LegendFontSize = FontSize;
                this.plot1.Model.LegendPosition = oxyLegendPosition;
            }

            //this.plot1.Model.LegendSymbolLength = 60;
        }

        /// <summary>
        /// Format the title.
        /// </summary>
        /// <param name="text">Text of the title</param>
        public void FormatTitle(string text)
        {
            this.plot1.Model.Title = text;
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
                    text = "<i>" + text + "<i/>";
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
                expander1.Foreach(delegate(Widget widget) { if (widget != label2) expander1.Remove(widget); });
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
        public void Export(ref Bitmap bitmap, bool legendOutside)
        {
            MemoryStream stream = new MemoryStream();
            PngExporter pngExporter = new PngExporter();
            pngExporter.Width = bitmap.Width;
            pngExporter.Height = bitmap.Height;
            pngExporter.Export(plot1.Model, stream);
            bitmap = new Bitmap(stream);
        }

        /// <summary>
        /// Export the graph to the clipboard
        /// </summary>
        public void ExportToClipboard()
        {
            MemoryStream stream = new MemoryStream();
            PngExporter pngExporter = new PngExporter();
            pngExporter.Width = 800;
            pngExporter.Height = 600;
            pngExporter.Export(plot1.Model, stream);
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            cb.Image = new Gdk.Pixbuf(stream);
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="menuText">Menu item text</param>
        /// <param name="ticked">Menu ticked?</param>
        /// <param name="onClick">Event handler for menu item click</param>
        public void AddContextAction(string menuText, bool ticked, System.EventHandler onClick)
        {
            /* TBI
            ToolStripMenuItem item = null;
            foreach (ToolStripMenuItem i in contextMenuStrip1.Items)
                if (i.Text == menuText)
                    item = i;
            if (item == null)
            {
                item = contextMenuStrip1.Items.Add(menuText) as ToolStripMenuItem;
                item.Click += onClick;
            }
            item.Checked = ticked;
            */
        }

        /// <summary>
        /// Event handler for when user clicks close
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCloseEditorPanel(object sender, EventArgs e)
        {
            /* TBI
            this.bottomPanel.Visible = false;
            this.splitter.Visible = false;
            */
        }

        /// <summary>
        /// Format axis tick labels so that there is a leading zero on the tick
        /// labels when necessary.
        /// </summary>
        /// <param name="axis">The axis to format</param>
        private void FormatAxisTickLabels(OxyPlot.Axes.Axis axis)
        {
            //axis.IntervalLength = 100;

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
            List<DataPoint> points = new List<DataPoint>();
            if (x != null && y != null && x != null && y != null)
            {
                // Create a new data point for each x.
                double[] xValues = GetDataPointValues(x.GetEnumerator(), xAxisType);
                double[] yValues = GetDataPointValues(y.GetEnumerator(), yAxisType);

                // Create data points
                for (int i = 0; i < xValues.Length; i++)
                    if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]))
                        points.Add(new DataPoint(xValues[i], yValues[i]));

                return points;
            }
            else
                return null;
        }

        /// <summary>Gets an array of values for the given enumerator</summary>
        /// <param name="xEnum">The enumumerator</param>
        /// <param name="axisType">Type of the axis.</param>
        /// <returns></returns>
        private double[] GetDataPointValues(IEnumerator enumerator, Models.Graph.Axis.AxisType axisType)
        {
            List<double> dataPointValues = new List<double>();

            enumerator.MoveNext();

            if (enumerator.Current.GetType() == typeof(DateTime))
            {
                this.EnsureAxisExists(axisType, typeof(DateTime));
                do
                {
                    DateTime d = Convert.ToDateTime(enumerator.Current);
                    dataPointValues.Add(DateTimeAxis.ToDouble(d));
                    if (d < smallestDate)
                        smallestDate = d;
                    if (d > largestDate)
                        largestDate = d;
                }
                while (enumerator.MoveNext());
            }
            else if (enumerator.Current.GetType() == typeof(double) || enumerator.Current.GetType() == typeof(float))
            {
                this.EnsureAxisExists(axisType, typeof(double));
                do
                    dataPointValues.Add(Convert.ToDouble(enumerator.Current));
                while (enumerator.MoveNext());
            }
            else
            {
                this.EnsureAxisExists(axisType, typeof(string));
                CategoryAxis axis = GetAxis(axisType) as CategoryAxis;
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

            return dataPointValues.ToArray();
        }


        /// <summary>
        /// Ensure the specified X exists. Uses the 'DataType' property of the DataColumn
        /// to determine the type of axis.
        /// </summary>
        /// <param name="axisType">The axis type to check</param>
        /// <param name="dataType">The data type of the axis</param>
        private void EnsureAxisExists(Models.Graph.Axis.AxisType axisType, Type dataType)
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
        private OxyPlot.Axes.Axis GetAxis(Models.Graph.Axis.AxisType axisType)
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
        private int GetAxisIndex(Models.Graph.Axis.AxisType axisType)
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
        private AxisPosition AxisTypeToPosition(Models.Graph.Axis.AxisType type)
        {
            if (type == Models.Graph.Axis.AxisType.Bottom)
            {
                return AxisPosition.Bottom;
            }
            else if (type == Models.Graph.Axis.AxisType.Left)
            {
                return AxisPosition.Left;
            }
            else if (type == Models.Graph.Axis.AxisType.Top)
            {
                return AxisPosition.Top;
            }

            return AxisPosition.Right;
        }

        /// <summary>
        /// User has double clicked somewhere on a graph.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMouseDoubleClick(object sender, OxyMouseDownEventArgs e)
        {
            Point Location = new Point((int)e.Position.X, (int)e.Position.Y);
            Cairo.Rectangle plotRect = this.plot1.Model.PlotArea.ToRect(false);
            Rectangle plotArea = new Rectangle((int)plotRect.X, (int)plotRect.Y, (int)plotRect.Width, (int)plotRect.Height);
            if (plotArea.Contains(Location))
            {
                Cairo.Rectangle legendRect = this.plot1.Model.LegendArea.ToRect(true);
                Rectangle legendArea = new Rectangle((int)legendRect.X, (int)legendRect.Y, (int)legendRect.Width, (int)legendRect.Height);
                if (legendArea.Contains(Location))
                {
                    int y = Convert.ToInt32(Location.Y - this.plot1.Model.LegendArea.Top);
                    int itemHeight = Convert.ToInt32(this.plot1.Model.LegendArea.Height) / this.plot1.Model.Series.Count;
                    int seriesIndex = y / itemHeight;
                    if (this.OnLegendClick != null)
                    {
                        LegendClickArgs args = new LegendClickArgs();
                        args.seriesIndex = seriesIndex;
                        args.controlKeyPressed = e.IsControlDown;
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

                if (this.GetAxis(Models.Graph.Axis.AxisType.Top) != null)
                {
                    titleArea = new Rectangle(plotArea.X, 0, plotArea.Width, plotArea.Y / 2);
                    topAxisArea = new Rectangle(plotArea.X, plotArea.Y / 2, plotArea.Width, plotArea.Y / 2);
                }

                Rectangle rightAxisArea = new Rectangle(plotArea.Right, plotArea.Top, MainWidget.Allocation.Width - plotArea.Right, plotArea.Height);
                Rectangle bottomAxisArea = new Rectangle(plotArea.Left, plotArea.Bottom, plotArea.Width, MainWidget.Allocation.Height - plotArea.Bottom);
                if (titleArea.Contains(Location))
                {
                    if (this.OnTitleClick != null)
                    {
                        this.OnTitleClick(sender, e);
                    }
                }

                if (this.OnAxisClick != null)
                {
                    if (leftAxisArea.Contains(Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Left);
                    }
                    else if (topAxisArea.Contains(Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Top);
                    }
                    else if (rightAxisArea.Contains(Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Right);
                    }
                    else if (bottomAxisArea.Contains(Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Bottom);
                    }
                }
            } 
        }

        /// <summary>
        /// User has clicked the caption
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCaptionLabelDoubleClick(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Type == Gdk.EventType.TwoButtonPress && e.Event.Button == 1 && OnCaptionClick != null)
                OnCaptionClick.Invoke(this, e);
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
            e.Handled = false;
            if (e.ChangedButton == OxyMouseButton.Left) /// Left clicks only
            {
                if (e.ClickCount == 1 && SingleClick != null)
                    SingleClick.Invoke(this, e);
                else /// Enable this when OxyPlot is fixed if (e.ClickCount == 2)  
                    OnMouseDoubleClick(sender, e);
            }
        }
    }
}
