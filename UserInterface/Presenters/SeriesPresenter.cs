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
        private Graph graph;

        /// <summary>
        /// The series view to work with.
        /// </summary>
        /// 
        private ISeriesView seriesView;

        /// <summary>
        /// The data store where the data is located
        /// </summary>
        private DataStore dataStore;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// A flag to stop recursion through the 'PopulateSeriesEditor' method
        /// </summary>
        private bool InPopulateSeriesEditor = false;

        /// <summary>
        /// Attach the model and view to this presenter.
        /// </summary>
        /// <param name="model">The graph model to work with</param>
        /// <param name="view">The series view to work with</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.graph = model as Graph;
            this.seriesView = view as SeriesView;
            this.explorerPresenter = explorerPresenter;
            this.dataStore = graph.DataStore;

            // Populate the series names.
            PopulateSeriesNames();

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnGraphModelChanged2;
        }

        /// <summary>
        /// Detach the model and view from this presenter.
        /// </summary>
        public void Detach()
        {
            DisconnectViewEvents();
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnGraphModelChanged2;
        }

        /// <summary>
        /// Connect all view events.
        /// </summary>
        private void ConnectViewEvents()
        {
            this.seriesView.SeriesSelected += OnSeriesSelected;
            this.seriesView.SeriesAdded += OnSeriesAdded;
            this.seriesView.SeriesDeleted += OnSeriesDeleted;
            this.seriesView.AllSeriesCleared += OnSeriesCleared;
            this.seriesView.SeriesRenamed += SeriesRenamed;
            this.seriesView.SeriesEditor.DataSourceChanged += OnDataSourceChanged;
            this.seriesView.SeriesEditor.SeriesTypeChanged += OnSeriesTypeChanged;
            this.seriesView.SeriesEditor.SeriesLineTypeChanged += OnSeriesLineTypeChanged;
            this.seriesView.SeriesEditor.SeriesMarkerTypeChanged += OnSeriesMarkerTypeChanged;
            this.seriesView.SeriesEditor.ColourChanged += OnColourChanged;
            this.seriesView.SeriesEditor.OverallRegressionChanged += OnOverallRegressionChanged;
            this.seriesView.SeriesEditor.RegressionChanged += OnRegressionChanged;
            this.seriesView.SeriesEditor.XOnTopChanged += OnXOnTopChanged;
            this.seriesView.SeriesEditor.YOnRightChanged += OnYOnRightChanged;
            this.seriesView.SeriesEditor.XChanged += OnXChanged;
            this.seriesView.SeriesEditor.YChanged += OnYChanged;
            this.seriesView.SeriesEditor.X2Changed += OnX2Changed;
            this.seriesView.SeriesEditor.Y2Changed += OnY2Changed;
            this.seriesView.SeriesEditor.ShowInLegendChanged += OnShowInLegendChanged;
            this.seriesView.SeriesEditor.CumulativeChanged += OnCumulativeChanged;
            this.seriesView.SeriesEditor.IncludeInDocumentationChanged += OnIncludeInDocumentationChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
            this.seriesView.SeriesSelected -= OnSeriesSelected;
            this.seriesView.SeriesAdded -= OnSeriesAdded;
            this.seriesView.SeriesDeleted -= OnSeriesDeleted;
            this.seriesView.AllSeriesCleared -= OnSeriesCleared;
            this.seriesView.SeriesRenamed -= SeriesRenamed;

            this.seriesView.SeriesEditor.DataSourceChanged -= OnDataSourceChanged;
            this.seriesView.SeriesEditor.SeriesTypeChanged -= OnSeriesTypeChanged;
            this.seriesView.SeriesEditor.SeriesLineTypeChanged -= OnSeriesLineTypeChanged;
            this.seriesView.SeriesEditor.SeriesMarkerTypeChanged -= OnSeriesMarkerTypeChanged;
            this.seriesView.SeriesEditor.ColourChanged -= OnColourChanged;
            this.seriesView.SeriesEditor.OverallRegressionChanged -= OnOverallRegressionChanged;
            this.seriesView.SeriesEditor.RegressionChanged -= OnRegressionChanged;
            this.seriesView.SeriesEditor.XOnTopChanged -= OnXOnTopChanged;
            this.seriesView.SeriesEditor.YOnRightChanged -= OnYOnRightChanged;
            this.seriesView.SeriesEditor.XChanged -= OnXChanged;
            this.seriesView.SeriesEditor.YChanged -= OnYChanged;
            this.seriesView.SeriesEditor.X2Changed -= OnX2Changed;
            this.seriesView.SeriesEditor.Y2Changed -= OnY2Changed;
            this.seriesView.SeriesEditor.ShowInLegendChanged -= OnShowInLegendChanged;
            this.seriesView.SeriesEditor.CumulativeChanged -= OnCumulativeChanged;
            this.seriesView.SeriesEditor.IncludeInDocumentationChanged -= OnIncludeInDocumentationChanged;
        }

        /// <summary>
        /// User has selected a series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesSelected(object sender, EventArgs e)
        {
            PopulateSeriesEditor();
        }

        /// <summary>
        /// User has added a series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesAdded(object sender, EventArgs e)
        {
            Series seriesToAdd;

            int seriesIndex = Array.IndexOf(this.seriesView.SeriesNames, this.seriesView.SelectedSeriesName);
            if (seriesIndex != -1)
            {
                seriesToAdd = ReflectionUtilities.Clone(this.graph.Series[seriesIndex]) as Series;
            }
            else
            {
                seriesToAdd = new Series();
                seriesToAdd.XAxis = Axis.AxisType.Bottom;
            }

            List<Series> allSeries = new List<Series>();
            allSeries.AddRange(graph.Series);
            allSeries.Add(seriesToAdd);
            seriesToAdd.Title = "Series" + allSeries.Count.ToString();
            Commands.ChangeProperty command = new Commands.ChangeProperty(this.graph, "Series", allSeries);
            this.explorerPresenter.CommandHistory.Add(command);
            this.PopulateSeriesNames();
            this.seriesView.SelectedSeriesName = seriesToAdd.Title;
        }

        /// <summary>
        /// User has deleted a series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesDeleted(object sender, EventArgs e)
        {
            int seriesIndex = Array.IndexOf(this.seriesView.SeriesNames, this.seriesView.SelectedSeriesName);
            if (seriesIndex != -1)
            {
                List<Series> allSeries = new List<Series>();
                allSeries.AddRange(graph.Series);
                allSeries.RemoveAt(seriesIndex);
                Commands.ChangeProperty command = new Commands.ChangeProperty(this.graph, "Series", allSeries);
                explorerPresenter.CommandHistory.Add(command);
                PopulateSeriesNames();
            }
        }  
  
        /// <summary>
        /// User has cleared all series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesCleared(object sender, EventArgs e)
        {
            Commands.ChangeProperty command = new Commands.ChangeProperty(this.graph, "Series", new List<Series>());
            explorerPresenter.CommandHistory.Add(command);
            PopulateSeriesNames();
        }

        /// <summary>
        /// User has renamed a series.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void SeriesRenamed(object sender, EventArgs e)
        {
            string[] seriesNames = this.seriesView.SeriesNames;
            for (int i = 0; i < seriesNames.Length; i++)
            {
                if (this.graph.Series[i].Title != seriesNames[i])
                {
                    this.explorerPresenter.CommandHistory.ModelChanged -= this.OnGraphModelChanged2;
                    Commands.ChangeProperty command = new Commands.ChangeProperty(this.graph.Series[i], "Title", seriesNames[i]);
                    explorerPresenter.CommandHistory.Add(command);
                    this.explorerPresenter.CommandHistory.ModelChanged += this.OnGraphModelChanged2;
                }
            }
        }  

        /// <summary>
        /// Set the value of the graph models property
        /// </summary>
        /// <param name="name">The name of the property to set</param>
        /// <param name="value">The value of the property to set it to</param>
        private void SetModelProperty(string name, object value)
        {
            Series series = GetCurrentSeries();
            Commands.ChangeProperty command = new Commands.ChangeProperty(series, name, value);
            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// Populate the view with series names.
        /// </summary>
        private void PopulateSeriesNames()
        {
            List<string> names = new List<string>();
            int counter = 0;
            foreach (Series series in this.graph.Series)
            {
                if (series.Title == null || series.Title == string.Empty)
                {
                    names.Add("Series " + counter.ToString());
                }
                else
                {
                    names.Add(series.Title);
                }

                counter++;
            }
            this.seriesView.SeriesNames = names.ToArray();
            PopulateSeriesEditor();
        }

        #region Events from the view

        /// <summary>
        /// Series type has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        void OnSeriesTypeChanged(object sender, EventArgs e)
        {
            Series.SeriesType seriesType = (Series.SeriesType)Enum.Parse(typeof(Series.SeriesType), this.seriesView.SeriesEditor.SeriesType);
            this.SetModelProperty("Type", seriesType);
        }

        /// <summary>
        /// Series line type has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesLineTypeChanged(object sender, EventArgs e)
        {
            Series.LineType lineType = (Series.LineType)Enum.Parse(typeof(Series.LineType), this.seriesView.SeriesEditor.SeriesLineType);
            this.SetModelProperty("Line", lineType);
        }
        
        /// <summary>
        /// Series marker type has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesMarkerTypeChanged(object sender, EventArgs e)
        {
            Series.MarkerType markerType = (Series.MarkerType)Enum.Parse(typeof(Series.MarkerType), this.seriesView.SeriesEditor.SeriesMarkerType);
            this.SetModelProperty("Marker", markerType);
        }

        /// <summary>
        /// Series color has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnColourChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Colour", this.seriesView.SeriesEditor.Colour);
        }

        /// <summary>
        /// Overall regression checkbox has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnOverallRegressionChanged(object sender, EventArgs e)
        {
            Commands.ChangeProperty command = new Commands.ChangeProperty(this.graph, "ShowRegressionLine", this.seriesView.SeriesEditor.OverallRegression);
            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// Regression checkbox has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnRegressionChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("ShowRegressionLine", this.seriesView.SeriesEditor.Regression);
        }

        /// <summary>
        /// X on top has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXOnTopChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Bottom;
            if (this.seriesView.SeriesEditor.XOnTop)
            {
                axisType = Axis.AxisType.Top;
            }
            this.SetModelProperty("XAxis", axisType);
            EnsureAllAxesExistInGraph();
        }

        /// <summary>
        /// Y on right has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYOnRightChanged(object sender, EventArgs e)
        {
            Axis.AxisType axisType = Axis.AxisType.Left;
            if (this.seriesView.SeriesEditor.YOnRight)
            {
                axisType = Axis.AxisType.Right;
            }
            this.SetModelProperty("YAxis", axisType);
            EnsureAllAxesExistInGraph();
        }

        /// <summary>
        /// X has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXChanged(object sender, EventArgs e)
        {
            GraphValues graphValues = new GraphValues();
            graphValues.TableName = this.seriesView.SeriesEditor.DataSource;
            graphValues.FieldName = this.seriesView.SeriesEditor.X;
            this.SetModelProperty("X", graphValues);
        }

        /// <summary>
        /// Y has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYChanged(object sender, EventArgs e)
        {
            GraphValues graphValues = new GraphValues();
            graphValues.TableName = this.seriesView.SeriesEditor.DataSource;
            graphValues.FieldName = this.seriesView.SeriesEditor.Y;
            this.SetModelProperty("Y", graphValues);
        }

        /// <summary>
        /// Cumulative check box has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Cumulative", this.seriesView.SeriesEditor.Cumulative);
        }

        /// <summary>
        /// X2 has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnX2Changed(object sender, EventArgs e)
        {
            GraphValues graphValues = new GraphValues();
            graphValues.TableName = this.seriesView.SeriesEditor.DataSource;
            graphValues.FieldName = this.seriesView.SeriesEditor.X2;
            this.SetModelProperty("X2", graphValues);
        }

        /// <summary>
        /// Y2 has been changed by the user.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnY2Changed(object sender, EventArgs e)
        {
            GraphValues graphValues = new GraphValues();
            graphValues.TableName = this.seriesView.SeriesEditor.DataSource;
            graphValues.FieldName = this.seriesView.SeriesEditor.Y2;
            this.SetModelProperty("Y2", graphValues);
        }

        /// <summary>
        /// User has changed the data source.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDataSourceChanged(object sender, EventArgs e)
        {
            if (this.dataStore != null)
            {
                Series series = this.GetCurrentSeries();
                if (series.X != null && series.X.TableName != this.seriesView.SeriesEditor.DataSource)
                {
                    OnXChanged(sender, e);
                }
                if (series.Y != null && series.Y.TableName != this.seriesView.SeriesEditor.DataSource)
                {
                    OnYChanged(sender, e);
                }
                if (series.X2 != null && series.X2.TableName != this.seriesView.SeriesEditor.DataSource)
                {
                    OnX2Changed(sender, e);
                }
                if (series.Y2 != null && series.Y2.TableName != this.seriesView.SeriesEditor.DataSource)
                {
                    OnY2Changed(sender, e);
                }
            }
        }

        /// <summary>
        /// User has changed the show in legend
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnShowInLegendChanged(object sender, EventArgs e)
        {
            if (this.dataStore != null)
            {
                this.SetModelProperty("ShowInLegend", this.seriesView.SeriesEditor.ShowInLegend);
            }
        }

        /// <summary>
        /// User has changed the show in legend
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnIncludeInDocumentationChanged(object sender, EventArgs e)
        {
            Commands.ChangeProperty command = new Commands.ChangeProperty(graph, "IncludeInDocumentation", this.seriesView.SeriesEditor.IncludeInDocumentation);
            this.explorerPresenter.CommandHistory.Add(command);
        }
        

        #endregion

        /// <summary>
        /// Gets the current selected series.
        /// </summary>
        /// <returns>Returns the currently selected series or throws if none selected</returns>
        private Series GetCurrentSeries()
        {
            int seriesIndex = Array.IndexOf(this.seriesView.SeriesNames, this.seriesView.SelectedSeriesName);
            if (seriesIndex != -1)
            {
                return this.graph.Series[seriesIndex];
            }
            throw new Exception("No series currently selected");
        }

        /// <summary>
        /// Populate the views series editor with the current selected series.
        /// </summary>
        private void PopulateSeriesEditor()
        {
            int seriesIndex = Array.IndexOf(this.seriesView.SeriesNames, this.seriesView.SelectedSeriesName);
            if (seriesIndex != -1 && seriesIndex < this.graph.Series.Count)
            {
                if (!InPopulateSeriesEditor)
                {
                    InPopulateSeriesEditor = true;
                    DisconnectViewEvents();

                    Series series = this.graph.Series[seriesIndex];

                    series.Title = this.seriesView.SeriesNames[seriesIndex];

                    this.seriesView.EditorVisible = true;

                    this.seriesView.SeriesEditor.OverallRegression = this.graph.ShowRegressionLine;

                    if (series.Type == Series.SeriesType.Line)
                        series.Type = Series.SeriesType.Scatter;

                    this.seriesView.SeriesEditor.SeriesType = series.Type.ToString();
                    this.seriesView.SeriesEditor.SeriesLineType = series.Line.ToString();
                    this.seriesView.SeriesEditor.SeriesMarkerType = series.Marker.ToString();
                    this.seriesView.SeriesEditor.Colour = series.Colour;
                    this.seriesView.SeriesEditor.Regression = series.ShowRegressionLine;
                    this.seriesView.SeriesEditor.XOnTop = series.XAxis == Axis.AxisType.Top;
                    this.seriesView.SeriesEditor.YOnRight = series.YAxis == Axis.AxisType.Right;
                    this.seriesView.SeriesEditor.ShowInLegend = series.ShowInLegend;
                    this.seriesView.SeriesEditor.Cumulative = series.Cumulative;
                    this.seriesView.SeriesEditor.IncludeInDocumentation = graph.IncludeInDocumentation;

                    // Populate the editor with a list of data sources.
                    List<string> dataSources = new List<string>();
                    if (dataStore != null)
                    {
                        foreach (string tableName in dataStore.TableNames)
                        {
                            if (tableName != "Messages" && tableName != "InitialConditions")
                                dataSources.Add(tableName);
                        }
                        dataSources.Sort();
                    }

                    this.seriesView.SeriesEditor.SetDataSources(dataSources.ToArray());

                    if (series.X != null)
                    {
                        this.seriesView.SeriesEditor.DataSource = series.X.TableName;
                    }
                    else if (dataSources.Count > 0)
                    {
                        this.seriesView.SeriesEditor.DataSource = dataSources[0];
                    }

                    if (this.seriesView.SeriesEditor.DataSource != null)
                    {
                        DataTable data = dataStore.GetData("*", this.seriesView.SeriesEditor.DataSource);
                        if (data != null)
                        {
                            this.seriesView.SeriesEditor.SetFieldNames(DataTableUtilities.GetColumnNames(data));
                        }
                    }

                    if (series.X != null)
                    {
                        this.seriesView.SeriesEditor.X = series.X.FieldName;
                    }
                    else
                    {
                        this.seriesView.SeriesEditor.X = null;
                    }

                    if (series.Y != null)
                    {
                        this.seriesView.SeriesEditor.Y = series.Y.FieldName;
                    }
                    else
                    {
                        this.seriesView.SeriesEditor.Y = null;
                    }

                    if (series.X2 != null)
                    {
                        this.seriesView.SeriesEditor.X2 = series.X2.FieldName;
                    }
                    else
                    {
                        this.seriesView.SeriesEditor.X2 = null;
                    }

                    if (series.Y2 != null)
                    {
                        this.seriesView.SeriesEditor.Y2 = series.Y2.FieldName;
                    }
                    else
                    {
                        this.seriesView.SeriesEditor.Y2 = null;
                    }

                    this.seriesView.SeriesEditor.ShowX2Y2(series.Type == Series.SeriesType.Area);


                    ConnectViewEvents();

                    InPopulateSeriesEditor = false;
                }
            }
            else
            {
                this.seriesView.EditorVisible = false;
            }
        }

        /// <summary>
        /// The graph model has changed - update the view.
        /// </summary>
        private void OnGraphModelChanged2(object G)
        {
            PopulateSeriesEditor();
        }

        /// <summary>
        /// Return a list of axis objects such that every series in AllSeries has an associated axis object.
        /// </summary>
        private void EnsureAllAxesExistInGraph()
        {
            // Get a list of all axis types that are referenced by the series.
            List<Models.Graph.Axis.AxisType> allAxisTypes = new List<Models.Graph.Axis.AxisType>();
            foreach (Series series in graph.Series)
            {
                allAxisTypes.Add(series.XAxis);
                allAxisTypes.Add(series.YAxis);
            }

            // Go through all graph axis objects. For each, check to see if it is still needed and
            // if so copy to our list.
            List<Axis> allAxes = new List<Axis>();
            bool unNeededAxisFound = false;
            foreach (Axis axis in this.graph.Axes)
            {
                if (allAxisTypes.Contains(axis.Type))
                    allAxes.Add(axis);
                else
                    unNeededAxisFound = true;
            }

            // Go through all series and make sure an axis object is present in our AllAxes list. If
            // not then go create an axis object.
            bool axisWasAdded = false;
            foreach (Series S in this.graph.Series)
            {
                if (!FindAxis(allAxes, S.XAxis))
                {
                    allAxes.Add(new Axis() { Type = S.XAxis });
                    axisWasAdded = true;
                }
                if (!FindAxis(allAxes, S.YAxis))
                {
                    allAxes.Add(new Axis() { Type = S.YAxis });
                    axisWasAdded = true;
                }
            }

            if (unNeededAxisFound || axisWasAdded)
            {
                Commands.ChangeProperty command = new Commands.ChangeProperty(graph, "Axes", allAxes);
                this.explorerPresenter.CommandHistory.Add(command);
            }
        }

        /// <summary>
        /// Go through the AllAxes list and return true if the specified AxisType is found in the list.
        /// </summary>
        private static bool FindAxis(List<Axis> AllAxes, Axis.AxisType AxisTypeToFind)
        {
            foreach (Axis A in AllAxes)
                if (A.Type == AxisTypeToFind)
                    return true;
            return false;
        }
    }
}
