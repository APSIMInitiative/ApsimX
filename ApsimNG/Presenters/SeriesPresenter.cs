using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using UserInterface.EventArguments;
using System.Linq;
using APSIM.Shared.Utilities;
using UserInterface.Interfaces;
using Models.Core;
using Models;
using UserInterface.Views;
using UserInterface.Commands;
using Models.Storage;
using APSIM.Shared.Graphing;
using Series = Models.Series;
using Configuration = Utility.Configuration;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A presenter class for graph series.
    /// </summary>
    public class SeriesPresenter : IPresenter
    {
        /// <summary>
        /// The storage
        /// </summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>The graph model to work with.</summary>
        private Series series;

        /// <summary>The series view to work with.</summary>
        private ISeriesView seriesView;

        /// <summary>The parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>Attach the model and view to this presenter.</summary>
        /// <param name="model">The graph model to work with</param>
        /// <param name="view">The series view to work with</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            explorerPresenter.MainPresenter.ClearStatusPanel();

            this.series = model as Series;
            this.seriesView = view as SeriesView;
            this.explorerPresenter = explorerPresenter;
            intellisense = new IntellisensePresenter(seriesView as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            Graph parentGraph = series.FindAncestor<Graph>();
            if (parentGraph != null)
            {
                try
                {
                    GraphPresenter = new GraphPresenter();
                    explorerPresenter.ApsimXFile.Links.Resolve(GraphPresenter);
                    GraphPresenter.Attach(parentGraph, seriesView.GraphView, explorerPresenter);
                }
                catch (Exception err)
                {
                    explorerPresenter.MainPresenter.ShowError(err);
                }
            }

            try
            {
                PopulateView();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }

            ConnectViewEvents();
        }

        /// <summary>Detach the model and view from this presenter.</summary>
        public void Detach()
        {
            seriesView.EndEdit();
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            GraphPresenter?.Detach();
            intellisense.Cleanup();

            DisconnectViewEvents();
        }

        /// <summary>The graph presenter</summary>
        public GraphPresenter GraphPresenter;

        /// <summary>Connect all view events.</summary>
        private void ConnectViewEvents()
        {
            seriesView.DataSource.Changed += OnDataSourceChanged;
            seriesView.SeriesType.Changed += OnSeriesTypeChanged;
            seriesView.LineType.Changed += OnLineTypeChanged;
            seriesView.MarkerType.Changed += OnMarkerTypeChanged;
            seriesView.LineThickness.Changed += OnLineThicknessChanged;
            seriesView.MarkerSize.Changed += OnMarkerSizeChanged;
            seriesView.Colour.Changed += OnColourChanged;
            seriesView.XOnTop.Changed += OnXOnTopChanged;
            seriesView.YOnRight.Changed += OnYOnRightChanged;
            seriesView.X.Changed += OnXChanged;
            seriesView.Y.Changed += OnYChanged;
            seriesView.X2.Changed += OnX2Changed;
            seriesView.Y2.Changed += OnY2Changed;
            seriesView.ShowInLegend.Changed += OnShowInLegendChanged;
            seriesView.IncludeSeriesNameInLegend.Changed += OnIncludeSeriesNameInLegendChanged;
            seriesView.YCumulative.Changed += OnCumulativeYChanged;
            seriesView.XCumulative.Changed += OnCumulativeXChanged;
            seriesView.Filter.Leave += OnFilterChanged;
            seriesView.Filter.IntellisenseItemsNeeded += OnIntellisenseItemsNeeded;
        }

        /// <summary>Disconnect all view events.</summary>
        private void DisconnectViewEvents()
        {
            seriesView.DataSource.Changed -= OnDataSourceChanged;
            seriesView.SeriesType.Changed -= OnSeriesTypeChanged;
            seriesView.LineType.Changed -= OnLineTypeChanged;
            seriesView.MarkerType.Changed -= OnMarkerTypeChanged;
            seriesView.LineThickness.Changed -= OnLineThicknessChanged;
            seriesView.MarkerSize.Changed -= OnMarkerSizeChanged;
            seriesView.Colour.Changed -= OnColourChanged;
            seriesView.XOnTop.Changed -= OnXOnTopChanged;
            seriesView.YOnRight.Changed -= OnYOnRightChanged;
            seriesView.X.Changed -= OnXChanged;
            seriesView.Y.Changed -= OnYChanged;
            seriesView.X2.Changed -= OnX2Changed;
            seriesView.Y2.Changed -= OnY2Changed;
            seriesView.ShowInLegend.Changed -= OnShowInLegendChanged;
            seriesView.IncludeSeriesNameInLegend.Changed -= OnIncludeSeriesNameInLegendChanged;
            seriesView.YCumulative.Changed -= OnCumulativeYChanged;
            seriesView.XCumulative.Changed -= OnCumulativeXChanged;
            seriesView.Filter.Leave -= OnFilterChanged;
            seriesView.Filter.IntellisenseItemsNeeded -= OnIntellisenseItemsNeeded;
        }

        /// <summary>Set the value of the graph models property</summary>
        /// <param name="name">The name of the property to set</param>
        /// <param name="value">The value of the property to set it to</param>
        private void SetModelProperty(string name, object value)
        {
            try
            {
                ChangeProperty command = new ChangeProperty(series, name, value);
                explorerPresenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>Set the value of the graph models property</summary>
        /// <param name="name">The name of the property to set</param>
        /// <param name="value">The value of the property to set it to</param>
        private void SetModelPropertyInAllSeries(string name, object value)
        {
            try
            {
                foreach (var s in series.Parent.FindAllChildren<Series>())
                {
                    ChangeProperty command = new ChangeProperty(s, name, value);
                    explorerPresenter.CommandHistory.Add(command);
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense window.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                // The completion options in the series filter will typically contain the trigger word,
                // e.g. "Maize.Total.Wt". We don't want to end up with "Maize.Maize.Total.Wt".
                if (args.ItemSelected.StartsWith(args.TriggerWord))
                {
                    int index = args.ItemSelected.IndexOf(args.TriggerWord);
                    if (index >= 0)
                        args.ItemSelected = args.ItemSelected.Substring(args.TriggerWord.Length);
                }
                string textBeforeCursor = seriesView.Filter.Text.Substring(0, seriesView.Filter.Offset);
                if (textBeforeCursor.EndsWith(".") && args.ItemSelected.StartsWith("."))
                    args.ItemSelected = args.ItemSelected.TrimStart('.');

                seriesView.Filter.InsertAtCursor(args.ItemSelected);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        #region Events from the view

        /// <summary>Series type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnSeriesTypeChanged(object sender, EventArgs e)
        {
            SeriesType seriesType = (SeriesType)Enum.Parse(typeof(SeriesType), this.seriesView.SeriesType.SelectedValue);
            this.SetModelProperty("Type", seriesType);

            // This doesn't quite work yet. If the previous series was a scatter plot, there is no x2, y2 to work with
            // and things go a bit awry.
            // this function is now called in disableUnusedControls if this needs to be added back later
            // this.seriesView.ShowX2Y2(series.Type == SeriesType.Area);

            // If the series is a box plot, then we want to disable certain unused controls
            // such as x variable, marker type, etc. These also need to be
            // re-enabled if we change series type.
            DisableUnusedControls();
        }

        /// <summary>Series line type has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnLineTypeChanged(object sender, EventArgs e)
        {
            LineType lineType;
            if (Enum.TryParse<LineType>(this.seriesView.LineType.SelectedValue, out lineType))
            {
                // Have not specified a vary by.
                bool setInAllSeries = series.FactorToVaryLines == "Graph series";
                if (setInAllSeries)
                {
                    SetModelPropertyInAllSeries("Line", lineType);
                    SetModelPropertyInAllSeries("FactorToVaryLines", null);
                }
                else
                {
                    SetModelProperty("Line", lineType);
                    SetModelProperty("FactorToVaryLines", null);
                }
            }
            else
            {
                bool setInAllSeries = seriesView.LineType.SelectedValue == "Vary by Graph series";
                if (setInAllSeries)
                    SetModelPropertyInAllSeries("FactorToVaryLines", this.seriesView.LineType.SelectedValue.Replace("Vary by ", ""));
                else
                    SetModelProperty("FactorToVaryLines", this.seriesView.LineType.SelectedValue.Replace("Vary by ", ""));
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
                // Have not specified a vary by.
                bool setInAllSeries = series.FactorToVaryMarkers == "Graph series";
                if (setInAllSeries)
                {
                    SetModelPropertyInAllSeries("Marker", markerType);
                    SetModelPropertyInAllSeries("FactorToVaryMarkers", null);
                }
                else
                {
                    SetModelProperty("Marker", markerType);
                    SetModelProperty("FactorToVaryMarkers", null);
                }
            }
            else
            {
                bool setInAllSeries = seriesView.MarkerType.SelectedValue == "Vary by Graph series";
                if (setInAllSeries)
                    SetModelPropertyInAllSeries("FactorToVaryMarkers", this.seriesView.MarkerType.SelectedValue.Replace("Vary by ", ""));
                else
                    SetModelProperty("FactorToVaryMarkers", this.seriesView.MarkerType.SelectedValue.Replace("Vary by ", ""));
            }
        }

        /// <summary>Series line thickness has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnLineThicknessChanged(object sender, EventArgs e)
        {
            LineThickness lineThickness;
            if (Enum.TryParse<LineThickness>(this.seriesView.LineThickness.SelectedValue, out lineThickness))
            {
                this.SetModelProperty("LineThickness", lineThickness);
            }
        }

        /// <summary>Series marker size has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnMarkerSizeChanged(object sender, EventArgs e)
        {
            MarkerSize markerSize;
            if (Enum.TryParse<MarkerSize>(this.seriesView.MarkerSize.SelectedValue, out markerSize))
            {
                this.SetModelProperty("MarkerSize", markerSize);
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
                // Have not specified a vary by.
                bool setInAllSeries = series.FactorToVaryColours == "Graph series";
                if (setInAllSeries)
                {
                    SetModelPropertyInAllSeries("Colour", obj);
                    SetModelPropertyInAllSeries("FactorToVaryColours", null);
                }
                else
                {
                    SetModelProperty("Colour", obj);
                    SetModelProperty("FactorToVaryColours", null);
                }
            }
            else
            {
                bool setInAllSeries = obj.ToString() == "Vary by Graph series";
                if (setInAllSeries)
                    SetModelPropertyInAllSeries("FactorToVaryColours", obj.ToString().Replace("Vary by ", ""));
                else
                    SetModelProperty("FactorToVaryColours", obj.ToString().Replace("Vary by ", ""));
            }
        }

        /// <summary>X on top has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnXOnTopChanged(object sender, EventArgs e)
        {
            AxisPosition axisType = AxisPosition.Bottom;
            if (this.seriesView.XOnTop.Checked)
            {
                axisType = AxisPosition.Top;
            }

            this.SetModelProperty("XAxis", axisType);
        }

        /// <summary>Y on right has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnYOnRightChanged(object sender, EventArgs e)
        {
            AxisPosition axisType = AxisPosition.Left;
            if (this.seriesView.YOnRight.Checked)
            {
                axisType = AxisPosition.Right;
            }

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
            this.SetModelProperty("Cumulative", this.seriesView.YCumulative.Checked);
        }

        /// <summary>Cumulative X check box has been changed by the user.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCumulativeXChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("CumulativeX", this.seriesView.XCumulative.Checked);
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
                List<string> warnings = PopulateFieldNames();
                if (warnings != null && warnings.Count > 0 && Configuration.Settings.EnableGraphDebuggingMessages)
                {
                    explorerPresenter.MainPresenter.ClearStatusPanel();
                    explorerPresenter.MainPresenter.ShowMessage(warnings, Simulation.MessageType.Warning);
                }
            }
        }

        /// <summary>User has changed the show in legend</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnShowInLegendChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("ShowInLegend", this.seriesView.ShowInLegend.Checked);
        }

        /// <summary>User has changed the include series name in legend</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnIncludeSeriesNameInLegendChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("IncludeSeriesNameInLegend", this.seriesView.IncludeSeriesNameInLegend.Checked);
        }

        /// <summary>User has changed the filter</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            this.SetModelProperty("Filter", this.seriesView.Filter.Text);
        }

        /// <summary>
        /// Invoked when the user is asking for items for the intellisense.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemsNeeded(object sender, NeedContextItemsArgs args)
        {
            try
            {
                if (intellisense.GenerateSeriesCompletions(args.Code, args.Offset, seriesView.DataSource.SelectedValue, storage.Reader))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        #endregion

        /// <summary>Populate the views series editor with the current selected series.</summary>
        private void PopulateView()
        {
            List<string> warnings = new List<string>();

            warnings.AddRange(PopulateMarkerDropDown());
            warnings.AddRange(PopulateLineDropDown());
            warnings.AddRange(PopulateColourDropDown());

            // Populate line thickness drop down.
            List<string> thicknesses = new List<string>(Enum.GetNames(typeof(LineThickness)));
            if (!thicknesses.Contains(series.LineThickness.ToString()) && !string.IsNullOrEmpty(series.LineThickness.ToString()))
            {
                // This should never happen...if one of these values is ever removed, a converter should be written.
                thicknesses.Add(series.LineThickness.ToString());
                warnings.Add(string.Format("WARNING: {0}: Selected line thickness '{1}' is invalid. This could be a relic from an older version of APSIM.", series.FullPath, series.LineThickness.ToString()));
            }
            this.seriesView.LineThickness.Values = thicknesses.ToArray();
            this.seriesView.LineThickness.SelectedValue = series.LineThickness.ToString();

            // Populate marker size drop down.
            List<string> sizes = new List<string>(Enum.GetNames(typeof(MarkerSize)));
            if (!sizes.Contains(series.MarkerSize.ToString()) && !string.IsNullOrEmpty(series.MarkerSize.ToString()))
            {
                // This should never happen...if one of these values is ever removed, a converter should be written.
                sizes.Add(series.MarkerSize.ToString());
                warnings.Add(string.Format("WARNING: {0}: Selected marker size '{1}' is invalid. This could be a relic from an older version of APSIM.", series.FullPath, series.MarkerSize));
            }
            this.seriesView.MarkerSize.Values = sizes.ToArray();
            this.seriesView.MarkerSize.SelectedValue = series.MarkerSize.ToString();

            // Populate series type drop down.
            List<string> seriesTypes = new List<string>(Enum.GetNames(typeof(SeriesType)));
            if (!seriesTypes.Contains(series.Type.ToString()) && !string.IsNullOrEmpty(series.Type.ToString()))
            {
                // This should never happen...if one of these values is ever removed, a converter should be written.
                seriesTypes.Add(series.Type.ToString());
                warnings.Add(string.Format("WARNING: {0}: Selected series type '{1}' is invalid. This could be a relic from an older version of APSIM.", series.FullPath, series.Type));
            }
            this.seriesView.SeriesType.Values = seriesTypes.ToArray();
            this.seriesView.SeriesType.SelectedValue = series.Type.ToString();

            // Populate checkboxes.
            this.seriesView.XOnTop.Checked = series.XAxis == AxisPosition.Top;
            this.seriesView.YOnRight.Checked = series.YAxis == AxisPosition.Right;
            this.seriesView.ShowInLegend.Checked = series.ShowInLegend;
            this.seriesView.IncludeSeriesNameInLegend.Checked = series.IncludeSeriesNameInLegend;
            this.seriesView.XCumulative.Checked = series.CumulativeX;
            this.seriesView.YCumulative.Checked = series.Cumulative;

            // Populate data source drop down.
            List<string> dataSources = storage.Reader.TableAndViewNames.ToList();
            if (!dataSources.Contains(series.TableName) && !string.IsNullOrEmpty(series.TableName))
            {
                dataSources.Add(series.TableName);
                warnings.Add(string.Format("WARNING: {0}: Selected Data Source '{1}' does not exist in the datastore. Have the simulations been run?", series.FullPath, series.TableName));
            }
            dataSources.Sort();
            this.seriesView.DataSource.Values = dataSources.ToArray();
            this.seriesView.DataSource.SelectedValue = series.TableName;

            //Show/Hide controls that are not relevant for the current graph type
            //Needs to run before we populate the fields, so we don't try to populate
            //fields with no values
            DisableUnusedControls();

            // Populate field name drop downs.
            warnings.AddRange(PopulateFieldNames());

            // Populate filter textbox.
            this.seriesView.Filter.Text = series.Filter;

            if (warnings != null && warnings.Count > 0 && Configuration.Settings.EnableGraphDebuggingMessages)
                explorerPresenter.MainPresenter.ShowMessage(warnings, Simulation.MessageType.Warning);
        }

        private void DisableUnusedControls()
        {
            // Box plots ignore x variable, markertype, marker size,
            // so don't make these controls editable if the series is a box plot.
            bool isBoxPlot = series.Type == SeriesType.Box;
            seriesView.MarkerSize.IsSensitive = !isBoxPlot;
            seriesView.MarkerType.IsSensitive = !isBoxPlot;
            seriesView.XCumulative.IsSensitive = !isBoxPlot;
            seriesView.XOnTop.IsSensitive = !isBoxPlot;

            //show X2 and Y2 if this is a region graph, hide if not
            if (series.Type == SeriesType.Region)
            {
                this.seriesView.ShowX2Y2(true);
            } else
            {
                this.seriesView.ShowX2Y2(false);
                //null the x2 and y2 field names so they don't cause errors if a model changes
                series.X2FieldName = null;
                series.Y2FieldName = null;
            }
        }

        /// <summary>Populate the line drop down.</summary>
        private List<string> PopulateLineDropDown()
        {
            List<string> warnings = new List<string>();

            List<string> values = new List<string>(Enum.GetNames(typeof(LineType)));

            var descriptors = series.GetDescriptorNames(storage.Reader);
            if (descriptors != null)
                values.AddRange(descriptors.Select(factorName => "Vary by " + factorName));

            string selectedValue;
            if (series.FactorToVaryLines == null)
                selectedValue = series.Line.ToString();
            else
                selectedValue = "Vary by " + series.FactorToVaryLines;

            if (!values.Contains(selectedValue) && !string.IsNullOrEmpty(selectedValue))
            {
                values.Add(selectedValue);
                warnings.Add(string.Format("WARNING: {0}: Selected line type '{1}' is invalid.", series.FullPath, selectedValue));
            }
            this.seriesView.LineType.Values = values.ToArray();
            this.seriesView.LineType.SelectedValue = selectedValue;

            return warnings;
        }

        /// <summary>Populate the marker drop down.</summary>
        private List<string> PopulateMarkerDropDown()
        {
            List<string> warnings = new List<string>();

            List<string> values = new List<string>(Enum.GetNames(typeof(MarkerType)));
            var descriptors = series.GetDescriptorNames(storage.Reader);
            if (descriptors != null)
                values.AddRange(descriptors.Select(factorName => "Vary by " + factorName));

            string selectedValue;
            if (series.FactorToVaryMarkers == null)
                selectedValue = series.Marker.ToString();
            else
                selectedValue = "Vary by " + series.FactorToVaryMarkers;

            if (!values.Contains(selectedValue) && !string.IsNullOrEmpty(selectedValue))
            {
                values.Add(selectedValue);
                warnings.Add(string.Format("WARNING: {0}: Selected marker type '{1}' is invalid.", series.FullPath, selectedValue));
            }

            this.seriesView.MarkerType.Values = values.ToArray();
            this.seriesView.MarkerType.SelectedValue = selectedValue;

            return warnings;
        }

        /// <summary>Populate the colour drop down in the view.</summary>
        private List<string> PopulateColourDropDown()
        {
            List<string> warnings = new List<string>();
            List<object> colourOptions = new List<object>();
            foreach (Color colour in ColourUtilities.Colours)
                colourOptions.Add(colour);

            // Send colour options to view.
            var descriptors = series.GetDescriptorNames(storage.Reader);

            if (descriptors != null)
                colourOptions.AddRange(descriptors.Select(factorName => "Vary by " + factorName));

            object selectedValue;
            if (series.FactorToVaryColours == null)
                selectedValue = series.Colour;
            else
                selectedValue = "Vary by " + series.FactorToVaryColours;

            if (!colourOptions.Contains(selectedValue) && selectedValue != null)
            {
                colourOptions.Add(selectedValue);
                // If selectedValue is not a string, then it is probably a custom colour.
                // In such a scenario, we don't show a warning, as we can display it with no problems.
                if (selectedValue is string)
                    warnings.Add(string.Format("WARNING: {0}: Selected colour '{1}' is invalid.", series.FullPath, selectedValue));
            }

            this.seriesView.Colour.Values = colourOptions.ToArray();
            this.seriesView.Colour.SelectedValue = selectedValue;

            return warnings;
        }

        /// <summary>Gets a list of valid field names for the view.</summary>
        private List<string> GetFieldNames()
        {
            List<string> fieldNames = new List<string>();

            if (this.seriesView.DataSource != null && !string.IsNullOrEmpty(this.seriesView.DataSource.SelectedValue))
            {
                fieldNames.Add("SimulationName");
                fieldNames.AddRange(storage.Reader.ColumnNames(seriesView.DataSource.SelectedValue));
                fieldNames.Sort();
            }
            return fieldNames;
        }

        /// <summary>
        /// Populates the field names in the view, and returns a list of warnings.
        /// </summary>
        /// <returns>List of warning messages.</returns>
        private List<string> PopulateFieldNames()
        {
            List<string> fieldNames = GetFieldNames();
            List<string> warnings = new List<string>();
            this.seriesView.X.Values = fieldNames.ToArray();
            this.seriesView.Y.Values = fieldNames.ToArray();
            this.seriesView.X2.Values = fieldNames.ToArray();
            this.seriesView.Y2.Values = fieldNames.ToArray();

            if (!this.seriesView.X.Values.Contains(series.XFieldName) && !string.IsNullOrEmpty(series.XFieldName))
            {
                this.seriesView.X.Values = this.seriesView.X.Values.Concat(new string[] { series.XFieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected X field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", series.FullPath, series.XFieldName, series.TableName));
            }
            this.seriesView.X.SelectedValue = series.XFieldName;

            if (!this.seriesView.Y.Values.Contains(series.YFieldName) && !string.IsNullOrEmpty(series.YFieldName))
            {
                this.seriesView.Y.Values = this.seriesView.Y.Values.Concat(new string[] { series.YFieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected Y field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", series.FullPath, series.YFieldName, series.TableName));
            }
            this.seriesView.Y.SelectedValue = series.YFieldName;

            if (!this.seriesView.X2.Values.Contains(series.X2FieldName) && !string.IsNullOrEmpty(series.X2FieldName))
            {
                this.seriesView.X2.Values = this.seriesView.X2.Values.Concat(new string[] { series.X2FieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected X2 field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", series.FullPath, series.X2FieldName, series.TableName));
            }
            this.seriesView.X2.SelectedValue = series.X2FieldName;

            if (!this.seriesView.Y2.Values.Contains(series.Y2FieldName) && !string.IsNullOrEmpty(series.Y2FieldName))
            {
                this.seriesView.Y2.Values = this.seriesView.Y2.Values.Concat(new string[] { series.Y2FieldName }).ToArray();
                warnings.Add(string.Format("WARNING: {0}: Selected Y2 field name '{1}' does not exist in the datastore table '{2}'. Have the simulations been run?", series.FullPath, series.Y2FieldName, series.TableName));
            }
            this.seriesView.Y2.SelectedValue = series.Y2FieldName;

            return warnings;
        }
    }
}
