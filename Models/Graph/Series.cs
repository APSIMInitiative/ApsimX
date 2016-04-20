// -----------------------------------------------------------------------
// <copyright file="Series.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Graph
{
    using System;
    using System.Drawing;
    using System.Xml.Serialization;
    using System.Data;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    using System.Collections;
    using Models.Core;
    using Models.Factorial;

    /// <summary>The class represents a single series on a graph</summary>
    [ValidParent(ParentType = typeof(Graph))]
    [ViewName("UserInterface.Views.SeriesView")]
    [PresenterName("UserInterface.Presenters.SeriesPresenter")]
    [Serializable]
    public class Series : Model, IGraphable
    {

        /// <summary>Constructor for a series</summary>
        public Series()
        {
            this.XAxis = Axis.AxisType.Bottom;
            this.FactorIndexToVaryColours = -1;
            this.FactorIndexToVaryLines = -1;
            this.FactorIndexToVaryMarkers = -1;
        }

        /// <summary>Gets or sets the series type</summary>
        public SeriesType Type { get; set; }

        /// <summary>Gets or sets the associated x axis</summary>
        public Axis.AxisType XAxis { get; set; }

        /// <summary>Gets or sets the associated y axis</summary>
        public Axis.AxisType YAxis { get; set; }

        /// <summary>
        /// Gets or sets the color represented as a red, green, blue integer
        /// </summary>
        public int ColourArgb { get; set; }

        /// <summary>Gets or sets the color</summary>
        [XmlIgnore]
        public Color Colour
        {
            get
            {
                return Color.FromArgb(this.ColourArgb);
            }

            set
            {
                this.ColourArgb = value.ToArgb();
            }
        }

        /// <summary>The FactorIndex to vary colours.</summary>
        public int FactorIndexToVaryColours { get; set; }

        /// <summary>The FactorIndex to vary markers types.</summary>
        public int FactorIndexToVaryMarkers { get; set; }

        /// <summary>The FactorIndex to vary line types.</summary>
        public int FactorIndexToVaryLines { get; set; }

        /// <summary>Gets or sets the marker size</summary>
        public MarkerType Marker { get; set; }

        /// <summary>Marker size.</summary>
        public MarkerSizeType MarkerSize { get; set; }

        /// <summary>Gets or sets the line type to show</summary>
        public LineType Line { get; set; }

        /// <summary>Gets or sets the line thickness</summary>
        public LineThicknessType LineThickness { get; set; }

        /// <summary>Gets or sets the name of the table to get data from.</summary>
        public string TableName { get; set; }

        /// <summary>Gets or sets the name of the x field</summary>
        public string XFieldName { get; set; }

        /// <summary>Gets or sets the name of the y field</summary>
        public string YFieldName { get; set; }

        /// <summary>Gets or sets the name of the x2 field</summary>
        public string X2FieldName { get; set; }

        /// <summary>Gets or sets the name of the y2 field</summary>
        public string Y2FieldName { get; set; }

        /// <summary>Gets or sets a value indicating whether the series should be shown in the legend</summary>
        public bool ShowInLegend { get; set; }

        /// <summary>Gets or sets a value indicating whether the series name should be shown in the legend</summary>
        public bool IncludeSeriesNameInLegend { get; set; }

        /// <summary>Gets or sets a value indicating whether the Y variables should be cumulative.</summary>
        public bool Cumulative { get; set; }

        /// <summary>Gets or sets a value indicating whether the X variables should be cumulative.</summary>
        public bool CumulativeX { get; set; }

        /// <summary>Optional data filter.</summary>
        public string Filter { get; set; }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        public void GetSeriesToPutOnGraph(List<SeriesDefinition> definitions)
        {
            List<SeriesDefinition> ourDefinitions = new List<SeriesDefinition>();

            // If this series doesn't have a table name then it must be getting its data from other models.
            if (TableName == null)
                ourDefinitions.Add(CreateDefinition(Name, null, Colour, Marker, Line, null));
            else
            {
                Simulation parentSimulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
                Zone parentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                Experiment parentExperiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;

                // If the graph is in a zone then just graph the zone.
                if (parentZone != null && !(parentZone is Simulation))
                {
                    GraphSimulation(parentZone, ourDefinitions);
                }
                else
                {
                    List<Simulation> simulations = new List<Simulation>();
                    List<Experiment> experiments = new List<Experiment>();

                    // Graph is sitting in a simulation so graph just that simulation.
                    if (parentSimulation != null)
                        GraphSimulation(parentSimulation, ourDefinitions);

                    // See if graph is inside an experiment. If so then graph all simulations in experiment.
                    else if (parentExperiment != null)
                        GraphExperiment(parentExperiment, ourDefinitions);

                    // Must be in a folder at the top level or at the top level of the .apsimx file. 
                    else
                    {
                        IModel parentOfGraph = this.Parent.Parent;

                        // Look for experiments.
                        foreach (Experiment experiment in Apsim.ChildrenRecursively(parentOfGraph, typeof(Experiment)))
                            experiments.Add(experiment);

                        // Look for simulations
                        foreach (Simulation simulation in Apsim.ChildrenRecursively(parentOfGraph, typeof(Simulation)))
                        {
                            if (simulation.Parent is Experiment)
                                { }
                            else
                                simulations.Add(simulation);
                        }
                    }

                    // Now create series definitions for each experiment found.
                    int colourIndex = Array.IndexOf(ColourUtilities.Colours, Colour);
                    if (colourIndex == -1)
                        colourIndex = 0;
                    MarkerType marker = Marker;
                    foreach (Experiment experiment in experiments)
                    {
                        string filter = "SimulationName IN " + "(" + StringUtilities.Build(experiment.Names(), delimiter: ",", prefix: "'", suffix: "'") + ")";
                        CreateDefinitions(experiment.BaseSimulation, experiment.Name, filter, ref colourIndex, ref marker, Line, ourDefinitions, experiment.Names());
                    }

                    // Now create series definitions for each simulation found.
                    marker = Marker;
                    foreach (Simulation simulation in simulations)
                    {
                        string filter = "SimulationName = '" + simulation.Name + "'";
                        CreateDefinitions(simulation, simulation.Name, filter, ref colourIndex, ref marker, Line, ourDefinitions, new string[] { simulation.Name });
                    }
                }
            }

            // Get all data.
            StoreDataInSeriesDefinitions(ourDefinitions);

            // We might have child models that wan't to add to our series definitions e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetSeriesToPutOnGraph(ourDefinitions);

            definitions.AddRange(ourDefinitions);
        }

        /// <summary>
        /// Graph the specified simulation, looking for zones.
        /// </summary>
        /// <param name="model">The model where the graph sits.</param>
        /// <param name="ourDefinitions">The list of series definitions to add to.</param>
        private void GraphSimulation(IModel model, List<SeriesDefinition> ourDefinitions)
        {
            Simulation parentSimulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;

            List<IModel> zones = Apsim.Children(model, typeof(Zone));
            if (zones.Count > 1)
            {
                int colourIndex = 0;
                foreach (Zone zone in zones)
                {
                    string filter = string.Format("SimulationName='{0}' and ZoneName='{1}'", parentSimulation.Name, zone.Name);
                    ourDefinitions.Add(CreateDefinition(zone.Name, filter, ColourUtilities.ChooseColour(colourIndex), Marker, Line,
                                                        new string[] { parentSimulation.Name }));
                    colourIndex++;
                }
            }
            else
            {
                string filter = string.Format("SimulationName='{0}'", parentSimulation.Name);
                ourDefinitions.Add(CreateDefinition(Name, filter, Colour, Marker, Line,
                                                    new string[] { parentSimulation.Name }));
            }
        }

        class FactorAndIndex
        {
            public enum TypeToVary { Colour, Line, Marker }
            public int factorValueIndex;
            public string factorName;
            public string factorValue;
            public TypeToVary typeToVary;
        }

        /// <summary>
        /// Graph the specified experiment.
        /// </summary>
        /// <param name="parentExperiment"></param>
        /// <param name="ourDefinitions"></param>
        private void GraphExperiment(Experiment parentExperiment, List<SeriesDefinition> ourDefinitions)
        {
            Factors factors = Apsim.Child(parentExperiment as IModel, typeof(Factors)) as Factors;
            if (factors != null)
            {
                // Given this example (from Teff.apsimx).
                //    Factors
                //       CV   - Gibe, Ziquala, Ayana, 04T19   (4 factor values)
                //       PP   - 1, 2, 3, 4, 5, 6              (6 factor values)
                // -----------------------------------------------------------
                //    If FactorIndexToVaryColours = 0  (index of CV factor)
                //       FactorIndexToVaryLines   = 1  (index of PP factor)
                //       FactorIndexToVaryMarkers = -1 (doesn't point to a factor)
                //    Then permutations will be:
                //       CVGibe & PP1
                //       CVZiquala & PP2
                //       CVAyana & PP3
                //       ... (24 in total - 4 x 6)
                // -----------------------------------------------------------
                //    If FactorIndexToVaryColours = 0  (index of CV factor)
                //       FactorIndexToVaryLines   = -1 (doesn't point to a factor)
                //       FactorIndexToVaryMarkers = -1 (doesn't point to a factor)
                //    Then permutations will be:
                //       CVGibe
                //       CVZiquala
                //       CVAyana
                //       CV04T19  (4 in total - 4)
                // -----------------------------------------------------------
                // The FactorIndexToVary... variables denote which factors should be 
                // separate series.

                List<List<FactorAndIndex>> factorIndexes = new List<List<FactorAndIndex>>();
                for (int f = 0; f != factors.Children.Count; f++)
                {
                    if (FactorIndexToVaryColours == f)
                        CreateFactorAndIndex(factors.Children[FactorIndexToVaryColours] as Factor, factorIndexes,
                                             FactorAndIndex.TypeToVary.Colour);

                    if (FactorIndexToVaryLines == f)
                        CreateFactorAndIndex(factors.Children[FactorIndexToVaryLines] as Factor, factorIndexes,
                                             FactorAndIndex.TypeToVary.Line);

                    if (FactorIndexToVaryMarkers == f)
                        CreateFactorAndIndex(factors.Children[FactorIndexToVaryMarkers] as Factor, factorIndexes,
                                             FactorAndIndex.TypeToVary.Marker);
                }

                List<List<FactorAndIndex>> permutations = MathUtilities.AllCombinationsOf(factorIndexes.ToArray());

                // If no 'vary by' were specified then create a dummy one. All data will be on one series.
                if (permutations == null || permutations.Count == 0)
                {
                    permutations = new List<List<FactorAndIndex>>();
                    permutations.Add(new List<FactorAndIndex>());
                }

                // Loop through all permutations and create a graph series definition for each.
                foreach (List<FactorAndIndex> combination in permutations)
                {
                    // Determine the marker, line and colour for this combination.
                    MarkerType marker = Marker;
                    LineType line = Line;
                    int colourIndex = Array.IndexOf(ColourUtilities.Colours, Colour);
                    if (colourIndex == -1)
                        colourIndex = 0;
                    string seriesName = string.Empty;
                    for (int i = 0; i < combination.Count; i++)
                    {
                        if (combination[i].typeToVary == FactorAndIndex.TypeToVary.Colour)
                            colourIndex = combination[i].factorValueIndex;
                        if (combination[i].typeToVary == FactorAndIndex.TypeToVary.Marker)
                            marker = GetEnumValue<MarkerType>(combination[i].factorValueIndex);
                        if (combination[i].typeToVary == FactorAndIndex.TypeToVary.Line)
                            line = GetEnumValue<LineType>(combination[i].factorValueIndex);
                        seriesName += combination[i].factorName + combination[i].factorValue;
                    }

                    string filter = GetFilter(parentExperiment, combination);

                    CreateDefinitions(parentExperiment.BaseSimulation, seriesName, filter, ref colourIndex,
                                      ref marker, line, ourDefinitions, parentExperiment.Names());
                }
            }
        }

        /// <summary>Get a .db filter for the specified combination.</summary>
        /// <param name="experiment">The experiment</param>
        /// <param name="combination">The combination</param>
        /// <returns>The filter</returns>
        private string GetFilter(Experiment experiment, List<FactorAndIndex> combination)
        {
            // Need to determine all the simulation names that match the specified combination.
            // If the combination is just a single factor value then the simulation names will be
            // a permutation of that factor value and all other factor values in other factors 
            // For example:
            //   IF combination is CVGibe
            //   THEN filter = SimulationName = 'VanDeldenCvGibePP1' or SimulationName = 'VanDeldenCvGibePP2' or SimulationName = 'VanDeldenCvGibePP3' ...
            // If the combintation of 2 factor values for 2 different factors then the filter will
            // be a permutation of those 2 factor values and all OTHER factor values in OTHER factors.
            // For example:
            //   IF combintation is CVGibePP1
            //   THEN filter = SimulationName = 'SimulationName = 'VanDeldenCvGibePP1'
            //   (This is because the example has no other factors other than CV and PP.
            Factors factors = Apsim.Child(experiment as IModel, typeof(Factors)) as Factors;

            List<List<KeyValuePair<string, string>>> simulationBits = new List<List<KeyValuePair<string, string>>>();

            foreach (Factor factor in factors.factors)
            {
                List<KeyValuePair<string, string>> names = new List<KeyValuePair<string, string>>();
                FactorAndIndex factorAndIndex = combination.Find(f => factor.Name == f.factorName);
                if (factorAndIndex == null)
                {
                    if (factor.Children.Count > 0)
                    {
                        foreach (IModel child in factor.Children)
                            names.Add(new KeyValuePair<string, string>(factor.Name, child.Name));
                    }
                    else
                    {
                        foreach (FactorValue factorValue in factor.CreateValues())
                        {
                            if (factorValue.Values.Count >= 1)
                                names.Add(new KeyValuePair<string, string>(factor.Name, factorValue.Values[0].ToString()));
                        }
                    }
                }
                else
                    names.Add(new KeyValuePair<string, string>(factor.Name, factorAndIndex.factorValue));
                simulationBits.Add(names);
            }

            List<List<KeyValuePair<string, string>>> combinations = MathUtilities.AllCombinationsOf<KeyValuePair<string, string>>(simulationBits.ToArray());

            string filter = string.Empty;
            foreach (List<KeyValuePair<string, string>> c in combinations)
            {
                if (filter != string.Empty)
                    filter += " or ";
                filter += "SimulationName = '" + experiment.Name;
                foreach (KeyValuePair<string, string> pair in c)
                    filter += pair.Key + pair.Value;
                filter += "'";
            }

            return filter;
        }

        /// <summary>Create a series of FactorAndIndex objects for the values of the specified factor.</summary>
        /// <param name="factor"></param>
        /// <param name="factorIndexes"></param>
        /// <param name="typeToVary"></param>
        private void CreateFactorAndIndex(Factor factor, List<List<FactorAndIndex>> factorIndexes, FactorAndIndex.TypeToVary typeToVary)
        {
            List<FactorAndIndex> factorValueIndexes = new List<FactorAndIndex>();
            List<FactorValue> factorValues = factor.CreateValues();
            for (int j = 0; j < factorValues.Count; j++)
            //for (int j = 0; j < factor.Children.Count; j++)
            {
                factorValueIndexes.Add(new FactorAndIndex()
                {
                    factorValueIndex = j,
                    factorName = factor.Name,
                    factorValue = factorValues[j].Name.Replace(factor.Name, ""),
                    //factorValue = factor.Children[j].Name,
                    typeToVary = typeToVary
                });
            }
            factorIndexes.Add(factorValueIndexes);
        }

        /// <summary>Increment an enumeration. Will wrap back to first one when it goes past the end of possible enumerations.
        /// </summary>
        /// <typeparam name="T">The enum type</typeparam>
        /// <param name="index">The index of the enum to return.</param>
        /// <returns>The specified enum value.</returns>
        private static T GetEnumValue<T>(int index)
        {
            List<T> enumValues = new List<T>();
            foreach (T value in Enum.GetValues(typeof(T)))
                if (value.ToString() != "None")
                    enumValues.Add(value);

            if (index < 0)
                throw new Exception("Invalid index found while getting " + enumValues[0].GetType().Name);

            while (index >= enumValues.Count)
                index -= enumValues.Count;

            return enumValues[index];
        }

        /// <summary>Creates series definitions for the specified simulation.</summary>
        /// <param name="simulation">The simulation.</param>
        /// <param name="baseTitle">The base title.</param>
        /// <param name="baseFilter">The base filter.</param>
        /// <param name="colourIndex">The index into the colour palette.</param>
        /// <param name="definitions">The definitions to add to.</param>
        /// <param name="marker">The marker type.</param>
        /// <param name="line">The line type.</param>
        /// <param name="simulationNames">Simulation names to include in data.</param>
        private void CreateDefinitions(Simulation simulation, string baseTitle, string baseFilter, ref int colourIndex,
                                       ref MarkerType marker, LineType line,
                                       List<SeriesDefinition> definitions,
                                       string[] simulationNames)
        {
            List<IModel> zones = Apsim.Children(simulation, typeof(Zone));
            if (zones.Count > 1)
            {
                foreach (Zone zone in zones)
                {
                    string zoneFilter = baseFilter + " AND ZoneName = '" + zone.Name + "'";
                    definitions.Add(CreateDefinition(baseTitle + " " + zone.Name, zoneFilter,
                                                     ColourUtilities.ChooseColour(colourIndex),
                                                     marker, line,
                                                     simulationNames));
                    colourIndex++;
                }
            }
            else
            {
                definitions.Add(CreateDefinition(baseTitle, baseFilter,
                                                 ColourUtilities.ChooseColour(colourIndex),
                                                 marker, line,
                                                 simulationNames));
                colourIndex++;
            }

            if (colourIndex >= ColourUtilities.Colours.Length)
            {
                colourIndex = 0;
                marker = IncrementMarker(marker);
            }
        }

        /// <summary>Increment marker type.</summary>
        /// <param name="marker">Marker type</param>
        private MarkerType IncrementMarker(MarkerType marker)
        {
            Array markers = Enum.GetValues(typeof(MarkerType));
            int markerIndex = Array.IndexOf(markers, marker);
            markerIndex++;
            if (markerIndex >= markers.Length)
                markerIndex = 0;
            return (MarkerType)markers.GetValue(markerIndex);
        }

        /// <summary>Creates a series definition.</summary>
        /// <param name="title">The title.</param>
        /// <param name="filter">The filter. Can be null.</param>
        /// <param name="colour">The colour.</param>
        /// <param name="line">The line type.</param>
        /// <param name="marker">The marker type.</param>
        /// <param name="simulationNames">A list of simulations to include in data.</param>
        /// <returns>The newly created definition.</returns>
        private SeriesDefinition CreateDefinition(string title, string filter, Color colour, MarkerType marker, LineType line, string[] simulationNames)
        {
            SeriesDefinition definition = new SeriesDefinition();
            definition.SimulationNames = simulationNames;
            definition.Filter = filter;
            definition.colour = colour;
            definition.title = title;
            if (IncludeSeriesNameInLegend)
                definition.title += ": " + Name;
            definition.line = line;
            definition.marker = marker;
            definition.lineThickness = LineThickness;
            definition.markerSize = MarkerSize;
            definition.showInLegend = ShowInLegend;
            definition.type = Type;
            definition.xAxis = XAxis;
            definition.xFieldName = XFieldName;
            definition.yAxis = YAxis;
            definition.yFieldName = YFieldName;
            return definition;
        }

        /// <summary>Give data to all series definitions.</summary>
        /// <param name="definitions">The definitions to satisfy.</param>
        private void StoreDataInSeriesDefinitions(List<SeriesDefinition> definitions)
        {
            // Get a list of all simulation names.
            SortedSet<string> simulationNames = new SortedSet<string>();
            foreach (SeriesDefinition definition in definitions)
            {
                if (definition.SimulationNames != null)
                    foreach (string simulationName in definition.SimulationNames)
                        simulationNames.Add(simulationName);
            }

            // Create a filter.
            string filter = string.Empty;
            foreach (string simulationName in simulationNames)
            {
                if (filter != string.Empty)
                    filter += ",";
                filter += "'" + simulationName + "'";
            }
            filter = "SimulationName IN (" + filter + ")";

            List<string> fieldNames = new List<string>();
            fieldNames.Add(XFieldName);
            fieldNames.Add(YFieldName);
            if (X2FieldName != null)
                fieldNames.Add(X2FieldName);
            if (Y2FieldName != null)
                fieldNames.Add(Y2FieldName);
            if ((Filter != null) && Filter.StartsWith("["))
            {
               string FilterName = "";
               int posCloseBracket = Filter.IndexOf(']');
               if (posCloseBracket == -1)
                       throw new Exception("Invalid filter column name: " + Filter);
               FilterName = Filter.Substring(1, posCloseBracket - 1);
               if (!fieldNames.Contains(FilterName))
                    fieldNames.Add(FilterName);
            }
            else if ((Filter != null) && (Filter != ""))
              throw new Exception("Column name to filter on must be within square brackets.  e.g [ColumnToFilter]");
            fieldNames.Add("ZoneName");
            // filter data for each definition.
            foreach (SeriesDefinition definition in definitions)
                GetData(definition);
        }

        /// <summary>Gets all series data and stores in the specified definition.</summary>
        /// <param name="definition">The definition to store the data in.</param>
        private void GetData(SeriesDefinition definition)
        {
            // If the table name is null then use reflection to get data from other models.
            if (TableName == null)
            {
                if (!String.IsNullOrEmpty(XFieldName))
                    definition.x = GetDataFromModels(XFieldName);
                if (!String.IsNullOrEmpty(YFieldName))
                    definition.y = GetDataFromModels(YFieldName);
                if (!String.IsNullOrEmpty(X2FieldName))
                    definition.x2 = GetDataFromModels(X2FieldName);
                if (!String.IsNullOrEmpty(Y2FieldName))
                    definition.y2 = GetDataFromModels(Y2FieldName);
            }
            else
            {
                Graph parentGraph = Parent as Graph;
                if (parentGraph != null)
                {

                    DataTable data = parentGraph.GetBaseData(TableName);
                    if (data != null)
                    {
                        string[] names = DataTableUtilities.GetColumnAsStrings(data, "SimulationName");

                        string FilterExpression = "";
                        if (Filter != null && Filter != string.Empty)
                        {
                            FilterExpression = Filter.Replace("[", "");
                            FilterExpression = FilterExpression.Replace("]", "");
                        }
                        string where = "(";
                        if (Filter != null && Filter != string.Empty)
                            where += "(";
                        where += definition.Filter;
                        if (Filter != null && Filter != string.Empty)
                            where += ") AND (" + FilterExpression + ")";
                        where += ")";

                        DataView dataView = new DataView(data);
                        if (where != "()")
                            try
                            {
                                dataView.RowFilter = where;
                            }
                            catch (Exception)
                            {

                            }

                        // If the field exists in our data table then return it.
                        if (data != null &&
                            XFieldName != null &&
                            YFieldName != null &&
                            data.Columns.Contains(XFieldName) &&
                            data.Columns.Contains(YFieldName) &&
                            dataView.Count > 0)
                        {
                            definition.x = GetDataFromTable(dataView, XFieldName);
                            definition.y = GetDataFromTable(dataView, YFieldName);
                            if (Cumulative)
                                definition.y = MathUtilities.Cumulative(definition.y as IEnumerable<double>);
                            if (CumulativeX)
                                definition.x = MathUtilities.Cumulative(definition.x as IEnumerable<double>);

                            if (X2FieldName != null && Y2FieldName != null &&
                                data.Columns.Contains(X2FieldName) && data.Columns.Contains(Y2FieldName))
                            {
                                definition.x2 = GetDataFromTable(dataView, X2FieldName);
                                definition.y2 = GetDataFromTable(dataView, Y2FieldName);
                            }
                            definition.simulationNamesForEachPoint = (IEnumerable<string>)GetDataFromTable(dataView, "SimulationName");
                        }
                    }
                }
            }
        }

        /// <summary>Gets a column of data from a table.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable GetDataFromTable(DataView data, string fieldName)
        {
            if (data.Table.Columns[fieldName].DataType == typeof(DateTime))
                return DataTableUtilities.GetColumnAsDates(data, fieldName);
            else if (data.Table.Columns[fieldName].DataType == typeof(string))
                return DataTableUtilities.GetColumnAsStrings(data, fieldName);
            else
                return DataTableUtilities.GetColumnAsDoubles(data, fieldName);
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

                IModel modelWithData = Apsim.Find(this, modelName) as IModel;
                if (modelWithData == null)
                {
                    // Try by assuming the name is a type.
                    Type t = ReflectionUtilities.GetTypeFromUnqualifiedName(modelName);
                    if (t != null)
                        modelWithData = Apsim.Find(this.Parent.Parent, t) as IModel;
                }

                if (modelWithData != null)
                {
                    // Use reflection to access a property.
                    object obj = Apsim.Get(modelWithData, namePath);
                    if (obj != null && obj.GetType().IsArray)
                        return obj as Array;
                }
            }

            return null;
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        /// <param name="annotations">A list of annotations to add to.</param>
        public void GetAnnotationsToPutOnGraph(List<Annotation> annotations)
        {
            // We might have child models that wan't to add to the annotations e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetAnnotationsToPutOnGraph(annotations);

        }

    }
}
