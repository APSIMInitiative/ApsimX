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
    using System.Windows.Forms;
    using Interfaces;
    using Models.Graph;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.WindowsForms;
    using EventArguments;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class GraphView : UserControl, Interfaces.IGraphView
    {
        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        private const double FontSize = 14;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        public GraphView()
        {
            this.InitializeComponent();
            this.plot1.Model = new PlotModel();
            this.splitter.Visible = false;
            this.bottomPanel.Visible = false;
            smallestDate = DateTime.MaxValue;
            largestDate = DateTime.MinValue;
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

        /// <summary>
        /// Left margin in pixels.
        /// </summary>
        public int LeftRightPadding { get; set; }

        /// <summary>
        /// Clear the graph of everything.
        /// </summary>
        public void Clear()
        {
            this.plot1.Model.Series.Clear();
            this.plot1.Model.Axes.Clear();
            this.plot1.Model.Annotations.Clear();

        }

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        public override void Refresh()
        {
            this.plot1.Model.DefaultFont = Font;
            this.plot1.Model.DefaultFontSize = FontSize;

            this.plot1.Model.PlotAreaBorderThickness = new OxyThickness(0);
            this.plot1.Model.LegendBorder = OxyColors.Transparent;
            this.plot1.Model.LegendBackground = OxyColors.White;
            this.plot1.Model.InvalidatePlot(true);

            if (this.LeftRightPadding != 0)
                this.Padding = new Padding(this.LeftRightPadding, 0, this.LeftRightPadding, 0);

            foreach (OxyPlot.Axes.Axis axis in this.plot1.Model.Axes)
            {
                this.FormatAxisTickLabels(axis);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void DrawLineAndMarkers(
             string title,
             IEnumerable x,
             IEnumerable y,
             Models.Graph.Axis.AxisType xAxisType,
             Models.Graph.Axis.AxisType yAxisType,
             Color colour,
             Models.Graph.Series.LineType lineType,
             Models.Graph.Series.MarkerType markerType,
             bool showOnLegend)
        {
            if (x != null && y != null)
            {
                Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                series.OnHoverOverPoint += OnHoverOverPoint;
                if (showOnLegend)
                    series.Title = title;
                series.Color = ConverterExtensions.ToOxyColor(colour);
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
                }
                
                // Marker type.
                MarkerType type;
                if (Enum.TryParse<MarkerType>(oxyMarkerName, out type))
                {
                    series.MarkerType = type;
                }

                series.MarkerSize = 7.0;
                series.MarkerStroke = ConverterExtensions.ToOxyColor(colour);
                if (filled)
                {
                    series.MarkerFill = ConverterExtensions.ToOxyColor(colour);
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
            ColumnXYSeries series = new ColumnXYSeries();
            if (showOnLegend)
                series.Title = title;
            series.FillColor = ConverterExtensions.ToOxyColor(colour);
            series.StrokeColor = ConverterExtensions.ToOxyColor(colour);
            series.ItemsSource = this.PopulateDataPointSeries(x, y, xAxisType, yAxisType);
            series.XAxisKey = xAxisType.ToString();
            series.YAxisKey = yAxisType.ToString();
            this.plot1.Model.Series.Add(series);
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
            series.Color = ConverterExtensions.ToOxyColor(colour);
            series.Fill = ConverterExtensions.ToOxyColor(colour);
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
            TextAnnotation annotation = new TextAnnotation();
            annotation.Text = text;
            annotation.TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left;
            annotation.TextVerticalAlignment = VerticalAlignment.Top;
            annotation.Stroke = OxyColors.White;
            annotation.TextPosition = new DataPoint(x, y);
            //annotation.XAxis = this.GetAxis(xAxisType);
            //annotation.YAxis = this.GetAxis(yAxisType);
            annotation.TextColor = ConverterExtensions.ToOxyColor(colour);
            annotation.FontSize = FontSize - 1;
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

            this.plot1.Model.LegendSymbolLength = 30;
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
        public void FormatCaption(string text)
        {
            captionLabel.MaximumSize = new Size(panel1.Width, 300);
            if (text != null && text != string.Empty)
            {
                captionLabel.Text = text;
                captionLabel.Font = new Font(Font, (float) (FontSize * 0.8));
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
        public void ShowEditorPanel(UserControl editor)
        {
            if (this.bottomPanel.Controls.Count > 1)
            {
                this.bottomPanel.Controls.RemoveAt(1);
            }

            this.bottomPanel.Controls.Add(editor);
            this.bottomPanel.Visible = true;
            this.bottomPanel.Height = editor.Height;
            this.splitter.Visible = true;
            editor.Dock = DockStyle.Fill;
            editor.SendToBack();
        }

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        public void Export(Bitmap bitmap)
        {
            this.plot1.Dock = DockStyle.None;
            this.plot1.Width = bitmap.Width;
            this.plot1.Height = bitmap.Height;

            LegendPosition savedLegendPosition = this.plot1.Model.LegendPosition;
            this.plot1.Model.LegendPlacement = LegendPlacement.Outside;

            this.plot1.Model.LegendPosition = LegendPosition.RightTop;
            this.plot1.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            this.plot1.Model.LegendPlacement = LegendPlacement.Inside;
            this.plot1.Model.LegendPosition = savedLegendPosition;
            this.plot1.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="buttonText">Text for button</param>
        /// <param name="onClick">Event handler for button click</param>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
            this.contextMenuStrip1.Items.Add(buttonText);
            this.contextMenuStrip1.Items[0].Click += onClick;
        }

        /// <summary>
        /// Event handler for when user clicks close
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCloseEditorPanel(object sender, EventArgs e)
        {
            this.bottomPanel.Visible = false;
            this.splitter.Visible = false;
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
            List<DataPoint> points = new List<DataPoint>();
            if (x != null && y != null && x != null && y != null)
            {
                // Create a new data point for each x.
                IEnumerator xEnum = x.GetEnumerator();
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
        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            Rectangle plotArea = this.plot1.Model.PlotArea.ToRect(false);
            if (plotArea.Contains(e.Location))
            {
                if (this.plot1.Model.LegendArea.ToRect(true).Contains(e.Location))
                {
                    int margin = Convert.ToInt32(this.plot1.Model.LegendMargin);
                    int y = Convert.ToInt32(e.Location.Y - this.plot1.Model.LegendArea.Top);
                    int itemHeight = Convert.ToInt32(this.plot1.Model.LegendArea.Height) / this.plot1.Model.Series.Count;
                    int seriesIndex = y / itemHeight;
                    if (this.OnLegendClick != null)
                    {
                        LegendClickArgs args = new LegendClickArgs();
                        args.seriesIndex = seriesIndex;
                        args.controlKeyPressed = ModifierKeys == Keys.Control;
                        this.OnLegendClick.Invoke(sender, args );
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

                Rectangle rightAxisArea = new Rectangle(plotArea.Right, plotArea.Top, this.Width - plotArea.Right, plotArea.Height);
                Rectangle bottomAxisArea = new Rectangle(plotArea.Left, plotArea.Bottom, plotArea.Width, this.Height - plotArea.Bottom);
                if (titleArea.Contains(e.Location))
                {
                    if (this.OnTitleClick != null)
                    {
                        this.OnTitleClick(sender, e);
                    }
                }

                if (this.OnAxisClick != null)
                {
                    if (leftAxisArea.Contains(e.Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Left);
                    }
                    else if (topAxisArea.Contains(e.Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Top);
                    }
                    else if (rightAxisArea.Contains(e.Location))
                    {
                        this.OnAxisClick.Invoke(Models.Graph.Axis.AxisType.Right);
                    }
                    else if (bottomAxisArea.Contains(e.Location))
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
        private void OnCaptionLabelDoubleClick(object sender, EventArgs e)
        {
            if (OnCaptionClick != null)
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
            try
            {
                plot1.Refresh();
            }
            catch (Exception)
            { }

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
    }
}

