using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Shared.Graphing;
using M = Models;
using Models.Core;
using System.Linq;

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
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);

            M.Graph graph = model as M.Graph;
            (tags[0] as Section).Children.Add(graph.ToGraph(graph.GetSeriesDefinitions()));

            return tags;
        }
    }
}
