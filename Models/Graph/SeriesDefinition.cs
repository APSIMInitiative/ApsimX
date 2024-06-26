using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core.Run;
using Models.Storage;

namespace Models
{

    /// <summary>
    /// A class for defining a graph series. A list of these is given to graph when graph is drawing itself.
    /// </summary>
    [Serializable]
    public class SeriesDefinition
    {
        /// <summary>Base series where most properties come from.</summary>
        public Series Series { get; set; }

        /// <summary>Series definition filter.</summary>
        public IEnumerable<string> InScopeSimulationNames { get; set; }

        /// <summary>User specified filter.</summary>
        private string userFilter;

        /// <summary>The name of the checkpoint to show.</summary>
        public string CheckpointName { get; private set; }

        /// <summary>Colour brightness modifier for the series definition in range [-1, 1].</summary>
        public double ColourModifier { get; private set; }

        /// <summary>
        /// Marker size modifier for the series definition in range [0, 1].
        /// Larger value means smaller markers.
        /// </summary>
        public double MarkerModifier { get; private set; }

        /// <summary>Constructor</summary>
        /// <param name="series">The series instance to initialise from.</param>
        /// <param name="checkpoint">The checkpoint name.</param>
        /// <param name="colModifier">The brightness modifier for colour in range  [-1, 1]. Negative means darker.</param>
        /// <param name="markerModifier">Marker size modifier in range [0, 1]. Larger value means smaller markers.</param>
        /// <param name="inScopeSimulationNames">A list of in scope simulation names.</param>
        /// <param name="filter">User specified filter.</param>
        /// <param name="descriptors">The descriptors for this series definition.</param>
        /// <param name="customTitle">The title to use for the definition.</param>
        public SeriesDefinition(Series series,
                                string checkpoint,
                                double colModifier,
                                double markerModifier,
                                IEnumerable<string> inScopeSimulationNames = null,
                                string filter = null,
                                List<SimulationDescription.Descriptor> descriptors = null,
                                string customTitle = null)
        {
            this.Series = series;
            CheckpointName = checkpoint;
            ColourModifier = colModifier;
            MarkerModifier = markerModifier;
            Colour = series.Colour;
            Line = series.Line;
            Marker = series.Marker;
            Descriptors = descriptors;
            ShowInLegend = series.ShowInLegend;
            Type = series.Type;
            XAxis = series.XAxis;
            YAxis = series.YAxis;
            XFieldName = series.XFieldName;
            YFieldName = series.YFieldName;
            X2FieldName = series.X2FieldName;
            Y2FieldName = series.Y2FieldName;

            if (customTitle == null)
            {
                if (descriptors == null || descriptors.Count == 0)
                    Title = series.Name;
                else
                {
                    // Determine the title of the series.
                    string title = null;
                    descriptors.ForEach(f => title += f.Value);
                    if (series.IncludeSeriesNameInLegend || title == "?")
                        title += ": " + series.Name;
                    Title = title;
                }
            }
            else
                Title = customTitle;


            if (CheckpointName != "Current")
                Title += " (" + CheckpointName + ")";

            this.InScopeSimulationNames = inScopeSimulationNames;
            userFilter = filter;
        }

        /// <summary>Constructor</summary>
        /// <param name="title">The series title.</param>
        /// <param name="colour">The series colour.</param>
        /// <param name="line">The series line type.</param>
        /// <param name="marker">The series marker type.</param>
        /// <param name="showInLegend">Show series in legend?</param>
        /// <param name="type">The series type.</param>
        /// <param name="xAxis">The location of the x axis.</param>
        /// <param name="yAxis">The location of the y axis.</param>
        /// <param name="x">X data points.</param>
        /// <param name="y">Y data points.</param>
        public SeriesDefinition(string title,
                                Color colour,
                                double[] x, double[] y,
                                LineType line = LineType.Solid,
                                MarkerType marker = MarkerType.None,
                                bool showInLegend = true,
                                SeriesType type = SeriesType.Scatter,
                                AxisPosition xAxis = AxisPosition.Bottom,
                                AxisPosition yAxis = AxisPosition.Left)
        {
            this.Title = title;
            this.Colour = colour;
            this.Line = line;
            this.Marker = marker;
            this.ShowInLegend = showInLegend;
            this.Type = type;
            this.XAxis = xAxis;
            this.YAxis = yAxis;
            this.X = x;
            this.Y = y;
        }

