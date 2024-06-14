using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Gtk;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.GtkSharp;
using UserInterface.Interfaces;
using System.Drawing;
using UserInterface.EventArguments;
using APSIM.Interop.Graphing.Extensions;

namespace UserInterface.Views
{
    /// <summary>
    /// A view that contains a graph and click zones for the user to allow
    /// editing various parts of the graph.
    /// </summary>
    /// <remarks>
    /// TODO : set the background colour of the first two rows to lightgray.
    /// </remarks>
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
        /// A list to hold all plots to make enumeration easier.
        /// </summary>
        private List<PlotView> plots = new List<PlotView>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeProxyView" /> class.
        /// </summary>
        public TreeProxyView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.TreeProxyView.glade");
            ScrolledWindow temporalDataTab = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            ScrolledWindow spatialDataTab = (ScrolledWindow)builder.GetObject("scrolledwindow2");
            VPaned mainPanel = (VPaned)builder.GetObject("vpaned1");
            Alignment constantsTab = (Alignment)builder.GetObject("alignment1");
            HBox graphContainer = (HBox)builder.GetObject("hbox1");
            mainWidget = mainPanel;

            TemporalDataGrid = new ContainerView(owner);
            temporalDataTab.Add((TemporalDataGrid as ViewBase).MainWidget);

            SpatialDataGrid = new ContainerView(owner);
            spatialDataTab.Add((SpatialDataGrid as ViewBase).MainWidget);

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

            Constants = new PropertyView(this);
            constantsTab.Add((Constants as ViewBase).MainWidget);
            MainWidget.ShowAll();
            mainWidget.Destroyed += MainWidgetDestroyed;
        }

        /// <summary>
        /// Depth midpoints of the soil layers.
        /// </summary>
        public double[] SoilMidpoints { get; set; }

        /// <summary>
        /// Grid which displays the temporal data.
        /// </summary>
        public ContainerView TemporalDataGrid { get; private set; }

        /// <summary>
        /// Grid which displays the spatial data.
        /// </summary>
        public ContainerView SpatialDataGrid { get; private set; }

        /// <summary>
        /// Constants grid.
        /// </summary>
        public IPropertyView Constants { get; private set; }

        /// <summary>
        /// Setup the graphs shown below the grids.
        /// </summary>
        public void DrawGraphs(DataTable spatialData)
        {
            double[] x = { 0, 0.5, 1, 1.5, 2, 2.5, 3, 4, 5, 6 };
            try
            {
                aboveGroundGraph.Model.Axes.Clear();
                aboveGroundGraph.Model.Series.Clear();
                belowGroundGraph.Model.Axes.Clear();
                belowGroundGraph.Model.Series.Clear();
                aboveGroundGraph.Model.Title = "Above Ground";
                aboveGroundGraph.Model.SetLegendBorder(OxyColors.Transparent);
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
                
                DataRow rowShade = spatialData.Rows[0];
                DataColumn col = spatialData.Columns[0];
                double[] yShade = new double[spatialData.Columns.Count - 1];

                aboveGroundGraph.Model.Axes.Add(agxAxis);
                aboveGroundGraph.Model.Axes.Add(agyAxis);

                for (int i = 1; i < spatialData.Columns.Count; i++)
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
                Color foregroundColour = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;
                Color backgroundColour = Utility.Configuration.Settings.DarkTheme ? Color.Black : Color.White;
                SetForegroundColour(aboveGroundGraph, foregroundColour);
                SetBackgroundColour(aboveGroundGraph, backgroundColour);
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
                belowGroundGraph.Model.SetLegendBorder(OxyColors.Transparent);
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
                
                for (int i = 1; i < spatialData.Columns.Count; i++)
                {
                    Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                    series.Title = spatialData.Columns[i].ColumnName;
                    double[] data = new double[spatialData.Rows.Count - 4];
                    for (int j = 4; j < spatialData.Rows.Count; j++)
                    {
                        if (spatialData.Rows[j].ItemArray[i].ToString().Length > 0) 
                            data[j - 4] = Convert.ToDouble(spatialData.Rows[j].ItemArray[i], System.Globalization.CultureInfo.InvariantCulture);
                        else
                            data[j - 4] = 0;
                    }

                    List<DataPoint> points = new List<DataPoint>();

                    for (int j = 0; j < Math.Min(data.Length, SoilMidpoints.Length); j++)
                    {
                        points.Add(new DataPoint(data[j], SoilMidpoints[j]));
                    }
                    series.ItemsSource = points;
                    belowGroundGraph.Model.Series.Add(series);
                }
                
                Color foregroundColour = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;
                Color backgroundColour = Utility.Configuration.Settings.DarkTheme ? Color.Black : Color.White;
                SetForegroundColour(belowGroundGraph, foregroundColour);
                SetBackgroundColour(belowGroundGraph, backgroundColour);
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

        private void SetBackgroundColour(PlotView graph, Color colour)
        {
            graph.Model.Background = Utility.Colour.ToOxy(colour);
        }

        private void SetForegroundColour(PlotView graph, Color colour)
        {
            OxyColor foregroundColour = Utility.Colour.ToOxy(colour);
            foreach (Axis oxyAxis in graph.Model.Axes)
            {
                oxyAxis.AxislineColor = foregroundColour;
                oxyAxis.ExtraGridlineColor = foregroundColour;
                oxyAxis.MajorGridlineColor = foregroundColour;
                oxyAxis.MinorGridlineColor = foregroundColour;
                oxyAxis.TicklineColor = foregroundColour;
                oxyAxis.MinorTicklineColor = foregroundColour;
                oxyAxis.TitleColor = foregroundColour;
                oxyAxis.TextColor = foregroundColour;

            }
            graph.Model.TextColor = foregroundColour;
            graph.Model.SetLegendTextColour(foregroundColour);
            graph.Model.SetLegendTitleColour(foregroundColour);
            graph.Model.SubtitleColor = foregroundColour;
            graph.Model.PlotAreaBorderColor = foregroundColour;
        }

        /// <summary>
        /// Invoked when the main widget is destroyed.
        /// Performs some cleanup, (hopefully) allowing this instance
        /// to be garbage collected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Dispose();
                mainWidget.Destroyed -= MainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}