// -----------------------------------------------------------------------
// <copyright file="EventNamesOnGraph.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
namespace Models.Graph
{
    using APSIM.Shared.Utilities;
    using Models.Core;
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
        private DataView data;

        /// <summary>The x variable name</summary>
        private string xFieldName;

        /// <summary>
        /// Gets or sets the column name to plot.
        /// </summary>
        [Description("The column name to plot")]
        public string ColumnName { get; set; }

        /// <summary>Called by the graph presenter to get a list of all actual series to put on the graph.</summary>
        /// <param name="definitions">A list of definitions to add to.</param>
        public void GetSeriesToPutOnGraph(List<SeriesDefinition> definitions)
        {
            if (definitions.Count > 0)
            {
                data = definitions[0].dataView;
                xFieldName = definitions[0].xFieldName;
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
                if (phenologyColumnName != null && data.Table.Columns.Contains(xFieldName))
                {
                    string[] names = DataTableUtilities.GetColumnAsStrings(data, phenologyColumnName);
                    DateTime[] dates = DataTableUtilities.GetColumnAsDates(data, xFieldName);
                    if (names.Length == dates.Length)
                    {
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (names[i] != "?")
                            {
                                // Add a line annotation.
                                LineAnnotation line = new LineAnnotation();
                                line.colour = Color.Black;
                                line.type = LineType.Dot;
                                line.x1 = dates[i];
                                line.y1 = double.MinValue;
                                line.x2 = dates[i];
                                line.y2 = double.MaxValue;
                                annotations.Add(line);

                                // Add a text annotation.
                                TextAnnotation text = new TextAnnotation();
                                text.text = names[i];
                                text.colour = Color.Black;
                                text.leftAlign = true;
                                text.x = dates[i];
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
        private string FindPhenologyStageColumn(DataView data)
        {
            var columnNames = data.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            return columnNames.Find(name => name.Contains(ColumnName));
        }

    }
}
