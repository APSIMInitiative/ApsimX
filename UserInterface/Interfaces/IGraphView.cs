// -----------------------------------------------------------------------
// <copyright file="IGraphView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;
    using System.Collections;
    using System.Drawing;
    using System.Windows.Forms;
    using Models.Graph;
    using EventArguments;

    /// <summary>
    /// Event arguments for a Axis click
    /// </summary>
    /// <param name="axisType">The type of axis clicked</param>
    public delegate void ClickAxisDelegate(Axis.AxisType axisType);

    /// <summary>
    /// This interface defines the API for talking to a GraphView.
    /// </summary>
    public interface IGraphView
    {
        /// <summary>
        /// Invoked when the user clicks on the plot area (the area inside the axes)
        /// </summary>
        event EventHandler OnPlotClick;

        /// <summary>
        /// Invoked when the user clicks on an axis.
        /// </summary>
        event ClickAxisDelegate OnAxisClick;

        /// <summary>
        /// Invoked when the user clicks on a legend.
        /// </summary>
        event EventHandler<LegendClickArgs> OnLegendClick;

        /// <summary>
        /// Invoked when the user clicks on the graph caption.
        /// </summary>
        event EventHandler OnCaptionClick;

        /// <summary>
        /// Invoked when the user hovers over a series point.
        /// </summary>
        event EventHandler<HoverPointArgs> OnHoverOverPoint;

        /// <summary>
        /// Left margin in pixels.
        /// </summary>
        int LeftRightPadding { get; set; }

        /// <summary>
        /// Show the specified editor.
        /// </summary>
        /// <param name="editor">Show the specified series editor</param>
        void ShowEditorPanel(UserControl editor);

        /// <summary>
        /// Clear the graph of everything.
        /// </summary>
        void Clear();

        /// <summary>
        /// Update the graph data sources; this causes the axes minima and maxima to be calculated
        /// </summary>
        void UpdateView();

        /// <summary>
        /// Refresh the graph.
        /// </summary>
        void Refresh();

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
        void DrawLineAndMarkers(
             string title, 
             IEnumerable x, 
             IEnumerable y,
             Models.Graph.Axis.AxisType xAxisType, 
             Models.Graph.Axis.AxisType yAxisType,
             Color colour,
             Models.Graph.LineType lineType,
             Models.Graph.MarkerType markerType,
            bool showInLegend);

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
        void DrawBar(
            string title, 
            IEnumerable x, 
            IEnumerable y, 
            Models.Graph.Axis.AxisType xAxisType, 
            Models.Graph.Axis.AxisType yAxisType, 
            Color colour,
            bool showInLegend);

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
        void DrawArea(
            string title,
            IEnumerable x1,
            IEnumerable y1,
            IEnumerable x2,
            IEnumerable y2,
            Models.Graph.Axis.AxisType xAxisType,
            Models.Graph.Axis.AxisType yAxisType,
            Color colour,
            bool showInLegend);

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
        void DrawText(
            string text, 
            double x, 
            double y,
            Models.Graph.Axis.AxisType xAxisType, 
            Models.Graph.Axis.AxisType yAxisType,
            Color colour);

        /// <summary>
        /// Format the specified axis.
        /// </summary>
        /// <param name="axisType">The axis type to format</param>
        /// <param name="title">The axis title. If null then a default axis title will be shown</param>
        /// <param name="inverted">Invert the axis?</param>
        /// <param name="minimum">Minimum axis scale</param>
        /// <param name="maximum">Maximum axis scale</param>
        /// <param name="interval">Axis scale interval</param>
        void FormatAxis(
            Models.Graph.Axis.AxisType axisType, 
            string title,
            bool inverted,
            double minimum,
            double maximum,
            double interval);

        /// <summary>
        /// Format the legend.
        /// </summary>
        /// <param name="legendPositionType">Position of the legend</param>
        void FormatLegend(Models.Graph.Graph.LegendPositionType legendPositionType);

        /// <summary>
        /// Format the title.
        /// </summary>
        /// <param name="text">Text of the title</param>
        void FormatTitle(string text);

        /// <summary>
        /// Format the footer.
        /// </summary>
        /// <param name="text">The text for the footer</param>
        /// <param name="italics">Italics?</param>
        void FormatCaption(string text, bool italics);

        /// <summary>
        /// Export the graph to the specified 'bitmap'
        /// </summary>
        /// <param name="bitmap">Bitmap to write to</param>
        /// <param name="legendOutside">Put legend outside of graph?</param>
        void Export(Bitmap bitmap, bool legendOutside);

        /// <summary>
        /// Add an action (on context menu) on the memo.
        /// </summary>
        /// <param name="menuText">Menu item text</param>
        /// <param name="ticked">Menu ticked?</param>
        /// <param name="onClick">Event handler for menu item click</param>
        void AddContextAction(string menuText, bool ticked, System.EventHandler onClick);

        /// <summary>
        /// Gets the maximum scale of the specified axis.
        /// </summary>
        double AxisMaximum(Models.Graph.Axis.AxisType axisType);

        /// <summary>
        /// Gets the minimum scale of the specified axis.
        /// </summary>
        double AxisMinimum(Models.Graph.Axis.AxisType axisType);

        /// <summary>
        /// Gets the interval (major step) of the specified axis.
        /// </summary>
        double AxisMajorStep(Models.Graph.Axis.AxisType axisType);
        
        /// <summary>Gets the series names.</summary>
        /// <returns></returns>
        string[] GetSeriesNames();
    }
}