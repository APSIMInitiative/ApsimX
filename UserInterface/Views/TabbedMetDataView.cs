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

// This is the view used by the WeatherFile component

namespace UserInterface.Views
{
    public delegate void BrowseDelegate(string FileName);

    interface IMetDataView
    {
        void PopulateData(DataTable Data);
        event BrowseDelegate OnBrowseClicked;
    }

    public partial class TabbedMetDataView : UserControl, IMetDataView
    {
        public event BrowseDelegate OnBrowseClicked;

        public TabbedMetDataView()
        {
            InitializeComponent();
            plot1.Model = new PlotModel();
            plot1.Model.TitleFontWeight = 0;
            plot1.Model.TitleFontSize = 11;
            plot1.Model.Title = "Long term average monthly rainfall";
        }

        public String Filename
        {
            get { return label1.Text; }
            set { label1.Text = value;}
        }
        public String Summarylabel
        {
            set { richTextBox1.Text = value; }
        }
        public void PopulateData(DataTable Data)
        {
            //fill the grid with data
            dataGridView1.DataSource = Data;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            openFileDialog1.FileName = label1.Text;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                label1.Text = openFileDialog1.FileName;
                OnBrowseClicked.Invoke(label1.Text);    //reload the grid with data
            }
        }
        //=====================================================================
        public void ChartTitle(String sTitle)
        {
            if (plot1.Model != null)
                plot1.Model.Title = sTitle;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AxisPosition"></param>
        /// <param name="Title"></param>
        public void PopulateAxis(AxisPosition AxisPosition, string Title)
        {
            OxyPlot.Axes.Axis Axis = GetAxis(AxisPosition);
            if (Axis != null)
                Axis.Title = Title;
        }
        public void RefreshGraph()
        {
            plot1.Model.RefreshPlot(true);
        }
        public void ClearSeries()
        {
            if (plot1.Model.Series != null)
                plot1.Model.Series.Clear();
            plot1.Model.Axes.Clear();
        }

        /// <summary>
        /// Create a bar series with the specified attributes.
        /// </summary>
        public void CreateBarSeries(double[] Data, string SeriesName,
                                    string XColumnName, string YColumnName,
                                    AxisPosition XAxisPosition, AxisPosition YAxisPosition)
        {
            Utility.ColumnXYSeries newSeries = new Utility.ColumnXYSeries();
            // Ensure both axes exist.
            EnsureAxisExists(XAxisPosition, XColumnName);
            EnsureAxisExists(YAxisPosition, YColumnName);

            GetAxis(AxisPosition.Bottom).MinorTickSize = 0;
            GetAxis(AxisPosition.Left).ShowMinorTicks = true;
            //horizontal grid
            GetAxis(AxisPosition.Left).MajorGridlineStyle = LineStyle.Solid;
            GetAxis(AxisPosition.Left).MinorGridlineStyle = LineStyle.Dot;

            // Populate the series.
            List<DataPoint> Points = new List<DataPoint>();
            for (int i = 0; i < Data.Length; i++)
            {
                DataPoint P = new DataPoint();
                P.X = i + 1;
                P.Y = Data[i];
                Points.Add(P);
            }
            newSeries.ItemsSource = Points;
            newSeries.FillColor = OxyColor.FromRgb(64, 191, 255);
            newSeries.ColumnWidth = 0.05;   //% of axis width
            plot1.Model.Series.Add(newSeries);
        }
        /// <summary>
        /// Ensure the specified X exists. Uses the 'DataType' property of the DataColumn
        /// to determine the type of axis.
        /// </summary>
        private void EnsureAxisExists(AxisPosition AxisPosition, String sTitle)
        {
            // Make sure we have an axis at the correct position.
            if (GetAxis(AxisPosition) == null)
            {
                plot1.Model.Axes.Add(new LinearAxis(AxisPosition, sTitle));
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
    }
}
