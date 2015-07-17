// -----------------------------------------------------------------------
// <copyright file="SeriesPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using Models.Graph;
    using Views;
    using Models;
    using System.Data;
    using System.Drawing;
    using Models.Core;
    using System.Reflection;
    using Interfaces;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A presenter class for graph series.
    /// </summary>
    class SeriesPresenter : IPresenter
    {
        /// <summary>
        /// The graph model to work with.
        /// </summary>
        private Series series;

        /// <summary>
        /// The series view to work with.
        /// </summary>
        /// 
        private ISeriesView seriesView;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The graph presenter</summary>
        private GraphPresenter graphPresenter;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The graph model to work with</param>
        /// <param name="view">The series view to work with</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.series = model as Series;
            this.seriesView = view as SeriesView;
            this.explorerPresenter = explorerPresenter;

            Graph parentGraph = Apsim.Parent(series, typeof(Graph)) as Graph;
            if (parentGraph != null)
            {
                graphPresenter = new GraphPresenter();
                graphPresenter.Attach(parentGraph, seriesView.GraphView, explorerPresenter);
            }

            PopulateView();

            ConnectViewEvents();
        }

        /// <summary>
        /// Detach the model and view from this presenter.
        /// </summary>
        public void Detach()
        {
            if (graphPresenter != null)
                graphPresenter.Detach();
            
            DisconnectViewEvents();
        }

        /// <summary>
        /// Connect all view events.
        /// </summary>
        private void ConnectViewEvents()
        {
            this.seriesView.DataSourceChanged += OnDataSourceChanged;
            this.seriesView.SeriesTypeChanged += OnSeriesTypeChanged;
            this.seriesView.SeriesLineTypeChanged += OnSeriesLineTypeChanged;
            this.seriesView.SeriesMarkerTypeChanged += OnSeriesMarkerTypeChanged;
            this.seriesView.ColourChanged += OnColourChanged;
            this.seriesView.XOnTopChanged += OnXOnTopChanged;
            this.seriesView.YOnRightChanged += OnYOnRightChanged;
            this.seriesView.XChanged += OnXChanged;
            this.seriesView.YChanged += OnYChanged;
            this.seriesView.X2Changed += OnX2Changed;
            this.seriesView.Y2Changed += OnY2Changed;
            this.seriesView.ShowInLegendChanged += OnShowInLegendChanged;
            this.seriesView.CumulativeYChanged += OnCumulativeYChanged;
            this.seriesView.CumulativeXChanged += OnCumulativeXChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            this.seriesView.DataSourceChanged -= OnDataSourceChanged;
            this.seriesView.SeriesTypeChanged -= OnSeriesTypeChanged;
            this.seriesView.SeriesLineTypeChanged -= OnSeriesLineTypeChanged;
            this.seriesView.SeriesMarkerTypeChanged -= OnSeriesMarkerTypeChanged;
            this.seriesView.ColourChanged -= OnColourChanged;
            this.seriesView.XOnTopChanged -= OnXOnTopChanged;
            this.seriesView.YOnRightChanged -= OnYOnRightChanged;
            this.seriesView.XChanged -= OnXChanged;
            this.seriesView.YChanged -= OnYChanged;
            this.seriesView.X2Changed -= OnX2Changed;
            this.seriesView.Y2Changed -= OnY2Changed;
            this.seriesView.ShowInLegendChanged -= OnShowInLegendChanged;
            this.seriesView.CumulativeYChanged -= OnCumulativeYChanged;
            this.seriesView.CumulativeXChanged -= OnCumulativeXChanged;
        }

        /// <summary>
        /// Set the value of the graph models property
        /// </summary>
        /// <param name="name">The name of the property to set</param>
        /// <param name="value">The value of the property to set it to</param>
        private void SetModelProperty(string name, object value)
        {
            Commands.ChangeProperty command = new Commands.ChangeProperty(series, name, value);
            this.explorerPresenter.CommandHistory.Add(command);
        }

        #region Events from the view

        /// <summary>
        /// Series type has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        void OnSeriesTypeChanged(object sender, EventArgs e)
        {
            SeriesType seriesType = (SeriesType)Enum.Parse(typeof(SeriesType), this.seriesView.SeriesType);
            this.SetModelProperty("Type", seriesType);
        }

        /// <summary>
        /// Series line type has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesLineTypeChanged(object sender, EventArgs e)
        {
            LineType lineType = (LineType)Enum.Parse(typeof(LineType), this.seriesView.SeriesLineType);
            this.SetModelProperty("Line", lineType);
        }
        
        /// <summary>
        /// Series marker type has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesMarkerTypeChanged(object sender, EventArgs e)
        {
            MarkerType markerType = (MarkerType)Enum.Parse(typeof(MarkerType), this.seriesView.SeriesMarkerType);
            this.SetModelProperty("Marker", markerType);
        }

        /// <summary>
        /// Series color has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnColourChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Colour", this.seriesView.Colour);
        }

        /// <summary>
        /// X on top has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXOnTopChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Bottom;
            if (this.seriesView.XOnTop)
                axisType = Axis.AxisType.Top;
            this.SetModelProperty("XAxis", axisType);
        }

        /// <summary>
        /// Y on right has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYOnRightChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Left;
            if (this.seriesView.YOnRight)
                axisType = Axis.AxisType.Right;
            this.SetModelProperty("YAxis", axisType);
        }

        /// <summary>
        /// X has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("XFieldName", seriesView.X);
        }

        /// <summary>
        /// Y has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("YFieldName", seriesView.Y);
        }

        /// <summary>
        /// Cumulative check box has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeYChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Cumulative", this.seriesView.CumulativeY);
        }

        /// <summary>
        /// Cumulative X check box has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("CumulativeX", this.seriesView.CumulativeX);
        }

        /// <summary>
        /// X2 has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnX2Changed(object sender, EventArgs e)
        {
            this.SetModelProperty("X2FieldName", seriesView.X2);
        }

        /// <summary>
        /// Y2 has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnY2Changed(object sender, EventArgs e)
        {
            this.SetModelProperty("Y2FieldName", seriesView.Y2);
        }

        /// <summary>
        /// User has changed the data source.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDataSourceChanged(object sender, EventArgs e)
        {
            if (series.TableName != this.seriesView.DataSource)
            {
                this.SetModelProperty("TableName", this.seriesView.DataSource);
                DataStore dataStore = new DataStore(series);
                PopulateFieldNames(dataStore);
                dataStore.Disconnect();
            }
        }

        /// <summary>
        /// User has changed the show in legend
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnShowInLegendChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("ShowInLegend", this.seriesView.ShowInLegend);
        }

        #endregion

        /// <summary>
        /// Populate the views series editor with the current selected series.
        /// </summary>
        private void PopulateView()
        {
            // Populate the editor with a list of data sources.
            DataStore dataStore = new DataStore(series);
            List<string> dataSources = new List<string>();
            foreach (string tableName in dataStore.TableNames)
            {
                if (tableName != "Messages" && tableName != "InitialConditions")
                    dataSources.Add(tableName);
            }
            dataSources.Sort();
            this.seriesView.SetDataSources(dataSources.ToArray());

            // Populate other controls.
            this.seriesView.SeriesType = series.Type.ToString();
            this.seriesView.SeriesLineType = series.Line.ToString();
            this.seriesView.SeriesMarkerType = series.Marker.ToString();
            this.seriesView.Colour = series.Colour;
            this.seriesView.XOnTop = series.XAxis == Axis.AxisType.Top;
            this.seriesView.YOnRight = series.YAxis == Axis.AxisType.Right;
            this.seriesView.ShowInLegend = series.ShowInLegend;
            this.seriesView.CumulativeX = series.CumulativeX;
            this.seriesView.CumulativeY = series.Cumulative;
            this.seriesView.DataSource = series.TableName;

            PopulateFieldNames(dataStore);
            dataStore.Disconnect();

            this.seriesView.X = series.XFieldName;
            this.seriesView.Y = series.YFieldName;
            this.seriesView.X2 = series.X2FieldName;
            this.seriesView.Y2 = series.Y2FieldName;

            this.seriesView.ShowX2Y2(series.Type == SeriesType.Area);
        }

        /// <summary>Populates the field names in the view.</summary>
        /// <param name="dataStore">The data store.</param>
        private void PopulateFieldNames(DataStore dataStore)
        {
            if (this.seriesView.DataSource != null)
            {
                DataTable data = dataStore.RunQuery("SELECT * FROM " + this.seriesView.DataSource + " LIMIT 1");
                if (data != null)
                    this.seriesView.SetFieldNames(DataTableUtilities.GetColumnNames(data));
            }
        }

    }
}
