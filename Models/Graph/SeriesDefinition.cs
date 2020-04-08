namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Models.Storage;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class for defining a graph series. A list of these is given to graph when graph is drawing itself.
    /// </summary>
    [Serializable]
    public class SeriesDefinition
    {
        /// <summary>Base series where most properties come from.</summary>
        private Series series;

        /// <summary>Series definition filter.</summary>
        private string scopeFilter;

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
        /// <param name="whereClauseForInScopeData">A SQL where clause to specify data that is in scope.</param>
        /// <param name="filter">User specified filter.</param>
        /// <param name="descriptors">The descriptors for this series definition.</param>
        /// <param name="customTitle">The title to use for the definition.</param>
        public SeriesDefinition(Series series,
                                string checkpoint,
                                double colModifier,
                                double markerModifier,
                                string whereClauseForInScopeData = null,
                                string filter = null,
                                List<SimulationDescription.Descriptor> descriptors = null,
                                string customTitle = null)
        {
            this.series = series;
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

            scopeFilter = whereClauseForInScopeData;
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
                                Axis.AxisType xAxis = Axis.AxisType.Bottom,
                                Axis.AxisType yAxis = Axis.AxisType.Left)
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
        public MarkerSizeType MarkerSize
        {
            get
            {
                if (series == null) // Can be null for regression lines or 1:1 lines
                    return MarkerSizeType.Normal;

                return series.MarkerSize;
            }
        }

        /// <summary>Gets the line thickness.</summary>
        public LineThicknessType LineThickness
        {
            get
            {
                if (series == null) // Can be null for regression lines or 1:1 lines
                    return LineThicknessType.Normal;
                else
                    return series.LineThickness;
            }
        }

        /// <summary>Gets the associated x axis.</summary>
        public Axis.AxisType XAxis { get; }

        /// <summary>Gets the associated y axis.</summary>
        public Axis.AxisType YAxis { get; }

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
        public DataTable Data { get; private set; }

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

        /// <summary>Gets the error values</summary>
        public IEnumerable<double> Error { get; private set; }

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
        /// <param name="reader">Storage reader.</param>
        /// <param name="simulationDescriptions">Complete list of simulation descriptions.</param>
        public void ReadData(IStorageReader reader, List<SimulationDescription> simulationDescriptions)
        {
            if (X != null && Y != null)
                return;

            if (series.TableName == null)
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
            else
            {
                var fieldsThatExist = reader.ColumnNames(series.TableName);

                // If we have descriptors, then use them to filter the data for this series.
                string filter = null;
                if (Descriptors != null)
                {
                    foreach (var descriptor in Descriptors)
                    {
                        if (fieldsThatExist.Contains(descriptor.Name))
                            filter = AddToFilter(filter, descriptor.Name + " = '" + descriptor.Value + "'");
                        else
                            filter = AddSimulationNameClauseToFilter(filter, descriptor, simulationDescriptions);
                    }

                    // Incorporate our scope filter if we haven't limited filter to particular simulations.
                    if (!filter.Contains("SimulationName IN"))
                        filter = AddToFilter(filter, scopeFilter);
                }
                else
                    filter = AddToFilter(filter, scopeFilter);

                if (!string.IsNullOrEmpty(userFilter))
                    filter = AddToFilter(filter, userFilter);

                // Get a list of fields to read from data store.
                var fieldsToRead = new List<string>();
                fieldsToRead.Add(XFieldName);
                fieldsToRead.Add(YFieldName);
                if (X2FieldName != null)
                    fieldsToRead.Add(X2FieldName);
                if (Y2FieldName != null)
                    fieldsToRead.Add(Y2FieldName);

                // Add any error fields to the list of fields to read.
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
                foreach (IGraphable series in Apsim.Children(series, typeof(IGraphable)))
                    fieldsToRead.AddRange(series.GetExtraFieldsToRead(this));

                // Checkpoints don't exist in observed files so don't pass a checkpoint name to 
                // GetData in this situation.
                string localCheckpointName = CheckpointName;
                if (!reader.ColumnNames(series.TableName).Contains("CheckpointID"))
                    localCheckpointName = null;

                // Go get the data.
                Data = reader.GetData(series.TableName, localCheckpointName, fieldNames: fieldsToRead.Distinct(), filter: filter);

                // Get the units for our x and y variables.
                XFieldUnits = reader.Units(series.TableName, XFieldName);
                YFieldUnits = reader.Units(series.TableName, YFieldName);

                // If data was found, populate our data (e.g. X and Y) properties.
                if (Data.Rows.Count > 0)
                {
                    X = GetDataFromTable(Data, XFieldName);
                    Y = GetDataFromTable(Data, YFieldName);
                    X2 = GetDataFromTable(Data, X2FieldName);
                    Y2 = GetDataFromTable(Data, Y2FieldName);
                    Error = GetErrorDataFromTable(Data, YFieldName);
                    if (series.Cumulative)
                        Y = MathUtilities.Cumulative(Y as IEnumerable<double>);
                    if (series.CumulativeX)
                        X = MathUtilities.Cumulative(X as IEnumerable<double>);
                }
            }
        }

        /// <summary>Add a 'SimulationName=' clause to filter using a descriptor.</summary>
        /// <param name="filter">Filter to add to.</param>
        /// <param name="descriptor">The descriptor to use to create the filter.</param>
        /// <param name="simulationDescriptions">Complete list of simulation descriptions.</param>
        private string AddSimulationNameClauseToFilter(string filter, SimulationDescription.Descriptor descriptor, List<SimulationDescription> simulationDescriptions)
        {
            var simulationNames = simulationDescriptions.FindAll(sim => sim.HasDescriptor(descriptor)).Select(sim => sim.Name);
            return AddToFilter(filter, "SimulationName IN (" +
                                StringUtilities.Build(simulationNames, ",", "'", "'") +
                                ")");
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
            }

            return fieldNames;
        }


        /// <summary>Return data using reflection</summary>
        /// <param name="fieldName">The field name to get data for.</param>
        /// <returns>The return data or null if not found</returns>
        private IEnumerable GetDataFromModels(string fieldName)
        {
            if (fieldName != null && fieldName.StartsWith("["))
            {
                int posCloseBracket = fieldName.IndexOf(']');
                if (posCloseBracket == -1)
                    throw new Exception("Invalid graph field name: " + fieldName);

                string modelName = fieldName.Substring(1, posCloseBracket - 1);
                string namePath = fieldName.Remove(0, posCloseBracket + 2);

                IModel modelWithData = Apsim.Find(series, modelName) as IModel;
                if (modelWithData == null)
                {
                    // Try by assuming the name is a type.
                    Type t = ReflectionUtilities.GetTypeFromUnqualifiedName(modelName);
                    if (t != null)
                    {
                        IModel parentOfGraph = series.Parent.Parent;
                        if (t.IsAssignableFrom(parentOfGraph.GetType()))
                            modelWithData = parentOfGraph;
                        else
                            modelWithData = Apsim.Find(parentOfGraph, t);
                    }
                }

                if (modelWithData != null)
                {
                    // Use reflection to access a property.
                    object obj = Apsim.Get(modelWithData, namePath);
                    if (obj != null && obj.GetType().IsArray)
                        return obj as Array;
                }
            }
            else
            {
                return Apsim.Get(series, fieldName) as IEnumerable;
            }

            return null;
        }

        /// <summary>Gets a column of data from a table.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable GetDataFromTable(DataTable data, string fieldName)
        {
            if (fieldName != null && data != null && data.Columns.Contains(fieldName))
            {
                if (data.Columns[fieldName].DataType == typeof(DateTime))
                    return DataTableUtilities.GetColumnAsDates(data, fieldName);
                else if (data.Columns[fieldName].DataType == typeof(string))
                    return DataTableUtilities.GetColumnAsStrings(data, fieldName);
                else
                    return DataTableUtilities.GetColumnAsDoubles(data, fieldName);
            }
            return null;
        }

        /// <summary>Gets a column of error data from a table.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable<double> GetErrorDataFromTable(DataTable data, string fieldName)
        {
            string errorFieldName = fieldName + "Error";
            if (fieldName != null && data != null && data.Columns.Contains(errorFieldName))
            {
                if (data.Columns[errorFieldName].DataType == typeof(float) ||
                    data.Columns[errorFieldName].DataType == typeof(double))
                    return DataTableUtilities.GetColumnAsDoubles(data, errorFieldName);
            }
            return null;
        }

    }
}
