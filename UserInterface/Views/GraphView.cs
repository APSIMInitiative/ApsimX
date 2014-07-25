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
    using System.Drawing;
    using System.Windows.Forms;
    using Interfaces;
    using Models.Graph;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using OxyPlot.WindowsForms;

    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    public partial class GraphView : UserControl, Interfaces.IGraphView
    {
        /// <summary>
        /// Overall font size for the graph.
        /// </summary>
        private const int FontSize = 16;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        public GraphView()
        {
            this.InitializeComponent();
            this.plot1.Model = new PlotModel();
            this.splitter.Visible = false;
            this.bottomPanel.Visible = false;
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
        public event EventHandler OnLegendClick;

        /// <summary>
        /// Invoked when the user clicks on the graph title.
        /// </summary>
        public event EventHandler OnTitleClick;

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
            this.plot1.Model.PlotAreaBorderThickness = 0;
            this.plot1.Model.PlotMargins = new OxyThickness(100, 0, 0, 0);
            this.plot1.Model.LegendBorder = OxyColors.Black;
            this.plot1.Model.LegendBackground = OxyColors.White;
            this.plot1.Model.RefreshPlot(true);

            foreach (OxyPlot.Axes.Axis axis in this.plot1.Model.Axes)
            {
                this.FormatAxisTickLabels(axis);
            }

            this.plot1.Model.RefreshPlot(true);
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
             Models.Graph.Series.MarkerType markerType)
        {
            if (x != null && y != null)
            {
                LineSeries series = new LineSeries(title);
                series.Color = ConverterExtensions.ToOxyColor(colour);
                series.ItemsSource = this.PopulateDataPointSeries(x, y, xAxisType, yAxisType);
                series.XAxisKey = xAxisType.ToString();
                series.YAxisKey = yAxisType.ToString();

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
            Color colour)
        {
            Utility.ColumnXYSeries series = new Utility.ColumnXYSeries();
            series.FillColor = ConverterExtensions.ToOxyColor(colour);
            series.StrokeColor = ConverterExtensions.ToOxyColor(colour);
            series.ItemsSource = this.PopulateDataPointSeries(x, y, xAxisType, yAxisType);
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
            Color colour)
        {
            AreaSeries series = new AreaSeries();
            series.Color = ConverterExtensions.ToOxyColor(colour);
            series.Fill = ConverterExtensions.ToOxyColor(colour);
            List<IDataPoint> points = this.PopulateDataPointSeries(x1, y1, xAxisType, yAxisType);
            List<IDataPoint> points2 = this.PopulateDataPointSeries(x2, y2, xAxisType, yAxisType);

            foreach (IDataPoint point in points)
            {
                series.Points.Add(point);
            }

            foreach (IDataPoint point in points2)
            {
                series.Points2.Add(point);
            }
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
            annotation.HorizontalAlignment = OxyPlot.HorizontalAlignment.Left;
            annotation.VerticalAlignment = VerticalAlignment.Top;
            annotation.Stroke = OxyColors.White;
            annotation.Position = new DataPoint(x, y);
            annotation.XAxis = this.GetAxis(xAxisType);
            annotation.YAxis = this.GetAxis(yAxisType);
            annotation.TextColor = ConverterExtensions.ToOxyColor(colour);
            this.plot1.Model.Annotations.Add(annotation);
        }

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        /// <param name="axisType">The axis type to format</param>
        /// <param name="title">The axis title. If null then a default axis title will be shown</param>
        /// <param name="inverted">Invert the axis?</param>
        public void FormatAxis(
            Models.Graph.Axis.AxisType axisType,
            string title,
            bool inverted)
        {
            OxyPlot.Axes.Axis oxyAxis = this.GetAxis(axisType);
            if (oxyAxis != null)
            {
                oxyAxis.FontSize = FontSize;
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
            this.plot1.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            this.plot1.Dock = DockStyle.Fill;
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
        private List<IDataPoint> PopulateDataPointSeries(
            IEnumerable x, 
            IEnumerable y,                                
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType)
        {
            List<string> xLabels = new List<string>();
            List<string> yLabels = new List<string>();
            Type xDataType = null;
            Type yDataType = null;
            List<IDataPoint> points = new List<IDataPoint>();
            if (x != null && y != null && x != null && y != null)
            {
                IEnumerator xEnum = x.GetEnumerator();
                IEnumerator yEnum = y.GetEnumerator();
                while (xEnum.MoveNext() && yEnum.MoveNext())
                {
                    bool xIsMissing = false;
                    bool yIsMissing = false;

                    DataPoint p = new DataPoint();
                    if (xEnum.Current.GetType() == typeof(DateTime))
                    {
                        p.X = DateTimeAxis.ToDouble(Convert.ToDateTime(xEnum.Current));
                        xDataType = typeof(DateTime);
                    }
                    else if (xEnum.Current.GetType() == typeof(double) || xEnum.Current.GetType() == typeof(float))
                    {
                        p.X = Convert.ToDouble(xEnum.Current);
                        xDataType = typeof(double);
                        xIsMissing = double.IsNaN(p.X);
                    }
                    else
                    {
                        xLabels.Add(xEnum.Current.ToString());
                        xDataType = typeof(string);
                    }

                    if (yEnum.Current.GetType() == typeof(DateTime))
                    {
                        p.Y = DateTimeAxis.ToDouble(Convert.ToDateTime(yEnum.Current));
                        yDataType = typeof(DateTime);
                    }
                    else if (yEnum.Current.GetType() == typeof(double) || yEnum.Current.GetType() == typeof(float))
                    {
                        p.Y = Convert.ToDouble(yEnum.Current);
                        yDataType = typeof(double);
                        yIsMissing = double.IsNaN(p.Y);
                    }
                    else
                    {
                        yLabels.Add(yEnum.Current.ToString());
                        yDataType = typeof(string);
                    }

                    if (!xIsMissing && !yIsMissing)
                    {
                        points.Add(p);
                    }
                }

                // Get the axes right for this data.
                if (xDataType != null)
                {
                    this.EnsureAxisExists(xAxisType, xDataType);
                }

                if (yDataType != null)
                {
                    this.EnsureAxisExists(yAxisType, yDataType);
                }

                if (xLabels.Count > 0)
                {
                    (this.GetAxis(xAxisType) as CategoryAxis).Labels = xLabels;
                }

                if (yLabels.Count > 0)
                {
                    (this.GetAxis(yAxisType) as CategoryAxis).Labels = yLabels;
                }

                return points;
            }
            else
            {
                return null;
            }
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
                    axisToAdd = new DateTimeAxis(position);
                }
                else if (dataType == typeof(double))
                {
                    axisToAdd = new LinearAxis(position);
                }
                else
                {
                    axisToAdd = new CategoryAxis(position);
                }

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
            AxisPosition position = this.AxisTypeToPosition(axisType);
            foreach (OxyPlot.Axes.Axis a in this.plot1.Model.Axes)
            {
                if (a.Position == position)
                {
                    return a;
                }
            }

            return null;
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
                    if (this.OnLegendClick != null)
                    {
                        this.OnLegendClick.Invoke(sender, e);
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
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="buttonText">Text for button</param>
        /// <param name="onClick">Event handler for button click</param>
        public void AddContextAction(string buttonText, System.EventHandler onClick)
        {
            contextMenuStrip1.Items.Add(buttonText);
            contextMenuStrip1.Items[0].Click += onClick;
        }
    }
}
