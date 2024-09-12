using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocNodule : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocNodule" /> class.
        /// </summary>
        public DocNodule(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);
            
            List<ITag> subTags = new List<ITag>();
            foreach (IModel child in model.FindAllChildren())
                subTags.AddRange(AutoDocumentation.Document(child, heading+1));

            tags.Add(new Section(subTags));

            return tags;
        }
    }
}
