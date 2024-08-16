using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using System.Linq;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocClock : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocClock" /> class.
        /// </summary>
        public DocClock(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override IEnumerable<ITag> Document(List<ITag> tags = null, int headingLevel = 0, int indent = 0)
        {
            List<ITag> newTags = base.Document(tags, headingLevel, indent).ToList();

            newTags.Add(new Section(model.Name, GetModelEventsInvoked(typeof(Clock), "OnDoCommence(object _, CommenceArgs e)", "CLEM", true)));
            
            return newTags;
        }
    }
}
