using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocGenericWithChildren : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocGenericWithChildren(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            List<ITag> subTags = new List<ITag>();
            foreach (IModel child in model.FindAllChildren())
                AutoDocumentation.Document(child, subTags, headingLevel+1, indent+1);

            newTags.Add(new Section(model.Name, subTags));

            return newTags;
        }
    }
}
