using System.Collections.Generic;
using APSIM.Shared.Documentation;
using M = Models;
using Models.Core;

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
            Section section = GetSummaryAndRemarksSection(model);

            M.Graph graph = model as M.Graph;
            section.Children.Add(graph.ToGraph(graph.GetSeriesDefinitions()));

            return new List<ITag>() {section};
        }
    }
}
