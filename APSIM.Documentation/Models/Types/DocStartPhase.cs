using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models;
using Models.PMF.Phen;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class StartPhase
    /// </summary>
    public class DocStartPhase : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartPhase" /> class.
        /// </summary>
        public DocStartPhase(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            var newTags = base.Document(tags, headingLevel, indent).ToList();

            var subTags = new List<ITag>
            {
                new Paragraph($"This phase goes from {(model as StartPhase).Start.ToLower()} to {(model as StartPhase).End.ToLower()}."),
                new Paragraph($"It has no length but sets plant status to emerged once progressed.")
            };

            newTags.Add(new Section(model.Name, subTags));
            return newTags;
        }
    }
}
