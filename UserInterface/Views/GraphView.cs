using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OxyPlot.WindowsForms;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Axes;
using Models.Graph;
using System.Collections;

namespace UserInterface.Views
{
    public delegate void ClickDelegate();
    public delegate void ClickAxisDelegate(AxisPosition Position);
    interface IGraphView
    {
        event ClickDelegate OnPlotClick;
        event ClickAxisDelegate OnAxisClick;
        event ClickDelegate OnLegendClick;

        /// <summary>
        /// Draw the graph based on the Graph model passed in.
        /// </summary>
        void DrawGraph(Graph Graph);

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        void ShowEditorPanel(UserControl Editor);

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

        /// <summary>
        /// Draw the graph based on the Graph model passed in.
        /// </summary>
        public void DrawGraph(Graph Graph)
        {
            plot1.Model.Series.Clear();
            plot1.Model.Axes.Clear();
            if (Graph != null)
            {
                if (Graph.Series != null)
                {
                    foreach (Models.Graph.Series S in Graph.Series)
                    {
                        if (S.X != null && S.Y != null)
                        {
                            
                            ItemsSeries Series = null;
                            if (S.Type == Models.Graph.Series.SeriesType.Line || S.Type == Models.Graph.Series.SeriesType.None)
                                Series = CreateLineSeries(S);
                            else if (S.Type == Models.Graph.Series.SeriesType.Bar)
                                Series = CreateBarSeries(S);
                            Series.ItemsSource = PopulateDataPointSeries(S.X, S.Y, S.XAxis, S.YAxis);
                            plot1.Model.Series.Add(Series);
                        }
                    }
                }
                foreach (Models.Graph.Axis A in Graph.Axes)
                    FormatAxis(A);
                plot1.Model.RefreshPlot(true);
            }
        }

        /// <summary>
        /// Create a line series with the specified attributes.
        /// </summary>
        private ItemsSeries CreateLineSeries(Models.Graph.Series S)
        {
            if (S.X != null && S.Y != null)
            {
                LineSeries Series = new LineSeries(S.Title);

                bool Filled = false;
                string OxyMarkerName = S.Marker.ToString();
                if (OxyMarkerName.StartsWith("Filled"))
                {
                    OxyMarkerName = OxyMarkerName.Remove(0, 6);
                    Filled = true;
                }
                MarkerType Type;
                if (Enum.TryParse<MarkerType>(OxyMarkerName, out Type))
                    Series.MarkerType = Type;

                if (S.Type == Models.Graph.Series.SeriesType.None)
                    Series.LineStyle = LineStyle.None;

                Series.Color = ConverterExtensions.ToOxyColor(S.Colour);
                Series.MarkerSize = 7.0;
                Series.MarkerStroke = ConverterExtensions.ToOxyColor(S.Colour);
                if (Filled)
                {
                    Series.MarkerFill = ConverterExtensions.ToOxyColor(S.Colour);
                    Series.MarkerStroke = OxyColors.White;
                }
                return Series;
            }
            return null;
        }

        /// <summary>
        /// Create a bar series with the specified attributes.
        /// </summary>
        private ItemsSeries CreateBarSeries(Models.Graph.Series S)
        {
            Utility.ColumnXYSeries Series = new Utility.ColumnXYSeries();
            Series.FillColor = ConverterExtensions.ToOxyColor(S.Colour);
            Series.StrokeColor = ConverterExtensions.ToOxyColor(S.Colour);
            return Series;
        }


        /// <summary>
        /// Populate the specified DataPointSeries with data from the data table.
        /// </summary>
        private List<DataPoint> PopulateDataPointSeries(GraphValues X, GraphValues Y,
                                                        Models.Graph.Axis.AxisType XAxisType, 
                                                        Models.Graph.Axis.AxisType YAxisType)
        {
            List<string> XLabels = new List<string>();
            List<string> YLabels = new List<string>();
            Type XDataType = null;
            Type YDataType = null;
            List<DataPoint> Points = new List<DataPoint>();
            if (X != null && Y != null && X.Values != null && Y.Values != null)
            {
                IEnumerator XEnum = X.Values.GetEnumerator();
                IEnumerator YEnum = Y.Values.GetEnumerator();
                while (XEnum.MoveNext() && YEnum.MoveNext())
                {
                    DataPoint P = new DataPoint();
                    if (XEnum.Current.GetType() == typeof(DateTime))
                    {
                        P.X = DateTimeAxis.ToDouble(Convert.ToDateTime(XEnum.Current));
                        XDataType = typeof(DateTime);
                    }
                    else if (XEnum.Current.GetType() == typeof(double) || XEnum.Current.GetType() == typeof(float))
                    {
                        P.X = Convert.ToDouble(XEnum.Current);
                        XDataType = typeof(double);
                    }
                    else
                    {
                        XLabels.Add(XEnum.Current.ToString());
                        XDataType = typeof(string);

                    }
                    if (YEnum.Current.GetType() == typeof(DateTime))
                    {
                        P.Y = DateTimeAxis.ToDouble(Convert.ToDateTime(YEnum.Current));
                        YDataType = typeof(DateTime);
                    }
                    else if (YEnum.Current.GetType() == typeof(double) || YEnum.Current.GetType() == typeof(float))
                    {
                        P.Y = Convert.ToDouble(YEnum.Current);
                        YDataType = typeof(double);
                    }
                    else
                    {
                        YLabels.Add(YEnum.Current.ToString());
                        YDataType = typeof(string);

                    } 
                    Points.Add(P);
                }

                // Get the axes right for this data.
                if (XDataType != null)
                    EnsureAxisExists(XAxisType, XDataType);
                if (YDataType != null)
                    EnsureAxisExists(YAxisType, YDataType);
                if (XLabels.Count > 0)
                    (GetAxis(XAxisType) as CategoryAxis).Labels = XLabels;
                if (YLabels.Count > 0)
                    (GetAxis(YAxisType) as CategoryAxis).Labels = YLabels;

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
        /// Format the axis according to the Axis model passed in.
        /// </summary>
        private void FormatAxis(Models.Graph.Axis Axis)
        {
            OxyPlot.Axes.Axis OxyAxis = GetAxis(Axis.Type);
            if (Axis != null && OxyAxis != null)
            {
                OxyAxis.Title = Axis.Title;
                OxyAxis.MinorTickSize = 0;
                if (Axis.Inverted)
                {
                    OxyAxis.StartPosition = 1;
                    OxyAxis.EndPosition = 0;
                }
                else
                {
                    OxyAxis.StartPosition = 0;
                    OxyAxis.EndPosition = 1;
                }
            }
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
                Rectangle TopAxisArea = new Rectangle(PlotArea.X, 0,
                                                       PlotArea.Width, PlotArea.Y);
                Rectangle RightAxisArea = new Rectangle(PlotArea.Right, PlotArea.Top,
                                                        Width - PlotArea.Right, PlotArea.Height);
                Rectangle BottomAxisArea = new Rectangle(PlotArea.Left, PlotArea.Bottom,
                                                         PlotArea.Width, Height - PlotArea.Bottom);
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