        /// <summary>Descriptors associate with this definition.</summary>
        public List<SimulationDescription.Descriptor> Descriptors { get; }

        /// <summary>Gets the colour.</summary>
        public Color Colour { get; set; }

        /// <summary>Gets the marker to show.</summary>
        public MarkerType Marker { get; set; }

        /// <summary>Gets the line type to show.</summary>
        public LineType Line { get; set; }

        /// <summary>Gets the series type.</summary>
        public SeriesType Type { get; }

        /// <summary>Gets the marker size.</summary>
        public MarkerSize MarkerSize
        {
            get
            {
                if (Series == null) // Can be null for regression lines or 1:1 lines
                    return MarkerSize.Normal;

                return Series.MarkerSize;
            }
        }

        /// <summary>Gets the line thickness.</summary>
        public LineThickness LineThickness
        {
            get
            {
                if (Series == null) // Can be null for regression lines or 1:1 lines
                    return LineThickness.Normal;
                else
                    return Series.LineThickness;
            }
        }

        /// <summary>Gets the associated x axis.</summary>
        public AxisPosition XAxis { get; }

        /// <summary>Gets the associated y axis.</summary>
        public AxisPosition YAxis { get; }

        /// <summary>Gets the x field name.</summary>
        public string XFieldName { get; }

        /// <summary>Gets the y field name.</summary>
        public string YFieldName { get; }

        /// <summary>Gets the x field name.</summary>
        public string X2FieldName { get; }

        /// <summary>Gets the y field name.</summary>
        public string Y2FieldName { get; }

        /// <summary>Units of measurement for X.</summary>
        public string XFieldUnits { get; private set; }

        /// <summary>Units of measurement for Y.</summary>
        public string YFieldUnits { get; private set; }

        /// <summary>Gets a value indicating whether this series should be shown in the level.</summary>
        public bool ShowInLegend { get; }

        /// <summary>Gets the title of the series</summary>
        public string Title { get; }

        /// <summary>Gets the dataview</summary>
        public DataView View { get; private set; }

        /// <summary>Gets the x values</summary>
        public IEnumerable X { get; private set; }

        /// <summary>Gets the y values</summary>
        public IEnumerable Y { get; private set; }

        /// <summary>Gets the x2 values</summary>
        public IEnumerable X2 { get; private set; }

        /// <summary>Gets the y2 values</summary>
        public IEnumerable Y2 { get; private set; }

        /// <summary>The simulation names for each point.</summary>
        public IEnumerable<string> SimulationNamesForEachPoint { get; private set; }

        /// <summary>Gets the error values for the x series</summary>
        public IEnumerable<double> XError { get; private set; }

        /// <summary>Gets the error values for the y series</summary>
        public IEnumerable<double> YError { get; private set; }

        /// <summary>Add a clause to the filter.</summary>
        /// <param name="filter">The filter to add to.</param>
        /// <param name="filterClause">The clause to add e.g. Exp = 'Exp1'.</param>
        private string AddToFilter(string filter, string filterClause)
        {
            if (filterClause != null)
            {
                if (filter == null)
                    return filterClause;
                else
                    return filter + " AND " + filterClause;
            }
            return filter;
        }

        /// <summary>A static setter function for colour from an index.</summary>
        /// <param name="definition">The series definition to change.</param>
        /// <param name="index">The colour index into the colour palette.</param>
        public static void SetColour(SeriesDefinition definition, int index)
        {
            definition.Colour = ColourUtilities.ChangeColorBrightness(ColourUtilities.Colours[index], definition.ColourModifier);
        }

