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

namespace UserInterface.Views
{
    public delegate void ClickDelegate();
    public delegate void ClickAxisDelegate(AxisPosition Position);
    interface IGraphView
    {
        event ClickDelegate OnPlotClick;
        event ClickAxisDelegate OnAxisClick;
        event ClickDelegate OnLegendClick;

        void RefreshGraph();
        void ClearSeries();
        void CreateLineSeries(DataTable Data, string SeriesName,
                                     string XColumnName, string YColumnName,
                                     AxisPosition XAxisPosition, AxisPosition YAxisPosition);

        void CreateBarSeries(DataTable Data, string SeriesName,
                                    string XColumnName, string YColumnName,
                                    AxisPosition XAxisPosition, AxisPosition YAxisPosition);

        void ShowEditorPanel(UserControl Editor);
        void PopulateAxis(AxisPosition AxisPosition, string Title);
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


        public void RefreshGraph()
        {
            plot1.Model.RefreshPlot(true);
        }

        public void ClearSeries()
        {
            plot1.Model.Series.Clear();
            plot1.Model.Axes.Clear();
        }

        /// <summary>
        /// Create a line series with the specified attributes.
        /// </summary>
        public void CreateLineSeries(DataTable Data, string SeriesName,
                                     string XColumnName, string YColumnName,
                                     AxisPosition XAxisPosition, AxisPosition YAxisPosition)
        {
            if (Data.Columns.IndexOf(XColumnName) != -1 && Data.Columns.IndexOf(YColumnName) != -1)
            {
                LineSeries Series = new LineSeries(SeriesName);
                PopulateDataPointSeries(Series, Data, XColumnName, YColumnName, XAxisPosition, YAxisPosition);
            }
        }

        /// <summary>
        /// Create a bar series with the specified attributes.
        /// </summary>
        public void CreateBarSeries(DataTable Data, string SeriesName,
                                    string XColumnName, string YColumnName,
                                    AxisPosition XAxisPosition, AxisPosition YAxisPosition)
        {
            Utility.ColumnXYSeries Series = new Utility.ColumnXYSeries();

            // get the x and y columns.
            DataColumn X = Data.Columns[XColumnName];
            DataColumn Y = Data.Columns[YColumnName];

            // Ensure both axes exist.
            EnsureAxisExists(XAxisPosition, X);
            EnsureAxisExists(YAxisPosition, Y);

            // Populate the series.
            foreach (DataRow Row in Data.Rows)
            {
                DataPoint P = new DataPoint();
                if (X.DataType == typeof(DateTime))
                    P.X = DateTimeAxis.ToDouble(Convert.ToDateTime(Row[X]));
                else
                    P.X = Convert.ToDouble(Row[X]);
                if (Y.DataType == typeof(DateTime))
                    P.Y = DateTimeAxis.ToDouble(Convert.ToDateTime(Row[Y]));
                else
                    P.Y = Convert.ToDouble(Row[Y]);
                Series.Points.Add(P);
            }
            plot1.Model.Series.Add(Series);
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
        }

        public void PopulateAxis(AxisPosition AxisPosition, string Title)
        {
            OxyPlot.Axes.Axis Axis = GetAxis(AxisPosition);
            if (Axis != null)
                Axis.Title = Title;
        }

        /// <summary>
        /// Populate the specified DataPointSeries with data from the data table.
        /// </summary>
        private void PopulateDataPointSeries(DataPointSeries Series, DataTable Data, string XColumnName, string YColumnName, AxisPosition XAxisPosition, AxisPosition YAxisPosition)
        {
            if (Data.Columns.IndexOf(XColumnName) != -1 && Data.Columns.IndexOf(YColumnName) != -1)
            {
                // get the x and y columns.
                DataColumn X = Data.Columns[XColumnName];
                DataColumn Y = Data.Columns[YColumnName];

                if (X != null && Y != null)
                {
                    // Ensure both axes exist.
                    EnsureAxisExists(XAxisPosition, X);
                    EnsureAxisExists(YAxisPosition, Y);

                    // Populate the series.
                    foreach (DataRow Row in Data.Rows)
                    {
                        DataPoint P = new DataPoint();
                        if (X.DataType == typeof(DateTime))
                            P.X = DateTimeAxis.ToDouble(Convert.ToDateTime(Row[X]));
                        else
                            P.X = Convert.ToDouble(Row[X]);
                        if (Y.DataType == typeof(DateTime))
                            P.Y = DateTimeAxis.ToDouble(Convert.ToDateTime(Row[Y]));
                        else
                            P.Y = Convert.ToDouble(Row[Y]);
                        Series.Points.Add(P);
                    }
                    plot1.Model.Series.Add(Series);
                }
            }
        }

        /// <summary>
        /// Ensure the specified X exists. Uses the 'DataType' property of the DataColumn
        /// to determine the type of axis.
        /// </summary>
        private void EnsureAxisExists(AxisPosition AxisPosition, DataColumn DataColumn)
        {
            // Make sure we have an x axis at the correct position.
            if (GetAxis(AxisPosition) == null)
            {
                if (DataColumn.DataType == typeof(DateTime))
                    plot1.Model.Axes.Add(new DateTimeAxis(AxisPosition));
                else
                    plot1.Model.Axes.Add(new LinearAxis(AxisPosition));
            }
        }

        /// <summary>
        /// Return an axis that has the specified Position. Returns null if not found.
        /// </summary>
        private OxyPlot.Axes.Axis GetAxis(AxisPosition Position)
        {
            foreach (OxyPlot.Axes.Axis A in plot1.Model.Axes)
            {
                if (A.Position == Position)
                    return A;
            }
            return null;
        }

        private void plot1_MouseDoubleClick(object sender, MouseEventArgs e)
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

        /// <summary>
        /// User has closed the bottom panel. Make it invisible.
        /// </summary>
        private void OnCloseButtonClick(object sender, EventArgs e)
        {
            BottomPanel.Visible = false;
            Splitter.Visible = false;
        }

        private void GraphView_Load(object sender, EventArgs e)
        {

        }


    }
}
