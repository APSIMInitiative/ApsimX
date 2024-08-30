using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{
    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocSimulation : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSimulation" /> class.
        /// </summary>
        public DocSimulation(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();
            
            foreach (IModel child in model.Children)
                newTags = AutoDocumentation.Document(child, newTags, headingLevel+1, indent+1).ToList();

            return newTags;
        }
    }
}
