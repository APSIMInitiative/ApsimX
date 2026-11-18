using System.Collections.Generic;
using APSIM.Documentation.Tag;
using M = Models;
using Models.Core;
using System.Linq;
using Models;
using APSIM.Shared.Graphing;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocGraph : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGraph" /> class.
        /// </summary>
        public DocGraph(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {

            bool showGraphs = true;
            List<Folder> folderAncestorList = model.Node.FindParents<Folder>().ToList();
            foreach (Folder folder in folderAncestorList)
                if (folder.ShowInDocs == false)
                    showGraphs = false;

            if (showGraphs)
            {
                Section section = GetSectionTitle(model);

                M.Graph graph = model as M.Graph;
                section.Children.Add(ToGraph(graph));

                return new List<ITag>() {section};
            }
            else
            {
                return new List<ITag>();
            }
        }

        /// <summary>
        /// Generated a 'standardised' graph, using the given series definitions.
        /// </summary>
        public static Tag.Graph ToGraph(M.Graph graph)
        {
            IEnumerable<SeriesDefinition> definitions = graph.GetSeriesDefinitions();
            LegendConfiguration legend = new LegendConfiguration(graph.LegendOrientation, graph.LegendPosition, !graph.LegendOutsideGraph);
            var xAxis = graph.Axis.FirstOrDefault(a => a.Position == AxisPosition.Bottom || a.Position == AxisPosition.Top);
            var yAxis = graph.Axis.FirstOrDefault(a => a.Position == AxisPosition.Left || a.Position == AxisPosition.Right);
            return new Tag.Graph(graph.Name, graph.FullPath, graph.GetSeries(definitions), xAxis, yAxis, legend);
        }
    }
}
