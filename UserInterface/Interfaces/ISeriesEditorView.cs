// -----------------------------------------------------------------------
// <copyright file="ISeriesEditorView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;
    using System.Data;
    using System.Drawing;

    /// <summary>
    /// This interface defines the API for talking to an initial water view.
    /// </summary>
    public interface ISeriesEditorView
    {
        /// <summary>
        /// Invoked when the user changes the series type
        /// </summary>
        event EventHandler OnSeriesTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series line type
        /// </summary>
        event EventHandler OnSeriesLineTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series marker type
        /// </summary>
        event EventHandler OnSeriesMarkerTypeChanged;

        /// <summary>
        /// Invoked when the user changes the color
        /// </summary>
        event EventHandler OnColourChanged;

        /// <summary>
        /// Invoked when the user changes the regression field
        /// </summary>
        event EventHandler OnRegressionChanged;

        /// <summary>
        /// Invoked when the user changes the x on top field
        /// </summary>
        event EventHandler OnXOnTopChanged;

        /// <summary>
        /// Invoked when the user changes the y on right field
        /// </summary>
        event EventHandler OnYOnRightChanged;

        /// <summary>
        /// Invoked when the user changes the x
        /// </summary>
        event EventHandler OnXChanged;

        /// <summary>
        /// Invoked when the user changes the y
        /// </summary>
        event EventHandler OnYChanged;

        /// <summary>
        /// Invoked when the user changes the x2
        /// </summary>
        event EventHandler OnX2Changed;

        /// <summary>
        /// Invoked when the user changes the y2
        /// </summary>
        event EventHandler OnY2Changed;

        /// <summary>
        /// Invoked when the user changes the data source
        /// </summary>
        event EventHandler OnDataSourceChanged;

        /// <summary>
        /// Invoked when the user changes the show in legend
        /// </summary>
        event EventHandler OnShowInLegendChanged;

        /// <summary>
        /// Invoked when the user changes the separate series
        /// </summary>
        event EventHandler OnSeparateSeriesChanged;

        /// <summary>
        /// Gets or sets the series type
        /// </summary>
        string SeriesType { get; set; }

        /// <summary>
        /// Gets or sets the series line type
        /// </summary>
        string SeriesLineType { get; set; }

        /// <summary>
        /// Gets or sets the series marker type
        /// </summary>
        string SeriesMarkerType { get; set; }

        /// <summary>
        /// Gets or sets the series color.
        /// </summary>
        Color Colour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether regression is turned on.
        /// </summary>
        bool Regression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether x is on top.
        /// </summary>
        bool XOnTop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether y is on right.
        /// </summary>
        bool YOnRight { get; set; }

        /// <summary>
        /// Gets or sets the x variable name
        /// </summary>
        string X { get; set; }

        /// <summary>
        /// Gets or sets the y variable name
        /// </summary>
        string Y { get; set; }

        /// <summary>
        /// Gets or sets the second x variable name
        /// </summary>
        string X2 { get; set; }

        /// <summary>
        /// Gets or sets the second y variable name
        /// </summary>
        string Y2 { get; set; }

        /// <summary>
        /// Gets or set the show in legend checkbox
        /// </summary>
        bool ShowInLegend { get; set; }

        /// <summary>
        /// Gets or set the separate series checkbox
        /// </summary>
        bool SeparateSeries { get; set; }

        /// <summary>
        /// Sets the list of available data sources.
        /// </summary>
        /// <param name="data">The available data sources</param>
        void SetDataSources(string[] dataSources);

        /// <summary>
        /// Gets or sets the selected data source name.
        /// </summary>
        string DataSource { get; set; }

        /// <summary>
        /// Provides data for the currently selected data source.
        /// </summary>
        /// <param name="data">The data to show</param>
        void SetData(DataTable data);

        /// <summary>
        /// Show the x2 an y2 fields?
        /// </summary>
        /// <param name="show">Indicates whether the fields should be shown</param>
        void ShowX2Y2(bool show);

    }
}