        /// <summary>A static setter function for line type from an index</summary>
        /// <param name="definition">The series definition to change.</param>
        /// <param name="index">The index</param>
        public static void SetLineType(SeriesDefinition definition, int index)
        {
            definition.Line = (LineType)Enum.GetValues(typeof(LineType)).GetValue(index);
        }

        /// <summary>A static setter function for marker from an index</summary>
        /// <param name="definition">The series definition to change.</param>
        /// <param name="index">The index</param>
        public static void SetMarker(SeriesDefinition definition, int index)
        {
            definition.Marker = (MarkerType)Enum.GetValues(typeof(MarkerType)).GetValue(index);
        }

        /// <summary>Reads all data from the specified reader.</summary>
        /// <param name="data">Data to read from.</param>
        /// <param name="simulationDescriptions">Complete list of simulation descriptions.</param>
        /// <param name="reader">Data store reader.</param>
        public void ReadData(DataTable data, List<SimulationDescription> simulationDescriptions, IStorageReader reader)
        {
            if (X != null && Y != null)
                return;

            if (Series.TableName == null)
                GetDataFromModels();
            else
            {
                List<string> columnNames = DataTableUtilities.GetColumnNames(data).ToList();
                List<string> simulationNames = new List<string>();

                if (Descriptors != null)
                {
                    //Get the name of each sim that has a matching descriptor to this graph
                    foreach (SimulationDescription sim in simulationDescriptions) {
                        bool matched = true;
                        foreach (SimulationDescription.Descriptor descriptor in Descriptors)
                        {
                            if (!sim.HasDescriptor(descriptor))
                            {
                                matched = false;
                            }
                            else 
                            {
                                //Remove this descriptor from column name so that it isn't used to filter again
                                if (descriptor.Name.CompareTo("Zone") != 0) 
                                    columnNames.Remove(descriptor.Name); 
                            }
                        }
                        if (matched) {
                            simulationNames.Add(sim.Name);
                        }
                    }
                }
                //if we don't have descriptors, get all sim names in scope instead
                else if (InScopeSimulationNames != null)
                    simulationNames = new List<string>(InScopeSimulationNames ?? Enumerable.Empty<string>());

                //Make a filter on matching columns that were sim descriptors (factors)
                string filter = GetFilter(columnNames);

                //Add our matching sim ids to the filter
                if (simulationNames.Any())
                {
                    var simulationIds = reader.ToSimulationIDs(simulationNames);
                    var simulationIdsCSV = StringUtilities.Build(simulationIds, ",");
                    if (string.IsNullOrEmpty(simulationIdsCSV))
                        return;
                    if (columnNames.Contains("SimulationID"))
                        filter = AddToFilter(filter, $"SimulationID in ({simulationIdsCSV})");
                }

                //cleanup filter
                filter = filter?.Replace('\"', '\'');
                filter = RemoveMiddleWildcards(filter);

                //apply our filter to the data
                View = new DataView(data);
                try
                {
                    View.RowFilter = filter;
                }
                catch (Exception ex)
                {
                    throw new Exception("Filter cannot be parsed: " + ex.Message);
                }

                // Get the units for our x and y variables.
                XFieldUnits = reader.Units(Series.TableName, XFieldName);
                YFieldUnits = reader.Units(Series.TableName, YFieldName);

                // If data was found, populate our data (e.g. X and Y) properties.
                if (View?.Count > 0)
                {
                    X = GetDataFromView(View, XFieldName);
                    Y = GetDataFromView(View, YFieldName);
                    X2 = GetDataFromView(View, X2FieldName);
                    Y2 = GetDataFromView(View, Y2FieldName);
                    XError = GetErrorDataFromView(View, XFieldName);
                    YError = GetErrorDataFromView(View, YFieldName);
                    if (Series.Cumulative)
                        Y = MathUtilities.Cumulative(Y as IEnumerable<double>);
                    if (Series.CumulativeX)
                        X = MathUtilities.Cumulative(X as IEnumerable<double>);
                }
            }
        }

