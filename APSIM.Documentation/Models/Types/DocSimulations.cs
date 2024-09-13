using System.Collections.Generic;
using APSIM.Shared.Documentation;
using DocumentFormat.OpenXml.Bibliography;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocSimulations : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSimulations" /> class.
        /// </summary>
        public DocSimulations(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);

            List<ITag> subTags = new List<ITag>();
            foreach (ITag tag in (tags[0] as Section).Children)
                subTags.Add(tag);
            foreach (IModel child in model.FindAllChildren<Folder>())
                subTags.AddRange(AutoDocumentation.Document(child, heading+1));

            return subTags;
        }
    }
}
