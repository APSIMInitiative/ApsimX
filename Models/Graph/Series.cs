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
    using System.Linq;
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
        
        /// <summary>A list of all factors that can be listed as 'vary by' in markers/line types etc.</summary>
        [XmlIgnore]
        public List<string> FactorNamesForVarying { get; set; }

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
                // Find a parent that heads the scope that we're going to graph
                IModel parent = FindParent();

                List<SimulationZone> simulationZones = null;
                do
                {
                    // Create a list of all simulation/zone objects that we're going to graph.
                    simulationZones = BuildListFromModel(parent);
                    parent = parent.Parent;
                }
                while (simulationZones.Count == 0 && parent != null);

                // Get rid of factors that don't vary across objects.
                RemoveFactorsThatDontVary(simulationZones);
                FactorNamesForVarying = GetFactorList(simulationZones);

                // Check for old .apsimx that doesn't vary colours, lines or markers but has experiments. In this 
                // new code, we want to make this situation explicit and say we'll vary colours and markers by
                // experiment.
                if (FactorIndexToVaryColours == -1 && FactorIndexToVaryLines == -1 && FactorIndexToVaryMarkers == -1 &&
                    FactorNamesForVarying.Contains("Experiment") && !ColourUtilities.Colours.Contains(Colour))
                {
                    FactorIndexToVaryColours = FactorNamesForVarying.IndexOf("Experiment");
                    FactorIndexToVaryMarkers = FactorIndexToVaryColours;
                }
                else if (!ColourUtilities.Colours.Contains(Colour))
                {
                    Colour = ColourUtilities.Colours[0];
                }

                // If a factor isn't being used to vary a colour/marker/line, then remove the factor. i.e. we
                // don't care about it.
                simulationZones = RemoveFactorsNotBeingUsed(simulationZones);

                // Get data for each simulation / zone object
                DataStore dataStore = new DataStore(this);
                DataTable baseData = GetBaseData(dataStore, simulationZones);
                dataStore.Disconnect();
                simulationZones.ForEach(simulationZone => simulationZone.CreateDataView(baseData, this));

                // Setup all colour, marker, line types etc in all simulation / zone objects.
                PaintAllSimulationZones(simulationZones);

                // Convert all simulation / zone objects to seriesdefinitions.
                simulationZones.ForEach(simZone => ourDefinitions.Add(ConvertToSeriesDefinition(simZone)));
            }

            // Get all data.
            //StoreDataInSeriesDefinitions(ourDefinitions);

            // We might have child models that want to add to our series definitions e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetSeriesToPutOnGraph(ourDefinitions);

            // Remove series that have no data.
            ourDefinitions.RemoveAll(d => !MathUtilities.ValuesInArray(d.x) || !MathUtilities.ValuesInArray(d.y));

            definitions.AddRange(ourDefinitions);
        }

        /// <summary>
        /// Go through all simulation zone objects and remove factors that don't vary between objects.
        /// </summary>
        /// <param name="simulationZones">A list of simulation zones.</param>
        private List<string> GetFactorList(List<SimulationZone> simulationZones)
        {
            List<string> factorNames = new List<string>();
            foreach (SimulationZone simZone in simulationZones)
                foreach (KeyValuePair<string, string> factorPair in simZone.factorNameValues)
                    factorNames.Add(factorPair.Key);
            return factorNames.Distinct().ToList();
        }

        /// <summary>
        /// Go through all simulation zone objects and remove factors that don't vary between objects.
        /// </summary>
        /// <param name="simulationZones">A list of simulation zones.</param>
        private void RemoveFactorsThatDontVary(List<SimulationZone> simulationZones)
        {
            foreach (string factorName in GetFactorList(simulationZones))
            {
                List<string> factorValues = new List<string>();
                simulationZones.ForEach(simZone => factorValues.Add(simZone.GetValueOf(factorName)));

                if (factorValues.Distinct().Count() == 1)
                {
                    // All factor values are the same so remove the factor.
                    simulationZones = RemoveFactorAndMerge(simulationZones, factorName);
                }
            }
        }

        /// <summary>
        /// Remove factors that aren't being used to vary visual elements (e.g. line/marker etc)
        /// </summary>
        /// <param name="simulationZones">A list of simulation zones.</param>
        private List<SimulationZone> RemoveFactorsNotBeingUsed(List<SimulationZone> simulationZones)
        {
            List<string> factorsToKeep = new List<string>();
            if (FactorIndexToVaryColours >= 0 && FactorIndexToVaryColours < FactorNamesForVarying.Count)
                factorsToKeep.Add(FactorNamesForVarying[FactorIndexToVaryColours]);
            if (FactorIndexToVaryLines >= 0 && FactorIndexToVaryLines < FactorNamesForVarying.Count)
                factorsToKeep.Add(FactorNamesForVarying[FactorIndexToVaryLines]);
            if (FactorIndexToVaryMarkers >= 0 && FactorIndexToVaryMarkers < FactorNamesForVarying.Count)
                factorsToKeep.Add(FactorNamesForVarying[FactorIndexToVaryMarkers]);

            foreach (string factorToRemove in GetFactorList(simulationZones).Except(factorsToKeep))
                simulationZones = RemoveFactorAndMerge(simulationZones, factorToRemove);

            // Ensure all simulation zones have the factor we're keeping.
            foreach (SimulationZone simZone in simulationZones)
                factorsToKeep.ForEach(f => simZone.AddFactor(f, "?"));

            return simulationZones;
        }

        /// <summary>
        /// Remove the specified factor from all simulation/zone objects and then merge
        /// all identical objects.
        /// </summary>
        /// <param name="simulationZones"></param>
        /// <param name="factorToIgnore"></param>
        private List<SimulationZone> RemoveFactorAndMerge(List<SimulationZone> simulationZones, string factorToIgnore)
        {
            simulationZones.ForEach(simZone => simZone.RemoveFactor(factorToIgnore));
            List<SimulationZone> newList = simulationZones.Distinct().ToList();
            foreach (SimulationZone simZone in newList)
            {
                foreach (SimulationZone duplicate in simulationZones.FindAll(s => s.Equals(simZone)))
                    duplicate.simulationNames.ForEach(simName => simZone.AddSimulationName(simName));
            }
            return newList;
        }

        /// <summary>Find a parent to base our series on.</summary>
        private IModel FindParent()
        {
            Type[] parentTypesToMatch = new Type[] { typeof(Simulation), typeof(Zone), typeof(Experiment),
                                                     typeof(Folder), typeof(Simulations) };

            IModel obj = Parent;
            do
            {
                foreach (Type typeToMatch in parentTypesToMatch)
                    if (typeToMatch.IsAssignableFrom(obj.GetType()))
                        return obj;
                obj = obj.Parent;
            }
            while (obj != null);
            return obj;
        }

        /// <summary>
        /// Create graph definitions for the specified model
        /// </summary>
        /// <param name="model"></param>
        private List<SimulationZone> BuildListFromModel(IModel model)
        {
            List<SimulationZone> simulationZonePairs = new List<Models.Graph.Series.SimulationZone>();
            if (model is Simulation || typeof(Zone).IsAssignableFrom(model.GetType()))
                simulationZonePairs.AddRange(BuildListFromSimulation(model));
            else if (model is Experiment)
                simulationZonePairs.AddRange(BuildListFromExperiment(model));
            else
            {
                foreach (IModel child in model.Children)
                {
                    if (child is Simulation || child is Experiment || child is Folder)
                        simulationZonePairs.AddRange(BuildListFromModel(child));
                }
            }
            return simulationZonePairs;
        }

        /// <summary>
        /// Build a list of simulation / zone pairs from the specified simulation
        /// </summary>
        /// <param name="model">This can be either a simulation or a zone</param>
        /// <returns>A list of simulation / zone pairs</returns>
        private List<SimulationZone> BuildListFromSimulation(IModel model)
        {
            IModel simulation = Apsim.Parent(model, typeof(Simulation));
            List<SimulationZone> simulationZonePairs = new List<SimulationZone>();
            foreach (Zone zone in Apsim.ChildrenRecursively(model, typeof(Zone)))
                simulationZonePairs.Add(new SimulationZone(simulation.Name, zone.Name));

            if (typeof(Zone).IsAssignableFrom(model.GetType()))
                simulationZonePairs.Add(new SimulationZone(simulation.Name, model.Name));
            return simulationZonePairs;
        }

        /// <summary>
        /// Build a list of simulation / zone pairs from the specified experiment
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private List<SimulationZone> BuildListFromExperiment(IModel model)
        {
            List<SimulationZone> simulationZonePairs = new List<SimulationZone>();
            foreach (SimulationZone simulationZonePair in BuildListFromSimulation((model as Experiment).BaseSimulation))
            {
                foreach (List<FactorValue> combination in (model as Experiment).AllCombinations())
                {
                    string zoneName = simulationZonePair.factorNameValues.Find(factorValue => factorValue.Key == "Zone").Value;
                    SimulationZone simulationZone = new SimulationZone(null, zoneName);
                    string simulationName = model.Name;
                    foreach (FactorValue value in combination)
                    {
                        simulationName += value.Name;
                        string factorName = value.Factor.Name;
                        if (value.Factor.Parent is Factor)
                        {
                            factorName = value.Factor.Parent.Name;
                        }
                        string factorValue = value.Name.Replace(factorName, "");
                        simulationZone.factorNameValues.Add(new KeyValuePair<string, string>(factorName, factorValue));
                    }
                    simulationZone.factorNameValues.Add(new KeyValuePair<string, string>("Experiment", (model as Experiment).Name));
                    simulationZone.simulationNames.Add(simulationName);
                    simulationZonePairs.Add(simulationZone);
                }
            }
            return simulationZonePairs;
        }

        /// <summary>
        /// Add the text to the specified filter.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="textToAdd"></param>
        /// <returns></returns>
        private static string AddToFilter(string filter, string textToAdd)
        {
            if (textToAdd != null)
            {
                if (filter != null)
                    filter += " AND ";
                filter += textToAdd;
            }
            return filter;
        }

        /// <summary>
        /// Paint the visual elements (colour, line and marker) of all simulation / zone pairs.
        /// </summary>
        /// <param name="simulationZones">The simulation/zone pairs to change</param>
        private void PaintAllSimulationZones(List<SimulationZone> simulationZones)
        {
            // Create an appropriate painter object
            SimulationZonePainter.IPainter painter;
            if (FactorIndexToVaryColours != -1 && FactorIndexToVaryColours < FactorNamesForVarying.Count)
            {
                string factorNameToVaryByColours = FactorNamesForVarying[FactorIndexToVaryColours];
                if (FactorIndexToVaryLines != -1)
                    painter = new SimulationZonePainter.DualPainter() { FactorName = factorNameToVaryByColours,
                        MaximumIndex1 = ColourUtilities.Colours.Length,
                        MaximumIndex2 = Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                        Setter1 = VisualElements.SetColour,
                        Setter2 = VisualElements.SetLineType };
                else if (FactorIndexToVaryMarkers != -1)
                    painter = new SimulationZonePainter.DualPainter() { FactorName = factorNameToVaryByColours,
                        MaximumIndex1 = ColourUtilities.Colours.Length,
                        MaximumIndex2 = Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                        Setter1 = VisualElements.SetColour,
                        Setter2 = VisualElements.SetMarker };
                else
                    painter = new SimulationZonePainter.SequentialPainter() { FactorName = factorNameToVaryByColours,
                                                                              MaximumIndex = ColourUtilities.Colours.Length,
                                                                              Setter = VisualElements.SetColour };
            }
            else if (FactorIndexToVaryLines != -1 && FactorIndexToVaryLines < FactorNamesForVarying.Count)
            {
                string factorNameToVaryByLine = FactorNamesForVarying[FactorIndexToVaryLines];
                painter = new SimulationZonePainter.SequentialPainter() { FactorName = factorNameToVaryByLine,
                                                                          MaximumIndex = Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type   
                                                                          Setter = VisualElements.SetLineType };
            }
            else if (FactorIndexToVaryMarkers != -1 && FactorIndexToVaryMarkers < FactorNamesForVarying.Count)
            {
                string factorNameToVaryByMarker = FactorNamesForVarying[FactorIndexToVaryMarkers];
                painter = new SimulationZonePainter.SequentialPainter() { FactorName = factorNameToVaryByMarker,
                                                                          MaximumIndex = Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                                                                          Setter = VisualElements.SetMarker };
            }
            else
                painter = new SimulationZonePainter.DefaultPainter() { Colour = Colour, LineType = Line, MarkerType = Marker };

            // Apply the painter to all simulation zone objects.
            foreach (SimulationZone simZone in simulationZones)
            {
                simZone.visualElement = new VisualElements();
                simZone.visualElement.colour = Colour;
                simZone.visualElement.Line = Line;
                simZone.visualElement.LineThickness = LineThickness;
                simZone.visualElement.Marker = Marker;
                simZone.visualElement.MarkerSize = MarkerSize;
                painter.PaintSimulationZone(simZone);
            }
        }

        /// <summary>Convert a simulation zone object into a series definition</summary>
        /// <param name="simulationZone">The object to convert</param>
        private SeriesDefinition ConvertToSeriesDefinition(SimulationZone simulationZone)
        {
            SeriesDefinition seriesDefinition = new Models.Graph.SeriesDefinition();
            seriesDefinition.type = Type;
            seriesDefinition.marker = simulationZone.visualElement.Marker;
            seriesDefinition.line = simulationZone.visualElement.Line;
            seriesDefinition.markerSize = simulationZone.visualElement.MarkerSize;
            seriesDefinition.lineThickness = simulationZone.visualElement.LineThickness;
            seriesDefinition.colour = simulationZone.visualElement.colour;
            seriesDefinition.xFieldName = XFieldName;
            seriesDefinition.yFieldName = YFieldName;
            seriesDefinition.xAxis = XAxis;
            seriesDefinition.yAxis = YAxis;
            seriesDefinition.showInLegend = ShowInLegend;
            seriesDefinition.title = simulationZone.GetSeriesTitle();
            if (IncludeSeriesNameInLegend)
                seriesDefinition.title += ": " + Name;
            if (simulationZone.data.Count > 0)
            {
                seriesDefinition.data = simulationZone.data.ToTable();
                seriesDefinition.x = GetDataFromTable(seriesDefinition.data, XFieldName);
                seriesDefinition.y = GetDataFromTable(seriesDefinition.data, YFieldName);
                seriesDefinition.x2 = GetDataFromTable(seriesDefinition.data, X2FieldName);
                seriesDefinition.y2 = GetDataFromTable(seriesDefinition.data, Y2FieldName);
                if (Cumulative)
                    seriesDefinition.y = MathUtilities.Cumulative(seriesDefinition.y as IEnumerable<double>);
                if (CumulativeX)
                    seriesDefinition.x = MathUtilities.Cumulative(seriesDefinition.x as IEnumerable<double>);
            }
            return seriesDefinition;
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

            return definition;
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
                    {
                        IModel parentOfGraph = this.Parent.Parent;
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
                return Apsim.Get(this, fieldName) as IEnumerable;
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

        /// <summary>
        /// Create a data view from the specified table and filter.
        /// </summary>
        /// <param name="dataStore">The datastore to read from.</param>
        /// <param name="simulationZones">The list of simulation / zone pairs.</param>
        private DataTable GetBaseData(DataStore dataStore, List<SimulationZone> simulationZones)
        {
            // Get a list of all simulation names in all simulationZones.
            List<string> simulationNames = new List<string>();
            simulationZones.ForEach(sim => simulationNames.AddRange(sim.simulationNames));

            string filter = null;
            foreach (string simulationName in simulationNames.Distinct())
            {
                if (filter != null)
                    filter += ",";
                filter += "'" + simulationName + "'";
            }
            filter = "SimulationName in (" + filter + ")";
            if (Filter != string.Empty)
                filter = AddToFilter(filter, Filter);

            List<string> fieldNames = new List<string>();
            if (dataStore.ColumnNames(TableName).Contains("Zone"))
                fieldNames.Add("Zone");
            fieldNames.Add(XFieldName);
            fieldNames.Add(YFieldName);
            if (X2FieldName != null && !fieldNames.Contains(X2FieldName))
                fieldNames.Add(X2FieldName);
            if (Y2FieldName != null && !fieldNames.Contains(Y2FieldName))
                fieldNames.Add(Y2FieldName);

            return dataStore.GetFilteredData(TableName, fieldNames.ToArray(), filter);
        }

        /// <summary>This class encapsulates a simulation / zone pair to put onto graph</summary>
        private class SimulationZone : IEquatable<SimulationZone>
        {
            public List<string> simulationNames = new List<string>();
            public List<KeyValuePair<string, string>> factorNameValues = new List<KeyValuePair<string, string>>();
            public DataView data;
            public VisualElements visualElement;

            /// <summary>Constructor</summary>
            /// <param name="simulationName"></param>
            /// <param name="zoneName"></param>
            internal SimulationZone(string simulationName, string zoneName)
            {
                if (simulationName != null)
                    simulationNames.Add(simulationName);
                factorNameValues.Add(new KeyValuePair<string, string>("Zone", zoneName));
            }

            /// <summary>Add a simulation name if it doesn't already exist.</summary>
            /// <param name="simulationName"></param>
            internal void AddSimulationName(string simulationName)
            {
                if (!simulationNames.Contains(simulationName))
                    simulationNames.Add(simulationName);
            }

            /// <summary>
            /// Create a data view from the specified table and filter.
            /// </summary>
            /// <param name="baseData">The datastore to read from.</param>
            /// <param name="series">The parent series.</param>
            internal void CreateDataView(DataTable baseData, Series series)
            {
                string filter = null;
                foreach (string simulationName in simulationNames)
                {
                    if (filter != null)
                        filter += ",";
                    filter += "'" + simulationName + "'";
                }
                filter = "SimulationName in (" + filter + ")";
                string zoneName = GetValueOf("Zone");
                if (zoneName != "?")
                    filter = AddToFilter(filter, "Zone='" + zoneName + "'");

                data = new DataView(baseData);
                data.RowFilter = filter;
            }

            /// <summary>
            /// Get the value of a factor.
            /// </summary>
            /// <param name="factorName"></param>
            /// <returns></returns>
            internal string GetValueOf(string factorName)
            {
                KeyValuePair<string, string> factorPair = factorNameValues.Find(f => f.Key == factorName);
                if (factorPair.Key == factorName)
                    return factorPair.Value;
                return "?";
            }

            /// <summary>Get a series title to put on the legend.</summary>
            internal string GetSeriesTitle()
            {
                string title = null;
                factorNameValues.ForEach(f => title += f.Value);
                return title;
            }

            /// <summary>Remove the specified factor if it exists.</summary>
            /// <param name="factorName">Name of factor to remove</param>
            internal void RemoveFactor(string factorName)
            {
                KeyValuePair<string, string> factorPair = factorNameValues.Find(f => f.Key == factorName);
                if (factorPair.Key == factorName)
                    factorNameValues.Remove(factorPair);
            }

            /// <summary>Add a factor if it doesn't already exist.</summary>
            /// <param name="factorName"></param>
            /// <param name="factorValue"></param>
            internal void AddFactor(string factorName, string factorValue)
            {
                KeyValuePair<string, string> factorPair = factorNameValues.Find(f => f.Key == factorName);
                if (factorPair.Key != factorName)
                    factorNameValues.Add(new KeyValuePair<string, string>(factorName, factorValue));
            }

            /// <summary>
            /// Equality comparer. 
            /// </summary>
            /// <param name="obj2">The object we're to compare with.</param>
            /// <returns>Returns true other is equal to this object</returns>
            public bool Equals(SimulationZone obj2)
            {
                if (factorNameValues.Count != obj2.factorNameValues.Count)
                    return false;
                for (int i = 0; i < factorNameValues.Count; i++)
                {
                    if (factorNameValues[i].Key != obj2.factorNameValues[i].Key ||
                        factorNameValues[i].Value != obj2.factorNameValues[i].Value)
                        return false;
                }
                return true;
            }

            /// <summary>
            /// Get hash code for SimulationZone pair.
            /// </summary>
            public override int GetHashCode()
            {
                int hash = 0;
                for (int i = 0; i < factorNameValues.Count; i++)
                {
                    hash += factorNameValues[i].Key.GetHashCode() + factorNameValues[i].Value.GetHashCode();
                }
                return hash;
            }

        }
        /// <summary>
        /// Represents the visual elements of a series.
        /// </summary>
        class VisualElements
        {
            /// <summary>Gets or set the colour</summary>
            public Color colour { get; set; }

            /// <summary>Gets or sets the marker size</summary>
            public MarkerType Marker { get; set; }

            /// <summary>Marker size.</summary>
            public MarkerSizeType MarkerSize { get; set; }

            /// <summary>Gets or sets the line type to show</summary>
            public LineType Line { get; set; }

            /// <summary>Gets or sets the line thickness</summary>
            public LineThicknessType LineThickness { get; set; }

            /// <summary>A static setter function for colour from an index</summary>
            /// <param name="visualElement">The visual element to change</param>
            /// <param name="index">The index</param>
            public static void SetColour(VisualElements visualElement, int index)
            {
                visualElement.colour = ColourUtilities.Colours[index];
            }

            /// <summary>A static setter function for line type from an index</summary>
            /// <param name="visualElement">The visual element to change</param>
            /// <param name="index">The index</param>
            public static void SetLineType(VisualElements visualElement, int index)
            {
                visualElement.Line = (LineType)Enum.GetValues(typeof(LineType)).GetValue(index);
            }

            /// <summary>A static setter function for marker from an index</summary>
            /// <param name="visualElement">The visual element to change</param>
            /// <param name="index">The index</param>
            public static void SetMarker(VisualElements visualElement, int index)
            {
                visualElement.Marker = (MarkerType)Enum.GetValues(typeof(MarkerType)).GetValue(index);
            }
        }

        /// <summary>
        /// This class paints (sets visual elements) of a group of simulation zone objects.
        /// </summary>
        class SimulationZonePainter
        {
            /// <summary>A delegate setter function.</summary>
            /// <param name="visualElement">The visual element to change</param>
            /// <param name="index">The index</param>
            public delegate void SetFunction(VisualElements visualElement, int index);

            /// <summary>A painter interface for setting visual elements of a simulation/zone pair</summary>
            public interface IPainter
            {
                void PaintSimulationZone(SimulationZone simulationZonePair);
            }

            /// <summary>A default painter for setting a simulation / zone pair to default values.</summary>
            public class DefaultPainter : IPainter
            {
                public Color Colour { get; set; }
                public LineType LineType { get; set; }
                public MarkerType MarkerType { get; set; }
                public void PaintSimulationZone(SimulationZone simulationZonePair)
                {
                    simulationZonePair.visualElement.colour = Colour;
                    simulationZonePair.visualElement.Line = LineType;
                    simulationZonePair.visualElement.Marker = MarkerType;
                }
            }

            /// <summary>A painter for setting a simulation / zone pair to consecutive values of a visual element.</summary>
            public class SequentialPainter : IPainter
            {
                private List<string> values = new List<string>();
                public string FactorName { get; set; }
                public int MaximumIndex { get; set; }
                public SetFunction Setter { get; set; }

                public void PaintSimulationZone(SimulationZone simulationZonePair)
                {
                    string factorValue = simulationZonePair.GetValueOf(FactorName);
                    int index = values.IndexOf(factorValue);
                    if (index == -1)
                    {
                        values.Add(factorValue);
                        index = values.Count - 1;
                    }
                    index = index % MaximumIndex;
                    Setter(simulationZonePair.visualElement, index);
                }
            }

            /// <summary>A painter for setting a simulation / zone pair to consecutive values of two visual elements.</summary>
            public class DualPainter : IPainter
            {
                private List<string> values = new List<string>();

                public int MaximumIndex1 { get; set; }
                public int MaximumIndex2 { get; set; }
                public string FactorName { get; set; }
                public SetFunction Setter1 { get; set; }
                public SetFunction Setter2 { get; set; }

                public void PaintSimulationZone(SimulationZone simulationZonePair)
                {
                    string factorValue = simulationZonePair.GetValueOf(FactorName);

                    int index1 = values.IndexOf(factorValue);
                    if (index1 == -1)
                    {
                        values.Add(factorValue);
                        index1 = values.Count - 1;
                    }
                    int index2 = index1 / MaximumIndex1;
                    index2 = index2 % MaximumIndex2;
                    index1 = index1 % MaximumIndex1;
                    Setter1(simulationZonePair.visualElement, index1);
                    Setter2(simulationZonePair.visualElement, index2);
                }
            }
        }

    }
}
