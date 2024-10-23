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
using Models.Agroforestry;
using System.Globalization;

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
            Paned mainPanel = (Paned)builder.GetObject("vpaned1");
            Box constantsTab = (Box)builder.GetObject("constantsBox");
            Box graphContainer = (Box)builder.GetObject("hbox1");
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
            constantsTab.PackStart((Constants as ViewBase).MainWidget, true, true, 0);
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
        public void DrawGraphs(TreeProxySpatial spatialData)
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

                aboveGroundGraph.Model.Axes.Add(agxAxis);
                aboveGroundGraph.Model.Axes.Add(agyAxis);

                var yShade = spatialData.Shade;
                for (int i = 0; i < x.Length; i++)
                {
                    pointsShade.Add(new DataPoint(x[i], yShade[i]));
                }

                seriesShade.Title = "Shade";
                seriesShade.ItemsSource = pointsShade;
                aboveGroundGraph.Model.Series.Add(seriesShade);
                Color foregroundColour = ConfigureColor(true);
                Color backgroundColour = ConfigureColor(false);
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

                foreach (var thCutoff in spatialData.THCutoffs)
                {
                    Utility.LineSeriesWithTracker series = new Utility.LineSeriesWithTracker();
                    series.Title = thCutoff.ToString();
                    double[] data = spatialData.Rld(thCutoff);

                    List<DataPoint> points = new List<DataPoint>();

                    for (int j = 0; j < Math.Min(data.Length, SoilMidpoints.Length); j++)
                    {
                        points.Add(new DataPoint(data[j], SoilMidpoints[j]));
                    }
                    series.ItemsSource = points;
                    belowGroundGraph.Model.Series.Add(series);
                }

                Color foregroundColour = ConfigureColor(true);
                Color backgroundColour = ConfigureColor(false);
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

        /// <summary>
        /// Configures foreground or background color.
        /// Used to take into account when a theme is changed and
        /// when a restart is required to change a theme.
        /// </summary>
        /// <param name="isForegroundColor"></param>
        /// <returns>Either Color.Black or Color.White</returns>
        private Color ConfigureColor(bool isForegroundColor)
        {
            Color returnColor = Color.FromArgb(255, 48, 48, 48);
            if (isForegroundColor)
            {
                if (Utility.Configuration.Settings.ThemeRestartRequired)
                {
                    returnColor = Utility.Configuration.Settings.DarkTheme ? Color.Black : Color.White;
                }
                else returnColor = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;

                return returnColor;
            }
            else
            {
                if (Utility.Configuration.Settings.ThemeRestartRequired)
                {
                    returnColor = Utility.Configuration.Settings.DarkTheme ? Color.White : Color.Black;
                }
                else returnColor = Utility.Configuration.Settings.DarkTheme ? Color.Black : Color.White;

                return returnColor;
            }
        }
    }
}