        /// <summary>
        /// Return a list of field names that this definition will read from the data table.
        /// </summary>
        /// <param name="fieldsThatExist"></param>
        /// <returns></returns>
        public List<string> GetFieldNames(List<string> fieldsThatExist)
        {
            var filter = GetFilter(fieldsThatExist);
            var fieldsToRead = new List<string>();
            fieldsToRead.Add(XFieldName);
            fieldsToRead.Add(YFieldName);
            if (!string.IsNullOrEmpty(X2FieldName))
                fieldsToRead.Add(X2FieldName);
            if (!string.IsNullOrEmpty(Y2FieldName))
                fieldsToRead.Add(Y2FieldName);
            var fieldsToAdd = new List<string>();
            foreach (var fieldName in fieldsToRead)
            {
                if (fieldsThatExist.Contains(fieldName + "Error"))
                    fieldsToAdd.Add(fieldName + "Error");
            }
            fieldsToRead.AddRange(fieldsToAdd);

            // Add any field names from the filter.
            fieldsToRead.AddRange(ExtractFieldNamesFromFilter(filter));

            // Add any fields from child graphable models.
            foreach (IGraphable series in Series.FindAllChildren<IGraphable>())
                fieldsToRead.AddRange(series.GetExtraFieldsToRead(this));
            return fieldsToRead;
        }

        /// <summary>
        /// Get the filter to use for filtering the data.
        /// </summary>
        /// <param name="fieldsThatExist"></param>
        /// <returns></returns>
        public string GetFilter(IEnumerable<string> fieldsThatExist)
        {
            string filter = null;
            if (Descriptors != null)
            {
                foreach (var descriptor in Descriptors)
                {
                    if (fieldsThatExist.Contains(descriptor.Name))
                    {
                        if (string.IsNullOrEmpty(descriptor.Value))
                            filter = AddToFilter(filter, $"[{descriptor.Name}] IS NULL");
                        else
                            filter = AddToFilter(filter, $"[{descriptor.Name}] = '{descriptor.Value}'");
                    }
                        
                }
            }
            if (!string.IsNullOrEmpty(userFilter))
                filter = AddToFilter(filter, userFilter);
            return filter;
        }

        /// <summary>
        /// Get all data from models.
        /// </summary>
        public void GetDataFromModels()
        {
            if (!String.IsNullOrEmpty(XFieldName))
                X = GetDataFromModels(XFieldName);
            if (!String.IsNullOrEmpty(YFieldName))
                Y = GetDataFromModels(YFieldName);
            if (!String.IsNullOrEmpty(X2FieldName))
                X2 = GetDataFromModels(X2FieldName);
            if (!String.IsNullOrEmpty(Y2FieldName))
                Y2 = GetDataFromModels(Y2FieldName);
        }

        /// <summary>Extract and return a list of field names from the filter.</summary>
        /// <param name="filter">Filter to extract field names from.</param>
        /// <returns>The field names or an empty list. Never null.</returns>
        private List<string> ExtractFieldNamesFromFilter(string filter)
        {
            var fieldNames = new List<string>();

            if (filter != null)
            {
                var localFilter = filter;

                // Look for XXX in ('asdf', 'qwer').
                string inPattern = @"(^|\s+)(?<FieldName>\S+)\s+IN\s+\(.+\)";
                Match match = Regex.Match(localFilter, inPattern);
                while (match.Success)
                {
                    if (match.Groups["FieldName"].Value != null)
                    {
                        fieldNames.Add(match.Groups["FieldName"].Value);
                        localFilter = localFilter.Remove(match.Index, match.Length);
                    }
                    match = Regex.Match(localFilter, inPattern);
                }

                // Remove brackets.
                localFilter = localFilter.Replace("(", "");
                localFilter = localFilter.Replace(")", "");
                localFilter = localFilter.Replace("[", "");
                localFilter = localFilter.Replace("]", "");

                // Look for individual filter clauses (e.g. A = B).
                string clausePattern = @"\[?(?<FieldName>[^\s\]]+)\]?\s*(=|>|<|>=|<=)\s*(|'|\[|\w)";
                match = Regex.Match(localFilter, clausePattern);
                while (match.Success)
                {
                    if (!string.IsNullOrWhiteSpace(match.Groups["FieldName"].Value))
                    {
                        fieldNames.Add(match.Groups["FieldName"].Value);
                        localFilter = localFilter.Remove(match.Index, match.Length);
                    }
                    match = Regex.Match(localFilter, clausePattern);
                }

                // Look for LIKE keyword
                string likePattern = @"(?<FieldName>\S+)\s+LIKE";
                match = Regex.Match(localFilter, likePattern);
                while (match.Success)
                {
                    if (!string.IsNullOrWhiteSpace(match.Groups["FieldName"].Value))
                    {
                        fieldNames.Add(match.Groups["FieldName"].Value);
                        localFilter = localFilter.Remove(match.Index, match.Length);
                    }
                    match = Regex.Match(localFilter, clausePattern);
                }
            }

            return fieldNames;
        }


