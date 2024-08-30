using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.Functions;

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
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            List<ITag> subTags = new List<ITag>();

            subTags.Add(new Paragraph($"*{model.Name}* is calculated using linear interpolation"));

            XYPairs xyPairs = model.FindChild<XYPairs>();
            if (xyPairs != null)
                subTags = AutoDocumentation.Document(xyPairs, subTags, headingLevel+1, indent+1).ToList();               

            newTags.Add(new Section(model.Name, subTags));

            return newTags;
        }
    }
}
