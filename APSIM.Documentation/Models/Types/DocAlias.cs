using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocAlias : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocAlias" /> class.
        /// </summary>
        public DocAlias(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int heading = 0)
        {
            List<ITag> tags = base.Document(heading);

            List<ITag> subTags = new List<ITag>()
            {
                new Paragraph($"An alias for {(model as Alias).FindAncestor<Cultivar>()?.Name}")
            };

            (tags[0] as Section).Children.AddRange(subTags);

            return tags;
        }
    }
}
