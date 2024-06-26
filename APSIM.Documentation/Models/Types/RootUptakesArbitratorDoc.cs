using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class RootUptakesArbitratorDoc : GenericDoc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RootUptakesArbitratorDoc" /> class.
        /// </summary>
        public RootUptakesArbitratorDoc(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            if (tags == null)
                tags = new List<ITag>();
            
            // add a heading.
            tags.Add(new Heading(model.Name, headingLevel));

            // write description of this class.
            AutoDocumentation.DocumentModelSummary(model, tags, headingLevel, indent, false);

            // write children.
            foreach (IModel child in model.FindAllChildren<Memo>())
                AutoDocumentation.Document(child, tags, headingLevel + 1, indent);

            return tags;
        }
    }
}
