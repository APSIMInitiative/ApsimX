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
    using System.Linq;
    using Models.Core;
    using System.Reflection;
    using Interfaces;
    using APSIM.Shared.Utilities;
    using Models.Factorial;

    /// <summary>
    /// A presenter class for graph series.
    /// </summary>
    class SeriesPresenter : IPresenter
    {
        /// <summary>The graph model to work with.</summary>
        private Series series;

        /// <summary>The series view to work with.</summary>
        private ISeriesView seriesView;

        /// <summary>The parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The graph presenter</summary>
        private GraphPresenter graphPresenter;

        /// <summary>Attach the model and view to this presenter.</summary>
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

        /// <summary>Detach the model and view from this presenter.</summary>
        public void Detach()
        {
            if (graphPresenter != null)
                graphPresenter.Detach();
            
            DisconnectViewEvents();
        }

        /// <summary>Connect all view events.</summary>
        private void ConnectViewEvents()
        {
            this.seriesView.DataSource.Changed += OnDataSourceChanged;
            this.seriesView.SeriesType.Changed += OnSeriesTypeChanged;
            this.seriesView.LineType.Changed += OnLineTypeChanged;
            this.seriesView.MarkerType.Changed += OnMarkerTypeChanged;
            this.seriesView.Colour.Changed += OnColourChanged;
            this.seriesView.XOnTop.Changed += OnXOnTopChanged;
            this.seriesView.YOnRight.Changed += OnYOnRightChanged;
            this.seriesView.X.Changed += OnXChanged;
            this.seriesView.Y.Changed += OnYChanged;
            this.seriesView.X2.Changed += OnX2Changed;
            this.seriesView.Y2.Changed += OnY2Changed;
            this.seriesView.ShowInLegend.Changed += OnShowInLegendChanged;
            this.seriesView.YCumulative.Changed += OnCumulativeYChanged;
            this.seriesView.XCumulative.Changed += OnCumulativeXChanged;
            this.seriesView.Filter.Changed += OnFilterChanged;
        }

        /// <summary>Disconnect all view events.</summary>
        private void DisconnectViewEvents()
        {
            this.seriesView.DataSource.Changed -= OnDataSourceChanged;
            this.seriesView.SeriesType.Changed -= OnSeriesTypeChanged;
            this.seriesView.LineType.Changed -= OnLineTypeChanged;
            this.seriesView.MarkerType.Changed -= OnMarkerTypeChanged;
            this.seriesView.Colour.Changed -= OnColourChanged;
            this.seriesView.XOnTop.Changed -= OnXOnTopChanged;
            this.seriesView.YOnRight.Changed -= OnYOnRightChanged;
            this.seriesView.X.Changed -= OnXChanged;
            this.seriesView.Y.Changed -= OnYChanged;
            this.seriesView.X2.Changed -= OnX2Changed;
            this.seriesView.Y2.Changed -= OnY2Changed;
            this.seriesView.ShowInLegend.Changed -= OnShowInLegendChanged;
            this.seriesView.YCumulative.Changed -= OnCumulativeYChanged;
            this.seriesView.XCumulative.Changed -= OnCumulativeXChanged;
            this.seriesView.Filter.Changed -= OnFilterChanged;
        }

        /// <summary>Set the value of the graph models property</summary>
        /// <param name="name">The name of the property to set</param>
        /// <param name="value">The value of the property to set it to</param>
        private void SetModelProperty(string name, object value)
        {
            Commands.ChangeProperty command = new Commands.ChangeProperty(series, name, value);
            this.explorerPresenter.CommandHistory.Add(command);
        }

        #region Events from the view

        /// <summary>Series type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        void OnSeriesTypeChanged(object sender, EventArgs e)
        {
            SeriesType seriesType = (SeriesType)Enum.Parse(typeof(SeriesType), this.seriesView.SeriesType.SelectedValue);
            this.SetModelProperty("Type", seriesType);
        }

        /// <summary>Series line type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnLineTypeChanged(object sender, EventArgs e)
        {
            LineType lineType;
            if (Enum.TryParse<LineType>(this.seriesView.LineType.SelectedValue, out lineType))
            {
                this.SetModelProperty("Line", lineType);
                this.SetModelProperty("FactorIndexToVaryLines", -1);

            }
            else
            {
                List<string> values = new List<string>();
                values.AddRange(FactorNames.Select(factorName => "Vary by " + factorName));
                int factorIndex = values.IndexOf(this.seriesView.LineType.SelectedValue);
                this.SetModelProperty("FactorIndexToVaryLines", factorIndex);
            }
        }
        
        /// <summary>Series marker type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMarkerTypeChanged(object sender, EventArgs e)
        {
            MarkerType markerType;
            if (Enum.TryParse<MarkerType>(this.seriesView.MarkerType.SelectedValue, out markerType))
            {
                this.SetModelProperty("Marker", markerType);
                this.SetModelProperty("FactorIndexToVaryMarkers", -1);
            }
            else
            {
                List<string> values = new List<string>();
                values.AddRange(FactorNames.Select(factorName => "Vary by " + factorName));
                int factorIndex = values.IndexOf(this.seriesView.MarkerType.SelectedValue);
                this.SetModelProperty("FactorIndexToVaryMarkers", factorIndex);
            }
        }

        /// <summary>Series color has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnColourChanged(object sender, EventArgs e)
        {
            object obj = seriesView.Colour.SelectedValue;
            if (obj is Color)
            {
                this.SetModelProperty("Colour", obj);
                this.SetModelProperty("FactorIndexToVaryColours", -1);
            }
            else
            {
                List<string> colourOptions = new List<string>();
                colourOptions.AddRange(FactorNames.Select(factorName => "Vary by " + factorName));
                int factorIndex = colourOptions.IndexOf(obj.ToString());
                this.SetModelProperty("FactorIndexToVaryColours", factorIndex);
            }
        }

        /// <summary>X on top has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXOnTopChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Bottom;
            if (this.seriesView.XOnTop.IsChecked)
                axisType = Axis.AxisType.Top;
            this.SetModelProperty("XAxis", axisType);
        }

        /// <summary>Y on right has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYOnRightChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Left;
            if (this.seriesView.YOnRight.IsChecked)
                axisType = Axis.AxisType.Right;
            this.SetModelProperty("YAxis", axisType);
        }

        /// <summary>X has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("XFieldName", seriesView.X.SelectedValue);
        }

        /// <summary>Y has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("YFieldName", seriesView.Y.SelectedValue);
        }

        /// <summary>Cumulative check box has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeYChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Cumulative", this.seriesView.YCumulative.IsChecked);
        }

        /// <summary>Cumulative X check box has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("CumulativeX", this.seriesView.XCumulative.IsChecked);
        }

        /// <summary>X2 has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnX2Changed(object sender, EventArgs e)
        {
            this.SetModelProperty("X2FieldName", seriesView.X2.SelectedValue);
        }

        /// <summary>Y2 has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnY2Changed(object sender, EventArgs e)
        {
            this.SetModelProperty("Y2FieldName", seriesView.Y2.SelectedValue);
        }

        /// <summary>User has changed the data source.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDataSourceChanged(object sender, EventArgs e)
        {
            if (series.TableName != this.seriesView.DataSource.SelectedValue)
            {
                this.SetModelProperty("TableName", this.seriesView.DataSource.SelectedValue);
                DataStore dataStore = new DataStore(series);
                PopulateFieldNames(dataStore);
                dataStore.Disconnect();
            }
        }

        /// <summary>User has changed the show in legend</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnShowInLegendChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("ShowInLegend", this.seriesView.ShowInLegend.IsChecked);
        }

        /// <summary>User has changed the filter</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Filter", this.seriesView.Filter.Value);
        }

        #endregion

        /// <summary>Populate the views series editor with the current selected series.</summary>
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
            this.seriesView.DataSource.Values = dataSources.ToArray();

            PopulateMarkerDropDown();
            PopulateLineDropDown();
            PopulateColourDropDown();

            // Populate other controls.
            this.seriesView.SeriesType.SelectedValue = series.Type.ToString();
            this.seriesView.XOnTop.IsChecked = series.XAxis == Axis.AxisType.Top;
            this.seriesView.YOnRight.IsChecked = series.YAxis == Axis.AxisType.Right;
            this.seriesView.ShowInLegend.IsChecked = series.ShowInLegend;
            this.seriesView.XCumulative.IsChecked = series.CumulativeX;
            this.seriesView.YCumulative.IsChecked = series.Cumulative;
            this.seriesView.DataSource.SelectedValue = series.TableName;
            this.seriesView.Filter.Value = series.Filter;

            PopulateFieldNames(dataStore);
            dataStore.Disconnect();

            this.seriesView.X.SelectedValue = series.XFieldName;
            this.seriesView.Y.SelectedValue = series.YFieldName;
            this.seriesView.X2.SelectedValue = series.X2FieldName;
            this.seriesView.Y2.SelectedValue = series.Y2FieldName;

            this.seriesView.ShowX2Y2(series.Type == SeriesType.Area);
        }

        /// <summary>Populate the line drop down.</summary>
        private void PopulateLineDropDown()
        {
            List<string> values = new List<string>(Enum.GetNames(typeof(LineType)));
            values.AddRange(FactorNames.Select(factorName => "Vary by " + factorName));
            this.seriesView.LineType.Values = values.ToArray();
            if (series.FactorIndexToVaryLines == -1)
                this.seriesView.LineType.SelectedValue = series.Line.ToString();
            else if (series.FactorIndexToVaryLines >= FactorNames.Count)
            {
                series.FactorIndexToVaryLines = -1;
                this.seriesView.LineType.SelectedValue = series.Line.ToString();
            }
            else
                this.seriesView.LineType.SelectedValue = "Vary by " + FactorNames[series.FactorIndexToVaryLines];
        }

        /// <summary>Populate the marker drop down.</summary>
        private void PopulateMarkerDropDown()
        {
            List<string> values = new List<string>(Enum.GetNames(typeof(MarkerType)));
            values.AddRange(FactorNames.Select(factorName => "Vary by " + factorName));
            this.seriesView.MarkerType.Values = values.ToArray();
            if (series.FactorIndexToVaryMarkers == -1)
                this.seriesView.MarkerType.SelectedValue = series.Marker.ToString();
            else if (series.FactorIndexToVaryMarkers >= FactorNames.Count)
            {
                series.FactorIndexToVaryMarkers = -1;
                this.seriesView.MarkerType.SelectedValue = series.Marker.ToString();
            }
            else
                this.seriesView.MarkerType.SelectedValue = "Vary by " + FactorNames[series.FactorIndexToVaryMarkers];
        }

        /// <summary>Populate the colour drop down in the view.</summary>
        private void PopulateColourDropDown()
        {
            List<object> colourOptions = new List<object>();
            foreach (Color colour in ColourUtilities.Colours)
                colourOptions.Add(colour);

            // Send colour options to view.
            colourOptions.AddRange(FactorNames.Select(factorName => "Vary by " + factorName));

            this.seriesView.Colour.Values = colourOptions.ToArray();
            if (series.FactorIndexToVaryColours == -1)
                this.seriesView.Colour.SelectedValue = series.Colour;
            else if (series.FactorIndexToVaryColours >= FactorNames.Count)
            {
                series.FactorIndexToVaryColours = -1;
                this.seriesView.Colour.SelectedValue = series.Colour;
            }
            else
                this.seriesView.Colour.SelectedValue = "Vary by " + FactorNames[series.FactorIndexToVaryColours];
        }

        /// <summary>Gets a list of factor names. Never returns null.</summary>
        private List<string> FactorNames
        {
            get
            {
                // Send colour options to view.
                Experiment experiment = Apsim.Parent(series, typeof(Experiment)) as Experiment;
                if (experiment != null)
                {
                    Factors factorsModel = Apsim.Child(experiment as IModel, typeof(Factors)) as Factors;
                    if (factorsModel != null)
                        return factorsModel.Children.Select(f => f.Name).ToList();
                }

                return new List<string>();
            }
        }

        /// <summary>Populates the field names in the view.</summary>
        /// <param name="dataStore">The data store.</param>
        private void PopulateFieldNames(DataStore dataStore)
        {
            if (this.seriesView.DataSource != null && this.seriesView.DataSource.SelectedValue != string.Empty)
            {
                DataTable data = dataStore.RunQuery("SELECT * FROM " + this.seriesView.DataSource.SelectedValue + " LIMIT 1");
                if (data != null)
                {
                    string[] fieldNames = DataTableUtilities.GetColumnNames(data);
                    Array.Sort(fieldNames);
                    this.seriesView.X.Values = fieldNames;
                    this.seriesView.Y.Values = fieldNames;
                    this.seriesView.X2.Values = fieldNames;
                    this.seriesView.Y2.Values = fieldNames;
                }
            }

        }

    }
}
