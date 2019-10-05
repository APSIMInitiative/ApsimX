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
    using Storage;
    using Models.Core.Run;
    using Models.CLEM;

    /// <summary>The class represents a single series on a graph</summary>
    [ValidParent(ParentType = typeof(Graph))]
    [ViewName("UserInterface.Views.SeriesView")]
    [PresenterName("UserInterface.Presenters.SeriesPresenter")]
    [Serializable]
    public class Series : Model, IGraphable
    {
        private List<SimulationDescription> simulationDescriptions;

        /// <summary>Constructor for a series</summary>
        public Series()
        {
            this.Checkpoint = "Current";
            this.XAxis = Axis.AxisType.Bottom;
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

        /// <summary>The factor to vary for colours.</summary>
        public string FactorToVaryColours { get; set; }

        /// <summary>The factor to vary for markers types.</summary>
        public string FactorToVaryMarkers { get; set; }

        /// <summary>The factor to vary for line types.</summary>
        public string FactorToVaryLines { get; set; }

        /// <summary>Gets or sets the marker size</summary>
        public MarkerType Marker { get; set; }

        /// <summary>Marker size.</summary>
        public MarkerSizeType MarkerSize { get; set; }

        /// <summary>Gets or sets the line type to show</summary>
        public LineType Line { get; set; }

        /// <summary>Gets or sets the line thickness</summary>
        public LineThicknessType LineThickness { get; set; }

        /// <summary>Gets or sets the checkpoint to get data from.</summary>
        public string Checkpoint { get; set; }

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
        
        /// <summary>A list of all descriptor names that can be listed as 'vary by' in markers/line types etc.</summary>
        public IEnumerable<string> GetDescriptorNames()
        {
            var names = new List<string>();
            foreach (var simulationDescription in FindSimulationDescriptions())
                names.AddRange(simulationDescription.Descriptors.Select(d => d.Name));
            names.Add("Graph series");
            return names.Distinct();
        }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        /// <param name="reader">A storage reader.</param>
        /// <param name="simulationFilter"></param>
        public void GetSeriesToPutOnGraph(IStorageReader reader, List<SeriesDefinition> definitions, List<string> simulationFilter = null)
        {
            List<SeriesDefinition> seriesDefinitions = new List<SeriesDefinition>();

            // If this series doesn't have a table name then it must be getting its data from other models.
            if (TableName == null)
            {
                seriesDefinitions.Add(new SeriesDefinition(this));
                seriesDefinitions[0].ReadData(reader, simulationDescriptions);
            }
            else
            {
                // TableName exists so get the vary by fields and the simulation descriptions.
                var varyByFieldNames = GetVaryByFieldNames();
                simulationDescriptions = FindSimulationDescriptions();
                if (simulationFilter == null)
                    simulationFilter = simulationDescriptions.Select(d => d.Name).Distinct().ToList();

                var whereClauseForInScopeData = CreateInScopeWhereClause(reader, simulationFilter);

                if (varyByFieldNames.Count == 0 || varyByFieldNames.Contains("Graph series"))
                {
                    // No vary by fields. Just plot the whole table in a single
                    // series with data that is in scope.
                    seriesDefinitions = new List<SeriesDefinition>() { new SeriesDefinition(this, whereClauseForInScopeData, Filter) };
                }
                else
                {
                    // There are one or more vary by fields. Create series definitions
                    // for each combination of vary by fields.
                    seriesDefinitions = CreateDefinitionsUsingVaryBy(varyByFieldNames, simulationDescriptions, whereClauseForInScopeData);
                }

                // If we don't have any definitions then see if the vary by fields
                // refer to string fields in the database table.
                if (seriesDefinitions.Count == 0)
                    seriesDefinitions = CreateDefinitionsFromFieldInTable(reader, varyByFieldNames, whereClauseForInScopeData);

                // Paint all definitions. 
                var painter = GetSeriesPainter();
                foreach (var seriesDefinition in seriesDefinitions)
                    painter.Paint(seriesDefinition);

                // Tell each series definition to read its data.
                foreach (var seriesDefinition in seriesDefinitions)
                    seriesDefinition.ReadData(reader, simulationDescriptions);

                // Remove series that have no data.
                seriesDefinitions.RemoveAll(d => !MathUtilities.ValuesInArray(d.X) || !MathUtilities.ValuesInArray(d.Y));
            }

            // We might have child models that want to add to our series definitions e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetSeriesToPutOnGraph(reader, seriesDefinitions);

            definitions.AddRange(seriesDefinitions);
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        /// <param name="annotations">A list of annotations to add to.</param>
        public void GetAnnotationsToPutOnGraph(List<Annotation> annotations)
        {
            // We might have child models that wan't to add to the annotations e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetAnnotationsToPutOnGraph(annotations);
        }

        /// <summary>Return a list of extra fields that the definition should read.</summary>
        /// <param name="seriesDefinition">The calling series definition.</param>
        /// <returns>A list of fields - never null.</returns>
        public IEnumerable<string> GetExtraFieldsToRead(SeriesDefinition seriesDefinition)
        {
            return new string[0];
        }

        /// <summary>
        /// Create series definitions assuming the vary by fields are text fields in the table.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="varyByFieldNames">The vary by fields.</param>
        /// <param name="whereClauseForInScopeData">An SQL WHERE clause for rows that are in scope.</param>
        private List<SeriesDefinition> CreateDefinitionsFromFieldInTable(IStorageReader reader, List<string> varyByFieldNames, string whereClauseForInScopeData)
        {
            List<SeriesDefinition> definitions = new List<SeriesDefinition>();

            var fieldsThatExist = reader.ColumnNames(TableName);
            var varyByThatExistInTable = varyByFieldNames.Where(v => fieldsThatExist.Contains(v)).ToList();

            var validValuesForEachVaryByField = new List<List<string>>();
            foreach (var varyByFieldName in varyByThatExistInTable)
            {
                var data = reader.GetData(TableName, 
                                            fieldNames: new string[] { varyByFieldName },
                                            filter: whereClauseForInScopeData,
                                            distinct: true);
                var values = DataTableUtilities.GetColumnAsStrings(data, varyByFieldName).Distinct().ToList();
                validValuesForEachVaryByField.Add(values);
            }

            foreach (var combination in MathUtilities.AllCombinationsOf(validValuesForEachVaryByField.ToArray(), reverse:true))
            {
                var descriptors = new List<SimulationDescription.Descriptor>();
                for (int i = 0; i < combination.Count; i++)
                    descriptors.Add(new SimulationDescription.Descriptor(varyByThatExistInTable[i], 
                                                                         combination[i]));
                definitions.Add(new SeriesDefinition(this, whereClauseForInScopeData, Filter, descriptors));
            }

            return definitions;
        }

        /// <summary>
        /// Create an SQL WHERE clause for rows that are in scope.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="simulationFilter">The names of simulatiosn that are in scope.</param>
        private string CreateInScopeWhereClause(IStorageReader reader, List<string> simulationFilter)
        {
            var fieldsThatExist = reader.ColumnNames(TableName);
            if (fieldsThatExist.Contains("SimulationID") || fieldsThatExist.Contains("SimulationName"))
            {
                // Extract all the simulation names from all descriptions.
                var simulationNames = simulationFilter.Distinct(); 

                string whereClause =  "SimulationName IN (" +
                                      StringUtilities.Build(simulationNames, ",", "'", "'") +
                                      ")";
                return whereClause;
            }
            else if (Filter != string.Empty)
                return Filter;
            else
                return null;
        }

        /// <summary>
        /// Create and return a list of series definitions for each group by field.
        /// </summary>
        /// <param name="varyByFieldNames">The vary by fields</param>
        /// <param name="simulationDescriptions">The simulation descriptions that are in scope.</param>
        /// <param name="whereClauseForInScopeData">An SQL WHERE clause for rows that are in scope.</param>
        private List<SeriesDefinition> CreateDefinitionsUsingVaryBy(List<string> varyByFieldNames, 
                                                                    List<SimulationDescription> simulationDescriptions,
                                                                    string whereClauseForInScopeData)
        {
            SplitDescriptionsWithSameDescriptors(simulationDescriptions);

            var definitions = new List<SeriesDefinition>();
            foreach (var simulationDescription in simulationDescriptions)
            {
                // Determine the descriptors to pass to the new definition that will
                // be created below. We only want to pass the 'vary by' descriptors.
                var descriptorsForDefinition = new List<SimulationDescription.Descriptor>();
                foreach (var descriptor in simulationDescription.Descriptors)
                {
                    if (varyByFieldNames.Contains(descriptor.Name))
                        descriptorsForDefinition.Add(descriptor);
                }

                // Try and find a definition that has the same descriptors.
                var foundDefinition = definitions.Find(d => SimulationDescription.Equals(d.Descriptors, descriptorsForDefinition));

                // Only create a definition if there are descriptors and there isn't
                // already a definition with the same descriptors.
                if (descriptorsForDefinition.Count > 0 && foundDefinition == null)
                {
                    // Create the definition.
                    definitions.Add(new SeriesDefinition(this,
                                                         whereClauseForInScopeData,
                                                         Filter,
                                                         descriptorsForDefinition));
                }
            }

            return definitions;
        }

        /// <summary>
        /// If a simulation description has the same descriptor more than once,
        /// split it into multiple descriptions.
        /// </summary>
        /// <remarks>
        /// A simulation description can have multiple zones
        /// e.g.
        ///    Sim1 Descriptors: SimulationName=abc, Zone=field1, Zone=field2, x=1, x=2
        /// Need to split this into 4 separate simulation descriptions:
        ///    Sim1 Descriptors: SimulationName=abc, Zone=field1, x=1
        ///    Sim2 Descriptors: SimulationName=abc, Zone=field1, x=2
        ///    Sim3 Descriptors: SimulationName=abc, Zone=field2, x=1
        ///    Sim4 Descriptors: SimulationName=abc, Zone=field2f, x=2
        /// </remarks>
        /// <param name="simulationDescriptions">Simulation descriptions.</param>
        private void SplitDescriptionsWithSameDescriptors(List<SimulationDescription> simulationDescriptions)
        {
            var newList = new List<SimulationDescription>();
            foreach (var simulationDescription in simulationDescriptions)
            {
                var descriptors = new List<List<SimulationDescription.Descriptor>>();
                var descriptorGroups = simulationDescription.Descriptors.GroupBy(d => d.Name);
                foreach (var group in descriptorGroups)
                    descriptors.Add(group.ToList());

                var allCombinations = MathUtilities.AllCombinationsOf(descriptors.ToArray());
                foreach (var combination in allCombinations)
                {
                    newList.Add(new SimulationDescription(null, simulationDescription.Name)
                    {
                        Descriptors = combination
                    });
                }
            }
            simulationDescriptions.Clear();
            simulationDescriptions.AddRange(newList);
        }

        /// <summary>
        /// Find and return a list of all simulation descriptions.
        /// </summary>
        public List<SimulationDescription> FindSimulationDescriptions()
        {
            // Find a parent that heads the scope that we're going to graph
            IModel parent = FindParent();

            List<SimulationDescription> simulationDescriptions = null;
            do
            {
                // Create a list of all simulation/zone objects that we're going to graph.
                simulationDescriptions = GetSimulationDescriptionsUnderModel(parent);
                parent = parent.Parent;
            }
            while (simulationDescriptions.Count == 0 && parent != null);
            return simulationDescriptions;
        }

        /// <summary>
        /// Get a list of simulation descriptions that are a child of the specified model.
        /// </summary>
        /// <param name="model">The model and it's child models to scan.</param>
        private List<SimulationDescription> GetSimulationDescriptionsUnderModel(IModel model)
        {
            var simulationDescriptions = new List<SimulationDescription>();
            if (model is ISimulationDescriptionGenerator)
                simulationDescriptions.AddRange((model as ISimulationDescriptionGenerator).GenerateSimulationDescriptions());
            else
            {
                foreach (IModel child in model.Children)
                {
                    if (child is Simulation || child is ISimulationDescriptionGenerator || child is Folder)
                        simulationDescriptions.AddRange(GetSimulationDescriptionsUnderModel(child));
                }
            }
            return simulationDescriptions;
        }
 
        /// <summary>Find a parent to base our series on.</summary>
        private IModel FindParent()
        {
            Type[] parentTypesToMatch = new Type[] { typeof(Simulation), typeof(Zone), typeof(ZoneCLEM), typeof(Experiment),
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

        /// <summary>Get series definition painter.</summary>
        /// <returns>Painter. Never returns null.</returns>
        private ISeriesDefinitionPainter GetSeriesPainter()
        {
            ISeriesDefinitionPainter painter;
            if (FactorToVaryColours != null)
            {
                if (FactorToVaryLines == FactorToVaryColours && FactorToVaryMarkers == FactorToVaryColours)
                    painter = new SequentialPainter
                        (this, FactorToVaryColours,
                         ColourUtilities.Colours.Length,
                         Enum.GetValues(typeof(MarkerType)).Length - 1, // minus 1 to avoid None type
                         Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                         SeriesDefinition.SetColour,
                         SeriesDefinition.SetMarker,
                         SeriesDefinition.SetLineType);
                else if (FactorToVaryLines == FactorToVaryColours)
                    painter = new SequentialPainter
                        (this, FactorToVaryColours,
                         ColourUtilities.Colours.Length, Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                         SeriesDefinition.SetColour,
                         SeriesDefinition.SetLineType);
                else if (FactorToVaryMarkers == FactorToVaryColours)
                    painter = new SequentialPainter
                       (this, FactorToVaryColours,
                        ColourUtilities.Colours.Length,
                        Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                        SeriesDefinition.SetColour,
                        SeriesDefinition.SetMarker);
                else if (FactorToVaryLines != null && FactorToVaryMarkers != null)
                    painter = new MultiDescriptorPainter
                       (FactorToVaryColours, FactorToVaryLines, FactorToVaryMarkers,
                        ColourUtilities.Colours.Length,
                        Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                        Enum.GetValues(typeof(MarkerType)).Length - 1, // minus 1 to avoid None type
                        SeriesDefinition.SetColour,
                        SeriesDefinition.SetLineType,
                        SeriesDefinition.SetMarker);

                else if (FactorToVaryLines != null)
                    painter = new MultiDescriptorPainter
                       (FactorToVaryColours,
                        FactorToVaryLines,
                        ColourUtilities.Colours.Length,
                        Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                        SeriesDefinition.SetColour,
                        SeriesDefinition.SetLineType);
                else if (FactorToVaryMarkers != null)
                    painter = new MultiDescriptorPainter
                       (FactorToVaryColours,
                        FactorToVaryMarkers,
                        ColourUtilities.Colours.Length,
                        Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                        SeriesDefinition.SetColour,
                        SeriesDefinition.SetMarker);
                else
                    painter = new SequentialPainter
                       (this, FactorToVaryColours,
                        ColourUtilities.Colours.Length,
                        SeriesDefinition.SetColour);
            }
            else if (FactorToVaryLines != null)
            {
                painter = new SequentialPainter
                   (this, FactorToVaryLines,
                    Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type   
                    SeriesDefinition.SetLineType);
            }
            else if (FactorToVaryMarkers != null)
            {
                painter = new SequentialPainter
                   (this, FactorToVaryMarkers,
                    Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                    SeriesDefinition.SetMarker);
            }
            else
                painter = new DefaultPainter(Colour, Line, Marker);
            return painter;
        }

        /// <summary>Return a list of field names that this series is varying.</summary>
        private List<string> GetVaryByFieldNames()
        {
            var groupByFieldNames = new List<string>();
            if (FactorToVaryColours != null)
                groupByFieldNames.Add(FactorToVaryColours);
            if (FactorToVaryLines != null)
                groupByFieldNames.Add(FactorToVaryLines);
            if (FactorToVaryMarkers != null)
                groupByFieldNames.Add(FactorToVaryMarkers);
            groupByFieldNames = groupByFieldNames.Distinct().ToList();
            return groupByFieldNames;
        }
    }
}
