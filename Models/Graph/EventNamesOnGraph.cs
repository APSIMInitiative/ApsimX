namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// A class for putting text annotations on a graph.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Series))]
    public class EventNamesOnGraph : Model, IGraphable
    {
        /// <summary>The table to search for phenological stage names.</summary>
        [NonSerialized]
        private DataTable data;

        /// <summary>The x variable name</summary>
        private string xFieldName;

        /// <summary>
        /// Gets or sets the column name to plot.
        /// </summary>
        [Description("The column name to plot")]
        [Display(Values = "GetValidColumnNames")]
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the simulation name to plot.
        /// </summary>
        [Description("Name of the simulation to plot")]
        [Display(Values = "GetValidSimNames")]
        public string SimulationName { get; set; }

        /// <summary>
        /// Gets a list of valid column names.
        /// </summary>
        public string[] GetValidColumnNames()
        {
            IDataStore storage = Apsim.Find(this, typeof(IDataStore)) as IDataStore;
            if (storage == null)
                return null;

            Series series = Apsim.Parent(this, typeof(Series)) as Series;
            if (series == null)
                return null;

            return storage.Reader.ColumnNames(series.TableName).ToArray();
        }

        /// <summary>
        /// Gets a list of names of simulations in scope.
        /// </summary>
        /// <returns></returns>
        public string[] GetValidSimNames()
        {
            return (Apsim.Parent(this, typeof(Series)) as Series)?.FindSimulationDescriptions()?.Select(s => s.Name)?.ToArray();
        }


        /// <summary>Return a list of extra fields that the definition should read.</summary>
        /// <param name="seriesDefinition">The calling series definition.</param>
        /// <returns>A list of fields - never null.</returns>
        public IEnumerable<string> GetExtraFieldsToRead(SeriesDefinition seriesDefinition)
        {
            if (string.IsNullOrEmpty(ColumnName))
                return new string[0];
            else
                return new string[] { ColumnName };
        }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        /// <param name="storage">Storage service</param>
        /// <param name="simulationFilter">(Optional) simulation name filter.</param>
        public void GetSeriesToPutOnGraph(IStorageReader storage, List<SeriesDefinition> definitions, List<string> simulationFilter = null)
        {
            data = null;
            if (definitions != null && definitions.Count > 0)
            {
                // Try to find a definition that has the correct simulation name.
                foreach (var definition in definitions)
                {
                    var simulationNameDescriptor = definition.Descriptors?.Find(desc => desc.Name == "SimulationName")?.Value;
                    if (simulationFilter != null && simulationFilter.Count > 0)
                        simulationNameDescriptor = simulationFilter[0];

                    if (simulationNameDescriptor != null && simulationNameDescriptor== SimulationName)
                        data = definition.Data;
                }

                if (data == null)
                    data = definitions.FirstOrDefault(d => d.Data != null)?.Data;
                xFieldName = definitions[0].XFieldName;
            }

        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        /// <param name="annotations">A list of annotations to add to.</param>
        public void GetAnnotationsToPutOnGraph(List<Annotation> annotations)
        {
            Graph parentGraph = Parent.Parent as Graph;

            if (data != null && ColumnName != null && xFieldName != null)
            {
                string phenologyColumnName = FindPhenologyStageColumn(data);
                if (phenologyColumnName != null && data.Columns.Contains(xFieldName))
                {
                    string[] names = DataTableUtilities.GetColumnAsStrings(data, phenologyColumnName);
                    List<object> x;
                    Type columnType = data.Columns[xFieldName].DataType;
                    if (columnType == typeof(DateTime))
                        x = DataTableUtilities.GetColumnAsDates(data, xFieldName).Cast<object>().ToList();
                    else if (columnType == typeof(int))
                        x = DataTableUtilities.GetColumnAsIntegers(data, xFieldName).Cast<object>().ToList();
                    else if (columnType == typeof(double))
                        x = DataTableUtilities.GetColumnAsDoubles(data, xFieldName).Cast<object>().ToList();
                    else
                        throw new Exception($"Error in EventNamesOnGraph {Name}: unknown column type '{columnType.FullName}' in column '{xFieldName}'");

                    if (names.Length == x.Count)
                    {
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (names[i] != "?" && !string.IsNullOrEmpty(names[i]))
                            {
                                // Add a line annotation.
                                LineAnnotation line = new LineAnnotation();
                                line.colour = Color.Black;
                                line.type = LineType.Dot;
                                line.x1 = x[i];
                                line.y1 = double.MinValue;
                                line.x2 = x[i];
                                line.y2 = double.MaxValue;
                                annotations.Add(line);

                                // Add a text annotation.

                                TextAnnotation text = new TextAnnotation();
                                text.text = names[i];
                                text.colour = Color.Black;
                                text.leftAlign = true;
                                text.x = x[i];

                                text.y = double.MinValue;
                                text.textRotation = 270;
                                annotations.Add(text);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Find and return the phenology stage column name.</summary>
        /// <param name="data">The data table to search</param>
        private string FindPhenologyStageColumn(DataTable data)
        {
            if (ColumnName == null || ColumnName == string.Empty)
                return null;

            var columnNames = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            return columnNames.Find(name => name.Contains(ColumnName));
        }

    }
}
