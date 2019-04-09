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
        public void GetSeriesToPutOnGraph(IStorageReader reader, List<SeriesDefinition> definitions)
        {
            List<SeriesDefinition> ourDefinitions = new List<SeriesDefinition>();

            // If this series doesn't have a table name then it must be getting its data from other models.
            if (TableName == null)
                ourDefinitions.Add(CreateDefinition(Name, null, Colour, Marker, Line, null, reader));
            else
            {
                var simulationDescriptions = FindSimulationDescriptions();

                // Only keep the simulation descriptions that we are varying.
                RemoveUnnessaryDescriptionsAndDescriptors(simulationDescriptions);

                SplitDescriptionsWithSameDescriptors(simulationDescriptions);

                // Remove simulation descriptions that have the same descriptors.
                simulationDescriptions = simulationDescriptions.Distinct(new SimulationDescriptionComparer()).ToList();

                // If the simulation descriptions list is empty then we aren't varying 
                // by any field so create a simulation description for the whole dataset.
                if (simulationDescriptions.Count == 0)
                    simulationDescriptions.Add(new SimulationDescription(null, Name));

                DataTable baseData = GetBaseData(reader, simulationDescriptions);

                // If there are vary by fields that aren't in descriptors of the 
                // simulationdescriptions then add them.
                EnsureVaryBysAreInDescriptors(FactorToVaryColours, simulationDescriptions, baseData);
                EnsureVaryBysAreInDescriptors(FactorToVaryMarkers, simulationDescriptions, baseData);
                EnsureVaryBysAreInDescriptors(FactorToVaryLines, simulationDescriptions, baseData);

                // Get data for each simulation / zone object
                if (baseData != null)
                {
                    if (baseData.Rows.Count > 0)
                        ourDefinitions = ConvertToSeriesDefinitions(simulationDescriptions, reader, baseData);
                    else if (Apsim.Parent(this, typeof(Simulation)).Parent is Experiment)
                        throw new Exception("Unable to find any data points - should this graph be directly under an experiment?");
                }
            }

            // We might have child models that want to add to our series definitions e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetSeriesToPutOnGraph(reader, ourDefinitions);

            // Remove series that have no data.
            ourDefinitions.RemoveAll(d => !MathUtilities.ValuesInArray(d.x) || !MathUtilities.ValuesInArray(d.y));

            definitions.AddRange(ourDefinitions);
        }

        /// <summary>
        /// Ensure the specified field name is in descriptors of the 
        /// simulationdescription. If not then create a simulation description
        /// for each valid value.
        /// </summary>
        /// <remarks>
        /// This is to support vary by on a string field of the data table. Needed
        /// by Morris 'Parameter' vary by.
        /// </remarks>
        /// <param name="varyByFieldName">The vary by field name to ensure is in the descriptors.</param>
        /// <param name="simulationDescriptions"></param>
        /// <param name="baseData"></param>
        private void EnsureVaryBysAreInDescriptors(string varyByFieldName, List<SimulationDescription> simulationDescriptions, DataTable baseData)
        {
            if (varyByFieldName != null)
            {
                var newList = new List<SimulationDescription>();

                foreach (var simulationDescription in simulationDescriptions)
                {
                    var descriptor = simulationDescription.Descriptors.Find(d => d.Name == varyByFieldName);
                    if (descriptor == null)
                    {
                        // We need to create a simulation description for each valid value of
                        // the descriptor.
                        var validValues = DataTableUtilities.GetColumnAsStrings(baseData, varyByFieldName).Distinct();
                        foreach (var value in validValues)
                        {
                            var newSimulationDescription = new SimulationDescription(null, simulationDescription.Name);
                            newSimulationDescription.Descriptors.AddRange(simulationDescription.Descriptors);
                            newSimulationDescription.Descriptors.Add(new SimulationDescription.Descriptor(varyByFieldName, value));
                            newList.Add(newSimulationDescription);
                        }
                    }
                    else
                        newList.Add(simulationDescription);
                }

                simulationDescriptions.Clear();
                simulationDescriptions.AddRange(newList);
            }
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
                {
                    var num = group.Count();
                    descriptors.Add(group.ToList());
                }

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
        /// If a simulation description doesn't have any descriptors that are being
        /// varied then remove it.
        /// </summary>
        /// <param name="simulationDescriptions"></param>
        private void RemoveUnnessaryDescriptionsAndDescriptors(List<SimulationDescription> simulationDescriptions)
        {
            var varyByFieldNames = GetVaryByFieldNames();
            foreach (var simulationDescription in simulationDescriptions)
            {
                // For this simulation description, determine which descriptors aren't 
                // being varied, add them to a removal list.
                var descriptorsToRemove = new List<SimulationDescription.Descriptor>();
                foreach (var descriptor in simulationDescription.Descriptors)
                {
                    if (!varyByFieldNames.Contains(descriptor.Name))
                        descriptorsToRemove.Add(descriptor);
                }

                // Remove all descriptors in the removal list.
                foreach (var descritorToRemove in descriptorsToRemove)
                    simulationDescription.Descriptors.Remove(descritorToRemove);
            }

            // Remove all simulation descriptions that don't have any descriptors.
            simulationDescriptions.RemoveAll(sd => sd.Descriptors.Count == 0);
        }

        /// <summary>
        /// Find and return a list of all simulation descriptions.
        /// </summary>
        private List<SimulationDescription> FindSimulationDescriptions()
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

        /// <summary>
        /// Paint the visual elements (colour, line and marker) of all simulation / zone pairs.
        /// </summary>
        /// <param name="simulationDescriptions">The simulation descriptions convert to series.</param>
        /// <param name="reader">Storage reader.</param>
        /// <param name="baseData">Base data.</param>
        private List<SeriesDefinition> ConvertToSeriesDefinitions(List<SimulationDescription> simulationDescriptions, IStorageReader reader, DataTable baseData)
        {
            // Create an appropriate painter object
            SimulationZonePainter.IPainter painter;
            if (FactorToVaryColours != null)
            {
                if (FactorToVaryLines == FactorToVaryColours && FactorToVaryMarkers == FactorToVaryColours)
                    painter = new SimulationZonePainter.SequentialPainter
                        (FactorToVaryColours,
                         ColourUtilities.Colours.Length,
                         Enum.GetValues(typeof(MarkerType)).Length - 1, // minus 1 to avoid None type
                         Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                         VisualElements.SetColour,
                         VisualElements.SetMarker,
                         VisualElements.SetLineType);
                else if (FactorToVaryLines == FactorToVaryColours)
                    painter = new SimulationZonePainter.SequentialPainter
                        (FactorToVaryColours,
                         ColourUtilities.Colours.Length, Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                         VisualElements.SetColour,
                         VisualElements.SetLineType);
                else if (FactorToVaryMarkers == FactorToVaryColours)
                    painter = new SimulationZonePainter.SequentialPainter
                       (FactorToVaryColours,
                        ColourUtilities.Colours.Length,
                        Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                        VisualElements.SetColour,
                        VisualElements.SetMarker);
                else if (FactorToVaryLines != null && FactorToVaryMarkers != null)
                    painter = new SimulationZonePainter.MultiDescriptorPainter()
                    {
                        DescriptorName1 = FactorToVaryColours,
                        DescriptorName2 = FactorToVaryLines,
                        DescriptorName3 = FactorToVaryMarkers,
                        MaximumIndex1 = ColourUtilities.Colours.Length,
                        MaximumIndex2 = Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                        MaximumIndex3 = Enum.GetValues(typeof(MarkerType)).Length - 1, // minus 1 to avoid None type
                        Setter1 = VisualElements.SetColour,
                        Setter2 = VisualElements.SetLineType,
                        Setter3 = VisualElements.SetMarker
                    };

                else if (FactorToVaryLines != null)
                    painter = new SimulationZonePainter.MultiDescriptorPainter()
                    {
                        DescriptorName1 = FactorToVaryColours,
                        DescriptorName2 = FactorToVaryLines,
                        MaximumIndex1 = ColourUtilities.Colours.Length,
                        MaximumIndex2 = Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type
                        Setter1 = VisualElements.SetColour,
                        Setter2 = VisualElements.SetLineType
                    };
                else if (FactorToVaryMarkers != null)
                    painter = new SimulationZonePainter.MultiDescriptorPainter()
                    {
                        DescriptorName1 = FactorToVaryColours,
                        DescriptorName2 = FactorToVaryMarkers,
                        MaximumIndex1 = ColourUtilities.Colours.Length,
                        MaximumIndex2 = Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                        Setter1 = VisualElements.SetColour,
                        Setter2 = VisualElements.SetMarker
                    };
                else
                    painter = new SimulationZonePainter.SequentialPainter
                       (FactorToVaryColours,
                        ColourUtilities.Colours.Length,
                        VisualElements.SetColour);
            }
            else if (FactorToVaryLines != null)
            {
                painter = new SimulationZonePainter.SequentialPainter
                   (FactorToVaryLines,
                    Enum.GetValues(typeof(LineType)).Length - 1, // minus 1 to avoid None type   
                    VisualElements.SetLineType);
            }
            else if (FactorToVaryMarkers != null)
            {
                painter = new SimulationZonePainter.SequentialPainter
                   (FactorToVaryMarkers,
                    Enum.GetValues(typeof(MarkerType)).Length - 1,// minus 1 to avoid None type
                    VisualElements.SetMarker);
            }
            else
                painter = new SimulationZonePainter.DefaultPainter() { Colour = Colour, LineType = Line, MarkerType = Marker };

            List<SeriesDefinition> definitions = new List<SeriesDefinition>();
            // Apply the painter to all simulation zone objects.
            foreach (var simulationDescription in simulationDescriptions)
            {
                VisualElements visualElement = new VisualElements();
                visualElement.colour = Colour;
                visualElement.Line = Line;
                visualElement.LineThickness = LineThickness;
                visualElement.Marker = Marker;
                visualElement.MarkerSize = MarkerSize;
                painter.PaintSimulationZone(simulationDescription, visualElement, this);

                SeriesDefinition seriesDefinition = new Models.Graph.SeriesDefinition();
                seriesDefinition.type = Type;
                seriesDefinition.marker = visualElement.Marker;
                seriesDefinition.line = visualElement.Line;
                seriesDefinition.markerSize = visualElement.MarkerSize;
                seriesDefinition.lineThickness = visualElement.LineThickness;
                seriesDefinition.colour = visualElement.colour;
                seriesDefinition.xFieldName = XFieldName;
                seriesDefinition.yFieldName = YFieldName;
                seriesDefinition.xAxis = XAxis;
                seriesDefinition.yAxis = YAxis;
                seriesDefinition.xFieldUnits = reader.Units(TableName, XFieldName);
                seriesDefinition.yFieldUnits = reader.Units(TableName, YFieldName);
                seriesDefinition.showInLegend = ShowInLegend;
                if (simulationDescription.Descriptors.Count == 0)
                    seriesDefinition.title = Name;
                else if (simulationDescription.Descriptors.Count == 1 && simulationDescription.Descriptors[0].Name == "Graph series")
                    seriesDefinition.title = Name;
                else
                {
                    simulationDescription.Descriptors.ForEach(f => seriesDefinition.title += f.Value);
                    if (IncludeSeriesNameInLegend || seriesDefinition.title == "?")
                    {
                        seriesDefinition.title += ": " + Name;
                    }
                }

                if (Checkpoint != "Current")
                    seriesDefinition.title += " (" + Checkpoint + ")";
                DataView data = new DataView(baseData);
                try
                {
                    var fieldsThatExist = reader.ColumnNames(TableName);

                    string rowFilter = null;

                    foreach (var descriptor in simulationDescription.Descriptors)
                    {
                        if (fieldsThatExist.Contains(descriptor.Name))
                        {
                            if (rowFilter != null)
                                rowFilter += " AND ";

                            rowFilter += descriptor.Name + " = '" + descriptor.Value + "'";
                        }
                        else
                        {
                            // Field doesn't exist. This typically happens in observed files that don't
                            // have the descriptor columns. Instead use the simulation name to match.
                            if (rowFilter != null)
                                rowFilter += " AND ";
                            rowFilter += "SimulationName = '" + simulationDescription.Name + "'";
                        }
                    }

                    data.RowFilter = rowFilter;
                }
                catch
                {

                }
                if (data.Count > 0)
                {
                    seriesDefinition.data = data.ToTable();
                    seriesDefinition.x = GetDataFromTable(seriesDefinition.data, XFieldName);
                    seriesDefinition.y = GetDataFromTable(seriesDefinition.data, YFieldName);
                    seriesDefinition.x2 = GetDataFromTable(seriesDefinition.data, X2FieldName);
                    seriesDefinition.y2 = GetDataFromTable(seriesDefinition.data, Y2FieldName);
                    seriesDefinition.error = GetErrorDataFromTable(seriesDefinition.data, YFieldName);
                    if (Cumulative)
                        seriesDefinition.y = MathUtilities.Cumulative(seriesDefinition.y as IEnumerable<double>);
                    if (CumulativeX)
                        seriesDefinition.x = MathUtilities.Cumulative(seriesDefinition.x as IEnumerable<double>);
                }
                definitions.Add(seriesDefinition);
            }
            return definitions;
        }

        /// <summary>Creates a series definition.</summary>
        /// <param name="title">The title.</param>
        /// <param name="filter">The filter. Can be null.</param>
        /// <param name="colour">The colour.</param>
        /// <param name="line">The line type.</param>
        /// <param name="marker">The marker type.</param>
        /// <param name="simulationNames">A list of simulations to include in data.</param>
        /// <param name="storage">Storage reader.</param>
        /// <returns>The newly created definition.</returns>
        private SeriesDefinition CreateDefinition(string title, string filter, Color colour, MarkerType marker, LineType line, string[] simulationNames, IStorageReader storage)
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
            else
            {
                List<string> fieldNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(XFieldName))
                    fieldNames.Add(XFieldName);
                if (!string.IsNullOrWhiteSpace(YFieldName))
                    fieldNames.Add(YFieldName);
                if (!string.IsNullOrWhiteSpace(X2FieldName))
                    fieldNames.Add(X2FieldName);
                if (!string.IsNullOrWhiteSpace(Y2FieldName))
                    fieldNames.Add(Y2FieldName);

                DataTable data = storage.GetData(TableName, fieldNames: fieldNames, filter: Filter);

                definition.x = GetDataFromTable(data, XFieldName);
                definition.y = GetDataFromTable(data, YFieldName);
                definition.x2 = GetDataFromTable(data, X2FieldName);
                definition.y2 = GetDataFromTable(data, Y2FieldName);
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

        /// <summary>Gets a column of error data from a table.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable GetErrorDataFromTable(DataTable data, string fieldName)
        {
            string errorFieldName = fieldName + "Error";
            if (fieldName != null && data != null && data.Columns.Contains(errorFieldName))
            {
                if (data.Columns[errorFieldName].DataType == typeof(double))
                    return DataTableUtilities.GetColumnAsDoubles(data, errorFieldName);
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
        /// <param name="descriptions">The list of simulation descriptions.</param>
        /// <param name="reader">Storage service</param>
        private DataTable GetBaseData(IStorageReader reader, List<SimulationDescription> descriptions)
        {
            // Get a list of groupby field names
            var groupByFieldNames = GetVaryByFieldNames();

            // Remove the 'special' vary by field names.
            groupByFieldNames.RemoveAll(f => f == "Graph series");

            // Add the groupby field nemas to the fieldNames we need to put in datatable.
            /*List<string> fieldNames = new List<string>();
            fieldNames.AddRange(groupByFieldNames);

            foreach (var description in descriptions)
                foreach (var descriptor in description.Descriptors)
                    if (!fieldNames.Contains(descriptor.Name))
                        fieldNames.Add(descriptor.Name);

            if (XFieldName != null)
                fieldNames.Add(XFieldName);
            if (YFieldName != null)
                fieldNames.Add(YFieldName);
            if (YFieldName != null)
            {
                if (reader.ColumnNames(TableName).Contains(YFieldName + "Error"))
                    fieldNames.Add(YFieldName + "Error");
            }
            if (X2FieldName != null)
                fieldNames.Add(X2FieldName);
            if (Y2FieldName != null)
                fieldNames.Add(Y2FieldName);

            // Add in column names from annotation series.
            foreach (EventNamesOnGraph annotation in Apsim.Children(this, typeof(EventNamesOnGraph)))
                fieldNames.Add(annotation.ColumnName);

            // Remove field names that don't exist.
            fieldNames.RemoveAll(f => !fieldsThatExist.Contains(f));
            fieldNames.Add("SimulationName");
            */

            var fieldsThatExist = reader.ColumnNames(TableName);

            // Create a filter to pass to GetData.
            string filterToUse;
            if (Filter == null || Filter == string.Empty)
                filterToUse = CreateRowFilter(descriptions, groupByFieldNames, fieldsThatExist);
            else
            {
                var f = CreateRowFilter(descriptions, groupByFieldNames, fieldsThatExist);
                if (f != null)
                    filterToUse = Filter + " AND (" + f + ")";
                else
                    filterToUse = Filter;
            }
            // Checkpoints don't exist in observed files so don't pass a checkpoint name to 
            // GetData in this situation.
            string checkpointName = null;
            if (fieldsThatExist.Contains("CheckpointID"))
                checkpointName = Checkpoint;

            // Go get the data.
            return reader.GetData(tableName: TableName, checkpointName: checkpointName, /*fieldNames: fieldNames.Distinct(),*/ filter: filterToUse);
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

        /// <summary>
        /// Create a row filter for the specified factors.
        /// </summary>
        /// <param name="simulationDescriptions">A list of simulation descriptions.</param>
        /// <param name="groupByFieldNames">The group by field names to add to row filter.</param>
        /// <param name="fieldsThatExist">Fields that exist in the table.</param>
        private string CreateRowFilter(IEnumerable<SimulationDescription> simulationDescriptions, IEnumerable<string> groupByFieldNames, List<string> fieldsThatExist)
        {
            string rowFilter = null;

            // Create a filter based on the VaryBy fields.
            foreach (var groupByFieldName in groupByFieldNames)
            {
                if (fieldsThatExist.Contains(groupByFieldName))
                {
                    // Get a list of valid values for this groupby field name.
                    var validgroupByValues = new List<string>();
                    foreach (var description in simulationDescriptions)
                    {
                        var descriptor = description.Descriptors.Find(d => d.Name == groupByFieldName);
                        if (descriptor != null)
                            validgroupByValues.Add(descriptor.Value);
                    }
                    validgroupByValues = validgroupByValues.Distinct().ToList();

                    // If we didn't find any group by values in the descriptors then the 
                    // group by field must be a string column of the datatable. For now 
                    // don't include the group by field in the filter.
                    if (validgroupByValues.Count > 0)
                    {
                        if (rowFilter != null)
                            rowFilter += " AND ";

                        if (validgroupByValues.Count == 1)
                            foreach (var value in validgroupByValues)
                                rowFilter += groupByFieldName + " = '" + value + "'";
                        else
                            rowFilter += groupByFieldName + " IN (" +
                                                          StringUtilities.Build(validgroupByValues, ",", "'", "'") +
                                                          ")";
                    }
                }
            }

            return rowFilter;
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
                void PaintSimulationZone(SimulationDescription simulationDescription, VisualElements visualElement, Series series);
            }

            /// <summary>A default painter for setting a simulation / zone pair to default values.</summary>
            public class DefaultPainter : IPainter
            {
                public Color Colour { get; set; }
                public LineType LineType { get; set; }
                public MarkerType MarkerType { get; set; }
                public void PaintSimulationZone(SimulationDescription simulationDescription, VisualElements visualElement, Series series)
                {
                    visualElement.colour = Colour;
                    visualElement.Line = LineType;
                    visualElement.Marker = MarkerType;
                }
            }

            /// <summary>A painter for setting the visual element of a simulation description using consecutive values of up to three visual elements.</summary>
            public class SequentialPainter : IPainter
            {
                private List<string> values = new List<string>();
                private List<Tuple<int, int, int>> indexMatrix = new List<Tuple<int, int, int>>();
                private string descriptorName;
                private SetFunction setter1 { get; set; }
                private SetFunction setter2 { get; set; }
                private SetFunction setter3 { get; set; }

                public SequentialPainter(string descriptorName,
                                         int maximumIndex1,
                                         SetFunction set1)
                {
                    this.descriptorName = descriptorName;
                    for (int i = 0; i < maximumIndex1; i++)
                        indexMatrix.Add(new Tuple<int, int, int>(i, -1, -1));
                    setter1 = set1;
                }

                public SequentialPainter(string descriptorName,
                                         int maximumIndex1, int maximumIndex2,
                                         SetFunction set1, SetFunction set2)
                {
                    this.descriptorName = descriptorName;
                    for (int j = 0; j < maximumIndex2; j++)
                        for (int i = 0; i < maximumIndex1; i++)
                            indexMatrix.Add(new Tuple<int, int, int>(i, j, -1));
                    setter1 = set1;
                    setter2 = set2;
                }

                public SequentialPainter(string descriptorName, 
                                         int maximumIndex1, int maximumIndex2, int maximumIndex3,
                                         SetFunction set1, SetFunction set2, SetFunction set3)
                {
                    this.descriptorName = descriptorName;
                    for (int k = 0; k < maximumIndex3; k++)
                        for (int j = 0; j < maximumIndex2; j++)
                            for (int i = 0; i < maximumIndex1; i++)
                                indexMatrix.Add(new Tuple<int, int, int>(i, j, k));
                    setter1 = set1;
                    setter2 = set2;
                    setter3 = set3;
                }

                public void PaintSimulationZone(SimulationDescription simulationDescription, VisualElements visualElement, Series series)
                {
                    int index;
                    if (descriptorName == "Graph series")
                    {
                        index = series.Parent.Children.IndexOf(series);
                    }
                    else
                    {
                        var descriptor = simulationDescription.Descriptors.Find(d => d.Name == descriptorName);

                        index = values.IndexOf(descriptor.Value);
                        if (index == -1)
                        {
                            values.Add(descriptor.Value);
                            index = values.Count - 1;
                        }
                        if (index >= indexMatrix.Count)
                            index = 0;
                    }
                    setter1(visualElement, indexMatrix[index].Item1);
                    setter2?.Invoke(visualElement, indexMatrix[index].Item2);
                    setter3?.Invoke(visualElement, indexMatrix[index].Item3);
                }
            }

            /// <summary>A painter for setting the visual element of a simulation description to values of two visual elements.</summary>
            public class MultiDescriptorPainter : IPainter
            {
                private List<string> values1 = new List<string>();
                private List<string> values2 = new List<string>();
                private List<string> values3 = new List<string>();

                public int MaximumIndex1 { get; set; }
                public int MaximumIndex2 { get; set; }
                public int MaximumIndex3 { get; set; }
                public string DescriptorName1 { get; set; }
                public string DescriptorName2 { get; set; }
                public string DescriptorName3 { get; set; }
                public SetFunction Setter1 { get; set; }
                public SetFunction Setter2 { get; set; }
                public SetFunction Setter3 { get; set; }

                public void PaintSimulationZone(SimulationDescription simulationDescription, VisualElements visualElement, Series series)
                {
                    var descriptor1 = simulationDescription.Descriptors.Find(d => d.Name == DescriptorName1);
                    string descriptorValue1 = descriptor1.Value;

                    int index1 = values1.IndexOf(descriptorValue1);
                    if (index1 == -1)
                    {
                        values1.Add(descriptorValue1);
                        index1 = values1.Count - 1;
                    }
                    index1 = index1 % MaximumIndex1;
                    Setter1(visualElement, index1);

                    var descriptor2 = simulationDescription.Descriptors.Find(d => d.Name == DescriptorName2);
                    if (descriptor2 != null)
                    {
                        string descriptorValue2 = descriptor2.Value;

                        int index2 = values2.IndexOf(descriptorValue2);
                        if (index2 == -1)
                        {
                            values2.Add(descriptorValue2);
                            index2 = values2.Count - 1;
                        }
                        index2 = index2 % MaximumIndex2;
                        Setter2(visualElement, index2);
                    }

                    if (DescriptorName3 != null)
                    {
                        var descriptor3 = simulationDescription.Descriptors.Find(d => d.Name == DescriptorName3);
                        var descriptorValue3 = descriptor3.Value;

                        var index3 = values3.IndexOf(descriptorValue3);
                        if (index3 == -1)
                        {
                            values3.Add(descriptorValue3);
                            index3 = values3.Count - 1;
                        }
                        index3 = index3 % MaximumIndex3;
                        Setter3(visualElement, index3);
                    }
                }
            }
        }

        /// <summary>
        /// A comparer that looks at a simulation descriptions descriptors.
        /// </summary>
        class SimulationDescriptionComparer : IEqualityComparer<SimulationDescription>
        {
            public bool Equals(SimulationDescription x, SimulationDescription y)
            {
                if (x.Descriptors.Count != y.Descriptors.Count)
                    return false;
                for (int i = 0; i < x.Descriptors.Count; i++)
                {
                    if (x.Descriptors[i].Name != y.Descriptors[i].Name ||
                        x.Descriptors[i].Value != y.Descriptors[i].Value)
                        return false;
                }
                return true;
            }

            public int GetHashCode(SimulationDescription obj)
            {
                int hash = 0;
                foreach (var descriptor in obj.Descriptors)
                    hash += descriptor.Name.GetHashCode() + descriptor.Value.GetHashCode();
                return hash;
            }
        }
    }
}
