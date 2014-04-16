using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OxyPlot.WindowsForms;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Axes;
using Models.Graph;
using System.Collections;
using OxyPlot.Annotations;

namespace UserInterface.Views
{
    public delegate void ClickDelegate();
    public delegate void ClickAxisDelegate(AxisPosition Position);
    interface IGraphView
    {
        event ClickDelegate OnPlotClick;
        event ClickAxisDelegate OnAxisClick;
        event ClickDelegate OnLegendClick;
        event ClickDelegate OnTitleClick;

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        void ShowEditorPanel(UserControl Editor);

        /// <summary>
        /// Clear the graph of everything.
        /// </summary>
        void Clear();

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        void Refresh();

        /// <summary>
        ///  Draw a line and markers series with the specified arguments.
        /// </summary>
        void DrawLineAndMarkers(string title, IEnumerable x, IEnumerable y, 
                                Models.Graph.Axis.AxisType xAxisType, Models.Graph.Axis.AxisType yAxisType, 
                                Color colour, 
                                Models.Graph.Series.LineType lineType, Models.Graph.Series.MarkerType markerType);

        /// <summary>
        /// Draw a bar series with the specified arguments.
        /// </summary>
        void DrawBar(string title, IEnumerable x, IEnumerable y, Models.Graph.Axis.AxisType xAxisType, Models.Graph.Axis.AxisType yAxisType, Color colour);

        /// <summary>
        /// Draw text on the graph at the specified coordinates.
        /// </summary>
        void DrawText(string text, double x, double y,
                      Models.Graph.Axis.AxisType xAxisType, Models.Graph.Axis.AxisType yAxisType,
                      Color colour);

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        void FormatAxis(Models.Graph.Axis.AxisType axisType, string p, bool p_2);

        /// <summary>
        /// Format the legend.
        /// </summary>
        void FormatLegend(Graph.LegendPositionType legendPositionType);

        /// <summary>
        /// Format the title.
        /// </summary>
        void FormatTitle(string text);
    }

    public partial class GraphView : UserControl, IGraphView
    {
        /// <summary>
        /// constructor
        /// </summary>
        public GraphView()
        {
            InitializeComponent();
            plot1.Model = new PlotModel();
            Splitter.Visible = false;
            BottomPanel.Visible = false;
        }

        public event ClickDelegate OnPlotClick;
        public event ClickAxisDelegate OnAxisClick;
        public event ClickDelegate OnLegendClick;
        public event ClickDelegate OnTitleClick;


        /// <summary>
        /// Clear the graph of everything.
        /// </summary>
        public void Clear()
        {
            plot1.Model.Series.Clear();
            plot1.Model.Axes.Clear();
        }

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        public override void Refresh()
        {
            plot1.Model.PlotAreaBorderThickness = 0;
            plot1.Model.LegendBorder = OxyColors.Black;
            plot1.Model.LegendBackground = OxyColors.White;
            plot1.Model.RefreshPlot(true);
        }

        /// <summary>
        /// Draw a line and markers series with the specified arguments.
        /// </summary>
        public void DrawLineAndMarkers(string title, IEnumerable x, IEnumerable y, 
                                       Models.Graph.Axis.AxisType xAxisType, Models.Graph.Axis.AxisType yAxisType, 
                                       Color colour, 
                                       Models.Graph.Series.LineType lineType, Models.Graph.Series.MarkerType markerType)
        {
            if (x != null && y != null)
            {
                LineSeries series = new LineSeries(title);
                series.Color = ConverterExtensions.ToOxyColor(colour);
                series.ItemsSource = PopulateDataPointSeries(x, y, xAxisType, yAxisType);


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
                    series.LineStyle = oxyLineType;
                
                // Marker type.
                MarkerType type;
                if (Enum.TryParse<MarkerType>(oxyMarkerName, out type))
                    series.MarkerType = type;

                //series.Color = OxyColors.White;
                series.MarkerSize = 7.0;
                series.MarkerStroke = ConverterExtensions.ToOxyColor(colour);
                if (filled)
                {
                    series.MarkerFill = ConverterExtensions.ToOxyColor(colour);
                    series.MarkerStroke = OxyColors.White;
                }
                plot1.Model.Series.Add(series);
            }
        }

        /// <summary>
        /// Draw a bar series with the specified arguments.
        /// </summary>
        public void DrawBar(string title, IEnumerable x, IEnumerable y,
                            Models.Graph.Axis.AxisType xAxisType, Models.Graph.Axis.AxisType yAxisType,
                            Color colour)
        {
            Utility.ColumnXYSeries series = new Utility.ColumnXYSeries();
            series.FillColor = ConverterExtensions.ToOxyColor(colour);
            series.StrokeColor = ConverterExtensions.ToOxyColor(colour);
            series.ItemsSource = PopulateDataPointSeries(x, y, xAxisType, yAxisType);
            plot1.Model.Series.Add(series);
        }

