using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using M = Models;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for LinearInterpolationFunction
    /// </summary>
    public class DocLinearInterpolationFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocLinearInterpolationFunction" /> class.
        /// </summary>
        public DocLinearInterpolationFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            List<ITag> tags = new List<ITag>();

            foreach (IModel child in model.Node.FindChildren<M.Documentation>())
                tags.AddRange(AutoDocumentation.DocumentModel(child).ToList());

            XYPairs xyPairs = model.Node.FindChild<XYPairs>();
            if (xyPairs != null)
                tags.AddRange(AutoDocumentation.DocumentModel(xyPairs));

            return tags;
        }
    }
}