        /// <summary>Return data using reflection</summary>
        /// <param name="fieldName">The field name to get data for.</param>
        /// <returns>The return data or null if not found</returns>
        private IEnumerable GetDataFromModels(string fieldName)
        {
            return Series.FindByPath(fieldName)?.Value as IEnumerable;
        }

        /// <summary>Gets a column of data from a view.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable GetDataFromView(DataView data, string fieldName)
        {
            if (fieldName != null && data != null && data.Table.Columns.Contains(fieldName))
            {
                if (data.Table.Columns[fieldName].DataType == typeof(DateTime))
                    return DataTableUtilities.GetColumnAsDates(data, fieldName);
                else if (data.Table.Columns[fieldName].DataType == typeof(string))
                    return DataTableUtilities.GetColumnAsStrings(data, fieldName);
                else if (data.Table.Columns[fieldName].DataType == typeof(int))
                    return DataTableUtilities.GetColumnAsIntegers(data, fieldName);
                else
                    return DataTableUtilities.GetColumnAsDoubles(data, fieldName);
            }
            return null;
        }

        /// <summary>Gets a column of error data from a view.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable<double> GetErrorDataFromView(DataView data, string fieldName)
        {
            string errorFieldName = fieldName + "Error";
            if (fieldName != null && data != null && data.Table.Columns.Contains(errorFieldName))
            {
                if (data.Table.Columns[errorFieldName].DataType == typeof(float) ||
                    data.Table.Columns[errorFieldName].DataType == typeof(double))
                    return DataTableUtilities.GetColumnAsDoubles(data, errorFieldName);
            }
            return null;
        }

        /// <summary>Rewrites a filter that has a wildcard in the middle of a field
        /// Wildcards in the middle are not supported by Row Filters, so it's broken into multiple filters</summary>
        /// <param name="filter">The filter as a string</param>
        /// <returns>The modified filter as a string</returns>
        private string RemoveMiddleWildcards(string filter)
        {
            if (filter == null || filter == "")
                return filter;

            string newString = filter;
            string likePattern = @"(.*\sLIKE\s['""])(.*?[^""'])([%*])([^""'][^%*]+)(.*)";
            bool stillMatches = true;

            //add a bit of protection to this loop so we can break it and return an error if it loops forever.
            int maxTries = 10;
            int loops = 0;
            while (stillMatches && loops < maxTries)
            {
                loops += 1;
                Match match = Regex.Match(newString, likePattern);
                stillMatches = match.Success;
                if (stillMatches)
                {
                    newString = "";
                    newString += match.Groups[1];
                    newString += match.Groups[2];
                    newString += match.Groups[3];
                    newString += "' AND ";
                    newString += match.Groups[1];
                    newString += match.Groups[3];
                    newString += match.Groups[4];
                    if (match.Groups.Count == 6)
                        newString += match.Groups[5];
                }
            }

            if (loops == maxTries)
                throw new Exception($"Row Filter '{filter}' contains wildcard characters that cannot be parsed correctly.");

            return newString;
        }

    }
}
