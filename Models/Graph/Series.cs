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
    [ViewName("UserInterface.Views.SeriesView")]
    [PresenterName("UserInterface.Presenters.SeriesPresenter")]
    [Serializable]
    public class Series : Model, IGraphable
    {
        /// <summary>Constructor for a series</summary>
        public Series()
        {
            this.XAxis = Axis.AxisType.Bottom;
            this.Colour = Color.Blue;
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

        /// <summary>Gets or sets the marker to show</summary>
        public MarkerType Marker { get; set; }

        /// <summary>Gets or sets the line type to show</summary>
        public LineType Line { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether the series should be shown in the legend
        /// </summary>
        public bool ShowInLegend { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Y variables should be cumulative.
        /// </summary>
        public bool Cumulative { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the X variables should be cumulative.
        /// </summary>
        public bool CumulativeX { get; set; }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        public void GetSeriesToPutOnGraph(List<SeriesDefinition> definitions)
        {
            List<SeriesDefinition> ourDefinitions = new List<SeriesDefinition>();

            // If this series doesn't have a table name then it must be getting its data from other models.
            if (TableName == null)
                ourDefinitions.Add(CreateDefinition(Name, null, Colour));
            else
            {
                Simulation parentSimulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
                Zone parentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                Experiment parentExperiment = Apsim.Parent(this, typeof(Experiment)) as Experiment;

                // If the graph is in a zone then just graph the zone.
                if (parentZone != null)
                {
                    string filter = string.Format("Name='{0}' and ZoneName='{1}'", parentSimulation.Name, parentZone.Name);
                    ourDefinitions.Add(CreateDefinition(Name + " " + parentZone.Name, filter, Colour));
                }
                else
                {
                    List<Simulation> simulations = new List<Simulation>();
                    List<Experiment> experiments = new List<Experiment>();

                    // Graph is sitting in a simulation so graph just that simulation.
                    if (parentSimulation != null)
                        simulations.Add(parentSimulation);
    
                    // See if graph is inside an experiment. If so then graph all simulations in experiment.
                    else if (parentExperiment != null)
                    {
                        int colourIndex1 = 0;
                        foreach (string simulationName in parentExperiment.Names())
                        {
                            string filter = "SimulationName = '" + simulationName + "'";
                            CreateDefinitions(parentExperiment.BaseSimulation, simulationName, filter, ref colourIndex1, ourDefinitions);
                        }
                    }

                    // Must be in a folder at the top level or at the top level of the .apsimx file. 
                    else
                    {
                        IModel parentOfGraph = this.Parent.Parent;

                        // Look for experiments.
                        foreach (Experiment experiment in Apsim.ChildrenRecursively(parentOfGraph, typeof(Experiment)))
                            experiments.Add(experiment);

                        // Look for simulations if we didn't find any experiments.
                        if (experiments.Count == 0)
                            foreach (Simulation simulation in Apsim.ChildrenRecursively(parentOfGraph, typeof(Simulation)))
                                simulations.Add(simulation);
                    }

                    // Now create series definitions for each experiment found.
                    int colourIndex = 0;
                    foreach (Experiment experiment in experiments)
                    {
                        string filter = "SimulationName IN " + "(" + StringUtilities.Build(experiment.Names(), delimiter: ",", prefix: "'", suffix: "'") + ")";
                        CreateDefinitions(experiment.BaseSimulation, experiment.Name, filter, ref colourIndex, ourDefinitions);
                    }

                    // Now create series definitions for each simulation found.
                    foreach (Simulation simulation in simulations)
                    {
                        string filter = "SimulationName = '" + simulation.Name + "'";
                        CreateDefinitions(simulation, simulation.Name, filter, ref colourIndex, ourDefinitions);
                    }
                }
            }

            // We might have child models that wan't to add to our series definitions e.g. regression.
            foreach (IGraphable series in Apsim.Children(this, typeof(IGraphable)))
                series.GetSeriesToPutOnGraph(ourDefinitions);

            definitions.AddRange(ourDefinitions);
        }

        /// <summary>Creates series definitions for the specified simulation.</summary>
        /// <param name="simulation">The simulation.</param>
        /// <param name="baseTitle">The base title.</param>
        /// <param name="baseFilter">The base filter.</param>
        /// <param name="colourIndex">The index into the colour palette.</param>
        /// <param name="definitions">The definitions to add to.</param>
        private void CreateDefinitions(Simulation simulation, string baseTitle, string baseFilter, ref int colourIndex, List<SeriesDefinition> definitions)
        {
            List<IModel> zones = Apsim.Children(simulation, typeof(Zone));
            if (zones.Count > 1)
            {
                foreach (Zone zone in zones)
                {
                    string zoneFilter = baseFilter + " AND ZoneName = '" + zone.Name + "'";
                    definitions.Add(CreateDefinition(baseTitle + " " + zone.Name, zoneFilter,
                                                     ColourUtilities.ChooseColour(colourIndex)));
                    colourIndex++;
                }
            }
            else
            {
                definitions.Add(CreateDefinition(baseTitle, baseFilter,
                                                 ColourUtilities.ChooseColour(colourIndex)));
                colourIndex++;
            }
        }

        /// <summary>Creates a series definition.</summary>
        /// <param name="title">The title.</param>
        /// <param name="filter">The filter. Can be null.</param>
        /// <param name="colour">The colour.</param>
        /// <returns>The newly created definition.</returns>
        private SeriesDefinition CreateDefinition(string title, string filter, Color colour)
        {
            SeriesDefinition definition = new SeriesDefinition();
            GetData(filter, definition);
            definition.colour = colour;
            definition.title = title;
            definition.line = Line;
            definition.marker = Marker;
            definition.showInLegend = ShowInLegend;
            definition.type = Type;
            definition.xAxis = XAxis;
            definition.xFieldName = XFieldName;
            definition.yAxis = YAxis;
            definition.yFieldName = YFieldName;
            return definition;
        }

        /// <summary>Gets all series data and stores in the specified definition.</summary>
        /// <param name="filter">The data filter to use.</param>
        /// <param name="definition">The definition to store the data in.</param>
        private void GetData(string filter, SeriesDefinition definition)
        {
            // If the table name is null then use reflection to get data from other models.
            if (TableName == null)
            {
                definition.x = GetDataFromModels(XFieldName);
                definition.y = GetDataFromModels(YFieldName);
            }
            else
            {
                List<string> fieldNames = new List<string>();
                fieldNames.Add(XFieldName);
                fieldNames.Add(YFieldName);
                if (X2FieldName != null)
                    fieldNames.Add(X2FieldName);
                if (Y2FieldName != null)
                    fieldNames.Add(Y2FieldName);

                DataStore dataStore = new DataStore(this);
                DataTable data = dataStore.GetFilteredData(TableName, fieldNames.ToArray(), filter);
                dataStore.Disconnect();

                // If the field exists in our data table then return it.
                if (data != null && data.Columns.Contains(XFieldName) && data.Columns.Contains(YFieldName))
                {
                    definition.x = GetDataFromTable(data, XFieldName);
                    definition.y = GetDataFromTable(data, YFieldName);
                    if (Cumulative)
                        definition.y = MathUtilities.Cumulative(definition.y as IEnumerable<double>);
                    if (CumulativeX)
                        definition.x = MathUtilities.Cumulative(definition.x as IEnumerable<double>);

                    if (X2FieldName != null && Y2FieldName != null &&
                        data.Columns.Contains(X2FieldName) && data.Columns.Contains(Y2FieldName))
                    {
                        definition.x2 = GetDataFromTable(data, X2FieldName);
                        definition.y2 = GetDataFromTable(data, Y2FieldName);
                    }
                    definition.simulationNamesForEachPoint = (IEnumerable<string>) GetDataFromTable(data, "SimulationName");
                }
            }
        }

        /// <summary>Gets a column of data from a table.</summary>
        /// <param name="data">The table</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The column of data.</returns>
        private IEnumerable GetDataFromTable(DataTable data, string fieldName)
        {
            if (data.Columns[fieldName].DataType == typeof(DateTime))
                return DataTableUtilities.GetColumnAsDates(data, fieldName);
            else if (data.Columns[fieldName].DataType == typeof(string))
                return DataTableUtilities.GetColumnAsStrings(data, fieldName);
            else
                return DataTableUtilities.GetColumnAsDoubles(data, fieldName);
        }

        /// <summary>Return data using reflection</summary>
        /// <param name="fieldName">The field name to get data for.</param>
        /// <returns>The return data or null if not found</returns>
        private IEnumerable GetDataFromModels(string fieldName)
        {
            if (fieldName.StartsWith("["))
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
