using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models
{

    /// <summary>
    /// A class for putting text annotations on a graph.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Series))]
    public class ShadedBarsOnGraph : Model, ICachableGraphable, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The table to search for phenological stage names.</summary>
        [NonSerialized]
        private DataView data;

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
            IDataStore storage = Structure.Find<IDataStore>();
            if (storage == null)
                return null;

            Series series = FindAncestor<Series>();
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
            return GraphPage.FindSimulationDescriptions(FindAncestor<Series>())?.Select(s => s.Name)?.ToArray();
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
        /// <param name="storage">Storage service</param>
        /// <param name="simDescriptions">A list of simulation descriptions that are in scope.</param>
        /// <param name="simulationFilter">(Optional) simulation name filter.</param>
        public IEnumerable<SeriesDefinition> CreateSeriesDefinitions(IStorageReader storage,
                                                                  List<SimulationDescription> simDescriptions,
                                                                  List<string> simulationFilter = null)
        {
            Series seriesAncestor = FindAncestor<Series>();
            if (seriesAncestor == null)
                throw new Exception("ShadedBarsOnGraph model must be a descendant of a series");
            IEnumerable<SeriesDefinition> definitions = seriesAncestor.CreateSeriesDefinitions(storage, simDescriptions, simulationFilter);

            return GetSeriesToPutOnGraph(storage, definitions, simulationFilter).ToList();
        }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        /// <param name="storage">Storage service</param>
        /// <param name="simulationFilter">(Optional) simulation name filter.</param>
        public IEnumerable<SeriesDefinition> GetSeriesToPutOnGraph(IStorageReader storage, IEnumerable<SeriesDefinition> definitions, List<string> simulationFilter = null)
        {
            data = null;
            if (definitions != null && definitions.Any())
            {
                // Try to find a definition that has the correct simulation name.
                foreach (var definition in definitions)
                {
                    var simulationNameDescriptor = definition.Descriptors?.Find(desc => desc.Name == "SimulationName")?.Value;
                    if (simulationFilter != null && simulationFilter.Count > 0)
                        simulationNameDescriptor = simulationFilter[0];

                    if (simulationNameDescriptor != null && simulationNameDescriptor == SimulationName)
                        data = definition.View;
                }

                if (data == null)
                    data = definitions.FirstOrDefault(d => d.View != null)?.View;
                xFieldName = definitions.First().XFieldName;
            }
            return new List<SeriesDefinition>();
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        public IEnumerable<IAnnotation> GetAnnotations()
        {
            List<IAnnotation> annotations = new List<IAnnotation>();

            if (data != null && ColumnName != null && xFieldName != null)
            {
                string columnName = FindColumn(data.Table);
                if (columnName != null && data.Table.Columns.Contains(xFieldName))
                {
                    string[] names = DataTableUtilities.GetColumnAsStrings(data, columnName);
                    List<object> x;
                    Type columnType = data.Table.Columns[xFieldName].DataType;
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
                        var baseColour = Color.LightBlue;
                        var colourMap = new Dictionary<string, Color>();
                        int startIndex = -1;
                        string startName = string.Empty;
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (startIndex == -1)
                            {
                                startIndex = i;
                                startName = names[i];
                            }
                            else if (names[i] != startName)
                            {
                                if (!string.IsNullOrEmpty(startName))
                                {
                                    // Add a line annotation.
                                    annotations.Add(CreateAnnotation(x, baseColour, colourMap, startIndex, startName, i));
                                }
                                startName = names[i];
                                startIndex = i;
                            }

                        }
                        if (startIndex != -1)
                            annotations.Add(CreateAnnotation(x, baseColour, colourMap, startIndex, startName, names.Length));
                    }
                }
            }
            return annotations;
        }

        private static IAnnotation CreateAnnotation(List<object> x, Color baseColour, Dictionary<string, Color> colourMap, int startIndex, string startName, int i)
        {
            var bar = new LineAnnotation();
            if (colourMap.ContainsKey(startName))
                bar.colour = colourMap[startName];
            else
            {
                bar.colour = ColourUtilities.ChangeColorBrightness(baseColour, colourMap.Count * 0.2);
                colourMap.Add(startName, bar.colour);
            }
            bar.type = LineType.Dot;
            bar.x1 = x[startIndex];
            bar.y1 = double.MinValue;
            bar.x2 = x[i - 1];
            bar.y2 = double.MaxValue;
            bar.InFrontOfSeries = false;
            bar.ToolTip = startName;
            return bar;
        }

        /// <summary>Find and return the phenology stage column name.</summary>
        /// <param name="data">The data table to search</param>
        private string FindColumn(DataTable data)
        {
            if (ColumnName == null || ColumnName == string.Empty)
                return null;

            var columnNames = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            return columnNames.Find(name => name.Contains(ColumnName));
        }
    }
}
