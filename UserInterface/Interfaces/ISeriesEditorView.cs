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
        event EventHandler SeriesTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series line type
        /// </summary>
        event EventHandler SeriesLineTypeChanged;

        /// <summary>
        /// Invoked when the user changes the series marker type
        /// </summary>
        event EventHandler SeriesMarkerTypeChanged;

        /// <summary>
        /// Invoked when the user changes the color
        /// </summary>
        event EventHandler ColourChanged;

        /// <summary>
        /// Invoked when the user changes the overall regression field
        /// </summary>
        event EventHandler OverallRegressionChanged;

        /// <summary>
        /// Invoked when the user changes the regression field
        /// </summary>
        event EventHandler RegressionChanged;

        /// <summary>
        /// Invoked when the user changes the x on top field
        /// </summary>
        event EventHandler XOnTopChanged;

        /// <summary>
        /// Invoked when the user changes the y on right field
        /// </summary>
        event EventHandler YOnRightChanged;

        /// <summary>
        /// Invoked when the user changes the cumulative field
        /// </summary>
        event EventHandler CumulativeChanged;

        /// <summary>
        /// Invoked when the user changes the x
        /// </summary>
        event EventHandler XChanged;

        /// <summary>
        /// Invoked when the user changes the y
        /// </summary>
        event EventHandler YChanged;

        /// <summary>
        /// Invoked when the user changes the x2
        /// </summary>
        event EventHandler X2Changed;

        /// <summary>
        /// Invoked when the user changes the y2
        /// </summary>
        event EventHandler Y2Changed;

        /// <summary>
        /// Invoked when the user changes the data source
        /// </summary>
        event EventHandler DataSourceChanged;

        /// <summary>
        /// Invoked when the user changes the show in legend
        /// </summary>
        event EventHandler ShowInLegendChanged;

        /// <summary>
        /// Invoked when the user changes the 'include in documentation' 
        /// </summary>
        event EventHandler IncludeInDocumentationChanged;

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
        /// Gets or sets a value indicating whether overall regression is turned on.
        /// </summary>
        bool OverallRegression { get; set; }

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
        /// Gets or sets a value indicating whether the series is cumulative.
        /// </summary>
        /// <value><c>true</c> if cumulative; otherwise, <c>false</c>.</value>
        bool Cumulative { get; set; }

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
        /// Gets or sets a value indicating whether the series should be shown in the legend
        /// </summary>
        bool ShowInLegend { get; set; }

        /// <summary>
        /// Gets or sets the selected data source name.
        /// </summary>
        string DataSource { get; set; }

        /// <summary>
        /// Gets or sets a list of field names 
        /// </summary>
        /// <param name="fieldNames">The available field names</param>
        void SetFieldNames(string[] fieldNames);    

        /// <summary>
        /// Sets the list of available data sources.
        /// </summary>
        /// <param name="dataSources">The available data sources</param>
        void SetDataSources(string[] dataSources);

        /// <summary>
        /// Show the x2 an y2 fields?
        /// </summary>
        /// <param name="show">Indicates whether the fields should be shown</param>
        void ShowX2Y2(bool show);

        /// <summary>
        /// Gets or sets a value indicating whether the graph should be included in documentation.
        /// </summary>
        bool IncludeInDocumentation { get; set; }
    }
}