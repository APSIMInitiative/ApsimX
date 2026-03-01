using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;
using Newtonsoft.Json;

namespace Models
{

    /// <summary>The class represents a single series on a graph</summary>
    [ValidParent(ParentType = typeof(Graph))]
    [ViewName("UserInterface.Views.SeriesView")]
    [PresenterName("UserInterface.Presenters.SeriesPresenter")]
    [Serializable]
    public class Series : Model, IGraphable, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>Constructor for a series</summary>
        public Series()
        {
            this.XAxis = AxisPosition.Bottom;
        }

        /// <summary>Gets or sets the series type</summary>
        public SeriesType Type { get; set; }

        /// <summary>Gets or sets the associated x axis</summary>
        public AxisPosition XAxis { get; set; }

        /// <summary>Gets or sets the associated y axis</summary>
        public AxisPosition YAxis { get; set; }

        /// <summary>
        /// Gets or sets the color represented as a red, green, blue integer
        /// </summary>
        public int ColourArgb { get; set; }

        /// <summary>Gets or sets the color</summary>
        [JsonIgnore]
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
        public MarkerSize MarkerSize { get; set; }

        /// <summary>Gets or sets the line type to show</summary>
        public LineType Line { get; set; }

        /// <summary>Gets or sets the line thickness</summary>
        public LineThickness LineThickness { get; set; }

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
        public IEnumerable<string> GetDescriptorNames(IStorageReader reader)
        {
            var simulationDescriptions = GraphPage.FindSimulationDescriptions(this);
            var names = new List<string>();
            foreach (var simulationDescription in simulationDescriptions)
                names.AddRange(simulationDescription.Descriptors.Select(d => d.Name));
            names.Add("Graph series");

            // Add all string and integer fields to descriptor names.
            foreach (var column in reader.GetColumns(TableName))
                if (column.Item2 == typeof(string) || column.Item2 == typeof(int))
                    if (column.Item1 != "CheckpointID" && column.Item1 != "SimulationID")
                        names.Add(column.Item1);

            return names.Distinct();
        }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="reader">A storage reader.</param>
        /// <param name="simulationDescriptions">A list of simulation descriptions that are in scope.</param>
        /// <param name="simulationFilter"></param>
        public IEnumerable<SeriesDefinition> CreateSeriesDefinitions(IStorageReader reader,
                                                                     List<SimulationDescription> simulationDescriptions,
                                                                     List<string> simulationFilter = null)
        {
            var seriesDefinitions = new List<SeriesDefinition>();

            // If this series doesn't have a table name then it must be getting its data from other models.
            if (TableName == null)
                seriesDefinitions.Add(new SeriesDefinition(this, "Current", colModifier: 0, markerModifier: 0));
            else
            {
                int checkpointNumber = 0;
                foreach (var checkpointName in reader.CheckpointNames)
                {
                    if (checkpointName == "Current" || reader.GetCheckpointShowOnGraphs(checkpointName))
                    {
                        // Colour modifier can be in range [-1, 1] but we will
                        // use only 0..1 so that we start with the normal colour
                        // and gradually get brighter.
                        double colourModifier = (double)checkpointNumber / reader.CheckpointNames.Count;
                        double markerModifier = 1 + 1.0 * checkpointNumber / reader.CheckpointNames.Count;

                        // TableName exists so get the vary by fields and the simulation descriptions.
                        var varyByFieldNames = GetVaryByFieldNames();
                        if (simulationFilter == null)
                            simulationFilter = simulationDescriptions.Select(d => d.Name).Distinct().ToList();

                        var inScopeSimulationNames = CreateInScopeWhereClause(reader, simulationFilter);

                        if (varyByFieldNames.Count == 0 || varyByFieldNames.Contains("Graph series"))
                        {
                            // No vary by fields. Just plot the whole table in a single
                            // series with data that is in scope.
                            seriesDefinitions.Add(new SeriesDefinition(this, checkpointName, colourModifier, markerModifier, inScopeSimulationNames, Filter));
                        }
                        else
                        {
                            // There are one or more vary by fields. Create series definitions
                            // for each combination of vary by fields.
                            seriesDefinitions.AddRange(CreateDefinitionsUsingVaryBy(varyByFieldNames, checkpointName, colourModifier, markerModifier, simulationDescriptions, inScopeSimulationNames));
                        }

                        // If we don't have any definitions then see if the vary by fields
                        // refer to string fields in the database table.
                        if (seriesDefinitions.Count == 0)
                            seriesDefinitions = CreateDefinitionsFromFieldInTable(reader, checkpointName, colourModifier, markerModifier, varyByFieldNames, inScopeSimulationNames);

                        // Paint all definitions.
                        var painter = GetSeriesPainter();
                        foreach (var seriesDefinition in seriesDefinitions)
                            painter.Paint(seriesDefinition);

                        checkpointNumber++;
                    }
                }
            }
            return seriesDefinitions;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="reader">A storage reader.</param>
        /// <param name="simulationDescriptions">A list of simulation descriptions.</param>
        /// <param name="seriesDefinitions">A list of series definitions.</param>
        /// <param name="simulationFilter"></param>
        public IEnumerable<SeriesDefinition> CreateChildSeriesDefinitions(IStorageReader reader, List<SimulationDescription> simulationDescriptions,
                                                 IEnumerable<SeriesDefinition> seriesDefinitions,
                                                 List<string> simulationFilter = null)
        {
            // We might have child models that want to add to our series definitions e.g. regression.
            foreach (IGraphable graphable in Structure.FindChildren<IGraphable>())
            {
                try
                {
                    if (graphable is ICachableGraphable cachable)
                        return cachable.GetSeriesToPutOnGraph(reader, seriesDefinitions, simulationFilter);
                    else
                        return graphable.CreateSeriesDefinitions(reader, simulationDescriptions, simulationFilter);
                }
                catch (Exception err)
                {
                    throw new Exception($"Unable to render graphable object {graphable.FullPath}", err);
                }
            }
            return Enumerable.Empty<SeriesDefinition>();
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        public IEnumerable<IAnnotation> GetAnnotations()
        {
            // We might have child models that wan't to add to the annotations e.g. regression.
            return Structure.FindChildren<IGraphable>().Where(g => g.Enabled).SelectMany(g => g.GetAnnotations());
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
        /// <param name="checkpointName">The checkpoint name.</param>
        /// <param name="colourModifier">Checkpoint colour modifer.</param>
        /// <param name="markerModifier">Checkpoint marker size modifier.</param>
        /// <param name="varyByFieldNames">The vary by fields.</param>
        /// <param name="inScopeSimulationNames">An SQL WHERE clause for rows that are in scope.</param>
        private List<SeriesDefinition> CreateDefinitionsFromFieldInTable(IStorageReader reader, string checkpointName, double colourModifier, double markerModifier, List<string> varyByFieldNames, IEnumerable<string> inScopeSimulationNames)
        {
            List<SeriesDefinition> definitions = new List<SeriesDefinition>();

            var fieldsThatExist = reader.ColumnNames(TableName);
            var varyByThatExistInTable = varyByFieldNames.Where(v => fieldsThatExist.Contains(v)).ToList();

            var validValuesForEachVaryByField = new List<List<string>>();
            foreach (var varyByFieldName in varyByThatExistInTable)
            {
                var data = reader.GetData(TableName,
                                            fieldNames: new string[] { varyByFieldName },
                                            simulationNames: inScopeSimulationNames,
                                            distinct: true);
                var values = DataTableUtilities.GetColumnAsStrings(data, varyByFieldName, CultureInfo.InvariantCulture).Distinct().ToList();
                validValuesForEachVaryByField.Add(values);
            }

            foreach (var combination in MathUtilities.AllCombinationsOf(validValuesForEachVaryByField.ToArray(), reverse: true))
            {
                var descriptors = new List<SimulationDescription.Descriptor>();
                for (int i = 0; i < combination.Count; i++)
                    descriptors.Add(new SimulationDescription.Descriptor(varyByThatExistInTable[i],
                                                                         combination[i]));
                definitions.Add(new SeriesDefinition(this, checkpointName, colourModifier, markerModifier, inScopeSimulationNames, Filter, descriptors));
            }

            return definitions;
        }

        /// <summary>
        /// Create an SQL WHERE clause for rows that are in scope.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="simulationFilter">The names of simulatiosn that are in scope.</param>
        private IEnumerable<string> CreateInScopeWhereClause(IStorageReader reader, List<string> simulationFilter)
        {
            var fieldsThatExist = reader.ColumnNames(TableName);
            if (fieldsThatExist.Contains("SimulationID") || fieldsThatExist.Contains("SimulationName"))
            {
                // Extract all the simulation names from all descriptions.
                return simulationFilter.Distinct();
            }
            else
                return null;
        }

        /// <summary>
        /// Create and return a list of series definitions for each group by field.
        /// </summary>
        /// <param name="varyByFieldNames">The vary by fields</param>
        /// <param name="checkpointName">Checkpoint name.</param>
        /// <param name="colourModifier">Checkpoint colour modifier.</param>
        /// <param name="markerModifier">Checkpoint marker size modifier.</param>
        /// <param name="simulationDescriptions">The simulation descriptions that are in scope.</param>
        /// <param name="whereClauseForInScopeData">An SQL WHERE clause for rows that are in scope.</param>
        private List<SeriesDefinition> CreateDefinitionsUsingVaryBy(List<string> varyByFieldNames,
                                                                    string checkpointName,
                                                                    double colourModifier,
                                                                    double markerModifier,
                                                                    List<SimulationDescription> simulationDescriptions,
                                                                    IEnumerable<string> whereClauseForInScopeData)
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
                    if (varyByFieldNames.Contains(descriptor.Name) || NeedsDescriptor(descriptor))
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
                                                         checkpointName,
                                                         colourModifier,
                                                         markerModifier,
                                                         whereClauseForInScopeData,
                                                         Filter,
                                                         descriptorsForDefinition));
                }
            }

            return definitions;
        }

        /// <summary>
        /// Check if the given descriptor is needed by the series definition.
        /// </summary>
        /// <param name="descriptor">The descriptor to be checked.</param>
        private bool NeedsDescriptor(SimulationDescription.Descriptor descriptor)
        {
            // We need a simulation name descriptor if any child event names
            // on graph components exist and require it.
            // todo: should probably add this into the IGraphable interface.
            if (descriptor.Name == "SimulationName")
            {
                EventNamesOnGraph events = Node.FindChild<EventNamesOnGraph>();
                if (events != null && !string.IsNullOrEmpty(events.SimulationName))
                    return true;
            }
            return false;
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