        /// <summary>
        /// Draw text on the graph at the specified coordinates.
        /// </summary>
        public void DrawText(string text, double x, double y, 
                             Models.Graph.Axis.AxisType xAxisType, Models.Graph.Axis.AxisType yAxisType,
                             Color colour)
        {
            TextAnnotation annotation = new TextAnnotation();
            annotation.Text = text;
            annotation.HorizontalAlignment = OxyPlot.HorizontalAlignment.Left;
            annotation.VerticalAlignment = VerticalAlignment.Top;
            annotation.Stroke = OxyColors.White;
            annotation.Position = new DataPoint(x, y);
            annotation.XAxis = GetAxis(xAxisType);
            annotation.YAxis = GetAxis(yAxisType);
            annotation.TextColor = ConverterExtensions.ToOxyColor(colour);
            plot1.Model.Annotations.Add(annotation);

        }

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        public void FormatAxis(Models.Graph.Axis.AxisType axisType, string title, bool inverted)
        {
            OxyPlot.Axes.Axis oxyAxis = GetAxis(axisType);
            if (oxyAxis != null)
            {
                oxyAxis.Title = title;
                oxyAxis.MinorTickSize = 0;
                oxyAxis.AxislineStyle = LineStyle.Solid;
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
        public void FormatLegend(Graph.LegendPositionType legendPositionType)
        {
            LegendPosition oxyLegendPosition;
            if (Enum.TryParse<LegendPosition>(legendPositionType.ToString(), out oxyLegendPosition))
                plot1.Model.LegendPosition = oxyLegendPosition;
        }

        /// <summary>
        /// Format the title.
        /// </summary>
        public void FormatTitle(string text)
        {
            plot1.Model.Title = text;
        }


        /// <summary>
        /// Populate the specified DataPointSeries with data from the data table.
        /// </summary>
        private List<DataPoint> PopulateDataPointSeries(IEnumerable x, IEnumerable y,
                                                        Models.Graph.Axis.AxisType xAxisType, 
                                                        Models.Graph.Axis.AxisType yAxisType)
        {
            List<string> xLabels = new List<string>();
            List<string> yLabels = new List<string>();
            Type xDataType = null;
            Type yDataType = null;
            List<DataPoint> Points = new List<DataPoint>();
            if (x != null && y != null && x != null && y != null)
            {
                IEnumerator xEnum = x.GetEnumerator();
                IEnumerator yEnum = y.GetEnumerator();
                while (xEnum.MoveNext() && yEnum.MoveNext())
                {
                    DataPoint P = new DataPoint();
                    if (xEnum.Current.GetType() == typeof(DateTime))
                    {
                        P.X = DateTimeAxis.ToDouble(Convert.ToDateTime(xEnum.Current));
                        xDataType = typeof(DateTime);
                    }
                    else if (xEnum.Current.GetType() == typeof(double) || xEnum.Current.GetType() == typeof(float))
                    {
                        P.X = Convert.ToDouble(xEnum.Current);
                        xDataType = typeof(double);
                    }
                    else
                    {
                        xLabels.Add(xEnum.Current.ToString());
                        xDataType = typeof(string);

                    }
                    if (yEnum.Current.GetType() == typeof(DateTime))
                    {
                        P.Y = DateTimeAxis.ToDouble(Convert.ToDateTime(yEnum.Current));
                        yDataType = typeof(DateTime);
                    }
                    else if (yEnum.Current.GetType() == typeof(double) || yEnum.Current.GetType() == typeof(float))
                    {
                        P.Y = Convert.ToDouble(yEnum.Current);
                        yDataType = typeof(double);
                    }
                    else
                    {
                        yLabels.Add(yEnum.Current.ToString());
                        yDataType = typeof(string);

                    } 
                    Points.Add(P);
                }

                // Get the axes right for this data.
                if (xDataType != null)
                    EnsureAxisExists(xAxisType, xDataType);
                if (yDataType != null)
                    EnsureAxisExists(yAxisType, yDataType);
                if (xLabels.Count > 0)
                    (GetAxis(xAxisType) as CategoryAxis).Labels = xLabels;
                if (yLabels.Count > 0)
                    (GetAxis(yAxisType) as CategoryAxis).Labels = yLabels;

                return Points;
            }
            else
                return null;
        }

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        public void ShowEditorPanel(UserControl Editor)
        {
            if (BottomPanel.Controls.Count > 1)
                BottomPanel.Controls.RemoveAt(1);
            BottomPanel.Controls.Add(Editor);
            BottomPanel.Visible = true;
            BottomPanel.Height = Editor.Height;
            Splitter.Visible = true;
            Editor.Dock = DockStyle.Fill;
            Editor.SendToBack();
        }

        /// <summary>
        /// Close the editor panel.
        /// </summary>
        public void CloseEditorPanel(object sender, EventArgs e)
        {
            BottomPanel.Visible = false;
            Splitter.Visible = false;
        }

        /// <summary>
        /// Ensure the specified X exists. Uses the 'DataType' property of the DataColumn
        /// to determine the type of axis.
        /// </summary>
        private void EnsureAxisExists(Models.Graph.Axis.AxisType AxisType, Type DataType)
        {
            // Make sure we have an x axis at the correct position.
            if (GetAxis(AxisType) == null)
            {
                AxisPosition Position = AxisTypeToPosition(AxisType);
                if (DataType == typeof(DateTime))
                    plot1.Model.Axes.Add(new DateTimeAxis(Position));
                else if (DataType == typeof(double))
                    plot1.Model.Axes.Add(new LinearAxis(Position));
                else
                    plot1.Model.Axes.Add(new CategoryAxis(Position));
            }
        }

        /// <summary>
        /// Return an axis that has the specified AxisType. Returns null if not found.
        /// </summary>
        private OxyPlot.Axes.Axis GetAxis(Models.Graph.Axis.AxisType AxisType)
        {
            AxisPosition Position = AxisTypeToPosition(AxisType);
            foreach (OxyPlot.Axes.Axis A in plot1.Model.Axes)
            {
                if (A.Position == Position)
                    return A;
            }
            return null;
        }

        /// <summary>
        /// Convert the Axis.AxisType into an OxyPlot.AxisPosition.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        private AxisPosition AxisTypeToPosition(Models.Graph.Axis.AxisType Type)
        {
            if (Type == Models.Graph.Axis.AxisType.Bottom)
                return AxisPosition.Bottom;
            else if (Type == Models.Graph.Axis.AxisType.Left)
                return AxisPosition.Left;
            else if (Type == Models.Graph.Axis.AxisType.Top)
                return AxisPosition.Top;
            return AxisPosition.Right;
        }

        /// <summary>
        /// User has double clicked somewhere on a graph.
        /// </summary>
        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            Rectangle PlotArea = plot1.Model.PlotArea.ToRect(false);
            if (PlotArea.Contains(e.Location))
            {
                if (plot1.Model.LegendArea.ToRect(true).Contains(e.Location))
                {
                    if (OnLegendClick != null) OnLegendClick.Invoke();
                }
                else
                    if (OnPlotClick != null) OnPlotClick.Invoke();
            }
            else 
            {
                Rectangle LeftAxisArea = new Rectangle(0, PlotArea.Y,
                                                       PlotArea.X, PlotArea.Height);

                Rectangle TitleArea = new Rectangle(PlotArea.X, 0,
                                                       PlotArea.Width, PlotArea.Y);
                Rectangle TopAxisArea = new Rectangle(PlotArea.X, 0,
                                                       PlotArea.Width, 0);

                if (GetAxis(Models.Graph.Axis.AxisType.Top) != null)
                {
                    TitleArea = new Rectangle(PlotArea.X, 0,
                                              PlotArea.Width, PlotArea.Y / 2);
                    TopAxisArea = new Rectangle(PlotArea.X, PlotArea.Y / 2,
                                                       PlotArea.Width, PlotArea.Y / 2);
                }

                Rectangle RightAxisArea = new Rectangle(PlotArea.Right, PlotArea.Top,
                                                        Width - PlotArea.Right, PlotArea.Height);
                Rectangle BottomAxisArea = new Rectangle(PlotArea.Left, PlotArea.Bottom,
                                                         PlotArea.Width, Height - PlotArea.Bottom);
                if (TitleArea.Contains(e.Location))
                {
                    if (OnTitleClick != null)
                        OnTitleClick();
                }
                
                if (OnAxisClick != null)
                {
                    if (LeftAxisArea.Contains(e.Location))
                        OnAxisClick.Invoke(AxisPosition.Left);
                    else if (TopAxisArea.Contains(e.Location))
                        OnAxisClick.Invoke(AxisPosition.Top);
                    else if (RightAxisArea.Contains(e.Location))
                        OnAxisClick.Invoke(AxisPosition.Right);
                    else if (BottomAxisArea.Contains(e.Location))
                        OnAxisClick.Invoke(AxisPosition.Bottom);
                }
            }
        }


    }
}
